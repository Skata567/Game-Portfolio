using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 판 동안의 특성 레벨, 티어별 특성 포인트, 특성 효과 조회를 담당하는 런타임 컨트롤러입니다.
/// GDD 기준 특성은 저장/로드 대상이 아니므로 PlayerPrefs를 쓰지 않고 씬 생명주기 안에서만 유지합니다.
/// </summary>
public class TraitController : MonoBehaviour
{
    public static TraitController Instance { get; private set; }

    // GDD에서 각 특성은 2레벨까지만 성장하므로 상수로 고정한다.
    // 나중에 3레벨 특성이 생기면 UI 별 표시 정책도 같이 바뀌어야 하므로 여기만 몰래 바꾸면 안 된다.
    private const int MaxTraitLevel = 2;

    [Header("특성 데이터")]
    [Tooltip("GDD 기준 9개 특성 데이터입니다. 비워두면 id/tier가 자동 생성되고, 아이콘은 UI 버튼 이미지로 대체됩니다. 이름/설명은 LocalizationManager가 담당합니다.")]
    [SerializeField] private TraitData[] traitData;

    // 런타임 상태는 Dictionary로 관리한다.
    // enum 전체를 초기화해두면 아직 찍지 않은 특성도 항상 0레벨로 안전하게 조회할 수 있다.
    private readonly Dictionary<TraitId, int> _levels = new();
    private readonly Dictionary<TraitTier, int> _pointsByTier = new();
    private readonly Dictionary<TraitId, TraitData> _dataById = new();

    // UI, 전투, 골드, 시야처럼 서로 다른 시스템이 특성 변경을 느슨하게 받을 수 있게 이벤트로 알린다.
    // 직접 UI를 찾아 갱신하면 특성 시스템이 화면 구조에 강하게 묶이므로 피한다.
    public event Action OnTraitChanged;
    public event Action<TraitId, int> OnTraitLevelChanged;
    public event Action<TraitTier, int> OnTraitPointChanged;

    public IReadOnlyList<TraitData> Traits => traitData;

    // 특성 이름+레벨별 효과 설명은 LocalizationManager의 "플레이어 특성" 항목에서 한 줄로 합쳐 관리한다.
    private static readonly Dictionary<TraitId, string> LocalizationKeysById = new()
    {
        { TraitId.Appraisal, "msg_감정" },
        { TraitId.Healthy_Body, "msg_건강한몸" },
        { TraitId.Survivalist, "msg_비상식량" },
        { TraitId.Physical, "msg_운동" },
        { TraitId.Greed, "msg_탐욕" },
        { TraitId.Lightfoot, "msg_조심스러운발걸음" },
        { TraitId.Trinket, "msg_액세서리중독" },
        { TraitId.HeavyBlow, "msg_강타" },
        { TraitId.EagleEye, "msg_넓은시야" }
    };

    // 특성 이름과 레벨별 효과가 합쳐진 표시용 텍스트를 반환한다 (1번째 줄: 이름, 이후 줄: 설명).
    public string GetTraitDisplayText(TraitId id)
    {
        if (LocalizationManager.Instance == null || !LocalizationKeysById.TryGetValue(id, out string key))
            return string.Empty;

        return LocalizationManager.Instance.Get(key);
    }

    /// <summary>
    /// 씬 안에서 사용할 특성 컨트롤러를 찾습니다.
    /// UI 패널이 비활성화되어 있어도 레벨업 보상 지급 시 컨트롤러를 찾을 수 있어야 해서 별도 helper로 분리했습니다.
    /// </summary>
    public static TraitController FindAvailableInstance()
    {
        if (Instance != null)
            return Instance;

        // 특성 UI 패널이 시작 시 비활성화되어 있으면 Awake가 아직 실행되지 않아 Instance가 비어 있을 수 있다.
        // 레벨업 보상 포인트가 사라지지 않도록 비활성 오브젝트까지 포함해서 한 번 찾아둔다.
        Instance = UnityEngine.Object.FindFirstObjectByType<TraitController>(FindObjectsInactive.Include);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TraitController: 씬에 특성 컨트롤러가 둘 이상 있습니다. 먼저 등록된 인스턴스를 사용합니다.");
            return;
        }

        Instance = this;

        // 기본 데이터 생성 -> ID 조회 테이블 생성 -> 런타임 상태 초기화 순서를 지킨다.
        // 이 순서가 바뀌면 UI가 켜지는 첫 프레임에 데이터 null 또는 포인트 누락이 생길 수 있다.
        EnsureTraitData();
        RebuildDataLookup();
        InitializeRuntimeState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public TraitData GetTraitData(TraitId id)
    {
        // Inspector에서 배열을 수정한 뒤 도메인 리로드 없이 호출될 수 있어 조회 직전에 테이블을 갱신한다.
        // 특성 수가 9개라 비용이 작고, 잘못된 캐시로 UI가 틀어지는 위험을 줄인다.
        EnsureTraitData();
        RebuildDataLookup();
        return _dataById.TryGetValue(id, out TraitData data) ? data : null;
    }

    public int GetTraitLevel(TraitId id)
    {
        return _levels.TryGetValue(id, out int level) ? level : 0;
    }

    public int GetPoint(TraitTier tier)
    {
        return _pointsByTier.TryGetValue(tier, out int point) ? point : 0;
    }

    public void AddPoint(TraitTier tier, int amount)
    {
        if (amount <= 0)
            return;

        // 특성 포인트는 GDD상 런마다 얻는 성장 자원이므로 PlayerPrefs에 저장하지 않는다.
        // 영구 저장을 넣으면 다음 판에 포인트가 남는 설계 오류가 생길 수 있다.
        _pointsByTier[tier] = GetPoint(tier) + amount;
        OnTraitPointChanged?.Invoke(tier, _pointsByTier[tier]);
        OnTraitChanged?.Invoke();
    }

    public bool TryUpgradeTrait(TraitId id)
    {
        // UI는 성공/실패 이유를 직접 판단하지 않고 이 메서드의 bool만 받는다.
        // 포인트 부족, 최대 레벨, 잘못된 ID 같은 방어 로직을 한곳에 모아 중복 버그를 줄인다.
        TraitData data = GetTraitData(id);
        if (data == null)
            return false;

        int currentLevel = GetTraitLevel(id);
        if (currentLevel >= MaxTraitLevel)
            return false;

        TraitTier tier = data.tier;
        int point = GetPoint(tier);
        if (point <= 0)
            return false;

        // 포인트 차감과 레벨 증가를 한 메서드에서 처리해 UI 버튼 연타로 상태가 어긋나는 일을 막는다.
        _pointsByTier[tier] = point - 1;
        int nextLevel = currentLevel + 1;
        _levels[id] = nextLevel;

        ApplyImmediateTraitEffect(id, currentLevel, nextLevel);

        OnTraitPointChanged?.Invoke(tier, _pointsByTier[tier]);
        OnTraitLevelChanged?.Invoke(id, nextLevel);
        OnTraitChanged?.Invoke();
        return true;
    }

    public float GetGoldMultiplier()
    {
        // 골드 시스템은 특성 레벨만 물어보면 되도록 배율 계산을 이쪽에 둔다.
        // 그래야 Wallet이 어떤 특성이 골드를 바꾸는지 알 필요가 없다.
        return GetTraitLevel(TraitId.Greed) switch
        {
            1 => 1.25f,
            2 => 1.5f,
            _ => 1f
        };
    }

    public int GetVisionBonus()
    {
        // 시야 반경은 여러 버프가 겹칠 수 있으므로 최종 반경 계산은 VisionSystem이 하고,
        // 특성은 자신이 더하는 보너스 값만 제공한다.
        return GetTraitLevel(TraitId.EagleEye);
    }

    public void GetBaseAttackDamageBonusMinMax(out int minBonus, out int maxBonus)
    {
        // HeavyBlow 2레벨은 +1~2처럼 범위 보너스라 min/max를 같이 제공한다.
        // 공격 계산기가 랜덤 굴림 범위를 만들 때 이 값을 그대로 더한다.
        switch (GetTraitLevel(TraitId.HeavyBlow))
        {
            case 1:
                minBonus = 1;
                maxBonus = 1;
                return;
            case 2:
                minBonus = 1;
                maxBonus = 2;
                return;
            default:
                minBonus = 0;
                maxBonus = 0;
                return;
        }
    }

    public float GetIdentifyWeaponSpeedMultiplier()
    {
        // Appraisal이 연결될 감정 시스템은 아직 완성 전이라 실제 처리 대신 조회용 hook만 먼저 둔다.
        return GetTraitLevel(TraitId.Appraisal) >= 1 ? 2f : 1f;
    }

    public float GetIdentifyArmorSpeedMultiplier()
    {
        return GetTraitLevel(TraitId.Appraisal) >= 1 ? 1.5f : 1f;
    }

    public bool ShouldInstantlyIdentifyWeapon()
    {
        return GetTraitLevel(TraitId.Appraisal) >= 2;
    }

    public float GetHealthyBodyRecoveryMultiplier()
    {
        return GetTraitLevel(TraitId.Healthy_Body) switch
        {
            1 => 1.25f,
            2 => 1.5f,
            _ => 1f
        };
    }

    public float GetCarefulStepSearchMultiplier()
    {
        return GetTraitLevel(TraitId.Lightfoot) switch
        {
            1 => 1.5f,
            2 => 2f,
            _ => 1f
        };
    }

    public float GetAccessoryEffectMultiplier()
    {
        return GetTraitLevel(TraitId.Trinket) switch
        {
            1 => 1.25f,
            2 => 1.5f,
            _ => 1f
        };
    }

    public int GetEmergencyFoodFloorCount()
    {
        // 층 스폰 예약 시스템이 생기면 이 값으로 몇 개 층에 비상식량을 예약할지 판단한다.
        return GetTraitLevel(TraitId.Survivalist) switch
        {
            1 => 2,
            2 => 3,
            _ => 0
        };
    }

    private void ApplyImmediateTraitEffect(TraitId id, int previousLevel, int nextLevel)
    {
        if (id != TraitId.Physical)
            return;

        // Physical 특성은 GDD상 힘 +1/+2라서 레벨이 오르는 순간 증가분만 반영합니다.
        // 런타임 비보존 특성이므로 별도 저장/복원 보정은 하지 않습니다.
        int delta = Mathf.Max(0, nextLevel - previousLevel);
        if (delta > 0 && NYHPlayerStats.Instance != null)
            NYHPlayerStats.Instance.AddStat(NYHStatType.Strength, delta);
    }

    private void EnsureTraitData()
    {
        if (traitData != null && traitData.Length > 0)
            return;

        // Inspector 데이터가 아직 안 들어간 상태에서도 UI/효과 테스트가 가능하도록 기본 GDD 데이터를 만든다.
        // 아이콘은 씬에 있는 버튼 이미지에서 fallback으로 가져가게 해서 데이터 입력 부담을 줄인다.
        traitData = CreateDefaultTraitData();
    }

    private void RebuildDataLookup()
    {
        _dataById.Clear();
        if (traitData == null)
            return;

        for (int i = 0; i < traitData.Length; i++)
        {
            TraitData data = traitData[i];
            if (data == null)
                continue;

            _dataById[data.id] = data;
        }
    }

    private void InitializeRuntimeState()
    {
        foreach (TraitId id in Enum.GetValues(typeof(TraitId)))
        {
            if (!_levels.ContainsKey(id))
                _levels[id] = 0;
        }

        foreach (TraitTier tier in Enum.GetValues(typeof(TraitTier)))
        {
            if (!_pointsByTier.ContainsKey(tier))
                _pointsByTier[tier] = 0;
        }
    }

    private static TraitData[] CreateDefaultTraitData()
    {
        return new[]
        {
            Create(TraitId.Appraisal, TraitTier.Tier1),
            Create(TraitId.Healthy_Body, TraitTier.Tier1),
            Create(TraitId.Survivalist, TraitTier.Tier1),
            Create(TraitId.Physical, TraitTier.Tier2),
            Create(TraitId.Greed, TraitTier.Tier2),
            Create(TraitId.Lightfoot, TraitTier.Tier2),
            Create(TraitId.Trinket, TraitTier.Tier3),
            Create(TraitId.HeavyBlow, TraitTier.Tier3),
            Create(TraitId.EagleEye, TraitTier.Tier3)
        };
    }

    private static TraitData Create(TraitId id, TraitTier tier)
    {
        return new TraitData
        {
            id = id,
            tier = tier
        };
    }
}
