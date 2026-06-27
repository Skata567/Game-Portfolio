using UnityEngine;
using PrototypeRT;

/// <summary>
/// 플레이어 입력을 GridPlayer의 이동/근접 공격/포션 사용 흐름으로 전달합니다.
/// 투척 무기는 인벤토리의 YJW 아이템 사용 흐름에서 처리합니다.
/// </summary>
public class GridPlayerInput : MonoBehaviour
{
    private GridPlayer _player;
    private CameraFollow _cameraFollow;
    private PrototypeRT.QuickSlotController _quickSlotController;

    /// <summary>
    /// 플레이어가 숫자키(1~0)를 눌러 퀵슬롯 사용을 요청할 때 발생합니다.
    /// int 매개변수는 0~9의 슬롯 인덱스입니다. (키보드 1 -> 0, 키보드 0 -> 9)
    /// </summary>
    public event System.Action<int> OnQuickSlotNumberPressed;
    public event System.Action<int> OnQuickSlotScroll;

    public void Init(GridPlayer player)
    {
        _player = player;
        // Safe Room 플래닝 모드 중엔 좌클릭이 카메라 드래그용이므로 근접 공격으로 흘려보내지 않게 참조 캐시.
        _cameraFollow = FindFirstObjectByType<CameraFollow>();
        _quickSlotController = FindFirstObjectByType<PrototypeRT.QuickSlotController>();
    }

    /// <summary>
    /// 현재 누르고 있는 이동 방향을 한 칸 이동 기준으로 반환합니다.
    /// </summary>
    public bool TryGetHeldMoveDirection(out Vector2Int direction)
    {
        if (KeyBindingManager.ShouldBlockGameplayInput)
        {
            direction = Vector2Int.zero;
            return false;
        }

        if (Input.GetKey(KeyBindingManager.GetGameplayKey(KeyBindingAction.MoveUp)))
        {
            direction = Vector2Int.up;
            return true;
        }
        if (Input.GetKey(KeyBindingManager.GetGameplayKey(KeyBindingAction.MoveDown)))
        {
            direction = Vector2Int.down;
            return true;
        }
        if (Input.GetKey(KeyBindingManager.GetGameplayKey(KeyBindingAction.MoveLeft)))
        {
            direction = Vector2Int.left;
            return true;
        }
        if (Input.GetKey(KeyBindingManager.GetGameplayKey(KeyBindingAction.MoveRight)))
        {
            direction = Vector2Int.right;
            return true;
        }

        direction = Vector2Int.zero;
        return false;
    }

    private void Update()
    {
        if (KeyBindingManager.ShouldBlockGameplayInput) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (_player.Health.IsDead) return;
        if (_player.Movement.IsMoving) return;
        if (TimeSystem.Instance.TimeRemaining <= 0f) return;

        bool canAct = _player.ActionDelay == null || _player.ActionDelay.CanAct;
        bool canMove = canAct || _player.Movement.IgnoresActionDelayForMovement;

        CheckQuickSlotInputs();

        if (canMove && TryGetHeldMoveDirection(out Vector2Int moveDirection))
        {
            _player.Movement.TryMove(moveDirection);
            return;
        }

        if (!canAct) return;

        if (Input.GetMouseButton(0))
        {
            // 플래닝 모드(Safe Room) 중엔 좌클릭이 카메라 드래그 입력 — 근접 공격 무시.
            if (_cameraFollow != null && _cameraFollow.IsPlanningMode) return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int clickedCell = GridSystem.Instance.WorldToGrid(worldPos);

                if (Input.GetMouseButtonDown(0))
                {
                    if (_quickSlotController == null)
                        _quickSlotController = FindFirstObjectByType<PrototypeRT.QuickSlotController>();

                    if (_quickSlotController != null && _quickSlotController.TryUseSelectedSlotAt(clickedCell))
                        return;
                }
            }
            return;
        }

        if (KeyBindingManager.GetGameplayKeyDown(KeyBindingAction.UseHealthPotion))
        {
            _player.Health.TryUsePotion();
        }
    }

    /// <summary>
    /// 매 프레임 숫자키(1~0) 입력을 검사하여 퀵슬롯 이벤트를 발생시킵니다.
    /// </summary>
    private void CheckQuickSlotInputs()
    {
        if (KeyBindingManager.ShouldBlockGameplayInput)
            return;

        for (int i = 0; i < 10; i++)
        {
            KeyBindingAction action = KeyBindingManager.GetQuickSlotAction(i);
            if (KeyBindingManager.GetGameplayKeyDown(action))
            {
                OnQuickSlotNumberPressed?.Invoke(i);
                break;
            }
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) OnQuickSlotScroll?.Invoke(-1);
        else if (scroll < 0f) OnQuickSlotScroll?.Invoke(1);
    }
}
