using System;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨과 경험치 통을 관리한다.
/// 실제 능력치 값은 NYHPlayerStats가 담당하고, 이 클래스는 경험치 누적/레벨업/레벨업 보너스 적용만 담당한다.
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    public static PlayerLevelSystem Instance { get; private set; }

    [Header("레벨 / 경험치")]
    [Tooltip("현재 플레이어 레벨입니다. 실제 스탯 값은 NYHPlayerStats가 담당하고, 이 값은 성장 단계만 나타냅니다.")]
    [SerializeField, Min(1)] private int level = 1;

    [Tooltip("최대 레벨입니다. 이 레벨에 도달하면 경험치를 얻어도 더 이상 누적/레벨업하지 않고 UI에는 MAX로 표시합니다.")]
    [SerializeField, Min(1)] private int maxLevel = 10;

    [Tooltip("현재 레벨에서 누적된 경험치입니다. 최대 레벨에서는 더 이상 의미가 없어서 0으로 유지됩니다.")]
    [SerializeField, Min(0)] private int currentExp;

    [Tooltip("1레벨에서 2레벨로 오르는 데 필요한 기본 경험치입니다.")]
    [SerializeField, Min(1)] private int baseRequiredExp = 20;

    [Tooltip("레벨이 오를수록 다음 레벨 요구 경험치가 증가하는 배율입니다. 예: 1.25면 레벨마다 약 25% 증가합니다.")]
    [SerializeField, Min(1f)] private float requiredExpGrowth = 1.25f;

    [Tooltip("GDD 기준 레벨업 필요 경험치입니다. index 0은 1->2레벨, index 1은 2->3레벨 요구량입니다.")]
    [SerializeField] private int[] requiredExpByLevel =
    {
        30,
        40,
        50,
        60,
        70,
        50,
        50,
        50,
        100
    };

    [Header("레벨업 보너스")]
    [Tooltip("레벨업할 때 증가하는 최대 체력입니다. 실제 적용은 NYHPlayerStats.AddStat(Health)로 전달됩니다.")]
    [SerializeField, Min(0)] private int healthPerLevel = 5;

    [Tooltip("레벨업할 때 증가하는 이동 속도입니다. GDD의 레벨별 이동 쿨타임 감소에 사용됩니다.")]
    [SerializeField, Min(0)] private int moveSpeedPerLevel = 1;

    /// <summary>
    /// 레벨이 하나 이상 올랐을 때 호출된다.
    /// UI 갱신, 레벨업 연출, 선택지 표시 등에 사용한다.
    /// </summary>
    public event Action OnLevelChanged;

    /// <summary>
    /// 경험치가 추가됐을 때 호출된다.
    /// amount는 이번에 들어온 경험치량이며, 현재 경험치 총량은 CurrentExp로 읽는다.
    /// </summary>
    public event Action<int> OnExpChanged;

    public int Level => level;
    // 경험치 저장
    public int CurrentExp => currentExp;
    public int RequiredExp => IsMaxLevel ? 0 : CalculateRequiredExp(level);
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => level >= maxLevel;
    public string ExpDisplayText => IsMaxLevel ? "MAX" : $"{currentExp} / {RequiredExp}";

    /// <summary>
    /// 씬에서 전역 접근할 레벨 시스템 인스턴스를 등록한다.
    /// GridPlayer의 RequireComponent로 플레이어에 붙도록 보장한다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PlayerLevelSystem: 씬에 레벨 시스템이 둘 이상 있습니다. 먼저 등록된 값을 사용합니다.");
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 적 사망 보상 이벤트를 구독한다.
    /// EnemyBase가 GameEvents.OnExpDropped를 발행하면 자동으로 경험치를 받는다.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnExpDropped += OnExpDropped;
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독을 해제해 중복 수신을 막는다.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnExpDropped -= OnExpDropped;
    }

    /// <summary>
    /// 현재 인스턴스가 파괴되면 싱글톤 참조를 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 경험치를 추가한다.
    /// 요구 경험치를 초과하면 여러 번 연속 레벨업할 수 있으며, 남은 경험치는 다음 레벨에 이월된다.
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        if (IsMaxLevel)
        {
            // 최대 레벨 이후 경험치를 계속 쌓아두면 나중에 maxLevel을 올렸을 때 즉시 여러 번 레벨업하는 문제가 생긴다.
            // GDD 기준 현재 성장 상한은 10레벨이므로, 상한에 도달하면 경험치 통은 MAX 표시용 상태로만 유지한다.
            currentExp = 0;
            OnExpChanged?.Invoke(amount);
            return;
        }

        currentExp += amount;
        bool leveledUp = false;

        while (!IsMaxLevel && currentExp >= RequiredExp)
        {
            currentExp -= RequiredExp;
            LevelUp();
            leveledUp = true;
        }

        if (IsMaxLevel)
            currentExp = 0;

        OnExpChanged?.Invoke(amount);
        if (leveledUp)
            OnLevelChanged?.Invoke();
    }

    /// <summary>
    /// 세이브 로드 시 레벨과 누적 경험치를 절대값으로 복원한다.
    /// 레벨업 보너스(스탯 증가)는 다시 적용하지 않는다. 스탯은 NYHPlayerStats.SetStats가 별도로 복원하기 때문이다.
    /// </summary>
    public void SetLevelExp(int newLevel, int newExp)
    {
        level = Mathf.Clamp(newLevel, 1, maxLevel);
        currentExp = IsMaxLevel ? 0 : Mathf.Max(0, newExp);

        OnLevelChanged?.Invoke();
        OnExpChanged?.Invoke(0);
    }

    /// <summary>
    /// 레벨을 1 올리고 레벨업 보너스를 NYHPlayerStats에 적용한다.
    /// 이 클래스는 성장 단계만 관리하고, 실제 능력치 저장은 NYHPlayerStats가 담당한다.
    /// </summary>
    private void LevelUp()
    {
        if (IsMaxLevel)
            return;

        level++;
        if (level > maxLevel)
            level = maxLevel;

        NYHPlayerStats stats = NYHPlayerStats.Instance;
        GrantTraitPointForLevel(level);

        if (stats == null) return;

        if (healthPerLevel > 0)
            stats.AddStat(NYHStatType.Health, healthPerLevel);

        if (moveSpeedPerLevel > 0)
            stats.AddStat(NYHStatType.MoveSpeed, moveSpeedPerLevel);
    }

    private void GrantTraitPointForLevel(int reachedLevel)
    {
        // GDD 기준 특성 포인트 지급 구간을 레벨업 시점에만 처리한다.
        // 특성 포인트는 런타임 성장 보상이므로 경험치 UI나 스탯 저장 로직에 섞지 않는다.
        TraitTier? tier = null;

        if (reachedLevel >= 2 && reachedLevel <= 4)
            tier = TraitTier.Tier1;
        else if (reachedLevel >= 5 && reachedLevel <= 7)
            tier = TraitTier.Tier2;
        else if (reachedLevel >= 8 && reachedLevel <= 10)
            tier = TraitTier.Tier3;

        if (tier.HasValue)
            TraitController.FindAvailableInstance()?.AddPoint(tier.Value, 1);
    }

    /// <summary>
    /// targetLevel에서 다음 레벨로 오르는 데 필요한 경험치를 계산한다.
    /// 공식: baseRequiredExp * requiredExpGrowth^(level - 1)
    /// </summary>
    private int CalculateRequiredExp(int targetLevel)
    {
        // 최대 레벨에서는 다음 레벨 요구 경험치가 없으므로 외부에는 RequiredExp 0과 ExpDisplayText MAX를 제공한다.
        if (targetLevel >= maxLevel)
            return 0;

        int index = Mathf.Max(0, targetLevel - 1);
        if (requiredExpByLevel != null && index < requiredExpByLevel.Length)
            return Mathf.Max(1, requiredExpByLevel[index]);

        return Mathf.Max(1, Mathf.RoundToInt(baseRequiredExp * Mathf.Pow(requiredExpGrowth, targetLevel - 1)));
    }

    /// <summary>
    /// 적 사망 보상 이벤트 콜백.
    /// position은 드랍 위치용 정보라 현재 경험치 계산에는 사용하지 않는다.
    /// </summary>
    private void OnExpDropped(Vector2Int position, int exp)
    {
        AddExp(exp);
    }

    /// <summary>
    /// 인스펙터에서 잘못된 값이 들어가지 않도록 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxLevel = Mathf.Max(1, maxLevel);
        level = Mathf.Min(level, maxLevel);
        currentExp = Mathf.Max(0, currentExp);
        if (level >= maxLevel)
            currentExp = 0;
        baseRequiredExp = Mathf.Max(1, baseRequiredExp);
        requiredExpGrowth = Mathf.Max(1f, requiredExpGrowth);
        healthPerLevel = Mathf.Max(0, healthPerLevel);
        moveSpeedPerLevel = Mathf.Max(0, moveSpeedPerLevel);

        if (requiredExpByLevel == null) return;
        for (int i = 0; i < requiredExpByLevel.Length; i++)
            requiredExpByLevel[i] = Mathf.Max(1, requiredExpByLevel[i]);
    }
}
