using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점에서 룬 상세 정보를 표시하는 공유 패널 매니저
/// 씬에 하나만 존재하며, 모든 ShopRuneButton들이 이 패널을 공유합니다
/// </summary>
public class RuneDetailPanel : MonoBehaviour
{
    #region UI Components

    [Header("룬 정보 UI")]
    [SerializeField] private Image detailRuneImage;         // 룬 아이콘
    [SerializeField] private Text detailRuneName;           // 룬 이름
    [SerializeField] private Text detailRarityText;         // 등급 텍스트
    [SerializeField] private Text detailPriceText;          // 가격 텍스트
    [SerializeField] private Text detailDescriptionText;    // 효과 설명 텍스트
    [SerializeField] private Text detailDropRuneText;       // 등장 위치 설명 텍스트
    [Header("필요 없는 설명 끄기")]
    [SerializeField] private Text detailText1;
    [SerializeField] private Text detailText2;
    [SerializeField] private Text detailText3;
    [SerializeField] private Text detailText4;

    #endregion

    #region Private Fields

    private RuneSO currentRune;                             // 현재 표시 중인 룬
    private int currentPrice;                               // 현재 룬 가격
    private UIManager currentUIManager;                     // 현재 UI 매니저 참조

    #endregion

    #region Public Methods

    /// <summary>
    /// 룬 상세 정보를 패널에 표시
    /// ShopRuneButtonController에서 호출됩니다
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    /// <param name="price">룬 가격</param>
    /// <param name="uiManager">UI 매니저 참조</param>
    public void ShowRuneDetail(RuneSO rune, int price, UIManager uiManager)
    {
        if (rune == null)
        {
            Debug.LogWarning("[RuneDetailPanel] 룬 데이터가 null입니다!");
            return;
        }

        currentRune = rune;
        currentPrice = price;
        currentUIManager = uiManager;

        UpdateDetailPanel();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 패널의 모든 UI 요소를 현재 룬 데이터로 업데이트
    /// </summary>
    private void UpdateDetailPanel()
    {
        if (currentRune == null) return;

        // 룬 아이콘
        if (detailRuneImage != null && currentRune.icon != null)
        {
            detailRuneImage.sprite = currentRune.icon;
            Debug.Log($"[RuneDetailPanel] 룬 아이콘 설정 완료: {currentRune.runeName}");
        }
        else
        {
            if (detailRuneImage == null)
                Debug.LogWarning("[RuneDetailPanel] detailRuneImage가 null입니다!");
            if (currentRune.icon == null)
                Debug.LogWarning($"[RuneDetailPanel] {currentRune.runeName}의 icon이 null입니다!");
        }

        // 룬 이름
        if (detailRuneName != null)
        {
            detailRuneName.text = currentRune.runeName;
        }

        // 등급
        if (detailRarityText != null)
        {
            detailRarityText.text = GetRarityKoreanName(currentRune.rarity);
            detailRarityText.color = GetRarityColor(currentRune.rarity);
        }

        // 가격
        if (detailPriceText != null)
        {
            detailPriceText.text = $"가격: {currentPrice} 다이아몬드";
        }

        // 효과 설명
        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = string.IsNullOrEmpty(currentRune.description)
                ? "설명이 없습니다."
                : currentRune.description;
        }

        // 등장 위치 설명
        if (detailDropRuneText != null)
        {
            switch (GetRarityKoreanName(currentRune.rarity))
            {
                case "일반":
                    detailDropRuneText.text = "획득처: 모집";
                    break;
                case "희귀":
                    detailDropRuneText.text = "획득처: 상점,엘리트방";
                    break;
                case "영웅":
                    detailDropRuneText.text = "획득처: 비밀상점,엘리트방";
                    break;
                case "전설":
                    detailDropRuneText.text = "획득처: 무작위 방";
                    break;
            }
            
        }

        // 필요 없는 텍스트 지우기 
        if (detailText1 != null || detailText2 != null || detailText3 != null || detailText4 != null)
        {
            detailText1.text = "";
            detailText2.text = "";
            detailText3.text = "";
            detailText4.text = "";
        }
    }

    /// <summary>
    /// 등급을 한글로 변환
    /// </summary>
    private string GetRarityKoreanName(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return "일반";
            case RuneRarity.Rare:
                return "희귀";
            case RuneRarity.Epic:
                return "영웅";
            case RuneRarity.Legendary:
                return "전설";
            default:
                return "알 수 없음";
        }
    }

    /// <summary>
    /// 등급별 텍스트 색상 반환
    /// </summary>
    private Color GetRarityColor(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return new Color(0.7f, 0.7f, 0.7f);  // 회색
            case RuneRarity.Rare:
                return new Color(0.3f, 0.6f, 1f);    // 파란색
            case RuneRarity.Epic:
                return new Color(0.7f, 0.3f, 1f);    // 보라색
            case RuneRarity.Legendary:
                return new Color(1f, 0.7f, 0.2f);    // 주황/금색
            default:
                return Color.white;
        }
    }

    #endregion
}
