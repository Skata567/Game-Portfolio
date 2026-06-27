using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 애니메이션 전용 컨트롤러.
/// 이동/공격/체력 로직이 Animator를 직접 알지 않도록 이벤트만 구독합니다.
/// 사용법:
/// 1. Player 오브젝트에 Animator와 이 컴포넌트를 붙인다.
/// 2. Animator Controller에 Attack, Die, Move Trigger와 IsMoving, IsDead Bool을 만든다.
/// 3. idleStateName/runStateName을 Animator의 실제 상태 이름과 맞춘다.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class GridPlayerAnimationController : MonoBehaviour
{
    [Header("Animator Parameters")]
    [Tooltip("공격 성공 시 사용하는 Trigger 이름.")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string hurtTriggerName = "Hurt";

    [Tooltip("사망 시 사용하는 Trigger 이름.")]
    [SerializeField] private string dieTriggerName = "Die";

    [Tooltip("이전 Move Trigger가 남아 있으면 겹쳐서 애니메이션이 꼬일 수 있어 이동 시작/종료 시 Reset만 한다.")]
    [SerializeField] private string moveTriggerName = "Move";

    [Tooltip("이동 중 true, 이동 종료 시 false가 되는 Bool 이름.")]
    [SerializeField] private string isMovingBoolName = "IsMoving";

    [Tooltip("사망 시 true로 고정하는 Bool 이름.")]
    [SerializeField] private string isDeadBoolName = "IsDead";

    [Header("Animator States")]
    [Tooltip("기본 Base Layer는 0. 다른 레이어에서 애니메이션이 재생될 때만 바꾼다.")]
    [SerializeField] private int baseLayerIndex;

    [Tooltip("이동이 끝났을 때 즉시 재생할 Idle 상태 이름.")]
    [SerializeField] private string idleStateName = "warrior idle";

    [Tooltip("이동을 시작할 때 즉시 재생할 Run/Walk 상태 이름.")]
    [SerializeField] private string runStateName = "warrior run";

    [Tooltip("공격할 때 즉시 처음부터 재생할 Animator 상태 이름.")]
    [SerializeField] private string attackStateName = "warrior single swing1";

    [Header("Facing")]
    [Tooltip("원본 스프라이트가 오른쪽을 보고 있으면 true, 왼쪽을 보고 있으면 false.")]
    [SerializeField] private bool faceRightByDefault = true;

    [Header("공격 애니메이션 속도")]
    [Tooltip("공격 애니메이션 배속이 이 값보다 높아지지 않게 제한합니다.")]
    [SerializeField, Min(1f)] private float maxAttackAnimationSpeed = 12f;

    [Tooltip("공격 후 Animator speed를 기본값으로 되돌리기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float attackSpeedResetDelay = 0.25f;

    [Tooltip("공격 애니메이션이 너무 짧아져 아예 안 보이지 않도록 보장할 최소 재생 시간입니다.")]
    [SerializeField, Min(0.01f)] private float minAttackAnimationDuration = 0.08f;

    [Tooltip("컨트롤러에서 공격 클립 길이를 찾지 못했을 때 사용할 기본 길이입니다.")]
    [SerializeField, Min(0.01f)] private float attackClipLengthFallback = 1.1f;

    [Header("Move Visual Readability")]
    [Tooltip("실제 이동 속도는 그대로 두고, 걷기/달리기 모션이 화면에 최소 이 시간만큼 보이게 합니다.")]
    [SerializeField, Min(0f)] private float minMoveAnimationVisibleTime = 0.12f;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private GridPlayer _player;
    private Coroutine _attackSpeedCoroutine;
    private Coroutine _moveFinishCoroutine;
    private bool _isDead;
    private bool _isFacingRight;
    private bool _isSubscribed;
    private bool _isMoveVisualActive;
    private bool _isAttackVisualActive;
    private float _cachedAttackClipLength = -1f;
    private float _lastMoveStartedTime;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _player = GetComponent<GridPlayer>();
        _isFacingRight = faceRightByDefault;
        ApplyFacing();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_attackSpeedCoroutine != null)
        {
            StopCoroutine(_attackSpeedCoroutine);
            _attackSpeedCoroutine = null;
        }

        if (_moveFinishCoroutine != null)
        {
            StopCoroutine(_moveFinishCoroutine);
            _moveFinishCoroutine = null;
        }

        if (_animator != null)
            _animator.speed = 1f;

        _isMoveVisualActive = false;
        _isAttackVisualActive = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (_isDead || !_isSubscribed || _player == null || _player.Movement == null) return;
        if (!_isMoveVisualActive || _player.Movement.IsMoving) return;

        // 고속 연속 이동 중에는 이동 코루틴 사이 빈 프레임에 Idle로 빠지지 않게 유지한다.
        // 키 입력을 놓았거나 다음 칸이 벽이면 즉시 Idle로 돌려 걷기 모션이 남지 않게 한다.
        if (!_player.Movement.ShouldKeepMoveAnimationAfterMove())
            OnMoveFinished();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (_player == null)
            _player = GetComponent<GridPlayer>();

        if (_player == null || _player.Movement == null || _player.Combat == null || _player.Health == null)
            return;

        // GridPlayer.Awake 이후에만 구독해 컴포넌트 초기화 순서 문제를 피한다.
        _player.Movement.OnMoveSucceeded += OnMoveSucceeded;
        _player.Movement.OnMoveFinished += OnMoveFinished;
        _player.Movement.OnMoveSpeedChanged += OnMoveSpeedChanged;
        _player.Combat.OnAttackPerformed += OnAttackPerformed;
        _player.Health.OnDamaged += OnDamaged;
        _player.Health.OnDied += OnDied;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _player == null) return;

        _player.Movement.OnMoveSucceeded -= OnMoveSucceeded;
        _player.Movement.OnMoveFinished -= OnMoveFinished;
        _player.Movement.OnMoveSpeedChanged -= OnMoveSpeedChanged;
        _player.Combat.OnAttackPerformed -= OnAttackPerformed;
        _player.Health.OnDamaged -= OnDamaged;
        _player.Health.OnDied -= OnDied;
        _isSubscribed = false;
    }

    private void OnMoveSucceeded(Vector2Int direction)
    {
        if (_isDead) return;

        if (_moveFinishCoroutine != null)
        {
            StopCoroutine(_moveFinishCoroutine);
            _moveFinishCoroutine = null;
        }

        _lastMoveStartedTime = Time.time;
        UpdateHorizontalFacing(direction);
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, true);
        _isMoveVisualActive = true;

        // Trigger가 남아 겹쳐 재생되는 문제를 막기 위해 이동 상태를 직접 재생한다.
        PlayStateIfConfigured(runStateName, keepCurrentRunTime: true);
    }

    private void OnMoveFinished()
    {
        if (_moveFinishCoroutine != null)
            return;

        float remainingVisibleTime = minMoveAnimationVisibleTime - (Time.time - _lastMoveStartedTime);
        if (remainingVisibleTime > 0f)
        {
            _moveFinishCoroutine = StartCoroutine(FinishMoveVisualAfterDelay(remainingVisibleTime));
            return;
        }

        ApplyMoveFinishedVisual();
    }

    private IEnumerator FinishMoveVisualAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        _moveFinishCoroutine = null;

        if (_isDead)
            yield break;

        if (_player != null && _player.Movement != null && _player.Movement.IsMoving)
            yield break;

        ApplyMoveFinishedVisual();
    }

    private void ApplyMoveFinishedVisual()
    {
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, false);
        _isMoveVisualActive = false;
        if (_isAttackVisualActive)
            return;

        if (_animator != null)
            _animator.speed = 1f;

        // 걷기 클립이 끝나기 기다리지 않고 즉시 Idle로 자른다.
        PlayStateIfConfigured(idleStateName, keepCurrentRunTime: false);
    }

    private void OnMoveSpeedChanged(float speedMultiplier)
    {
        if (_isDead || _animator == null) return;

        // GridPlayerMovement가 계산한 배속을 적용한다.
        // MoveSpeed 5 이후 애니메이션 배속 고정 정책은 이동 스크립트에서 결정한다.
        _animator.speed = speedMultiplier;
    }

    //최종 공격 성공시
    private void OnAttackPerformed(Vector2Int direction)
    {
        if (_isDead) return;

        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.PlayerAttack);
        UpdateHorizontalFacing(direction);
        ResetTriggerIfConfigured(hurtTriggerName);
        ApplyAttackAnimationSpeed();

        if (!PlayStateIfConfigured(attackStateName, keepCurrentRunTime: false))
            SetTriggerIfConfigured(attackTriggerName);
    }

    private void OnDamaged(int damage)
    {
        if (_isDead || _player.Health.IsDead) return;
        if (_isAttackVisualActive) return;

        SetTriggerIfConfigured(hurtTriggerName);
    }

    private void OnDied()
    {
        if (_isDead) return;

        _isDead = true;
        _isMoveVisualActive = false;
        _isAttackVisualActive = false;
        if (_attackSpeedCoroutine != null)
        {
            StopCoroutine(_attackSpeedCoroutine);
            _attackSpeedCoroutine = null;
        }

        if (_moveFinishCoroutine != null)
        {
            StopCoroutine(_moveFinishCoroutine);
            _moveFinishCoroutine = null;
        }

        if (_animator != null)
            _animator.speed = 1f;

        ResetTriggerIfConfigured(attackTriggerName);
        ResetTriggerIfConfigured(hurtTriggerName);
        ResetTriggerIfConfigured(moveTriggerName);
        SetBoolIfConfigured(isMovingBoolName, false);
        SetBoolIfConfigured(isDeadBoolName, true);
        SetTriggerIfConfigured(dieTriggerName);
    }

    private void UpdateHorizontalFacing(Vector2Int direction)
    {
        if (direction.x == 0) return;

        _isFacingRight = direction.x > 0;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        // 원본 스프라이트가 오른쪽을 본다면 반대로 flipX만 뒤집어 좌우 리소스 중복을 줄인다.
        _spriteRenderer.flipX = faceRightByDefault ? !_isFacingRight : _isFacingRight;
    }

    private void ApplyAttackAnimationSpeed()
    {
        if (_animator == null) return;

        if (_attackSpeedCoroutine != null)
            StopCoroutine(_attackSpeedCoroutine);

        float targetDuration = GetAttackAnimationTargetDuration();
        _isAttackVisualActive = true;
        _animator.speed = GetAttackAnimationSpeed(targetDuration);
        _attackSpeedCoroutine = StartCoroutine(RestoreAnimatorSpeedAfterAttack(targetDuration));
    }

    private IEnumerator RestoreAnimatorSpeedAfterAttack(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (_animator != null && !_isDead)
            _animator.speed = _isMoveVisualActive ? _animator.speed : 1f;

        _isAttackVisualActive = false;
        _attackSpeedCoroutine = null;
    }

    private float GetAttackAnimationSpeed(float targetDuration)
    {
        float clipLength = GetAttackClipLength();
        float safeDuration = Mathf.Max(0.01f, targetDuration);
        float speed = clipLength / safeDuration;
        return Mathf.Clamp(speed, 1f, maxAttackAnimationSpeed);
    }

    private float GetAttackAnimationTargetDuration()
    {
        float actionInterval = _player != null && _player.Combat != null
            ? _player.Combat.GetMeleeActionInterval()
            : attackSpeedResetDelay;

        return Mathf.Max(minAttackAnimationDuration, actionInterval);
    }

    private float GetAttackClipLength()
    {
        if (_cachedAttackClipLength > 0f)
            return _cachedAttackClipLength;

        _cachedAttackClipLength = attackClipLengthFallback;

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return _cachedAttackClipLength;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == attackStateName)
            {
                _cachedAttackClipLength = Mathf.Max(0.01f, clip.length);
                break;
            }
        }

        return _cachedAttackClipLength;
    }

    private bool PlayStateIfConfigured(string stateName, bool keepCurrentRunTime)
    {
        if (string.IsNullOrEmpty(stateName) || !CanUseAnimator())
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!_animator.HasState(baseLayerIndex, stateHash))
            return false;

        float normalizedTime = 0f;
        if (keepCurrentRunTime && IsCurrentState(stateHash))
            normalizedTime = _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).normalizedTime;

        _animator.Play(stateHash, baseLayerIndex, normalizedTime);
        return true;
    }

    private bool IsCurrentState(int stateHash)
    {
        if (_animator == null) return false;
        return _animator.GetCurrentAnimatorStateInfo(baseLayerIndex).shortNameHash == stateHash;
    }

    private void SetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !CanUseAnimator() || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.SetTrigger(triggerName);
    }

    private void ResetTriggerIfConfigured(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || !CanUseAnimator() || !HasParameter(triggerName, AnimatorControllerParameterType.Trigger)) return;
        _animator.ResetTrigger(triggerName);
    }

    private void SetBoolIfConfigured(string boolName, bool value)
    {
        if (string.IsNullOrEmpty(boolName) || !CanUseAnimator() || !HasParameter(boolName, AnimatorControllerParameterType.Bool)) return;
        _animator.SetBool(boolName, value);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (!CanUseAnimator()) return false;

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool CanUseAnimator()
    {
        // 씬 전환/사망 처리 중 비활성화된 Animator에 Play/Trigger를 보내면 Unity 경고가 발생한다.
        return _animator != null
            && _animator.runtimeAnimatorController != null
            && _animator.isActiveAndEnabled
            && _animator.gameObject.activeInHierarchy;
    }
}
