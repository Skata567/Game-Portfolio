using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 버튼에 마우스를 올렸을 때 스킬 설명을 표시하는 툴팁 매니저
/// UnitsUIManager에 종속되어 배틀 씬에서만 사용됩니다.
///
/// 사용 방법:
/// 1. UnitsUIManager에 이 스크립트 컴포넌트 추가
/// 2. tooltipPanel, tooltipText를 Inspector에서 연결
/// 3. SkillButtonController에서 ShowTooltip/HideTooltip 호출
/// </summary>
public class SkillTooltipManager : MonoBehaviour
{
    #region 툴팁 UI 컴포넌트
    [Header("툴팁 UI 설정")]
    [Tooltip("반투명 검정 배경 패널")]
    [SerializeField] private GameObject tooltipPanel;

    [Tooltip("흰색 텍스트 (스킬 설명)")]
    [SerializeField] private Text tooltipText;

    [Tooltip("마우스 커서로부터의 오프셋 (픽셀 단위)")]
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);
    #endregion

    #region 초기화
    private void Awake()
    {
        // 초기 상태 비활성화
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    #endregion

    #region 툴팁 표시/숨김 메서드
    /// <summary>
    /// 툴팁을 표시하고 설명 텍스트를 설정합니다.
    /// </summary>
    /// <param name="description">스킬 설명 텍스트</param>
    public void ShowTooltip(string description)
    {
        if (tooltipPanel == null || tooltipText == null)
        {
            Debug.LogWarning("[SkillTooltipManager] tooltipPanel 또는 tooltipText가 할당되지 않았습니다!");
            return;
        }

        tooltipText.text = description;
        tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// 툴팁을 숨깁니다.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    #endregion

    #region 마우스 위치 추적
    /// <summary>
    /// 매 프레임 마우스 위치를 추적하여 툴팁을 따라다니게 합니다.
    /// LateUpdate를 사용하여 다른 UI 업데이트 이후 실행됩니다.
    /// 화면 경계를 체크하여 툴팁이 화면 밖으로 나가지 않도록 합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector3 mousePosition = Input.mousePosition;
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

            if (tooltipRect == null)
            {
                // RectTransform이 없으면 기본 동작
                tooltipPanel.transform.position = mousePosition + new Vector3(offset.x, offset.y, 0f);
                return;
            }

            // 툴팁 크기 가져오기
            float tooltipWidth = tooltipRect.rect.width;
            float tooltipHeight = tooltipRect.rect.height;

            // 기본 오프셋 적용
            Vector3 targetPosition = mousePosition + new Vector3(offset.x, offset.y, 0f);

            // 화면 경계 체크 및 조정
            // 오른쪽 경계 체크
            if (targetPosition.x + tooltipWidth > Screen.width)
            {
                targetPosition.x = mousePosition.x - tooltipWidth - Mathf.Abs(offset.x);
            }

            // 왼쪽 경계 체크
            if (targetPosition.x < 0)
            {
                targetPosition.x = mousePosition.x + Mathf.Abs(offset.x);
            }

            // 하단 경계 체크 (화면 아래로 나가면 위로 표시)
            if (targetPosition.y - tooltipHeight < 0)
            {
                targetPosition.y = mousePosition.y + tooltipHeight + Mathf.Abs(offset.y);
            }

            // 상단 경계 체크
            if (targetPosition.y > Screen.height)
            {
                targetPosition.y = mousePosition.y - Mathf.Abs(offset.y);
            }

            tooltipPanel.transform.position = targetPosition;
        }
    }
    #endregion
}
