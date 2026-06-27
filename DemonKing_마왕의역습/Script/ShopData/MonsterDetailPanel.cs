using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점에서 몬스터 상세 정보를 표시하는 공유 패널 매니저
/// 씬에 하나만 존재하며, 모든 ShopMonsterButton들이 이 패널을 공유합니다
/// </summary>
public class MonsterDetailPanel : MonoBehaviour
{
    #region UI Components

    [Header("몬스터 정보 UI")]
    [SerializeField] private Image detailMonsterImage;          // 몬스터 이미지
    [SerializeField] private Text detailMonsterName;            // 몬스터 이름
    [SerializeField] private Text detailRaceText;               // 종족 텍스트
    [SerializeField] private Text detailOwnedText;              // 보유량 텍스트
    [SerializeField] private Text detailDamageText;             // 공격력 텍스트
    [SerializeField] private Text detailHpText;                 // 체력 텍스트
    [SerializeField] private Text detailSpeedText;              // 속도 텍스트
    [SerializeField] private Text detailCostText;               // 코스트 텍스트
    [SerializeField] private Text detailSkillDescText;          // 스킬 설명 텍스트

    #endregion

    #region Private Fields

    private UnitStock currentMonster;                           // 현재 표시 중인 몬스터
    private UIManager currentUIManager;                         // 현재 UI 매니저 참조

    #endregion

    #region Public Methods

    /// <summary>
    /// 몬스터 상세 정보를 패널에 표시
    /// ShopMonsterButtonController에서 호출됩니다
    /// </summary>
    /// <param name="monster">표시할 몬스터 데이터</param>
    /// <param name="uiManager">UI 매니저 참조</param>
    public void ShowMonsterDetail(UnitStock monster, UIManager uiManager)
    {
        if (monster == null)
        {
            Debug.LogWarning("[MonsterDetailPanel] 몬스터 데이터가 null입니다!");
            return;
        }

        currentMonster = monster;
        currentUIManager = uiManager;

        UpdateDetailPanel();
    }

    /// <summary>
    /// 현재 표시 중인 몬스터와 같다면 보유량 업데이트
    /// 구매 후 실시간 반영용
    /// </summary>
    public void RefreshIfShowing(UnitStock monster)
    {
        if (currentMonster != null && currentMonster.enemyId == monster.enemyId)
        {
            UpdateDetailPanel();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 패널의 모든 UI 요소를 현재 몬스터 데이터로 업데이트
    /// </summary>
    private void UpdateDetailPanel()
    {
        if (currentMonster == null) return;

        // 몬스터 이미지
        if (detailMonsterImage != null && currentMonster.icon != null)
        {
            detailMonsterImage.sprite = currentMonster.icon;
        }

        // 몬스터 이름
        if (detailMonsterName != null)
        {
            detailMonsterName.text = currentMonster.displayName;
        }

        // 종족 (한글 변환) ㅊ
        if (detailRaceText != null)
        {
            detailRaceText.text = $"종족: {GetRaceKoreanName(currentMonster.category)}";
        }

        // 보유량
        if (detailOwnedText != null)
        {
            int ownedCount;
            if (TutorialManager.IsTutorialMode)
            {
                // 튜토리얼 모드: 튜토리얼 전용 데이터 사용
                ownedCount = TutorialManager.Instance.GetTutorialMonsterCount(currentMonster.enemyId);
            }
            else
            {
                // 일반 모드: 실제 데이터 사용
                ownedCount = currentMonster.owned;
            }

            detailOwnedText.text = $"보유량: {ownedCount}";
        }

        //체력
        if(detailHpText != null)
        {
            detailHpText.text = $"체력: {currentMonster.hp}";
        }

        // 공격력
        if (detailDamageText != null)
        {
            detailDamageText.text = $"공격력: {currentMonster.damage}";
        }

        // 코스트
        if (detailCostText != null)
        {
            detailCostText.text = $"코스트: {currentMonster.cost}";
        }

        // 속도
        if (detailSpeedText != null)
        {
            detailSpeedText.text = $"속도: {currentMonster.minSpeed} ~ {currentMonster.maxSpeed}";
        }

        // 스킬 설명
        if (detailSkillDescText != null)
        {
            detailSkillDescText.text = string.IsNullOrEmpty(currentMonster.ex)
                ? "스킬 설명 없음"
                : currentMonster.ex;
        }
    }

    /// <summary>
    /// 영문 종족명을 한글로 변환
    /// </summary>
    /// <param name="category">영문 종족명</param>
    /// <returns>한글 종족명</returns>
    private string GetRaceKoreanName(string category)
    {
        if (string.IsNullOrEmpty(category))
            return "알 수 없음";

        switch (category)
        {
            case "Ain":
                return "아인";
            case "Evil":
                return "마족";
            case "UnDead":
                return "언데드";
            case "UnHoly":
                return "타락";
            case "Suin":
                return "수인";
            case "Werewolf":
                return "늑대의 왕";
            case "Troll":
                return "트롤의 왕";
            case "Org":
                return "아인의 왕";
            case "Dragon":
                return "하늘의 왕";
            case "Tyranno":
                return "대지의 왕";
            default:
                return category; // 알 수 없는 종족은 원본 그대로 표시

        }
    }

    #endregion
}
