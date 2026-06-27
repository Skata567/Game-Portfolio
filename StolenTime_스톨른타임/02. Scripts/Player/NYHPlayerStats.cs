using System;
using UnityEngine;
using PrototypeRT;

public enum NYHStatType
{
    // 최대 체력. 레벨업/장비/보상으로 영구 증가할 수 있다.
    Health,

    // 근접 공격, 투척 무기 등 플레이어가 주는 기본 피해량 보정.
    Strength,

    // 격자 이동 속도. GridPlayerMovement의 이동 시간/후딜 계산에 사용된다.
    MoveSpeed,

    // 공격 애니메이션 속도와 공격 관련 시간 계산에 사용된다.
    AttackSpeed,

    // 받는 피해를 고정값으로 감소시키는 방어 스탯.
    Defense,

    // 공격자의 정확도와 비교해 피격 무효 확률을 만드는 회피 스탯.
    Evasion,

    // 이전 데이터 호환용. 실제 레벨/경험치는 PlayerLevelSystem에서 관리한다.
    Level,

    // 배고픔 수치. 시간에 따라 감소하며 0이 되면 굶주림 상태가 된다.
    Hunger,

    // 명중률 보정값. 기존 enum 직렬화 값을 밀지 않기 위해 마지막에 추가한다.
    Accuracy
}

/// <summary>
/// 플레이어의 영구/기본 스탯(최대 체력, 근력, 이동/공격 속도, 방어/회피)을 중앙에서 관리하는 데이터 허브 클래스입니다.
/// 장비 장착, 레벨업, 보상 등에 의해 스탯 수치가 증감될 때 이를 추적하고 이벤트를 발생시킵니다.
/// 
/// 임시로 스탯을 변화시키는 상태이상은 StatusController가, 경험치 및 레벨업 시스템은 PlayerLevelSystem이 
/// 각각 별도로 관리하며, 본 클래스는 순수 베이스 스탯만을 전담합니다.
/// </summary>
public class NYHPlayerStats : MonoBehaviour
{
    public static NYHPlayerStats Instance { get; private set; }

    [Header("기본 스탯")]
    [Tooltip("플레이어 최대 체력입니다. GridPlayerHealth의 MaxHp 계산에 사용됩니다.")]
    [SerializeField, Min(1)] private int maxHealth = 20;

    [Tooltip("플레이어의 기본 힘 입니다.일부 무기 장착 계산에 사용됩니다.")]
    [SerializeField, Min(0)] private int strength = 10;

    [Tooltip("플레이어 이동 속도입니다. 값이 높을수록 한 칸 이동 시간이 짧아집니다.")]
    [SerializeField, Min(0)] private int moveSpeed = 1;

    [Tooltip("플레이어 공격 속도입니다. 값이 높을수록 공격 애니메이션 속도가 빨라집니다.")]
    [SerializeField, Min(0)] private int attackSpeed = 1;

    [Tooltip("플레이어 명중률 보정값입니다. 현재는 확장용 스탯이며, 명중 판정 시스템에서 사용할 수 있습니다.")]
    [SerializeField, Range(0, 100)] private int accuracy = 15;

    [Header("방어 스탯")]
    [Tooltip("받는 피해를 고정값으로 감소시키는 방어력입니다. 최종 피해는 최소 0까지 내려갑니다.")]
    [SerializeField, Min(0)] private int defense;

    [Tooltip("몬스터 정확도와 비교해 공격을 무효화할 확률을 만듭니다. 실제 무효화 확률은 최대 50%로 제한됩니다.")]
    [SerializeField, Range(0, 100)] private int evasion = 20;

    [Header("상태 스탯")]
    [Tooltip("배고픔 수치입니다. 10초마다 1씩 감소하며 0이 되면 굶주림(체력감소, 쿨타임 증가) 상태가 됩니다.")]
    [SerializeField, Range(0, 10)] private int hunger = 10;

    /// <summary>
    /// 스탯 값이 변경됐을 때 호출된다.
    /// UI, 장비 조건 갱신, 파생 능력치 갱신 등에 사용한다.
    /// </summary>
    public event Action OnStatsChanged;

    /// <summary>
    /// 특정 스탯이 증가/감소했을 때 호출된다.
    /// GridPlayerHealth는 Health 증가 이벤트를 받아 최대 체력 증가분만큼 회복한다.
    /// </summary>
    public event Action<NYHStatType, int> OnStatAdded;

    public int MaxHealth => maxHealth;
    public int Strength => strength;
    public int MoveSpeed => moveSpeed;
    public int AttackSpeed => attackSpeed;
    public int Accuracy => accuracy;
    public int Defense => defense;
    public int Evasion => evasion;
    public int Hunger => hunger;

    /// <summary>
    /// 씬에서 전역 접근할 플레이어 스탯 인스턴스를 등록한다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NYHPlayerStats: 씬에 스탯 컴포넌트가 둘 이상 있습니다. 먼저 등록된 값을 사용합니다.");
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 현재 인스턴스가 파괴될 때 싱글톤 참조를 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 외부 요인(장비 스왑, 레벨업 보상 등)에 의해 지정된 스탯을 amount만큼 영구/일시적으로 증감시킵니다.
    /// 체력(Health)의 경우 최대 체력을 변경하며, OnStatAdded 이벤트를 통해 GridPlayerHealth 등에서 실제 현재 체력을 동기화합니다.
    /// </summary>
    public void AddStat(NYHStatType type, int amount)
    {
        switch (type)
        {
            case NYHStatType.Health:
                maxHealth = Mathf.Max(1, maxHealth + amount);
                OnStatAdded?.Invoke(type, amount);
                OnStatsChanged?.Invoke();
                return;
            case NYHStatType.Strength:
                strength = Mathf.Max(0, strength + amount);
                break;
            case NYHStatType.MoveSpeed:
                // MoveSpeed는 정수 스탯이므로 전달된 증가량을 한 번만 반영한다.
                // 기존에는 amount / 100과 amount가 모두 더해져 큰 보상 값에서 이동속도가 비정상적으로 튀었다.
                moveSpeed = Mathf.Max(0, moveSpeed + amount);
                break;
            case NYHStatType.AttackSpeed:
                attackSpeed = Mathf.Max(0, attackSpeed + amount);
                break;
            case NYHStatType.Accuracy:
                accuracy = Mathf.Clamp(accuracy + amount, 0, 100);
                break;
            case NYHStatType.Defense:
                defense = Mathf.Max(0, defense + amount);
                break;
            case NYHStatType.Evasion:
                evasion = Mathf.Clamp(evasion + amount, 0, 100);
                break;
            case NYHStatType.Hunger:
                hunger = Mathf.Clamp(hunger + amount, 0, 10);
                break;
            case NYHStatType.Level:
                Debug.LogWarning("NYHPlayerStats: Level은 PlayerLevelSystem에서 관리합니다.");
                return;
        }

        OnStatAdded?.Invoke(type, amount);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 세이브 로드 시 베이스 스탯을 절대값으로 한 번에 복원합니다.
    /// AddStat의 누적/회복 부수효과를 피하기 위해 필드를 직접 세팅하고, 마지막에 변경 이벤트만 발생시킵니다.
    /// 현재 체력 복원은 GridPlayerHealth가 별도로 처리하므로 여기서 Health 회복 이벤트는 보내지 않습니다.
    /// </summary>
    public void SetStats(int maxHealth, int strength, int moveSpeed, int attackSpeed, int accuracy, int defense, int evasion, int hunger)
    {
        this.maxHealth = Mathf.Max(1, maxHealth);
        this.strength = Mathf.Max(0, strength);
        this.moveSpeed = Mathf.Max(0, moveSpeed);
        this.attackSpeed = Mathf.Max(0, attackSpeed);
        this.accuracy = Mathf.Clamp(accuracy, 0, 100);
        this.defense = Mathf.Max(0, defense);
        this.evasion = Mathf.Clamp(evasion, 0, 100);
        this.hunger = Mathf.Clamp(hunger, 0, 10);

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 플레이어의 방어력(고정 감소)을 적용하여 실제로 깎일 최종 피해량을 산출해 반환합니다.
    /// 회피는 공격자의 정확도가 필요하므로 EnemyBase.TryHitPlayer()에서 먼저 판정합니다.
    /// </summary>
    public int CalculateIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;

        return Mathf.Max(0, rawDamage - defense);
    }

    /// <summary>
    /// 인스펙터에서 잘못된 값이 들어가지 않도록 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        strength = Mathf.Max(0, strength);
        moveSpeed = Mathf.Max(0, moveSpeed);
        attackSpeed = Mathf.Max(0, attackSpeed);
        accuracy = Mathf.Clamp(accuracy, 0, 100);
        defense = Mathf.Max(0, defense);
        evasion = Mathf.Clamp(evasion, 0, 100);
    }
}

public readonly struct NYHAttackDamageRange
{
    public readonly int MinDamage;
    public readonly int MaxDamage;
    public readonly int BonusMax;
    public readonly bool IsUnarmed;
    public readonly string WeaponName;

    public NYHAttackDamageRange(int minDamage, int maxDamage, int bonusMax, bool isUnarmed, string weaponName)
    {
        MinDamage = Mathf.Max(0, minDamage);
        MaxDamage = Mathf.Max(MinDamage, maxDamage);
        BonusMax = Mathf.Max(0, bonusMax);
        IsUnarmed = isUnarmed;
        WeaponName = weaponName;
    }

    public string ToDisplayText()
    {
        if (MinDamage == MaxDamage)
            return MinDamage.ToString();

        return BonusMax > 0
            ? $"{MinDamage}~{MaxDamage} (+0~{BonusMax})"
            : $"{MinDamage}~{MaxDamage}";
    }
}

public static class NYHPlayerAttackCalculator
{
    private const int UnarmedDamage = 1;

    public static NYHAttackDamageRange CalculateMeleeRange(NYHPlayerStats stats, NYHEquipmentController equipmentController)
    {
        if (stats == null)
            return new NYHAttackDamageRange(UnarmedDamage, UnarmedDamage, 0, true, "맨손");

        WeaponInstance weaponInstance = GetEquippedWeaponInstance(equipmentController);
        if (weaponInstance != null && weaponInstance.Weapon != null)
        {
            CloseWeapon weapon = weaponInstance.Weapon;
            GetTraitAttackBonus(out int traitMinBonus, out int traitMaxBonus);
            int strengthBonusMax = Mathf.Max(0, stats.Strength - weapon.CanUseStrength);
            int minDamage = Mathf.Max(0, weaponInstance.GetMinDamage() + traitMinBonus);
            int maxDamage = Mathf.Max(minDamage, weaponInstance.GetMaxDamage() + strengthBonusMax + traitMaxBonus);

            return new NYHAttackDamageRange(minDamage, maxDamage, strengthBonusMax, false, weapon.Name);
        }

        CloseWeapon fallbackWeapon = GetEquippedCloseWeapon(equipmentController);
        if (fallbackWeapon == null)
        {
            GetTraitAttackBonus(out int unarmedTraitMinBonus, out int unarmedTraitMaxBonus);
            return new NYHAttackDamageRange(
                UnarmedDamage + unarmedTraitMinBonus,
                UnarmedDamage + unarmedTraitMaxBonus,
                0,
                true,
                "맨손");
        }

        GetTraitAttackBonus(out int fallbackTraitMinBonus, out int fallbackTraitMaxBonus);
        int fallbackStrengthBonusMax = Mathf.Max(0, stats.Strength - fallbackWeapon.CanUseStrength);
        int fallbackMinDamage = Mathf.Max(0, fallbackWeapon.MinDamage + fallbackTraitMinBonus);
        int fallbackMaxDamage = Mathf.Max(fallbackMinDamage, fallbackWeapon.MaxDamage + fallbackStrengthBonusMax + fallbackTraitMaxBonus);

        return new NYHAttackDamageRange(fallbackMinDamage, fallbackMaxDamage, fallbackStrengthBonusMax, false, fallbackWeapon.Name);
    }

    public static int RollMeleeDamage(NYHPlayerStats stats, NYHEquipmentController equipmentController)
    {
        NYHAttackDamageRange range = CalculateMeleeRange(stats, equipmentController);
        if (range.MinDamage == range.MaxDamage)
            return range.MinDamage;

        // UnityEngine.Random.Range(int, int)는 maxExclusive라서 +1 한다.
        return UnityEngine.Random.Range(range.MinDamage, range.MaxDamage + 1);
    }

    private static CloseWeapon GetEquippedCloseWeapon(NYHEquipmentController equipmentController)
    {
        if (equipmentController == null)
            return null;

        return equipmentController.TryGetEquipped(EquipmentSlot.Weapon, out NYHEquipmentItemInstance item)
            ? item.Data as CloseWeapon
            : null;
    }

    private static WeaponInstance GetEquippedWeaponInstance(NYHEquipmentController equipmentController)
    {
        if (equipmentController == null)
            return null;

        if (equipmentController.TryGetEquippedInventoryItem(EquipmentSlot.Weapon, 0, out InventoryItem itemView)
            && itemView != null
            && itemView.ItemInstance is WeaponInstance viewWeapon)
        {
            return viewWeapon;
        }

        if (equipmentController.TryGetEquipped(EquipmentSlot.Weapon, out NYHEquipmentItemInstance equipped)
            && equipped != null
            && equipped.SourceInstance is WeaponInstance sourceWeapon)
        {
            return sourceWeapon;
        }

        return null;
    }

    private static void GetTraitAttackBonus(out int minBonus, out int maxBonus)
    {
        TraitController traitController = TraitController.FindAvailableInstance();
        if (traitController != null)
        {
            // HeavyBlow는 장비 데이터 자체를 바꾸지 않고 공격 계산 결과에만 더한다.
            // 장비 원본 수치를 오염시키면 장착 해제/특성 초기화 때 되돌리기 어렵기 때문이다.
            traitController.GetBaseAttackDamageBonusMinMax(out minBonus, out maxBonus);
            return;
        }

        minBonus = 0;
        maxBonus = 0;
    }
}

public static class NYHPlayerDefenseCalculator
{
    public static int RollArmorDefense(NYHEquipmentController equipmentController)
    {
        ArmorInstance armor = GetEquippedArmorInstance(equipmentController);
        return armor != null ? armor.GetDefense() : 0;
    }

    public static (int min, int max) GetArmorDefenseRange(NYHEquipmentController equipmentController)
    {
        ArmorInstance armor = GetEquippedArmorInstance(equipmentController);
        if (armor != null)
            return (armor.GetMinDefense(), armor.GetMaxDefense());
        return (0, 0);
    }

    private static ArmorInstance GetEquippedArmorInstance(NYHEquipmentController equipmentController)
    {
        if (equipmentController == null)
            return null;

        if (equipmentController.TryGetEquippedInventoryItem(EquipmentSlot.Armor, 0, out InventoryItem itemView)
            && itemView != null
            && itemView.ItemInstance is ArmorInstance viewArmor)
        {
            return viewArmor;
        }

        if (equipmentController.TryGetEquipped(EquipmentSlot.Armor, out NYHEquipmentItemInstance equipped)
            && equipped != null
            && equipped.SourceInstance is ArmorInstance sourceArmor)
        {
            return sourceArmor;
        }

        return null;
    }
}
