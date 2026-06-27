// #if UNITY_EDITOR || DEVELOPMENT_BUILD

using PrototypeRT;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 기능을 강제로 테스트하기 위한 에디터 전용 디버그 리모컨.
/// ContextMenu 또는 단축키로 체력, 경험치, 상태이상, 인벤토리, 장비 기능을 확인한다.
/// </summary>
public class DebugPlayer : MonoBehaviour
{
    [Header("대상 참조")]
    [Tooltip("디버그 명령을 적용할 플레이어입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private GridPlayer player;

    [Tooltip("플레이어 체력 컴포넌트입니다. 비워두면 player 또는 씬에서 자동으로 찾습니다.")]
    [SerializeField] private GridPlayerHealth health;

    [Tooltip("플레이어 레벨/경험치 시스템입니다. 비워두면 player 또는 씬에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerLevelSystem levelSystem;

    [Tooltip("플레이어 상태이상 컨트롤러입니다. 비워두면 player 또는 씬에서 자동으로 찾습니다.")]
    [SerializeField] private StatusController statusController;

    [Tooltip("인벤토리 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private InventoryController inventoryController;

    [Tooltip("장비 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private NYHEquipmentController equipmentController;

    [Tooltip("스탯/체력/경험치 Text를 새로고침할 UI입니다. 비활성 패널 안에 있으면 직접 연결하는 것이 가장 안전합니다.")]
    [SerializeField] private PlayerStatsView playerStatsView;

    [Header("체력 디버그")]
    [Tooltip("Damage Player 실행 시 줄 피해량입니다.")]
    [SerializeField, Min(1)] private int damageAmount = 10;

    [Tooltip("Heal Player 실행 시 회복할 체력량입니다.")]
    [SerializeField, Min(1)] private int healAmount = 10;

    [Tooltip("Add Potion 실행 시 추가할 포션 개수입니다.")]
    [SerializeField, Min(1)] private int potionAmount = 1;

    [Header("경험치 디버그")]
    [Tooltip("Add Exp 실행 시 추가할 경험치량입니다.")]
    [SerializeField, Min(1)] private int expAmount = 10;

    [Header("Time Debug")]
    [Tooltip("Add Time 실행 시 남은 시간에 더할 초 단위 값입니다.")]
    [SerializeField, Min(0.1f)] private float debugTimeAmount = 10f;

    [Header("Accuracy Debug")]
    [Tooltip("Decrease Accuracy 실행 시 낮출 명중률입니다. Miss 판정 확인용으로만 사용합니다.")]
    [SerializeField, Min(1)] private int accuracyDebugAmount = 20;

    [Header("상태이상 디버그")]
    [Tooltip("Apply Status 실행 시 적용할 상태이상 종류입니다.")]
    [SerializeField] private StatusType statusType = StatusType.Poison;

    [Tooltip("상태이상이 유지될 플레이어 행동 턴 수입니다.")]
    [SerializeField, Min(1)] private int statusDurationTurns = 3;

    [Tooltip("상태이상의 피해량 또는 강도 값입니다.")]
    [SerializeField, Min(1)] private int statusPower = 1;

    [Header("아이템 / 장비 디버그")]
    [Tooltip("Add Debug Item 실행 시 인벤토리에 넣을 아이템 데이터입니다.")]
    [SerializeField] private List<ItemBase> itemsToAdd = new();

    [Tooltip("Equip Debug Item 실행 시 장착할 장비 데이터입니다. ItemBase의 Slot이 None이면 장착되지 않습니다.")]
    [SerializeField] private ItemBase itemToEquip;

    [Header("단축키")]
    [Tooltip("켜면 Update에서 아래 단축키 입력을 받아 디버그 기능을 실행합니다.")]
    [SerializeField] private bool enableHotkeys = true;
    [Tooltip("플레이어에게 damageAmount만큼 피해를 주는 키입니다.")]
    [SerializeField] private KeyCode damageKey = KeyCode.Alpha1;
    [Tooltip("플레이어를 healAmount만큼 회복시키는 키입니다.")]
    [SerializeField] private KeyCode healKey = KeyCode.Alpha2;
    [Tooltip("플레이어에게 매우 큰 피해를 줘 사망 흐름을 확인하는 키입니다.")]
    [SerializeField] private KeyCode killKey = KeyCode.Alpha3;
    [Tooltip("마우스가 가리키는 몬스터에게 damageAmount만큼 피해를 주는 키입니다.")]
    [SerializeField] private KeyCode damageMonsterKey = KeyCode.Alpha0;
    [Tooltip("PlayerLevelSystem에 expAmount만큼 경험치를 추가하는 키입니다.")]
    [SerializeField] private KeyCode addExpKey = KeyCode.Alpha4;
    [Tooltip("TimeSystem에 debugTimeAmount만큼 남은 시간을 추가하는 키입니다.")]
    [SerializeField] private KeyCode addTimeKey = KeyCode.F8;
    [Tooltip("itemToAdd를 인벤토리에 추가하는 키입니다.")]
    [SerializeField] private KeyCode addItemKey = KeyCode.Alpha5;
    [Tooltip("itemToEquip를 장비 슬롯에 장착해보는 키입니다.")]
    [SerializeField] private KeyCode equipItemKey = KeyCode.Alpha6;
    [Tooltip("힘/이속/공속/회피를 각각 1씩 올리는 키입니다.")]
    [SerializeField] private KeyCode upgradeKey = KeyCode.Alpha7;
    [Tooltip("힘/이속/공속/회피를 각각 1씩 내리는 키입니다.")]
    [SerializeField] private KeyCode downgradeKey = KeyCode.Alpha8;
    [Tooltip("상태이상을 확인하는 키입니다..")]
    [SerializeField] private KeyCode effectKey = KeyCode.Alpha9;
    [Tooltip("플레이어 명중률만 accuracyDebugAmount만큼 낮춰 Miss 판정을 확인하는 키입니다.")]
    [SerializeField] private KeyCode decreaseAccuracyKey = KeyCode.F6;
    [Tooltip("플레이어 명중률만 accuracyDebugAmount만큼 다시 올리는 키입니다.")]
    [SerializeField] private KeyCode increaseAccuracyKey = KeyCode.F7;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!enableHotkeys) return;

        if (Input.GetKeyDown(damageKey))
            DamagePlayer();

        if (Input.GetKeyDown(healKey))
            HealPlayer();

        if (Input.GetKeyDown(killKey))
            KillPlayer();

        if (Input.GetKeyDown(damageMonsterKey))
            DamageMonsterAtMouse();

        if (Input.GetKeyDown(addExpKey))
            AddExp();

        if (Input.GetKeyDown(addTimeKey))
            AddTime();

        if (Input.GetKeyDown(addItemKey))
            AddDebugItem();

        if (Input.GetKeyDown(equipItemKey))
            EquipDebugItem();
        if (Input.GetKeyDown(upgradeKey))
            PlayerUpgrade();
        if (Input.GetKeyDown(downgradeKey))
            PlayerDowngrade();
        if(Input.GetKeyDown(effectKey))
            ApplyStatus();

        if (Input.GetKeyDown(decreaseAccuracyKey))
            DecreaseAccuracy();

        if (Input.GetKeyDown(increaseAccuracyKey))
            IncreaseAccuracy();
    
    }

    /// <summary>
    /// 씬 배치가 바뀌었거나 참조가 비어 있을 때 다시 대상을 찾는다.
    /// </summary>
    [ContextMenu("Debug/Refresh References")]
    public void ResolveReferences()
    {
        if (player == null)
            player = FindAnyObjectByType<GridPlayer>();

        if (health == null && player != null)
            health = player.Health != null ? player.Health : player.GetComponent<GridPlayerHealth>();

        if (health == null)
            health = FindAnyObjectByType<GridPlayerHealth>();

        if (levelSystem == null)
            levelSystem = player != null ? player.GetComponent<PlayerLevelSystem>() : FindAnyObjectByType<PlayerLevelSystem>();

        if (statusController == null && player != null)
            statusController = player.GetComponent<StatusController>();

        if (statusController == null && player == null)
            statusController = FindAnyObjectByType<StatusController>();

        if (inventoryController == null)
            inventoryController = FindAnyObjectByType<InventoryController>();

        if (equipmentController == null)
            equipmentController = FindAnyObjectByType<NYHEquipmentController>();

        if (playerStatsView == null)
            playerStatsView = FindStatsView();
    }

    /// <summary>
    /// 스탯 UI를 강제로 새로고침한다.
    /// 비활성 인벤토리 패널 안의 PlayerStatsView도 찾아본다.
    /// </summary>
    [ContextMenu("Debug/Refresh Stats UI")]
    public void RefreshStatsUI()
    {
        if (playerStatsView == null)
            playerStatsView = FindStatsView();

        if (playerStatsView == null)
        {
            Debug.LogWarning("[DebugPlayer] PlayerStatsView를 찾지 못했습니다. 비활성 UI라면 인스펙터에 직접 연결하세요.");
            return;
        }

        playerStatsView.Refresh();
    }

    private PlayerStatsView FindStatsView()
    {
        PlayerStatsView activeView = FindAnyObjectByType<PlayerStatsView>();
        if (activeView != null) return activeView;

        PlayerStatsView[] allViews = Resources.FindObjectsOfTypeAll<PlayerStatsView>();
        for (int i = 0; i < allViews.Length; i++)
        {
            PlayerStatsView view = allViews[i];
            if (view != null && view.gameObject.scene.IsValid())
                return view;
        }

        return null;
    }

    /// <summary>
    /// 전투/이동 체감 확인용으로 주요 전투 스탯을 한 번에 1씩 올립니다.
    /// 디버그 전용이므로 레벨업 보상 계산과는 분리되어 있습니다.
    /// </summary>
    public void PlayerUpgrade()
    {
        NYHPlayerStats stats = NYHPlayerStats.Instance;
        if (stats == null) return;

        stats.AddStat(NYHStatType.Strength, 1);
        stats.AddStat(NYHStatType.MoveSpeed, 1);
        stats.AddStat(NYHStatType.AttackSpeed, 1);
        stats.AddStat(NYHStatType.Accuracy, 1);
        stats.AddStat(NYHStatType.Evasion, 1);

        RefreshStatsUI();
    }

    /// <summary>
    /// PlayerUpgrade로 올린 주요 전투 스탯을 한 번에 1씩 낮춥니다.
    /// NYHPlayerStats.AddStat 내부 Clamp 덕분에 음수로 내려가지 않습니다.
    /// </summary>
    public void PlayerDowngrade()
    {
        NYHPlayerStats stats = NYHPlayerStats.Instance;
        if (stats == null) return;

        stats.AddStat(NYHStatType.Strength, -1);
        stats.AddStat(NYHStatType.MoveSpeed, -1);
        stats.AddStat(NYHStatType.AttackSpeed, -1);
        stats.AddStat(NYHStatType.Accuracy, -1);
        stats.AddStat(NYHStatType.Evasion, -1);

        RefreshStatsUI();
    }

    /// <summary>
    /// Miss 테스트를 위해 플레이어 명중률만 낮춥니다.
    /// 다른 전투 스탯까지 바꾸면 원인 파악이 어려워서 Accuracy만 조정합니다.
    /// </summary>
    [ContextMenu("Debug/Decrease Accuracy")]
    public void DecreaseAccuracy()
    {
        AdjustAccuracy(-accuracyDebugAmount);
    }

    /// <summary>
    /// Decrease Accuracy로 낮춘 명중률을 되돌릴 때 사용합니다.
    /// </summary>
    [ContextMenu("Debug/Increase Accuracy")]
    public void IncreaseAccuracy()
    {
        AdjustAccuracy(accuracyDebugAmount);
    }

    /// <summary>
    /// 적 회피율이 낮은 상황에서도 Miss를 강제로 보기 위해 명중률을 0까지 내립니다.
    /// </summary>
    [ContextMenu("Debug/Set Accuracy To Zero")]
    public void SetAccuracyToZero()
    {
        if (!TryGetPlayerStats(out NYHPlayerStats stats)) return;

        AdjustAccuracy(-stats.Accuracy);
    }


    /// <summary>
    /// damageAmount만큼 플레이어에게 피해를 줘 피격/사망/HP UI 갱신 흐름을 확인합니다.
    /// </summary>
    [ContextMenu("Debug/Damage Player")]
    public void DamagePlayer()
    {
        if (!TryGetHealth(out GridPlayerHealth targetHealth)) return;

        targetHealth.TakeDamage(damageAmount);
        Debug.Log($"[DebugPlayer] 플레이어 피해: {damageAmount}, HP {targetHealth.CurrentHp}/{targetHealth.MaxHp}");
        RefreshStatsUI();
    }

    /// <summary>
    /// healAmount만큼 플레이어를 회복시켜 HP 회복과 UI 갱신을 확인합니다.
    /// </summary>
    [ContextMenu("Debug/Heal Player")]
    public void HealPlayer()
    {
        if (!TryGetHealth(out GridPlayerHealth targetHealth)) return;

        targetHealth.Heal(healAmount);
        Debug.Log($"[DebugPlayer] 플레이어 회복: {healAmount}, HP {targetHealth.CurrentHp}/{targetHealth.MaxHp}");
        RefreshStatsUI();
    }

    /// <summary>
    /// 플레이어 HP보다 큰 피해를 줘 사망 처리와 사망 애니메이션 흐름을 확인합니다.
    /// </summary>
    [ContextMenu("Debug/Kill Player")]
    public void KillPlayer()
    {
        if (!TryGetHealth(out GridPlayerHealth targetHealth)) return;

        targetHealth.TakeDamage(targetHealth.MaxHp + 999999);
        Debug.Log($"[DebugPlayer] 플레이어 사망 테스트 실행, HP {targetHealth.CurrentHp}/{targetHealth.MaxHp}");
        RefreshStatsUI();
    }

    /// <summary>
    /// 포션 개수를 늘려 인벤토리 없이 포션 UI/사용 흐름을 확인합니다.
    /// </summary>
    /// <summary>
    /// 마우스가 가리키는 그리드 칸의 몬스터에게 피해를 주고, 실제 적용된 피해량을 로그로 남깁니다.
    /// 방어력/피해 경감이 있는 적은 요청 피해와 실제 HP 감소량이 다를 수 있습니다.
    /// </summary>
    [ContextMenu("Debug/Damage Monster At Mouse")]
    public void DamageMonsterAtMouse()
    {
        if (!TryGetMouseGridPosition(out Vector2Int targetCell)) return;

        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("[DebugPlayer] TurnManager를 찾지 못해 몬스터를 찾을 수 없습니다.");
            return;
        }

        IGridEntity entity = TurnManager.Instance.GetEntityAt(targetCell);
        IDamageable damageable = entity as IDamageable;
        if (damageable == null)
        {
            Debug.LogWarning($"[DebugPlayer] {targetCell} 위치에 피해를 줄 몬스터가 없습니다.");
            return;
        }

        if (player != null && ReferenceEquals(entity, player))
        {
            Debug.LogWarning("[DebugPlayer] 선택한 대상이 플레이어입니다. 몬스터만 피해 테스트합니다.");
            return;
        }

        int beforeHp = damageable.CurrentHp;
        damageable.TakeDamage(damageAmount);
        int actualDamage = Mathf.Max(0, beforeHp - damageable.CurrentHp);
        string targetName = GetDebugTargetName(damageable);

        Debug.Log($"[DebugPlayer] 몬스터 피해: {targetName}, 요청 피해 {damageAmount}, 실제 HP 감소 {actualDamage}, HP {damageable.CurrentHp}/{damageable.MaxHp}");
    }

    [ContextMenu("Debug/Add Potion")]
    public void AddPotion()
    {
        if (!TryGetHealth(out GridPlayerHealth targetHealth)) return;

        targetHealth.AddPotion(potionAmount);
        Debug.Log($"[DebugPlayer] 포션 추가: {potionAmount}, 현재 포션 {targetHealth.PotionCount}");
        RefreshStatsUI();
    }

    /// <summary>
    /// 현재 포션을 1개 사용해 즉시 회복과 지속 회복 흐름을 확인합니다.
    /// </summary>
    [ContextMenu("Debug/Use Potion")]
    public void UsePotion()
    {
        if (!TryGetHealth(out GridPlayerHealth targetHealth)) return;

        targetHealth.TryUsePotion();
        Debug.Log($"[DebugPlayer] 포션 사용 시도, HP {targetHealth.CurrentHp}/{targetHealth.MaxHp}, 포션 {targetHealth.PotionCount}");
        RefreshStatsUI();
    }

    /// <summary>
    /// 타이머 검증 중 바로 시간을 보충할 수 있도록 TimeSystem의 공개 API만 사용합니다.
    /// </summary>
    [ContextMenu("Debug/Add Time")]
    public void AddTime()
    {
        if (!TryGetTimeSystem(out TimeSystem timeSystem)) return;

        timeSystem.AddTime(debugTimeAmount);
        Debug.Log($"[DebugPlayer] Time added: {debugTimeAmount:0.##}s, remaining {timeSystem.TimeRemaining:0.##}s");
    }

    [ContextMenu("Debug/Add Exp")]
    public void AddExp()
    {
        if (!TryGetLevelSystem(out PlayerLevelSystem targetLevel)) return;

        targetLevel.AddExp(expAmount);
        Debug.Log($"[DebugPlayer] 경험치 추가: {expAmount}, Lv {targetLevel.Level}, EXP {targetLevel.ExpDisplayText}");
        RefreshStatsUI();
    }

    /// <summary>
    /// 설정한 상태이상을 플레이어 위치에 강제로 적용해 상태 컨트롤러 흐름을 확인합니다.
    /// </summary>
    [ContextMenu("Debug/Apply Status")]
    public void ApplyStatus()
    {
        if (!TryGetStatusController(out StatusController targetStatus)) return;

        IGridEntity targetEntity = player != null ? player : FindAnyObjectByType<GridPlayer>();
        Vector2Int targetPosition = targetEntity != null ? targetEntity.GridPosition : Vector2Int.zero;
        targetStatus.ApplyStatus(new StatusRequest(targetEntity, targetPosition, statusType, statusDurationTurns, statusPower, null));

        Debug.Log($"[DebugPlayer] 상태이상 적용: {statusType}, {statusDurationTurns}턴, 강도 {statusPower}");
        RefreshStatsUI();
    }

    /// <summary>
    /// itemToAdd에 연결한 아이템 데이터를 인벤토리에 직접 추가합니다.
    /// </summary>
    [ContextMenu("Debug/Add Debug Item")]
    public void AddDebugItem()
    {
        if (!TryGetInventory(out InventoryController targetInventory)) return;
        
        if (itemsToAdd == null || itemsToAdd.Count == 0)
        {
            Debug.LogWarning("[DebugPlayer] itemToAdd가 비어 있습니다.");
            return;
        }

        int successCount  = 0;

        foreach (var item in itemsToAdd)
        {
            bool added = targetInventory.TryAddItem(item.CreateInstance());
            if (added) successCount++;

            Debug.Log($"[DebugPlayer] 인벤토리 아이템 추가: {item.Name}, 성공 {added}");
        }

        Debug.Log($"[DebugPlayer] 인벤토리 아이템 추가: {successCount}/{itemsToAdd.Count}, 성공 {successCount}");
        RefreshStatsUI();
    }

    /// <summary>
    /// itemToEquip에 연결한 장비 데이터를 장착 컨트롤러에 직접 전달합니다.
    /// </summary>
    [ContextMenu("Debug/Equip Debug Item")]
    public void EquipDebugItem()
    {
        if (!TryGetEquipment(out NYHEquipmentController targetEquipment)) return;
        if (itemToEquip == null)
        {
            Debug.LogWarning("[DebugPlayer] itemToEquip이 비어 있습니다.");
            return;
        }

        bool equipped = targetEquipment.TryEquip(itemToEquip);
        Debug.Log($"[DebugPlayer] 장비 장착: {itemToEquip.Name}, 성공 {equipped}");
        RefreshStatsUI();
    }

    private bool TryGetHealth(out GridPlayerHealth targetHealth)
    {
        ResolveReferences();
        targetHealth = health;

        if (targetHealth != null) return true;

        Debug.LogWarning("[DebugPlayer] GridPlayerHealth를 찾지 못했습니다.");
        return false;
    }

    private bool TryGetLevelSystem(out PlayerLevelSystem targetLevel)
    {
        ResolveReferences();
        targetLevel = levelSystem;

        if (targetLevel != null) return true;

        Debug.LogWarning("[DebugPlayer] PlayerLevelSystem을 찾지 못했습니다.");
        return false;
    }

    private bool TryGetTimeSystem(out TimeSystem timeSystem)
    {
        timeSystem = TimeSystem.Instance != null ? TimeSystem.Instance : FindAnyObjectByType<TimeSystem>();

        if (timeSystem != null) return true;

        Debug.LogWarning("[DebugPlayer] TimeSystem을 찾지 못해 시간을 추가할 수 없습니다.");
        return false;
    }

    private bool TryGetStatusController(out StatusController targetStatus)
    {
        ResolveReferences();
        targetStatus = statusController;

        if (targetStatus != null) return true;

        if (player != null)
        {
            targetStatus = player.gameObject.AddComponent<StatusController>();
            statusController = targetStatus;
            Debug.Log("[DebugPlayer] 플레이어에 StatusController가 없어 디버그용으로 추가했습니다.");
            return true;
        }

        Debug.LogWarning("[DebugPlayer] StatusController를 찾지 못했습니다.");
        return false;
    }

    private bool TryGetInventory(out InventoryController targetInventory)
    {
        ResolveReferences();
        targetInventory = inventoryController;

        if (targetInventory != null) return true;

        Debug.LogWarning("[DebugPlayer] InventoryController를 찾지 못했습니다.");
        return false;
    }

    private bool TryGetEquipment(out NYHEquipmentController targetEquipment)
    {
        ResolveReferences();
        targetEquipment = equipmentController;

        if (targetEquipment != null) return true;

        Debug.LogWarning("[DebugPlayer] NYHEquipmentController를 찾지 못했습니다.");
        return false;
    }

    private bool TryGetPlayerStats(out NYHPlayerStats stats)
    {
        ResolveReferences();

        stats = NYHPlayerStats.Instance;
        if (stats == null && player != null)
            stats = player.GetComponent<NYHPlayerStats>();

        if (stats != null) return true;

        Debug.LogWarning("[DebugPlayer] NYHPlayerStats를 찾지 못해 명중률을 조정할 수 없습니다.");
        return false;
    }

    private void AdjustAccuracy(int amount)
    {
        if (!TryGetPlayerStats(out NYHPlayerStats stats)) return;

        int before = stats.Accuracy;
        stats.AddStat(NYHStatType.Accuracy, amount);
        int after = stats.Accuracy;

        Debug.Log($"[DebugPlayer] Accuracy changed: {before} -> {after} ({amount})");
        RefreshStatsUI();
    }

    private bool TryGetMouseGridPosition(out Vector2Int gridPosition)
    {
        gridPosition = Vector2Int.zero;

        if (Camera.main == null)
        {
            Debug.LogWarning("[DebugPlayer] Main Camera를 찾지 못해 마우스 위치를 그리드로 변환할 수 없습니다.");
            return false;
        }

        if (GridSystem.Instance == null)
        {
            Debug.LogWarning("[DebugPlayer] GridSystem을 찾지 못해 마우스 위치를 그리드로 변환할 수 없습니다.");
            return false;
        }

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridPosition = GridSystem.Instance.WorldToGrid(worldPosition);
        return true;
    }

    private string GetDebugTargetName(IDamageable target)
    {
        if (target is Object targetObject)
            return targetObject.name;

        return target != null ? target.GetType().Name : "Unknown";
    }
}

// #endif
