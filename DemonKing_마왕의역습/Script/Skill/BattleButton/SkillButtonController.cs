using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 버튼의 이름과 툴팁을 관리하는 컨트롤러
/// EventTrigger를 통해 마우스 이벤트를 처리합니다.
///
/// 사용 방법:
/// 1. 스킬 버튼 GameObject에 이 스크립트 추가
/// 2. buttonText를 Inspector에서 연결 (버튼 내부 Text 컴포넌트)
/// 3. UnitsUIManager에서 Setup() 메서드로 스킬 정보 전달
/// 4. EventTrigger 컴포넌트를 자동으로 추가하여 이벤트 처리
/// </summary>
[RequireComponent(typeof(EventTrigger))]
public class SkillButtonController : MonoBehaviour
{
    #region UI 컴포넌트
    [Header("버튼 UI 설정")]
    [Tooltip("스킬 이름을 표시할 Text 컴포넌트들 (모든 버튼 상태의 Text를 자동으로 찾음)")]
    private Text[] buttonTexts;
    #endregion

    #region 스킬 데이터
    private string skillName = "스킬";
    private string skillDescription = "스킬 설명이 없습니다.";
    private SkillTooltipManager tooltipManager;
    #endregion

    #region 초기화
    private void Awake()
    {
        // 모든 자식 오브젝트에서 Text 컴포넌트 찾기 (비활성화된 것도 포함)
        buttonTexts = GetComponentsInChildren<Text>(true);

        if (buttonTexts == null || buttonTexts.Length == 0)
        {
            Debug.LogWarning($"[SkillButtonController] {gameObject.name}의 자식 오브젝트에서 Text 컴포넌트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[SkillButtonController] {gameObject.name}에서 {buttonTexts.Length}개의 Text를 찾았습니다.");
        }

        // EventTrigger 설정
        SetupEventTrigger();
    }
    #endregion

    #region 스킬 정보 설정
    /// <summary>
    /// 스킬 정보를 설정하고 버튼 텍스트를 업데이트합니다.
    /// UnitsUIManager에서 유닛 선택 시 호출합니다.
    /// </summary>
    /// <param name="manager">툴팁 매니저 참조</param>
    /// <param name="name">스킬 이름</param>
    /// <param name="description">스킬 설명</param>
    public void Setup(SkillTooltipManager manager, string name, string description)
    {
        tooltipManager = manager;
        skillName = name;
        skillDescription = description;

        // 버튼 텍스트 갱신
        UpdateButtonText();
    }

    /// <summary>
    /// 모든 버튼 상태의 텍스트를 스킬 이름으로 변경합니다.
    /// (Normal/Selected/Disabled 버튼 모두 갱신)
    /// </summary>
    private void UpdateButtonText()
    {
        if (buttonTexts != null && buttonTexts.Length > 0)
        {
            foreach (Text text in buttonTexts)
            {
                if (text != null)
                {
                    text.text = skillName;
                }
            }
        }
    }
    #endregion

    #region EventTrigger 설정
    /// <summary>
    /// EventTrigger 컴포넌트를 자동으로 설정하여 마우스 이벤트를 처리합니다.
    /// PointerEnter와 PointerExit 이벤트를 등록합니다.
    /// </summary>
    private void SetupEventTrigger()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        // 기존 이벤트 초기화
        trigger.triggers.Clear();

        // PointerEnter 이벤트 추가 (마우스 올림)
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) => { OnPointerEnterHandler(); });
        trigger.triggers.Add(entryEnter);

        // PointerExit 이벤트 추가 (마우스 벗어남)
        EventTrigger.Entry entryExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) => { OnPointerExitHandler(); });
        trigger.triggers.Add(entryExit);
    }
    #endregion

    #region 마우스 이벤트 핸들러
    /// <summary>
    /// 마우스가 버튼 위에 올라왔을 때 툴팁을 표시합니다.
    /// </summary>
    private void OnPointerEnterHandler()
    {
        if (tooltipManager != null)
        {
            tooltipManager.ShowTooltip(skillDescription);
        }
    }

    /// <summary>
    /// 마우스가 버튼에서 벗어났을 때 툴팁을 숨깁니다.
    /// </summary>
    private void OnPointerExitHandler()
    {
        if (tooltipManager != null)
        {
            tooltipManager.HideTooltip();
        }
    }
    #endregion
}
