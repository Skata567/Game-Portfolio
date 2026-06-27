using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어/엔티티에게 걸린 시간제 상태이상을 관리합니다.
/// 몬스터, 보스, 아이템, 트랩은 ApplyStatus로 요청만 보내고 실제 지속시간/틱 피해/이동 보정은 여기서 처리합니다.
/// </summary>
public class StatusController : MonoBehaviour
{
    [Header("상태이상 기본값")]
    [Tooltip("독, 화상, 출혈, 굶주림처럼 주기적으로 피해를 주는 상태의 기본 틱 간격입니다.")]
    [SerializeField, Min(0.05f)] private float defaultDamageTickInterval = 1f;

    [Header("이동 쿨타임 배율")]
    [Tooltip("둔화 상태일 때 이동 쿨타임에 곱할 배율입니다.")]
    [SerializeField, Min(0.01f)] private float slowMoveMultiplier = 1.5f;

    [Tooltip("냉기 상태일 때 이동 쿨타임에 곱할 배율입니다.")]
    [SerializeField, Min(0.01f)] private float chillMoveMultiplier = 1.25f;

    [Tooltip("굶주림 상태일 때 이동 쿨타임에 곱할 배율입니다.")]
    [SerializeField, Min(0.01f)] private float hungerMoveMultiplier = 1.3f;

    [Tooltip("신속 상태일 때 이동 쿨타임에 곱할 배율입니다. 1보다 작으면 빨라집니다.")]
    [SerializeField, Min(0.01f)] private float hasteMoveMultiplier = 0.7f;

    [Header("공격 쿨타임 배율")]
    [Tooltip("냉기 상태일 때 공격 쿨타임과 공격 시간 비용에 곱할 배율입니다.")]
    [SerializeField, Min(0.01f)] private float chillAttackMultiplier = 1.3f;

    private readonly List<ActiveStatus> _statuses = new();
    private IDamageable _damageable;

    /// <summary>
    /// 상태 목록이 추가/갱신/삭제될 때 호출됩니다.
    /// PlayerStatusVisual 같은 표시 전용 스크립트가 이 이벤트를 구독합니다.
    /// </summary>
    public event Action OnStatusChanged;

    public bool IsPoisoned => HasStatus(StatusType.Poison);
    public bool IsBurning => HasStatus(StatusType.Burn);
    public bool IsBleeding => HasStatus(StatusType.Bleed);
    public bool IsSlowed => HasStatus(StatusType.Slow) || HasStatus(StatusType.Cripple);
    public bool IsChilled => HasStatus(StatusType.Chill);
    public bool IsHungry => HasStatus(StatusType.Hunger);
    public bool IsFloating => HasStatus(StatusType.Float);
    public bool IsHasted => HasStatus(StatusType.Haste);
    public bool IsPurifiedStatus => HasStatus(StatusType.Purified);

    // 기존 코드 호환용 별칭입니다.
    public bool IsCrippled => IsSlowed;
    public bool IsBlind => HasStatus(StatusType.Blind);
    public bool IsWeak => HasStatus(StatusType.Weakness);
    public bool IsParalyzed => HasStatus(StatusType.Paralysis);

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    private void Update()
    {
        if (TimeSystem.Instance != null && !TimeSystem.Instance.IsRunning)
            return;

        TickStatuses(Time.deltaTime);
    }

    /// <summary>
    /// 상태이상을 적용합니다.
    /// 같은 상태가 이미 있으면 남은 시간과 위력 중 더 큰 값만 유지합니다.
    /// </summary>
    public void ApplyStatus(StatusRequest request)
    {
        if (request.DurationTurns <= 0 && request.Power <= 0) return;

        if (IsPurified(request.StatusType)) return;

        // 상태 로직은 이 컴포넌트가 담당하고, 색/아이콘 같은 표시는 PlayerStatusVisual이 담당합니다.
        // 몬스터와 보스는 StatusController가 동적으로 붙는 경우가 많으므로 여기서 비주얼도 같이 보장합니다.
        PlayerStatusVisual.EnsureFor(gameObject);

        float duration = Mathf.Max(0.1f, request.DurationTurns);
        int power = Mathf.Max(1, request.Power);
        float tickInterval = GetTickInterval(request.StatusType);

        for (int i = 0; i < _statuses.Count; i++)
        {
            ActiveStatus current = _statuses[i];
            if (current.Type != request.StatusType) continue;

            current.RemainingTime = Mathf.Max(current.RemainingTime, duration);
            current.Power = Mathf.Max(current.Power, power);
            current.TickInterval = Mathf.Max(0.05f, tickInterval);
            current.TickTimer = Mathf.Min(current.TickTimer, current.TickInterval);
            _statuses[i] = current;
            RaiseStatusChanged();
            return;
        }

        _statuses.Add(new ActiveStatus(request.StatusType, duration, power, tickInterval));
        RaiseStatusChanged();
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.StatusApplied);
    }

    /// <summary>
    /// 지정한 상태가 현재 활성화되어 있는지 확인합니다.
    /// 남은 시간이 0 이하인 상태는 없는 상태로 취급합니다.
    /// </summary>
    public bool HasStatus(StatusType type)
    {
        for (int i = 0; i < _statuses.Count; i++)
        {
            if (_statuses[i].Type == type && _statuses[i].RemainingTime > 0f)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 상태이상 비주얼/UI가 현재 상태 목록을 읽을 수 있게 복사본을 채워줍니다.
    /// </summary>
    public void GetActiveStatusTypes(List<StatusType> results)
    {
        if (results == null) return;

        results.Clear();
        for (int i = 0; i < _statuses.Count; i++)
        {
            if (_statuses[i].RemainingTime > 0f)
                results.Add(_statuses[i].Type);
        }
    }

    /// <summary>
    /// 이동 한 번에 걸리는 시간을 상태이상에 맞게 보정합니다.
    /// 둔화/냉기/굶주림은 느리게, 신속은 빠르게 만듭니다.
    /// </summary>
    public float ModifyMoveTimeCost(float baseCost)
    {
        float result = baseCost;
        if (IsSlowed) result *= slowMoveMultiplier;
        if (IsChilled) result *= chillMoveMultiplier;
        if (IsHungry) result *= hungerMoveMultiplier;
        if (IsParalyzed) result *= 2f;
        if (IsHasted) result *= hasteMoveMultiplier;
        return Mathf.Max(0.01f, result);
    }

    /// <summary>
    /// 공격 쿨타임 또는 공격 시간 비용을 상태이상에 맞게 보정합니다.
    /// 현재는 냉기만 공격을 느리게 만듭니다.
    /// </summary>
    public float ModifyAttackCooldown(float baseCooldown)
    {
        float result = baseCooldown;
        if (IsChilled) result *= chillAttackMultiplier;
        return Mathf.Max(0.01f, result);
    }

    /// <summary>
    /// 플레이어가 주는 피해량을 상태이상에 맞게 보정합니다.
    /// 기존 Weakness 호환용으로 절반 피해 처리를 남겨둡니다.
    /// </summary>
    public int ModifyOutgoingDamage(int baseDamage)
    {
        if (baseDamage <= 0) return 0;
        if (IsWeak) return Mathf.Max(1, Mathf.CeilToInt(baseDamage * 0.5f));
        return baseDamage;
    }

    /// <summary>
    /// 부유 상태가 Hazard 효과를 무시할 수 있는지 외부에서 확인하는 진입점입니다.
    /// Hazard 쪽에서 이 값을 보고 피해/상태 적용을 건너뛰면 됩니다.
    /// </summary>
    public bool ShouldIgnoreHazard()
    {
        return IsFloating;
    }

    /// <summary>
    /// 정화 포션처럼 특정 상태를 강제로 지워야 할 때 사용하는 제거 함수입니다.
    /// 출혈은 기획상 해제 불가라서 별도 허용값이 없으면 제거하지 않습니다.
    /// </summary>
    public bool TryRemoveStatus(StatusType type, bool allowUnremovable = false)
    {
        if (!allowUnremovable && IsUnremovable(type)) return false;

        for (int i = _statuses.Count - 1; i >= 0; i--)
        {
            if (_statuses[i].Type != type) continue;

            _statuses.RemoveAt(i);
            RaiseStatusChanged();
            return true;
        }

        return false;
    }

    private void TickStatuses(float deltaTime)
    {
        if (_statuses.Count == 0) return;

        bool changed = false;

        for (int i = _statuses.Count - 1; i >= 0; i--)
        {
            ActiveStatus status = _statuses[i];
            status.RemainingTime -= deltaTime;

            if (IsTickDamageStatus(status.Type))
            {
                status.TickTimer -= deltaTime;
                if (status.TickTimer <= 0f)
                {
                    ApplyTickEffect(status);
                    status.TickTimer += status.TickInterval;
                }
            }

            if (status.RemainingTime <= 0f)
            {
                _statuses.RemoveAt(i);
                changed = true;
            }
            else
            {
                _statuses[i] = status;
            }
        }

        if (changed)
            RaiseStatusChanged();
    }

    private void ApplyTickEffect(ActiveStatus status)
    {
        if (_damageable == null || _damageable.IsDead) return;

        switch (status.Type)
        {
            case StatusType.Poison:
            case StatusType.Burn:
            case StatusType.Bleed:
            case StatusType.Corrosion:
            case StatusType.Ooze:
                _damageable.TakeDamage(Mathf.Max(1, status.Power));
                break;
        }
    }

    private bool IsTickDamageStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Poison:
            case StatusType.Burn:
            case StatusType.Bleed:
            case StatusType.Corrosion:
            case StatusType.Ooze:
                return true;
            default:
                return false;
        }
    }

    private bool IsUnremovable(StatusType type)
    {
        return type == StatusType.Bleed;
    }

    // 정화 상태가 활성화된 동안 Poison·Slow·Cripple은 새로 걸리지 않는다.
    private bool IsPurified(StatusType incoming)
    {
        if (!IsPurifiedStatus)
            return false;

        return incoming == StatusType.Poison
            || incoming == StatusType.Slow
            || incoming == StatusType.Cripple;
    }

    private float GetTickInterval(StatusType type)
    {
        return IsTickDamageStatus(type) ? defaultDamageTickInterval : 0f;
    }

    private void RaiseStatusChanged()
    {
        OnStatusChanged?.Invoke();
    }

    private struct ActiveStatus
    {
        public StatusType Type;
        public float RemainingTime;
        public float TickTimer;
        public float TickInterval;
        public int Power;

        public ActiveStatus(StatusType type, float remainingTime, int power, float tickInterval)
        {
            Type = type;
            RemainingTime = remainingTime;
            TickInterval = Mathf.Max(0.05f, tickInterval);
            TickTimer = TickInterval;
            Power = power;
        }
    }
}
