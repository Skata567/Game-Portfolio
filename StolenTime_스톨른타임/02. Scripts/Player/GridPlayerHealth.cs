using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 체력 증감, 사망 처리, 그리고 전용 회복 아이템(포션) 사용 로직을 전담하는 클래스입니다.
/// 스탯 시스템(NYHPlayerStats)과 연동하여 방어력을 계산한 최종 피해를 적용하며,
/// 포션 사용 시 즉시 회복과 함께 AnimationCurve를 활용한 지속적인(Regen) 회복 로직을 동시에 실행합니다.
/// </summary>
public class GridPlayerHealth : MonoBehaviour, IDamageable
{
    public static GridPlayerHealth Instance { get; private set; }

    [Header("기본 체력")]
    [Tooltip("NYHPlayerStats가 없을 때 사용할 기본 최대 체력입니다. Stats 컴포넌트가 있으면 MaxHealth 값을 우선 사용합니다.")]
    [SerializeField] private int maxHp = 100;

    [Tooltip("게임 시작 시 플레이어가 들고 시작할 포션 개수입니다.")]
    [SerializeField] private int startPotionCount = 1;

    [Header("Debug Damage")]
    [Tooltip("에디터에서 지정 키를 눌러 플레이어에게 피해를 주는 테스트 기능을 사용할지 결정합니다.")]
    [SerializeField] private bool enableDebugDamageKey = true;

    [Tooltip("디버그 피해를 줄 키입니다. enableDebugDamageKey가 켜져 있을 때만 동작합니다.")]
    [SerializeField] private KeyCode debugDamageKey = KeyCode.H;

    [Tooltip("디버그 피해 키를 눌렀을 때 적용할 원본 피해량입니다. 방어/회피 계산은 그대로 거칩니다.")]
    [SerializeField, Min(1)] private int debugDamageAmount = 10;

    [Header("포션 회복 곡선")]
    [Tooltip("X: 0~1 진행률 (0=사용 직후, 1=25초 후), Y: 0~1 누적 회복량 비율")]
    [SerializeField] private AnimationCurve potionRegenCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.12f, 0.6f),   // ~3초까지 60% 누적
        new Keyframe(0.4f, 0.85f),   // ~10초까지 85%
        new Keyframe(1f, 1f));        // 25초에 100%
    [Tooltip("포션의 지속 회복이 끝날 때까지 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float potionDuration = 25f;

    private GridPlayer _player;
    private NYHPlayerStats _stats;
    private NYHEquipmentController _equipmentController;
    private Coroutine _regenCoroutine;

    public event Action OnHealthChanged;
    public event Action<int> OnDamaged;

    /// <summary>
    /// 플레이어 체력이 0이 되어 사망이 확정됐을 때 호출된다.
    /// 사용 예: GridPlayerAnimationController가 구독해서 사망 애니메이션을 재생한다.
    /// </summary>
    public event Action OnDied;

    /// <summary>현재 남아있는 플레이어의 실제 체력 수치입니다.</summary>
    public int CurrentHp { get; private set; }
    
    /// <summary>현재 적용된 최대 체력 수치입니다. 플레이어 스탯이 존재하면 동기화되고 없으면 인스펙터 기본값을 사용합니다.</summary>
    public int MaxHp => _stats != null ? _stats.MaxHealth : maxHp;
    
    /// <summary>플레이어 사망 여부를 반환합니다. (CurrentHp가 0 이하일 때 true)</summary>
    public bool IsDead => CurrentHp <= 0;
    
    /// <summary>현재 소지 중인 포션의 개수입니다.</summary>
    public int PotionCount { get; private set; }

#if UNITY_EDITOR
    private void Update()
    {
        if (!enableDebugDamageKey || IsDead) return;

        if (Input.GetKeyDown(debugDamageKey))
            TakeDamage(debugDamageAmount);
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GridPlayerHealth: 씬에 체력 컴포넌트가 둘 이상 있습니다. 현재 씬의 Player 구성을 확인하세요.");
            return;
        }

        // 기존 외부 코드 호환을 위해 Instance는 남기되, DontDestroyOnLoad는 사용하지 않는다.
        Instance = this;
    }

    /// <summary>
    /// GridPlayer에서 호출하는 초기화 진입점입니다.
    /// 스탯 컴포넌트와 연결하고 현재 체력/포션 수를 시작 값으로 세팅합니다.
    /// </summary>
    public void Init(GridPlayer player)
    {
        _player = player;
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null)
            _stats = NYHPlayerStats.Instance;
        if (_stats != null)
            _stats.OnStatAdded += OnStatAdded;
        _equipmentController = GetComponent<NYHEquipmentController>();

        CurrentHp = MaxHp;
        PotionCount = startPotionCount;
        RaiseHealthChanged();
    }

    /// <summary>
    /// 외부 요인(몬스터, 함정 등)에 의해 플레이어가 피해를 입었을 때 호출됩니다.
    /// 스탯 시스템(NYHPlayerStats)이 존재할 경우 방어력을 계산한 '최종 데미지'만을 적용합니다.
    /// 회피는 공격자의 정확도와 함께 판단해야 하므로 몬스터 공격 쪽에서 먼저 처리합니다.
    /// 체력이 0 이하로 떨어지면 사망 처리를 수행합니다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        TakeDamageWithCombatText(amount, amount);
    }

    public void TakeDamageWithCombatText(int amount, int criticalReferenceMaxDamage)
    {
        if (IsDead || amount <= 0) return;

        // 최종 데미지 산출 로직 위임 (방어, 회피 반영)
        int finalDamage = CalculateIncomingDamage(amount);
        if (finalDamage <= 0)
        {
            PublishCombatText(0, CombatTextType.Damage);
            return;
        }

        CurrentHp = Mathf.Max(0, CurrentHp - finalDamage);

        if (finalDamage > 0)
            GetArmorMagic()?.OnHit();

        OnDamaged?.Invoke(finalDamage);
        GameEvents.OnPlayerDamaged?.Invoke(finalDamage);
        PublishCombatText(finalDamage, GetCombatTextType(finalDamage, criticalReferenceMaxDamage));
        RaiseHealthChanged();

        if (CurrentHp <= 0)
        {
            OnDied?.Invoke();
            GameEvents.OnPlayerDied?.Invoke();
            TimeSystem.Instance.StopTimer();
        }
    }

    /// <summary>
    /// 플레이어 HP를 회복합니다. 사망 상태에서는 회복하지 않습니다.
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        int beforeHp = CurrentHp;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        int healedAmount = Mathf.Max(0, CurrentHp - beforeHp);
        if (healedAmount > 0)
            PublishHealCombatText(healedAmount);
        RaiseHealthChanged();
    }

    /// <summary>
    /// 세이브 로드 시 현재 체력을 절대값으로 복원합니다.
    /// 최대 체력 기준으로 클램프하므로 스탯(MaxHealth) 복원 이후에 호출해야 합니다.
    /// </summary>
    public void SetCurrentHp(int hp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
        RaiseHealthChanged();
    }

    /// <summary>
    /// 세이브 로드 시 보유 포션 수를 절대값으로 복원합니다.
    /// </summary>
    public void SetPotionCount(int count)
    {
        PotionCount = Mathf.Max(0, count);
        RaiseHealthChanged();
    }

    /// <summary>
    /// 포션 개수를 추가하고 HP UI가 포션 수를 다시 읽을 수 있도록 변경 이벤트를 보냅니다.
    /// </summary>
    public void AddPotion(int count = 1)
    {
        PotionCount += count;
        RaiseHealthChanged();
    }

    /// <summary>
    /// 포션을 1개 소비해서 즉시 회복과 지속 회복을 시작합니다.
    /// 포션 사용도 플레이어 행동이므로 TurnManager에 행동 비용을 알립니다.
    /// </summary>
    public void TryUsePotion()
    {
        if (PotionCount <= 0 || IsDead) return;
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct) return;

        PotionCount--;
        RaiseHealthChanged();
        GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.PotionDrink);

        // 즉시 +50% MaxHp
        Heal(Mathf.RoundToInt(MaxHp * 0.5f));

        // 진행 중인 회복 코루틴 취소 후 새로 시작
        if (_regenCoroutine != null) StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenOverTime());

        float actionDelay = _player.Config.potionTimeDealy;
        _player.ActionDelay?.StartDelay(actionDelay);
        if (_player.Movement != null && _player.Movement.IgnoresActionDelayForMovement)
            TurnManager.Instance.OnPlayerActionCompletedWithoutEnemyTurns(actionDelay);
        else
            TurnManager.Instance.OnPlayerActionCompleted(actionDelay);
    }

    private IEnumerator RegenOverTime()
    {
        float elapsed = 0f;
        float pool = MaxHp * 0.5f;  // 시간에 걸쳐 회복할 총량 = 50% MaxHp
        float deliveredFraction = 0f;

        while (elapsed < potionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / potionDuration);
            float currentFraction = Mathf.Clamp01(potionRegenCurve.Evaluate(progress));

            float deltaFraction = currentFraction - deliveredFraction;
            if (deltaFraction > 0f)
            {
                int delta = Mathf.RoundToInt(pool * deltaFraction);
                if (delta > 0)
                {
                    Heal(delta);
                    deliveredFraction += (float)delta / pool;
                }
            }

            if (IsDead) yield break;
            yield return null;
        }

        _regenCoroutine = null;
    }

    private void OnDestroy()
    {
        // 플레이어 체력은 현재 씬의 Player 컴포넌트가 소유하므로 전역 싱글톤으로 남기지 않는다.
        // 이벤트 구독만 정리해서 씬 전환/비활성화 후 파괴된 Player 참조가 남는 것을 막는다.
        if (Instance == this)
            Instance = null;

        if (_stats != null)
            _stats.OnStatAdded -= OnStatAdded;
    }

    /// <summary>
    /// 최대 체력 스탯이 증가했을 때 현재 체력도 같은 양만큼 보정합니다.
    /// 최대 체력만 늘고 현재 체력이 그대로 남는 어색함을 막기 위한 연결 지점입니다.
    /// </summary>
    private void OnStatAdded(NYHStatType type, int amount)
    {
        if (type == NYHStatType.Health && amount > 0)
            Heal(amount);

        RaiseHealthChanged();
    }

    // 장착 갑옷의 MagicData를 가져옴
    private MagicData GetArmorMagic()
    {
        if (_equipmentController == null)
            return null;
        if (!_equipmentController.TryGetEquipped(EquipmentSlot.Armor, out NYHEquipmentItemInstance item))
            return null;
        return item.SourceInstance is EquipmentInstance inst ? inst.magic : null;
    }

    /// <summary>
    /// 원본 피해량을 실제 HP 차감량으로 바꿉니다.
    /// 스탯 컴포넌트가 없으면 방어/회피 없이 원본 피해를 그대로 반환합니다.
    /// </summary>
    private int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;
        int armorDefense = NYHPlayerDefenseCalculator.RollArmorDefense(_equipmentController);
        int flatDefense = _stats != null ? _stats.Defense : 0;
        return Mathf.Max(0, rawDamage - armorDefense - flatDefense);
    }

    private CombatTextType GetCombatTextType(int finalDamage, int criticalReferenceMaxDamage)
    {
        if (finalDamage <= 0 || criticalReferenceMaxDamage <= 0)
            return CombatTextType.Damage;

        return finalDamage >= Mathf.CeilToInt(criticalReferenceMaxDamage * 0.8f)
            ? CombatTextType.CriticalDamage
            : CombatTextType.Damage;
    }

    private void PublishCombatText(int finalDamage, CombatTextType type)
    {
        // TODO(UI): 플레이어 피격 숫자는 CombatFloatingTextSpawner의 spawnOffset으로 머리 위 위치를 조정합니다.
        Transform target = _player != null ? _player.transform : transform;
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(target.position, finalDamage.ToString(), finalDamage, type));
    }

    private void PublishHealCombatText(int healedAmount)
    {
        // TODO(UI): 회복 숫자도 같은 CombatFloatingTextSpawner 설정을 사용합니다. 색상은 healColor에서 바꾸면 됩니다.
        Transform target = _player != null ? _player.transform : transform;
        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(target.position, healedAmount.ToString(), healedAmount, CombatTextType.Heal));
    }

    /// <summary>
    /// HP나 포션 수가 바뀐 뒤 UI/피드백 스크립트에게 변경 사실을 알립니다.
    /// </summary>
    private void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }
}
