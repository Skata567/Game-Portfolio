using UnityEngine;

public enum TrapType
{
    Spike,
    Alarm,
    WornDart,
    PoisonDart,
    Disintegration,
    Gateway,
    Chilling,
    Frost,
    Burning,
    Blazing,
    Shocking,
    Storm,
    Guardian,
    Gripping,
    Flashing,
    Teleportation,
    Warping,
    Ooze,
    ConfusionGas,
    ToxicGas,
    CorrosiveGas,
    Flock,
    Summoning,
    Weakening,
    Cursing,
    Geyser,
    Explosive,
    Rockfall,
    Pitfall,
    Distortion,
    Disarming,
    Grim
}

public enum TrapDamageMode
{
    FlatRange,
    CurrentHpRatio,
    MaxHpMinimum,
    Grim
}

[CreateAssetMenu(fileName = "TrapData", menuName = "60s Dungeon/Trap Data")]
public class TrapData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("트랩을 구분하기 위한 내부 ID입니다. 같은 ID가 중복되지 않게 관리합니다.")]
    public string id;

    [Tooltip("인스펙터나 디버그 UI에서 보여줄 트랩 이름입니다.")]
    public string displayName;

    [Tooltip("트랩의 큰 분류입니다. 실제 효과는 트랩 오브젝트에 붙은 ITrapEffect 컴포넌트가 실행합니다.")]
    public TrapType trapType;

    [Header("등장/공개")]
    [Tooltip("이 트랩이 등장할 수 있는 최소 층입니다.")]
    [Min(1)] public int floorMin = 1;

    [Tooltip("이 트랩이 등장할 수 있는 최대 층입니다.")]
    [Min(1)] public int floorMax = 24;

    [Tooltip("활성화 전까지 숨겨진 스프라이트로 표시할지 정합니다.")]
    public bool startsHidden;

    [Tooltip("숨김 여부와 관계없이 항상 보이는 트랩인지 정합니다.")]
    public bool isAlwaysVisible;

    [Tooltip("숨겨진 트랩이 주변 탐색이나 공개 효과로 드러날 확률입니다.")]
    [Range(0f, 1f)] public float revealChance = 0.3f;

    [Header("발동")]
    [Tooltip("켜면 한 번 발동한 뒤에도 다시 밟을 때마다 효과가 발동합니다.")]
    public bool repeatable;

    [Tooltip("켜면 반복 불가능한 트랩이 발동 후 비활성화됩니다.")]
    public bool destroyAfterTrigger = true;

    [Header("공통 수치")]
    [Tooltip("데미지 계산 방식입니다. 고정 범위, 현재 체력 비율, 최소 피해 보장, 즉사형 중에서 선택합니다.")]
    public TrapDamageMode damageMode = TrapDamageMode.FlatRange;

    [Tooltip("FlatRange 방식에서 사용할 최소 피해량입니다.")]
    public int damageMin = 10;

    [Tooltip("FlatRange 방식에서 사용할 최대 피해량입니다.")]
    public int damageMax = 10;

    [Tooltip("CurrentHpRatio 또는 MaxHpMinimum 방식에서 현재 체력에 곱할 최소 비율입니다.")]
    [Range(0f, 2f)] public float currentHpRatioMin = 0.5f;

    [Tooltip("CurrentHpRatio 또는 MaxHpMinimum 방식에서 현재 체력에 곱할 최대 비율입니다.")]
    [Range(0f, 2f)] public float currentHpRatioMax = 0.67f;

    [Tooltip("MaxHpMinimum 방식에서 최대 체력 기준으로 보장할 최소 피해 비율입니다.")]
    [Range(0f, 1f)] public float maxHpMinimumRatio = 0.2f;

    [Tooltip("범위형 효과가 영향을 주는 반경입니다. 0이면 중심 칸만 사용합니다.")]
    [Min(0)] public int radius;

    [Tooltip("상태 이상, 지형 변화, 지연 효과가 유지되는 턴 수입니다.")]
    [Min(0)] public int durationTurns;

    [Tooltip("상태 이상 강도입니다. 독 피해량, 약화 수치처럼 효과 컴포넌트가 해석해서 사용합니다.")]
    public int statusPower;

    [Tooltip("지연형 효과가 몇 번의 플레이어 행동 후 발동할지 정합니다.")]
    [Min(0)] public int delayTurns;

    [Header("호환/기존 수치")]
    [Tooltip("기존 트랩 데이터 호환용 피해량입니다. damageMin과 damageMax가 둘 다 0일 때만 사용됩니다.")]
    public int damage = 10;

    [Tooltip("알람 트랩처럼 경보나 호출 효과가 유지되는 시간 또는 턴 수입니다.")]
    public int alarmDuration = 10;

    [Header("표시")]
    [Tooltip("트랩이 드러난 상태에서 보여줄 스프라이트입니다.")]
    public Sprite visibleSprite;

    [Tooltip("트랩이 숨겨진 상태에서 보여줄 스프라이트입니다. 비워두면 숨김 상태에서 보이지 않습니다.")]
    public Sprite hiddenSprite;

    [Tooltip("트랩이 발동된 뒤 보여줄 스프라이트입니다. 비워두면 드러난 스프라이트를 계속 사용합니다.")]
    public Sprite triggeredSprite;

    public int RollDamage()
    {
        int min = Mathf.Min(damageMin, damageMax);
        int max = Mathf.Max(damageMin, damageMax);
        if (min == 0 && max == 0) return damage;
        return Random.Range(min, max + 1);
    }

    public int CalculateDamage(IDamageable target)
    {
        if (target == null) return RollDamage();

        switch (damageMode)
        {
            case TrapDamageMode.CurrentHpRatio:
                return Mathf.Max(1, Mathf.RoundToInt(target.CurrentHp * Random.Range(currentHpRatioMin, currentHpRatioMax)));
            case TrapDamageMode.MaxHpMinimum:
            {
                int ratioDamage = Mathf.RoundToInt(target.CurrentHp * Random.Range(currentHpRatioMin, currentHpRatioMax));
                int minimumDamage = Mathf.RoundToInt(target.MaxHp * maxHpMinimumRatio);
                return Mathf.Max(1, Mathf.Max(ratioDamage, minimumDamage));
            }
            case TrapDamageMode.Grim:
                if (target.CurrentHp >= Mathf.CeilToInt(target.MaxHp * 0.9f))
                    return Mathf.Max(0, target.CurrentHp - 1);
                return Mathf.Max(1, target.CurrentHp);
            default:
                return RollDamage();
        }
    }
}
