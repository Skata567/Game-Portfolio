using System.Collections.Generic;
using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// KDU/YJW가 쓰는 기존 ItemPickup을 직접 수정하지 않고, 바닥 아이템 줍기/버리기 순서를 중앙에서 관리합니다.
    /// 등록된 ItemPickup 컴포넌트는 입력 중복을 막기 위해 비활성화하고, 표시/수집은 이 컨트롤러가 대신 처리합니다.
    /// 
    /// **[최적화 내역]** 
    /// 기존의 매 프레임 아이템 스캔(Polling) 방식을 제거하고, 플레이어가 행동을 완료하거나 적이 죽었을 때 발생하는
    /// 시스템 이벤트(GameEvents)를 수신하여 아이템을 스캔하는 Event-Driven 구조로 개편되었습니다.
    /// </summary>
    public class GroundItemController : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("바닥에 생성할 기존 ItemPickup 프리팹입니다. KDU MapItemSpawner/GridChest와 같은 프리팹을 연결합니다.")]
        [SerializeField] private ItemPickup itemPickupPrefab;

        [Tooltip("아이템을 줍고 버릴 기준 플레이어입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private GridPlayer player;

        [Tooltip("아이템을 넣을 인벤토리 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private InventoryController inventoryController;

        [Header("입력")]
        [Tooltip("수동 줍기 입력을 사용할지 정합니다. 실제 키는 KeyBindingManager의 Pickup 설정을 사용합니다.")]
        [SerializeField] private bool manualPickupEnabled = true;

        [Header("스캔")]
        // 같은 칸에 여러 아이템을 버릴 수 있으므로, 셀 하나가 ItemPickup 리스트 하나를 가집니다.
        // 리스트 뒤쪽이 "나중에 떨어진 아이템"이라 수동 줍기에서 마지막 아이템부터 꺼낼 수 있습니다.
        private readonly Dictionary<Vector2Int, List<ItemPickup>> _itemsByCell = new();

        // KDU 스포너/상자가 만든 ItemPickup을 주기적으로 스캔하므로, 같은 오브젝트를 중복 등록하지 않기 위해 보관합니다.
        private readonly HashSet<ItemPickup> _registeredPickups = new();

        // 투척 무기 착지처럼 인벤토리 추가 대신 커스텀 동작이 필요한 픽업에 콜백을 저장합니다.
        private readonly Dictionary<ItemPickup, System.Func<bool>> _pickupCallbacks = new();

        // 플레이어 이동 완료 이벤트를 중복 구독하면 자동 줍기가 여러 번 실행되므로 구독 상태를 따로 기록합니다.
        private bool _movementSubscribed;

        /// <summary>
        /// InventoryController가 런타임에 자동 생성한 경우 참조를 주입합니다.
        /// 인벤토리와 바닥 아이템 컨트롤러는 서로 연결되어야 드랍/줍기가 같은 수량 규칙을 씁니다.
        /// </summary>
        public void Configure(InventoryController inventory)
        {
            inventoryController = inventory;
            ResolveReferences();
            SubscribeMovement();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeMovement();
            GameEvents.OnVisionUpdated += OnVisionUpdated;
            GameEvents.OnPlayerActionCompleted += OnPlayerActionCompleted;
            GameEvents.OnEnemyKilled += OnEnemyKilled;
            GameEvents.OnFloorReset += OnFloorReset;
        }

        private void Start()
        {
            // KDU MapItemSpawner/GridChest가 이미 만들어 둔 아이템도 이 컨트롤러 규칙으로 흡수합니다.
            ScanAndRegisterPickups();

            // 시작 위치에 아이템이 있으면 자동 줍기 규칙과 같은 방식으로 한 번 처리합니다.
            if (IsAutoPickupEnabled())
                TryCollectAllAtPlayerCell();
        }

        private void OnDisable()
        {
            GameEvents.OnVisionUpdated -= OnVisionUpdated;
            GameEvents.OnPlayerActionCompleted -= OnPlayerActionCompleted;
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
            GameEvents.OnFloorReset -= OnFloorReset;
            UnsubscribeMovement();
            _itemsByCell.Clear();
            _registeredPickups.Clear();
            _pickupCallbacks.Clear();
        }

        private void Update()
        {
            if (KeyBindingManager.ShouldBlockGameplayInput)
                return;

            if (!manualPickupEnabled || !KeyBindingManager.GetGameplayKeyDown(KeyBindingAction.Pickup))
                return;

            // 수동 줍기는 한 번에 하나만 줍고, 마지막에 떨어진 아이템부터 처리합니다.
            TryCollectLatestAtPlayerCell();
        }

        /// <summary>
        /// 현재 플레이어가 서 있는 칸에 아이템을 버립니다.
        /// 인벤토리 드래그 드랍은 플레이어 위치 기준이라 별도 셀을 받지 않는 이 경로를 사용합니다.
        /// </summary>
        public bool DropItem(ItemInstance item, int amount)
        {
            ResolveReferences();
            if (player == null)
                return false;

            return DropItem(item, amount, player.GridPosition);
        }

        /// <summary>
        /// 지정한 그리드 셀에 기존 KDU/YJW ItemPickup 프리팹을 생성하고 중앙 관리 목록에 등록합니다.
        /// 장비 강화/저주/감정 상태가 보존되도록 ItemInstance를 새로 만들지 않고 그대로 넣습니다.
        /// </summary>
        public bool DropItem(ItemInstance item, int amount, Vector2Int cell, System.Func<bool> onPickup = null)
        {
            if (item == null || GridSystem.Instance == null || itemPickupPrefab == null)
                return false;

            Vector3 worldPos = GridSystem.Instance.GridToWorld(cell);
            ItemPickup pickup = Instantiate(itemPickupPrefab, worldPos, Quaternion.identity);

            // YJW ItemPickup.Start는 itemSO가 있으면 새 인스턴스를 만들기 때문에, 드랍 아이템은 item만 넣어 상태를 보존합니다.
            pickup.item = item;
            pickup.itemSO = null;
            pickup.Initialize(cell);

            GroundItemStack stack = EnsureStack(pickup);
            stack.SetAmount(amount);

            ApplyIcon(pickup);
            RegisterPickup(pickup);

            if (onPickup != null)
                _pickupCallbacks[pickup] = onPickup;

            return true;
        }

        /// <summary>
        /// 플레이어 현재 칸의 마지막 아이템 하나를 줍습니다.
        /// 수동 줍기 키는 이 메서드만 호출해서 한 번에 여러 개 먹는 문제를 막습니다.
        /// </summary>
        public bool TryCollectLatestAtPlayerCell()
        {
            ResolveReferences();
            if (player == null)
                return false;

            return TryCollectLatestAt(player.GridPosition);
        }

        /// <summary>
        /// 플레이어 현재 칸의 아이템을 인벤토리에 들어가는 만큼 전부 줍습니다.
        /// 자동 줍기는 이 메서드를 사용하며, 공간 부족 아이템은 바닥에 남깁니다.
        /// </summary>
        public int TryCollectAllAtPlayerCell()
        {
            ResolveReferences();
            if (player == null)
                return 0;

            return TryCollectAllAt(player.GridPosition);
        }

        /// <summary>
        /// 특정 셀의 마지막 아이템 하나를 줍습니다.
        /// 리스트 끝이 최신 드랍 아이템이므로 LIFO 순서가 유지됩니다.
        /// </summary>
        public bool TryCollectLatestAt(Vector2Int cell)
        {
            ScanAndRegisterPickups();

            List<ItemPickup> list = GetCleanList(cell);
            if (list == null || list.Count == 0)
                return false;

            ItemPickup latest = list[^1];
            return TryCollectPickup(latest, list, cell);
        }

        /// <summary>
        /// 특정 셀의 아이템을 가능한 만큼 전부 줍습니다.
        /// 한 아이템이 크기 때문에 실패해도, 더 작은 다른 아이템은 들어갈 수 있으므로 계속 시도합니다.
        /// </summary>
        public int TryCollectAllAt(Vector2Int cell)
        {
            ScanAndRegisterPickups();

            List<ItemPickup> list = GetCleanList(cell);
            if (list == null || list.Count == 0)
                return 0;

            int collected = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                ItemPickup pickup = list[i];
                if (pickup == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (!TryCollectPickup(pickup, list, cell))
                    continue;

                collected++;
            }

            RemoveListIfEmpty(cell, list);
            return collected;
        }

        /// <summary>
        /// ItemPickup 하나를 실제 인벤토리로 옮깁니다.
        /// 성공한 경우에만 목록에서 제거하고 GameObject를 삭제해서, 인벤토리 가득 참 상황에서 아이템이 사라지지 않게 합니다.
        /// </summary>
        private bool TryCollectPickup(ItemPickup pickup, List<ItemPickup> list, Vector2Int cell)
        {
            if (pickup == null) return false;

            // 투척 무기 회수처럼 인벤토리 추가 대신 커스텀 동작이 필요한 경우 콜백으로 처리합니다.
            if (_pickupCallbacks.TryGetValue(pickup, out System.Func<bool> callback))
            {
                bool success = callback();
                if (success)
                {
                    ItemPickupAudio.RequestPickupAudio(pickup.item);
                    _pickupCallbacks.Remove(pickup);
                    list.Remove(pickup);
                    _registeredPickups.Remove(pickup);
                    RemoveListIfEmpty(cell, list);
                    Destroy(pickup.gameObject);
                }
                return success;
            }

            ResolveReferences();
            if (pickup.item == null || inventoryController == null)
                return false;

            int amount = GetAmount(pickup);
            if (!inventoryController.TryAddItem(pickup.item, amount))
                return false;

            ItemPickupAudio.RequestPickupAudio(pickup.item);
            list.Remove(pickup);
            _registeredPickups.Remove(pickup);
            RemoveListIfEmpty(cell, list);
            Destroy(pickup.gameObject);
            return true;
        }

        /// <summary>
        /// 씬에 존재하는 기존 ItemPickup을 중앙 관리 목록에 편입합니다.
        /// 기존 ItemPickup.Update가 Space 입력을 직접 처리하므로, 등록 후에는 컴포넌트만 꺼서 입력 중복을 막습니다.
        /// </summary>
        private void RegisterPickup(ItemPickup pickup)
        {
            if (pickup == null || _registeredPickups.Contains(pickup))
                return;

            // 아직 Start에서 item을 만들기 전인 KDU 스폰 아이템은 다음 스캔에서 다시 등록합니다.
            if (pickup.item == null)
                return;

            Vector2Int cell = pickup.GridPosition;
            if (!_itemsByCell.TryGetValue(cell, out List<ItemPickup> list))
            {
                list = new List<ItemPickup>();
                _itemsByCell[cell] = list;
            }

            list.Add(pickup);
            _registeredPickups.Add(pickup);

            // 기존 ItemPickup의 Space 줍기와 충돌하지 않게 입력 Update를 끕니다. SpriteRenderer는 계속 표시됩니다.
            pickup.enabled = false;
            ApplyIcon(pickup);
            UpdatePickupVisibility(pickup);
        }

        /// <summary>
        /// 플레이어가 한 턴의 행동(이동, 공격, 상자 열기 등)을 완료했을 때 시스템(GameEvents)에서 호출됩니다.
        /// 이 시점에 상자에서 튀어나온 새로운 바닥 아이템(ItemPickup)이 있는지 씬을 스캔하여 중앙 관리 목록에 등록합니다.
        /// </summary>
        /// <param name="timeCost">이번 행동에 소모된 턴 시간 (여기서는 사용하지 않음)</param>
        private void OnPlayerActionCompleted(float timeCost)
        {
            ScanAndRegisterPickups();
        }

        /// <summary>
        /// 몬스터가 죽어서 보상(아이템)을 바닥에 떨어뜨렸을 때 시스템(GameEvents)에서 호출됩니다.
        /// 몬스터가 드랍한 새로운 바닥 아이템(ItemPickup)을 씬에서 스캔하여 중앙 관리 목록에 등록합니다.
        /// </summary>
        /// <param name="pos">적 사망 위치</param>
        /// <param name="timeBonus">획득한 보너스 시간</param>
        private void OnEnemyKilled(Vector2Int pos, float timeBonus)
        {
            ScanAndRegisterPickups();
        }

        /// <summary>
        /// 새 층 생성 직전(GameEvents.OnFloorReset) 호출. 이전 층 바닥에 남은 아이템을 전부 파괴합니다.
        /// 스포너가 만든 아이템은 각 스포너 Clear가 처리하지만, 버린/상자·몬스터 드롭은 여기서만 정리됩니다.
        /// </summary>
        private void OnFloorReset()
        {
            ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null)
                    Destroy(pickups[i].gameObject);
            }

            GoldPickup[] golds = FindObjectsByType<GoldPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < golds.Length; i++)
            {
                if (golds[i] != null)
                    Destroy(golds[i].gameObject);
            }

            _itemsByCell.Clear();
            _registeredPickups.Clear();
            _pickupCallbacks.Clear();
        }

        /// <summary>
        /// 현재 씬의 모든 활성 ItemPickup을 찾아 등록합니다.
        /// 아직 Start가 끝나지 않아 item이 null인 픽업은 다음 스캔에서 다시 시도합니다.
        /// </summary>
        private void ScanAndRegisterPickups()
        {
            ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
                RegisterPickup(pickups[i]);
        }

        /// <summary>
        /// 특정 셀의 목록을 가져오면서 Destroy된 픽업 참조를 청소합니다.
        /// Unity 오브젝트는 파괴 후 null처럼 보이므로 주기적으로 정리해야 목록 순서가 꼬이지 않습니다.
        /// </summary>
        private List<ItemPickup> GetCleanList(Vector2Int cell)
        {
            if (!_itemsByCell.TryGetValue(cell, out List<ItemPickup> list))
                return null;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }

            RemoveListIfEmpty(cell, list);
            return list.Count > 0 ? list : null;
        }

        private void RemoveListIfEmpty(Vector2Int cell, List<ItemPickup> list)
        {
            if (list != null && list.Count == 0)
                _itemsByCell.Remove(cell);
        }

        /// <summary>
        /// 바닥 아이템 수량을 읽습니다.
        /// KDU/YJW가 만든 일반 드랍은 GroundItemStack이 없으므로 1개로 취급합니다.
        /// </summary>
        private static int GetAmount(ItemPickup pickup)
        {
            GroundItemStack stack = pickup != null ? pickup.GetComponent<GroundItemStack>() : null;
            return stack != null ? stack.Amount : 1;
        }

        /// <summary>
        /// 인벤토리 스택 전체를 버릴 때 수량을 보존하기 위한 보조 컴포넌트를 보장합니다.
        /// 기존 ItemPickup을 수정하지 않으려는 목적 때문에 별도 컴포넌트로 분리했습니다.
        /// </summary>
        private static GroundItemStack EnsureStack(ItemPickup pickup)
        {
            GroundItemStack stack = pickup.GetComponent<GroundItemStack>();
            if (stack == null)
                stack = pickup.gameObject.AddComponent<GroundItemStack>();

            return stack;
        }

        /// <summary>
        /// ItemPickup 컴포넌트를 꺼도 SpriteRenderer는 살아 있으므로, 바닥 아이콘은 여기서 직접 맞춰줍니다.
        /// 인벤토리에서 버린 아이템은 itemSO가 비어 있고 item.data만 있으므로 둘 다 확인합니다.
        /// </summary>
        private static void ApplyIcon(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
            if (renderer == null)
                return;

            Sprite icon = pickup.itemSO != null ? pickup.itemSO.Icon
                : pickup.item != null && pickup.item.data != null ? pickup.item.data.Icon
                : null;

            if (icon != null)
                renderer.sprite = icon;
        }

        /// <summary>
        /// 기존 ItemPickup을 비활성화하면 그 안의 시야 갱신도 멈춥니다.
        /// 그래서 VisionSystem 이벤트는 이 컨트롤러가 대신 받아 등록된 픽업들의 표시 여부를 갱신합니다.
        /// </summary>
        private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
        {
            foreach (List<ItemPickup> list in _itemsByCell.Values)
            {
                for (int i = 0; i < list.Count; i++)
                    UpdatePickupVisibility(list[i]);
            }
        }

        /// <summary>
        /// 탐험된 셀의 아이템만 보이게 하는 기존 ItemPickup 표시 규칙을 유지합니다.
        /// </summary>
        private static void UpdatePickupVisibility(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
            if (renderer == null || VisionSystem.Instance == null)
                return;

            renderer.enabled = VisionSystem.Instance.IsExplored(pickup.GridPosition);
        }

        /// <summary>
        /// 이동이 끝난 칸에서 자동 줍기를 실행합니다.
        /// 이동 중간이 아니라 완료 시점에 처리해야 논리 GridPosition과 화면 위치가 어긋나지 않습니다.
        /// </summary>
        private void OnPlayerMoveFinished()
        {
            if (IsAutoPickupEnabled())
                TryCollectAllAtPlayerCell();
        }

        private bool IsAutoPickupEnabled()
        {
            return KeyBindingManager.AutoPickupEnabled;
        }

        /// <summary>
        /// 인스펙터 연결이 비어 있어도 테스트할 수 있게 씬에서 필요한 참조를 찾습니다.
        /// 명시 연결이 있으면 그 값을 우선 사용합니다.
        /// </summary>
        private void ResolveReferences()
        {
            if (player == null)
                player = FindFirstObjectByType<GridPlayer>();

            if (inventoryController == null)
                inventoryController = FindFirstObjectByType<InventoryController>();
        }

        /// <summary>
        /// 플레이어 이동 완료 이벤트를 구독합니다.
        /// Configure/Awake/OnEnable 순서가 달라도 한 번만 연결되도록 _movementSubscribed로 막습니다.
        /// </summary>
        private void SubscribeMovement()
        {
            if (_movementSubscribed || player == null || player.Movement == null)
                return;

            player.Movement.OnMoveFinished += OnPlayerMoveFinished;
            _movementSubscribed = true;
        }

        /// <summary>
        /// 씬 전환/비활성화 때 이벤트 참조가 남지 않도록 해제합니다.
        /// </summary>
        private void UnsubscribeMovement()
        {
            if (!_movementSubscribed || player == null || player.Movement == null)
                return;

            player.Movement.OnMoveFinished -= OnPlayerMoveFinished;
            _movementSubscribed = false;
        }
    }
}
