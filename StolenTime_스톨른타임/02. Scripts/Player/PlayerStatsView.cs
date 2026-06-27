using TMPro;
using UnityEngine;
using PrototypeRT;

/// <summary>
/// 플레이어 스탯 UI를 갱신하는 화면 표시 전용 컴포넌트.
/// 실제 값은 NYHPlayerStats, GridPlayerHealth, PlayerLevelSystem에서 읽고,
/// 이 클래스는 Text에 보기 좋게 써 주는 역할만 맡는다.
/// </summary>
public class PlayerStatsView : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("현재 체력/최대 체력을 읽어올 플레이어 체력 컴포넌트입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private GridPlayerHealth playerHealth;

    [Tooltip("레벨과 경험치를 읽어올 플레이어 레벨 시스템입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerLevelSystem playerLevel;

    [Tooltip("현재 장착 무기를 읽어 공격력 표시를 계산할 장비 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private NYHEquipmentController equipmentController;

    [Header("요약 텍스트")]
    [Tooltip("체력, 힘, 이동 속도, 공격 속도, 레벨, 방어력, 회피, 경험치를 한 번에 표시할 Text입니다.")]
    [SerializeField] private TMP_Text summaryText;

    [Header("개별 텍스트")]
    [Tooltip("체력 수치만 표시할 Text입니다. 라벨은 별도 LocalizedText가 담당합니다. 예: 80/100")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("힘 수치를 표시할 Text입니다.")]
    [SerializeField] private TMP_Text strengthText;

    [Tooltip("이동 속도 수치를 표시할 Text입니다.")]
    [SerializeField] private TMP_Text moveSpeedText;

    [Tooltip("공격 속도 수치를 표시할 Text입니다.")]
    [SerializeField] private TMP_Text attackSpeedText;

    [Tooltip("현재 레벨을 표시할 Text입니다.")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("현재 경험치와 다음 레벨 요구 경험치를 표시할 Text입니다. 최대 레벨이면 EXP MAX로 표시합니다.")]
    [SerializeField] private TMP_Text expText;

    [Tooltip("방어력 수치를 표시할 Text입니다.")]
    [SerializeField] private TMP_Text defenseText;

    [Tooltip("회피율 수치만 표시할 Text입니다. 예: 10%")]
    [SerializeField] private TMP_Text evasionText;
    [Tooltip("공격력을 표시할 Text입니다.")]
    [SerializeField] private TMP_Text attackText;
    [Tooltip("명중률 수치만 표시할 Text입니다. 예: 85%")]
    [SerializeField] private TMP_Text accuracyText;

    /// <summary>
    /// UI가 켜질 때 참조를 찾고 이벤트 구독을 시작한다.
    /// 이미 값이 바뀐 상태로 켜질 수 있으므로 마지막에 즉시 Refresh를 호출한다.
    /// </summary>
    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    /// <summary>
    /// UI가 꺼질 때 이벤트 구독을 해제한다.
    /// 비활성화된 UI가 계속 이벤트를 받거나 중복 구독되는 문제를 막는다.
    /// </summary>
    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 씬 초기화 순서 때문에 다른 컴포넌트가 늦게 준비되는 경우를 대비해 한 번 더 보정한다.
    /// </summary>
    private void Start()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    /// <summary>
    /// 인스펙터에서 직접 연결하지 않은 참조를 씬에서 자동으로 찾는다.
    /// </summary>
    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<GridPlayerHealth>();

        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevelSystem>();

        if (equipmentController == null)
            equipmentController = FindFirstObjectByType<NYHEquipmentController>();
    }

    /// <summary>
    /// 스탯/체력/레벨/경험치가 바뀔 때마다 UI를 자동 갱신하도록 이벤트를 연결한다.
    /// 연결 전에 한 번 빼고 다시 더해서 중복 구독을 방지한다.
    /// </summary>
    private void Subscribe()
    {
        if (NYHPlayerStats.Instance != null)
        {
            NYHPlayerStats.Instance.OnStatsChanged -= Refresh;
            NYHPlayerStats.Instance.OnStatsChanged += Refresh;
            NYHPlayerStats.Instance.OnStatAdded -= OnStatAdded;
            NYHPlayerStats.Instance.OnStatAdded += OnStatAdded;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= Refresh;
            playerHealth.OnHealthChanged += Refresh;
        }

        if (playerLevel != null)
        {
            playerLevel.OnLevelChanged -= Refresh;
            playerLevel.OnLevelChanged += Refresh;
            playerLevel.OnExpChanged -= OnExpChanged;
            playerLevel.OnExpChanged += OnExpChanged;
        }

        if (equipmentController != null)
        {
            equipmentController.OnEquipped -= OnEquipmentChanged;
            equipmentController.OnEquipped += OnEquipmentChanged;
            equipmentController.OnUnequipped -= OnEquipmentChanged;
            equipmentController.OnUnequipped += OnEquipmentChanged;
            equipmentController.OnInventoryItemStateChanged -= OnEquipmentItemStateChanged;
            equipmentController.OnInventoryItemStateChanged += OnEquipmentItemStateChanged;
        }
    }

    /// <summary>
    /// Subscribe에서 연결한 이벤트를 모두 해제한다.
    /// </summary>
    private void Unsubscribe()
    {
        if (NYHPlayerStats.Instance != null)
        {
            NYHPlayerStats.Instance.OnStatsChanged -= Refresh;
            NYHPlayerStats.Instance.OnStatAdded -= OnStatAdded;
        }

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;

        if (playerLevel != null)
        {
            playerLevel.OnLevelChanged -= Refresh;
            playerLevel.OnExpChanged -= OnExpChanged;
        }

        if (equipmentController != null)
        {
            equipmentController.OnEquipped -= OnEquipmentChanged;
            equipmentController.OnUnequipped -= OnEquipmentChanged;
            equipmentController.OnInventoryItemStateChanged -= OnEquipmentItemStateChanged;
        }
    }

    /// <summary>
    /// 현재 플레이어 데이터를 읽어서 모든 Text를 즉시 다시 쓴다.
    /// 인벤토리 패널을 열 때 수동 호출해도 되고, 이벤트를 통해 자동 호출되어도 된다.
    /// </summary>
    public void Refresh()
    {
        if (playerHealth == null || playerLevel == null)
            ResolveReferences();

        NYHPlayerStats playerStats = NYHPlayerStats.Instance;
        if (playerStats == null && playerHealth == null && playerLevel == null)
        {
            SetText(summaryText, "");
            return;
        }

        string health = playerHealth != null ? $"{playerHealth.CurrentHp} / {playerHealth.MaxHp}" : "-";
        string strength = playerStats != null ? playerStats.Strength.ToString() : "-";
        string moveSpeed = playerStats != null ? playerStats.MoveSpeed.ToString() : "-";
        string attackSpeed = playerStats != null ? playerStats.AttackSpeed.ToString() : "-";
        string accuracy = playerStats != null ? $"{playerStats.Accuracy}%" : "-";
        string level = playerLevel != null ? playerLevel.Level.ToString() : "-";
        string defense = playerStats != null ? playerStats.Defense.ToString() : "-";
        string evasion = playerStats != null ? $"{playerStats.Evasion}%" : "-";
        string exp = playerLevel != null ? playerLevel.ExpDisplayText : "-";
        string attack = playerStats != null
            ? NYHPlayerAttackCalculator.CalculateMeleeRange(playerStats, equipmentController).ToDisplayText()
            : "-";


        SetText(healthText, health);
        SetText(strengthText, strength);
        SetText(moveSpeedText, moveSpeed);
        SetText(attackSpeedText, attackSpeed);
        SetText(levelText, level);
        SetText(defenseText, defense);
        SetText(evasionText, evasion);
        SetText(expText, exp);
        SetText(attackText, attack);
        SetText(accuracyText, accuracy);

        SetText(summaryText,
            $"명중률 {accuracy}\n" +
            $"공격력 {attack}\n" +
            $"힘 {strength}\n" +
            $"이동 속도 {moveSpeed}\n" +
            $"공격 속도 {attackSpeed}\n" +
            $"레벨 {level}\n" +
            $"방어력 {defense}\n" +
            $"회피 {evasion}\n" +
            $"경험치 {exp}" +
            $"명중률 {accuracy}"
            );

    }

    /// <summary>
    /// 연결되지 않은 Text는 조용히 건너뛰고, 연결된 Text만 값을 갱신한다.
    /// </summary>
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    /// <summary>
    /// 특정 스탯이 증가했을 때 UI를 갱신한다.
    /// amount를 별도 표시하지 않고 전체 표시값을 다시 계산한다.
    /// </summary>
    private void OnStatAdded(NYHStatType type, int amount)
    {
        Refresh();
    }

    /// <summary>
    /// 경험치가 추가됐을 때 UI를 갱신한다.
    /// amount는 이번 획득량이며, 현재 누적 경험치는 PlayerLevelSystem에서 다시 읽는다.
    /// </summary>
    private void OnExpChanged(int amount)
    {
        Refresh();
    }

    private void OnEquipmentChanged(EquipmentSlot slot, NYHEquipmentItemInstance item)
    {
        Refresh();
    }

    private void OnEquipmentItemStateChanged(EquipmentSlot slot, int slotIndex, InventoryItem item)
    {
        Refresh();
    }
}
