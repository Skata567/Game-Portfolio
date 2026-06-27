using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가이드 패널 관리 스크립트
/// 튜토리얼 페이지를 순차적으로 표시
/// </summary>
public class GuidePanel : MonoBehaviour
{
    #region UI 컴포넌트들

    [Header("페이지")]
    public GameObject[] pageObject;    // 인스펙터에서 설정하는 페이지 배열

    [Header("UI 요소들")]
    public GameObject HelpPanel;
    public Text titleText;          // 타이틀 UI 텍스트
    public Text pageText;           // 페이지 번호 텍스트 (예: "1 / 5")
    public Button nextbutton;       // 다음 버튼
    public Button beforbutton;      // 이전 버튼

    private int PageIndex = -1;

    #endregion

    #region Private Fields

    private int currentPage = 0;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        ShowPage(0);
        UpdateButtonStates();
    }

    #endregion

    #region Page Navigation

    /// <summary>
    /// 특정 페이지를 표시
    /// </summary>
    /// <param name="pageNumber">표시할 페이지 번호 (0부터 시작)</param>
    private void ShowPage(int pageNumber)
    {
        // 배열 범위 체크
        if (pageObject == null || pageObject.Length == 0)
        {
            Debug.LogError("[GuidePanel] pages 배열이 비어있습니다!");
            return;
        }

        if (pageNumber < 0 || pageNumber >= pageObject.Length)
        {
            Debug.LogError($"[GuidePanel] 페이지 인덱스 초과: {pageNumber}/{pageObject.Length}");
            return;
        }

        if(pageObject != null)
        {
            for (int i = 0; i < pageObject.Length; i++)
            {
                pageObject[i].SetActive(false);
            }

            if (PageIndex != -1 && PageIndex != pageNumber)
            {
                pageObject[PageIndex].SetActive(false);
            }
            pageObject[pageNumber].SetActive(true);
            PageIndex = pageNumber;
        } 

        // currentPage 업데이트
        currentPage = pageNumber;

        if (pageText != null)
            pageText.text = $"{currentPage + 1} / {pageObject.Length}";
    }

    /// <summary>
    /// 다음 페이지로 이동
    /// </summary>
    public void NextPage()
    {
        if (currentPage < pageObject.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// 이전 페이지로 이동
    /// </summary>
    public void BeforePage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }

        UpdateButtonStates();
    }

    #endregion

    #region Button State Management

    /// <summary>
    /// 버튼 상태 업데이트 (첫 페이지/마지막 페이지에서 버튼 비활성화)
    /// </summary>
    private void UpdateButtonStates()
    {
        if (nextbutton == null || beforbutton == null)
        {
            Debug.LogWarning("[GuidePanel] 버튼이 할당되지 않았습니다!");
            return;
        }

        // 다음 버튼 상태
        if (currentPage < pageObject.Length - 1)
        {
            nextbutton.image.color = Color.white;
            nextbutton.interactable = true;
        }
        else  // 마지막 페이지
        {
            nextbutton.image.color = Color.gray;
            nextbutton.interactable = false;
        }

        // 이전 버튼 상태
        if (currentPage > 0)
        {
            beforbutton.image.color = Color.white;
            beforbutton.interactable = true;
        }
        else  // 첫 페이지
        {
            beforbutton.image.color = Color.gray;
            beforbutton.interactable = false;
        }
    }

    #endregion

    public void OffHelpPannel()
    {
        HelpPanel.SetActive(false);
    }
}
