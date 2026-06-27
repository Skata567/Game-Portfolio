using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor;

/// <summary>
/// 스킬 정보 창을 관리하는 UI 매니저
/// - 스킬 버튼 클릭 시 해당 스킬의 세부 정보를 표시
/// - 정보 창에서 구매 업그레이드 진행
/// - 단일 책임 원칙: 스킬 정보 표시만 담당
///
/// 사용 방법:
/// 1. 스킬 정보 창 UI를 제작한 뒤 스크립트를 부착
/// 2. Inspector에서 UI 요소들을 연결
/// 3. SkillUpgrade에서 ShowSkillInfo() 메서드를 호출
///
/// UI 구조 예시:
/// SkillInfoPanel
/// └─── SkillNumberText (스킬 번호)
/// └─── SkillNameText (스킬 이름)
/// └─── SkillDescriptionText (스킬 설명)
/// └─── CostText (업그레이드 비용)
/// └─── UpgradeButton (업그레이드 버튼)
/// </summary>
public class SkillUIManager : MonoBehaviour
{
    [Header("UI 패널 설정")]
    [SerializeField] private GameObject skillInfoPanel;      // 스킬 정보 창 패널
    [SerializeField] private Text skillNumberText;           // 스킬 단계 텍스트
    [SerializeField] private Text skillNameText;             // 스킬 이름 텍스트
    [SerializeField] private Text skillDescriptionText;      // 스킬 설명 텍스트
    [SerializeField] private Text costText;                  // 업그레이드 비용 텍스트
    [SerializeField] private Text resourceTypeText;          // 필요 자원 타입 텍스트
    [SerializeField] private Button upgradeButton;           // 업그레이드 버튼

    [Header("외부 참조")]
    [SerializeField] private SkillDatabase skillDatabase;    // 스킬 정보 데이터베이스

    // 현재 표시 중인 스킬 정보
    private SkillInfo currentSkillInfo;
    private SkillType currentSkillType;
    private int currentSkillIndex;

    // 업그레이드 완료 시 콜백 액션
    private System.Action<SkillType, int> onUpgradeCallback;

    /// <summary>
    /// 초기화 메서드
    /// - UI 요소들을 자동으로 찾고 이벤트를 연결
    /// - SkillDatabase를 리소스에서 자동으로 찾기 시도
    /// </summary>
    void Start()
    {
        // 자동으로 UI 요소들을 찾기 (Inspector에서 설정하지 않은 경우)
        FindUIComponentsIfNull();

        // SkillDatabase 자동 찾기
        if (skillDatabase == null)
        {
            skillDatabase = Resources.Load<SkillDatabase>("SkillDatabase");
            if (skillDatabase == null)
            {
                Debug.LogError("[SkillInfoUIManager] SkillDatabase를 찾을 수 없습니다! Resources 폴더에 SkillDatabase를 배치하거나 Inspector에서 직접 연결하세요.");
            }
        }

        // 버튼 이벤트 연결
        SetupButtons();

        // 초기에는 정보 창을 숨김
        HideSkillInfo();
    }

    /// <summary>
    /// UI 컴포넌트들을 자동으로 찾는 메서드
    /// - Inspector에서 설정하지 않았을 때 자동으로 찾기 시도
    /// - 권장사항: Start()에서 한 번만 실행
    /// </summary>
    private void FindUIComponentsIfNull()
    {
        // 스킬 정보 패널이 없다면 자기 자신을 패널로 설정
        if (skillInfoPanel == null)
            skillInfoPanel = gameObject;

        // 각 UI 요소들을 자동으로 찾기
        if (skillNumberText == null)
            skillNumberText = transform.Find("SkillNumberText")?.GetComponent<Text>();

        if (skillNameText == null)
            skillNameText = transform.Find("SkillNameText")?.GetComponent<Text>();

        if (skillDescriptionText == null)
            skillDescriptionText = transform.Find("SkillExText")?.GetComponent<Text>();

        if (costText == null)
            costText = transform.Find("SkillCostText")?.GetComponent<Text>();

        if (resourceTypeText == null)
            resourceTypeText = transform.Find("SkillTypeText")?.GetComponent<Text>();

        if (upgradeButton == null)
            upgradeButton = GetComponentInChildren<Button>();


    }

    /// <summary>
    /// 버튼 이벤트를 설정하는 메서드
    /// - 업그레이드 버튼과 닫기 버튼의 이벤트를 연결
    /// </summary>
    private void SetupButtons()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
        else
        {
            Debug.LogWarning("[SkillInfoUIManager] 업그레이드 버튼을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 스킬 정보 창을 표시하는 메서드
    /// - SkillUpgrade에서 호출하여 특정 스킬의 정보를 표시
    /// </summary>
    /// <param name="skillType">표시할 스킬 타입</param>
    /// <param name="skillIndex">스킬 인덱스 (0-4)</param>
    /// <param name="upgradeCallback">업그레이드 완료 시 호출될 콜백</param>
    ///
    /// 사용 예시:
    /// skillInfoUI.ShowSkillInfo(SkillType.Ain, 0, (type, index) => {
    ///     Debug.Log($"{type} 스킬 {index} 업그레이드 완료!");
    /// });
    public void ShowSkillInfo(SkillType skillType, int skillIndex, System.Action<SkillType, int> upgradeCallback)
    {
        if (skillDatabase == null)
        {
            Debug.LogError("[SkillInfoUIManager] SkillDatabase가 설정되지 않았습니다!");
            return;
        }

        // 스킬 정보 가져오기
        currentSkillInfo = skillDatabase.GetSkillInfo(skillType, skillIndex);
        currentSkillType = skillType;
        currentSkillIndex = skillIndex;
        onUpgradeCallback = upgradeCallback;

        // UI 업데이트
        UpdateSkillInfoUI();

        // 패널 표시
        if (skillInfoPanel != null)
        {
            skillInfoPanel.SetActive(true);
            /*Debug.Log($"[SkillInfoUIManager] {skillType} 스킬 {skillIndex + 1}단계 정보 표시");*/
        }
        else
        {
           /* Debug.LogError("[SkillInfoUIManager] 스킬 정보 패널이 설정되지 않았습니다!");*/
        }
    }

    /// <summary>
    /// 스킬 정보 창을 숨기는 메서드
    /// - 정보 창을 닫고 현재 스킬 정보를 초기화
    /// </summary>
    public void HideSkillInfo()
    {
        if (skillInfoPanel != null)
        {
            skillInfoPanel.SetActive(false);
        }

        // 현재 정보 초기화
        currentSkillInfo = default;
        currentSkillType = default;
        currentSkillIndex = -1;
        onUpgradeCallback = null;

    }

    /// <summary>
    /// 스킬 정보 UI를 업데이트하는 메서드
    /// - 현재 스킬의 정보를 UI에 반영
    /// - 업그레이드 가능 여부에 따라 버튼 상태 변경
    /// </summary>
    private void UpdateSkillInfoUI()
    {
        // 스킬 번호 설정
        if (skillNumberText != null)
        {
            int skillLevel = currentSkillIndex + 1; // 1부터 시작
            string romanLevel = ToRomanNumeral(skillLevel);
            skillNumberText.text = $"{romanLevel}";
        }

        // 스킬 이름 설정
        if (skillNameText != null)
        {
            skillNameText.text = currentSkillInfo.skillName;
        }

        // 스킬 설명 설정
        if (skillDescriptionText != null)
        {
            skillDescriptionText.text = currentSkillInfo.description;
        }

        // 업그레이드 비용 설정
        if (costText != null)
        {
            costText.text = $"비용: {currentSkillInfo.cost}";
        }

        // 필요 자원 타입 설정
        if (resourceTypeText != null)
        {
            resourceTypeText.text = currentSkillInfo.resourceType;
        }

        // 업그레이드 버튼 상태 업데이트
        UpdateUpgradeButtonState();
    }

    /// <summary>
    /// 업그레이드 버튼의 상태를 업데이트하는 메서드
    /// - 플레이어의 자원과 스킬 해금 상태를 확인하여 버튼 활성화/비활성화
    /// </summary>
    private void UpdateUpgradeButtonState()
    {
        if (upgradeButton == null) return;

        // UIManager에서 플레이어 자원 정보 가져오기
        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[SkillInfoUIManager] UIManager를 찾을 수 없습니다!");
            upgradeButton.interactable = false;
            UpdateUpgradeButtonText("오류");
            return;
        }

        // SkillUpgrade에서 현재 스킬 레벨 확인
        SkillUpgrade skillUpgrade = FindAnyObjectByType<SkillUpgrade>();
        if (skillUpgrade == null)
        {
            Debug.LogError("[SkillInfoUIManager] SkillUpgrade를 찾을 수 없습니다!");
            upgradeButton.interactable = false;
            UpdateUpgradeButtonText("오류");
            return;
        }

        // 현재 스킬 레벨 가져오기
        int currentLevel = skillUpgrade.GetSkillLevel(currentSkillType);

        // 순차 해금 확인
        if (currentSkillIndex != currentLevel)
        {
            if (currentSkillIndex < currentLevel)
            {
                // 이미 업그레이드한 스킬
                upgradeButton.interactable = false;
                UpdateUpgradeButtonText("업그레이드완료");
            }
            else
            {
                // 아직 해금되지 않은 스킬
                upgradeButton.interactable = false;
                UpdateUpgradeButtonText("해금되지 않음");
            }
            return;
        }

        // 자원 확인
        bool canAfford = false;
        switch (currentSkillInfo.resourceType)
        {
            case "다이아몬드":
                canAfford = uiManager.playerDiamond >= currentSkillInfo.cost;
                break;
            case "미스릴":
                canAfford = uiManager.playerMithril >= currentSkillInfo.cost;
                break;
            default:
                Debug.LogWarning($"[SkillInfoUIManager] 알 수 없는 자원 타입: {currentSkillInfo.resourceType}");
                break;
        }

        // 버튼 상태 설정
        upgradeButton.interactable = canAfford;
        UpdateUpgradeButtonText(canAfford ? "업그레이드" : "자원 부족");
        if (canAfford)
            UpdateUpgradeButtonText("업그레이드");
        else
            UpdateUpgradeButtonText("자원 부족");
    }

    /// <summary>
    /// 업그레이드 버튼의 텍스트를 업데이트하는 메서드
    /// </summary>
    /// <param name="text">설정할 텍스트</param>
    private void UpdateUpgradeButtonText(string text)
    {
        if (upgradeButton != null)
        {
            Text buttonText = upgradeButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = text;

                switch (text)
                {
                case "업그레이드":
                    buttonText.color = Color.white;
                    break;
                case "업그레이드완료":
                    buttonText.color = Color.yellow;
                    break;
                case "해금되지 않음":
                        buttonText.color = Color.white;
                    break;
                case "자원 부족":
                    buttonText.color = Color.gray;
                    break;
                case "오류":
                    buttonText.color = Color.red;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 업그레이드 버튼 클릭 시 호출되는 메서드
    /// - 실제 업그레이드 로직은 SkillUpgrade에 위임
    /// - 업그레이드 완료 후 정보 창 업데이트 또는 닫기
    /// </summary>
    private void OnUpgradeButtonClicked()
    {
        if (onUpgradeCallback == null)
        {
            Debug.LogError("[SkillInfoUIManager] 업그레이드 콜백이 설정되지 않았습니다!");
            return;
        }


        // SkillUpgrade에 업그레이드 요청
        onUpgradeCallback.Invoke(currentSkillType, currentSkillIndex);

        // 업그레이드 완료 후 정보 창 닫기 (선택적)
        HideSkillInfo();
    }

    /// <summary>
    /// 외부에서 호출하여 UI를 새로고침하는 메서드
    /// - 플레이어 자원이 변경되었을 때 버튼 상태 업데이트
    /// </summary>
    public void RefreshUI()
    {
        if (skillInfoPanel != null && skillInfoPanel.activeInHierarchy)
        {
            UpdateUpgradeButtonState();
        }
    }

    /// <summary>
    /// 업그레이드 단계 숫자를 로마 숫자로 변환하는 메서드
    /// - 최대 10단계
    /// </summary>
    private string ToRomanNumeral(int number)
    {
        switch (number)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            case 4: return "IV";
            case 5: return "V";
            case 6: return "VI";
            case 7: return "VII";
            case 8: return "VIII";
            case 9: return "IX";
            case 10: return "X";
            default:
                Debug.LogWarning($"[SkillInfoUIManager] 지원하지 않는 로마숫자: {number}");
                return number.ToString(); // 범위 초과시 일반 숫자
        }
    }





    /*    /// <summary>
        /// 에디터에서 UI 요소들을 자동으로 찾아서 연결하는 메서드
        /// - Inspector에서 "Auto Find Components" 버튼으로 수동 호출
        /// </summary>
        [ContextMenu("Auto Find UI Components")]
        private void AutoFindComponents()
        {
            FindUIComponentsIfNull();
            Debug.Log("[SkillInfoUIManager] UI 컴포넌트 자동 찾기 완료!");
        }
    */

    /*#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용 메서드: Inspector에서 컴포넌트 자동 찾기 버튼 추가
        /// </summary>
        [UnityEditor.CustomEditor(typeof(SkillInfoUIManager))]
        public class SkillInfoUIManagerEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EditorGUILayout.Space();
                if (GUILayout.Button("Auto Find UI Components"))
                {
                    SkillInfoUIManager manager = (SkillInfoUIManager)target;
                    manager.AutoFindComponents();
                }
            }
        }
    #endif*/
}
