using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특성 패널의 선택 표시와 강화 버튼 상태를 갱신하는 UI 전용 컨트롤러입니다.
/// 실제 포인트 소비와 효과 적용은 TraitController가 담당하고, 이 클래스는 화면 반영만 담당합니다.
/// </summary>
public class TraitUIController : MonoBehaviour
{
    [Serializable]
    public class TraitButtonEntry
    {
        [Header("버튼 연결")]
        [Tooltip("이 버튼을 눌렀을 때 선택할 특성 ID입니다. 오른쪽 9칸 아이콘 순서에 맞게 지정하세요.")]
        public TraitId traitId;

        [Tooltip("오른쪽 특성 아이콘의 Button 컴포넌트입니다. 클릭하면 상세 패널이 이 특성으로 바뀝니다.")]
        public Button button;

        [Tooltip("오른쪽 특성 아이콘의 Image 컴포넌트입니다. 상세 패널 아이콘도 이 이미지를 우선 사용합니다.")]
        public Image iconImage;
    }

    [Header("컨트롤러 참조")]
    [Tooltip("특성 레벨/포인트를 관리하는 컨트롤러입니다. 비워두면 씬에서 자동으로 찾지만, 가능하면 직접 연결하는 편이 안전합니다.")]
    [SerializeField] private TraitController traitController;

    [Tooltip("오른쪽 특성 아이콘 버튼 목록입니다. 9개 특성을 모두 넣고, 각 항목의 TraitId를 GDD 순서에 맞게 지정하세요.")]
    [SerializeField] private TraitButtonEntry[] traitButtons;

    [Header("상세 표시")]
    [Tooltip("왼쪽 상세 영역에 표시되는 큰 특성 아이콘입니다. 선택한 특성 아이콘으로 자동 교체됩니다.")]
    [SerializeField] private Image selectedTraitIcon;

    [Tooltip("왼쪽 상세 영역에 표시되는 특성 이름 텍스트입니다.")]
    [SerializeField] private TMP_Text selectedTraitNameText;

    [Tooltip("왼쪽 설명칸 텍스트입니다. 1/2레벨 효과를 합친 로컬라이즈 텍스트를 표시합니다.")]
    [SerializeField] private TMP_Text selectedTraitDescriptionText;

    [Tooltip("하단 스킬 포인트 숫자 텍스트입니다. 선택한 특성의 티어 포인트만 표시합니다.")]
    [SerializeField] private TMP_Text skillPointText;

    [Header("레벨 별 표시")]
    [Tooltip("0/1/2레벨을 한 이미지로 표현할 별 Image입니다. 0=빈칸, 1=반칸, 2=꽉찬 별로 바뀝니다.")]
    [SerializeField] private Image traitLevelStarImage;

    [Tooltip("특성 0레벨일 때 표시할 빈 별 스프라이트입니다.")]
    [SerializeField] private Sprite emptyStarSprite;

    [Tooltip("특성 1레벨일 때 표시할 반 별 스프라이트입니다.")]
    [SerializeField] private Sprite halfStarSprite;

    [Tooltip("특성 2레벨일 때 표시할 꽉 찬 별 스프라이트입니다.")]
    [SerializeField] private Sprite fullStarSprite;

    [Header("업그레이드")]
    [Tooltip("선택한 특성을 올리는 버튼입니다. 포인트가 없거나 최대 레벨이면 자동으로 비활성화됩니다.")]
    [SerializeField] private Button upgradeButton;

    private TraitId _selectedTraitId;
    private bool _hasSelection;

    private void Awake()
    {
        if (traitController == null)
            traitController = TraitController.FindAvailableInstance();

        // 버튼 클릭 연결은 Awake에서 한 번만 한다.
        // OnEnable마다 다시 등록하면 패널을 열 때마다 클릭 이벤트가 중복 실행될 수 있다.
        RegisterTraitButtons();

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(UpgradeSelectedTrait);
    }

    private void OnEnable()
    {
        if (traitController == null)
            traitController = TraitController.FindAvailableInstance();

        if (traitController != null)
            traitController.OnTraitChanged += Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;

        // 패널을 처음 열었을 때 아무것도 선택되지 않은 빈 화면이 나오지 않도록 첫 번째 특성을 자동 선택한다.
        SelectInitialTrait();
        Refresh();
    }

    private void OnDisable()
    {
        // 비활성화 때 이벤트 구독을 풀어야 패널을 여러 번 열고 닫아도 Refresh가 중복 호출되지 않는다.
        if (traitController != null)
            traitController.OnTraitChanged -= Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
    }

    public void SelectTrait(TraitId traitId)
    {
        // 아이콘 클릭은 먼저 "선택"만 한다.
        // 클릭 즉시 강화까지 하면 설명을 읽으려는 입력과 포인트 소비 입력이 섞여 실수하기 쉽다.
        _selectedTraitId = traitId;
        _hasSelection = true;
        Refresh();
    }

    public void UpgradeSelectedTrait()
    {
        if (!_hasSelection || traitController == null)
            return;

        // 실제 성공/실패 판정은 TraitController가 담당한다.
        // UI 쪽에서 포인트를 직접 줄이면 다른 시스템과 상태가 어긋날 수 있다.
        traitController.TryUpgradeTrait(_selectedTraitId);
    }

    private void RegisterTraitButtons()
    {
        if (traitButtons == null)
            return;

        for (int i = 0; i < traitButtons.Length; i++)
        {
            TraitButtonEntry entry = traitButtons[i];
            if (entry == null || entry.button == null)
                continue;

            TraitId capturedId = entry.traitId;
            // 루프 변수 i를 직접 캡처하지 않고 traitId 값을 따로 잡아둔다.
            // 버튼을 누를 때 모든 버튼이 마지막 항목을 가리키는 클로저 문제를 막기 위해서다.
            entry.button.onClick.AddListener(() => SelectTrait(capturedId));
        }
    }

    private void SelectInitialTrait()
    {
        if (_hasSelection)
            return;

        if (traitButtons != null)
        {
            for (int i = 0; i < traitButtons.Length; i++)
            {
                if (traitButtons[i] == null)
                    continue;

                SelectTrait(traitButtons[i].traitId);
                return;
            }
        }

        SelectTrait(TraitId.Appraisal);
    }

    private void Refresh()
    {
        if (!_hasSelection || traitController == null)
            return;

        TraitData data = traitController.GetTraitData(_selectedTraitId);
        if (data == null)
            return;

        int level = traitController.GetTraitLevel(_selectedTraitId);
        // 버튼에 연결한 아이콘을 상세 패널의 기준으로 쓴다.
        // TraitData.icon이 잘못 연결되어 있어도 실제 UI 버튼과 상세 아이콘이 서로 어긋나지 않게 하기 위해서다.
        Sprite buttonIcon = FindButtonIcon(_selectedTraitId);
        Sprite icon = buttonIcon != null ? buttonIcon : data.icon;

        if (selectedTraitIcon != null)
        {
            selectedTraitIcon.sprite = icon;
            selectedTraitIcon.enabled = icon != null;
        }

        SplitDisplayText(traitController.GetTraitDisplayText(_selectedTraitId), out string name, out string description);

        if (selectedTraitNameText != null)
            selectedTraitNameText.text = name;

        if (selectedTraitDescriptionText != null)
            selectedTraitDescriptionText.text = description;

        if (skillPointText != null)
            skillPointText.text = traitController.GetPoint(data.tier).ToString();

        RefreshStar(level);
        RefreshUpgradeButton(data, level);
    }

    private void RefreshStar(int level)
    {
        if (traitLevelStarImage == null)
            return;

        // 사용자가 준비한 빈칸/반칸/꽉찬 별 스프라이트를 0/1/2레벨에 매핑한다.
        // 별 오브젝트 여러 개를 직접 켜고 끄는 방식보다 현재 UI 구조에서는 연결 실수가 적다.
        traitLevelStarImage.sprite = level switch
        {
            0 => emptyStarSprite,
            1 => halfStarSprite,
            _ => fullStarSprite
        };

        traitLevelStarImage.enabled = traitLevelStarImage.sprite != null;
    }

    private void RefreshUpgradeButton(TraitData data, int level)
    {
        if (upgradeButton == null || traitController == null || data == null)
            return;

        // 강화 가능 여부를 버튼 상태로 보여줘서 포인트가 없을 때 불필요한 클릭 피드백을 줄인다.
        bool canUpgrade = level < 2 && traitController.GetPoint(data.tier) > 0;
        upgradeButton.interactable = canUpgrade;
    }

    private void SplitDisplayText(string fullText, out string name, out string description)
    {
        // LocalizationManager 텍스트는 "이름\n레벨1 효과, 레벨2 효과" 형태로 합쳐져 있어 첫 줄만 이름으로 떼어낸다.
        if (string.IsNullOrEmpty(fullText))
        {
            name = string.Empty;
            description = string.Empty;
            return;
        }

        int newlineIndex = fullText.IndexOf('\n');
        if (newlineIndex < 0)
        {
            name = fullText;
            description = string.Empty;
            return;
        }

        name = fullText.Substring(0, newlineIndex);
        description = fullText.Substring(newlineIndex + 1);
    }

    private Sprite FindButtonIcon(TraitId traitId)
    {
        if (traitButtons == null)
            return null;

        for (int i = 0; i < traitButtons.Length; i++)
        {
            TraitButtonEntry entry = traitButtons[i];
            if (entry != null && entry.traitId == traitId && entry.iconImage != null)
                return entry.iconImage.sprite;
        }

        return null;
    }
}
