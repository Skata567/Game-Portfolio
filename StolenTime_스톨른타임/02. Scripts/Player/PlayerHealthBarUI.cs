using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP/EXP 값을 UI Image fillAmount와 텍스트에 반영하는 바 전용 컴포넌트입니다.
/// </summary>
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Value Sources")]
    [Tooltip("기존 씬 직렬화 연결을 유지하기 위해 필드명 helth를 그대로 둡니다.")]
    [SerializeField] private GridPlayerHealth helth;

    [Tooltip("EXP 바가 참조할 플레이어 레벨/경험치 컴포넌트입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerLevelSystem playerLevel;

    [Header("Fill Images")]
    [Tooltip("현재 체력 비율을 표시할 Filled Image입니다.")]
    [SerializeField] private Image fillImage;

    [Tooltip("현재 경험치 비율을 표시할 Filled Image입니다.")]
    [SerializeField] private Image expFillImage;

    [Header("Value Texts")]
    [Tooltip("체력을 '현재 / 최대' 형식으로 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("경험치를 '현재 / 최대' 형식으로 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text expText;

    private void Awake()
    {
        ResolveReferences();
        ConfigureFillImage(fillImage);
        ConfigureFillImage(expFillImage);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (helth != null)
            helth.OnHealthChanged += Refresh;

        if (playerLevel != null)
        {
            playerLevel.OnExpChanged += OnExpChanged;
            playerLevel.OnLevelChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (helth != null)
            helth.OnHealthChanged -= Refresh;

        if (playerLevel != null)
        {
            playerLevel.OnExpChanged -= OnExpChanged;
            playerLevel.OnLevelChanged -= Refresh;
        }
    }

    private void ResolveReferences()
    {
        if (helth == null)
            helth = FindFirstObjectByType<GridPlayerHealth>();

        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevelSystem>();
    }

    private void Refresh()
    {
        RefreshHealth();
        RefreshExp();
    }

    private void RefreshHealth()
    {
        if (helth == null) return;

        int maxHp = Mathf.Max(0, helth.MaxHp);
        int currentHp = Mathf.Clamp(helth.CurrentHp, 0, maxHp);

        if (fillImage != null)
        {
            ConfigureFillImage(fillImage);
            fillImage.fillAmount = maxHp > 0
                ? (float)currentHp / maxHp
                : 0f;
        }

        if (healthText != null)
            healthText.text = $"{currentHp} / {maxHp}";
    }

    private void RefreshExp()
    {
        if (playerLevel == null) return;

        int requiredExp = Mathf.Max(0, playerLevel.RequiredExp);
        int currentExp = Mathf.Max(0, playerLevel.CurrentExp);

        if (expFillImage != null)
        {
            ConfigureFillImage(expFillImage);
            expFillImage.fillAmount = requiredExp > 0
                ? Mathf.Clamp01((float)currentExp / requiredExp)
                : 1f;
        }

        if (expText != null)
            expText.text = requiredExp > 0 ? $"{currentExp} / {requiredExp}" : "MAX";
    }

    private void OnExpChanged(int amount)
    {
        Refresh();
    }

    private static void ConfigureFillImage(Image image)
    {
        if (image == null) return;

        // fillAmount가 실제 막대 길이에 반영되려면 Image가 Filled 타입이어야 합니다.
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
}
