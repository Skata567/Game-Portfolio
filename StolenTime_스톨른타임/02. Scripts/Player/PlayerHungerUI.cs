using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 배고픔(Hunger) 수치를 UI Image의 fillAmount로 반영하는 전용 스크립트입니다.
/// NYHPlayerStats의 OnStatsChanged 이벤트를 구독해서 갱신합니다.
/// </summary>
public class PlayerHungerUI : MonoBehaviour
{
    [Header("스탯 참조")]
    [Tooltip("배고픔 수치를 읽어올 플레이어 스탯 컴포넌트입니다. 비워두면 씬에서 NYHPlayerStats를 찾습니다.")]
    [SerializeField] private NYHPlayerStats stats;

    [Header("UI 참조")]
    [Tooltip("현재 배고픔 비율을 표시할 Image입니다. Image Type은 Filled여야 합니다.")]
    [SerializeField] private Image fillImage;

    [Tooltip("배고픔 수치를 텍스트로 표시할 경우 연결합니다. (예: 10/10)")]
    [SerializeField] private UnityEngine.UI.Text hungerText;

    private void Awake()
    {
        if (stats == null)
            stats = FindFirstObjectByType<NYHPlayerStats>();
    }

    private void OnEnable()
    {
        if (stats == null) return;

        stats.OnStatsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (stats == null) return;

        stats.OnStatsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (stats == null) return;

        float maxHunger = 10f; // 배고픔 최대치는 GDD상 고정 10
        float currentHunger = stats.Hunger;

        if (fillImage != null)
        {
            fillImage.fillAmount = currentHunger / maxHunger;
        }

        if (hungerText != null)
        {
            hungerText.text = $"{currentHunger}/{maxHunger}";
        }
    }
}
