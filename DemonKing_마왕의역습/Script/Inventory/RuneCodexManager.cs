using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 룬 도감 UI 매니저 (몬스터 도감과 동일한 패턴)
/// 룬 도감 표시 및 상세정보 창 관리
///
/// 주요 기능:
/// - 모든 룬 표시 (획득/미획득 모두)
/// - 미획득 룬은 검정 실루엣 + 정보 숨김
/// - 클릭 시 상세 정보 패널 표시
/// - RuneDatabase와 연동하여 보유 여부 확인
/// </summary>
public class RuneCodexManager : MonoBehaviour
{
    #region UI Components

    [Header("룬 도감 UI")]
    [SerializeField] private Transform runeGridParent;           // 룬 아이콘 그리드 부모
    [SerializeField] private GameObject runeIconPrefab;          // 룬 아이콘 프리팹

    [Header("상세 정보 패널")]
    [SerializeField] private GameObject detailPanel;             // 상세 정보 패널
    [SerializeField] private Image detailRuneImage;              // 룬 아이콘 이미지
    [SerializeField] private Text detailRuneNameText;            // 룬 이름
    [SerializeField] private Text detailRarityText;              // 등급
    [SerializeField] private Text detailDescriptionText;         // 효과 설명
    [SerializeField] private Text detailDropRuneText;            // 등장 위치 설명 텍스트
    [SerializeField] private Text detialRuneOwendText;           // 보유 여부 텍스트
    [Header("필요 없는 설명 끄기")] 
    [SerializeField] private Text detailText1;
    [SerializeField] private Text detailText2;
    [SerializeField] private Text detailText3;

    #endregion

    #region Private Fields

    private RuneDatabase runeDBCache;                            // 캐시된 RuneDatabase 참조
    private RuneSO currentDetailRune;                            // 현재 상세정보 표시 중인 룬

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeServices();
        InitializeDetailPanel();
        UpdateFullCodexUI();
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 필요한 서비스들 초기화
    /// RuneDatabase 인스턴스 캐싱
    /// </summary>
    private void InitializeServices()
    {
        runeDBCache = RuneDatabase.Instance;

        if (runeDBCache == null)
        {
            Debug.LogError("[RuneCodexManager] RuneDatabase 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 상세정보 창 초기 설정
    /// 시작 시 숨김 처리
    /// </summary>
    private void InitializeDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    #endregion

    #region Public Methods - Codex Update

    /// <summary>
    /// 전체 룬 도감 UI 업데이트
    /// 모든 룬을 표시 (획득/미획득 모두)
    ///
    /// 작동 방식:
    /// 1. 기존 아이콘들 제거 (ClearCodex)
    /// 2. RuneDatabase에서 모든 룬 가져오기
    /// 3. 각 룬마다 아이콘 생성
    ///
    /// 호출 시점:
    /// - Start()에서 최초 호출
    /// - 룬 획득 후 갱신 시 호출
    /// </summary>
    public void UpdateFullCodexUI()
    {
        if (runeGridParent == null) return;

        Debug.Log("=== 전체 룬 도감 업데이트 시작 ===");
        ClearCodex();

        // RuneDatabase에서 모든 룬 가져오기
        AddAllRunesToCodex();

        Debug.Log("=== 전체 룬 도감 업데이트 완료 ===");
    }

    #endregion

    #region Private Methods - Rune Creation

    /// <summary>
    /// 모든 룬을 도감에 추가 (모든 룬 표시, 미획득 룬 포함)
    ///
    /// 작동 방식:
    /// 1. RuneDatabase에서 전체 룬 목록 가져오기
    /// 2. 각 룬마다 CreateRuneIcon() 호출
    /// 3. 획득/미획득 여부는 RuneCodexIcon에서 자동 처리
    /// </summary>
    private void AddAllRunesToCodex()
    {
        if (runeDBCache == null || runeIconPrefab == null) return;

        // RuneDatabase의 전체 룬 개수 확인
        int totalRuneCount = runeDBCache.GetTotalRuneCount();
        Debug.Log($"전체 룬 개수: {totalRuneCount}");

        // Resources/Runes 폴더에서 모든 룬 로드
        RuneSO[] allRunes = Resources.LoadAll<RuneSO>(runeDBCache.runesFolderPath);

        // 등급 순 정렬 (일반 → 희귀 → 영웅 → 전설)
        var runeList = new System.Collections.Generic.List<RuneSO>(allRunes);
        runeList.Sort((a, b) => a.rarity.CompareTo(b.rarity));

        RuneSO firstRune = null;

        foreach (var rune in runeList)
        {
            if (rune != null)
            {
                // 첫 번째 룬 저장
                if (firstRune == null)
                {
                    firstRune = rune;
                }

                CreateRuneIcon(rune);
            }
        }

        Debug.Log($"[RuneCodexManager] {allRunes.Length}개 룬 아이콘 생성 완료");

        // 첫 번째 룬 자동 선택 (상세 정보 표시)
        if (firstRune != null)
        {
            ShowRuneDetail(firstRune);
            Debug.Log($"[RuneCodexManager] 첫 번째 룬 자동 선택: {firstRune.runeName}");
        }
    }

    /// <summary>
    /// 룬 아이콘 생성
    ///
    /// 작동 방식:
    /// 1. 프리팹 인스턴스화
    /// 2. RuneCodexIcon 컴포넌트 가져오기
    /// 3. SetupRune() 호출하여 데이터 설정
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    private void CreateRuneIcon(RuneSO rune)
    {
        GameObject iconObj = Instantiate(runeIconPrefab, runeGridParent);
        var iconController = iconObj.GetComponent<RuneCodexIcon>();

        if (iconController != null)
        {
            iconController.SetupRune(rune, this);
        }
        else
        {
            Debug.LogWarning($"[RuneCodexManager] RuneCodexIcon 컴포넌트를 찾을 수 없습니다: {rune.runeName}");
        }
    }

    #endregion

    #region Public Methods - Detail Display

    /// <summary>
    /// 룬 상세정보 창 표시
    /// 획득 여부에 따라 정보 표시/숨김 처리
    ///
    /// 작동 방식:
    /// 1. 현재 룬 데이터 저장
    /// 2. UpdateDetailUI() 호출하여 UI 업데이트
    /// 3. 상세 패널 활성화
    ///
    /// 호출 시점:
    /// - RuneCodexIcon에서 룬 클릭 시 호출
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    public void ShowRuneDetail(RuneSO rune)
    {
        if (detailPanel == null || rune == null) return;

        currentDetailRune = rune;

        UpdateDetailUI(rune);
        detailPanel.SetActive(true);

        Debug.Log($"[RuneCodexManager] 룬 상세정보 표시: {rune.runeName}");
        Debug.Log($"[RuneInventoryPanel] {rune.runeName}: {(int)rune.rarity}");
    }

    /// <summary>
    /// 상세정보 창 닫기
    ///
    /// 작동 방식:
    /// 1. 상세 패널 비활성화
    /// 2. 현재 룬 데이터 초기화
    /// </summary>
    public void HideRuneDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
        currentDetailRune = null;

        Debug.Log("[RuneCodexManager] 룬 상세정보 닫기");
    }

    #endregion

    #region Private Methods - Detail UI Update

    /// <summary>
    /// 상세정보 UI 업데이트 (획득/미획득 여부에 따라 정보 숨김 처리)
    ///
    /// 작동 원리:
    /// 1. RuneDatabase에서 보유 여부 확인
    /// 2. 획득 룬 또는 도감에 등록된 룬: 모든 정보 표시
    /// 3. 미획득 + 미등록 룬: 이름/스탯/설명 ???로 표시, 이미지 검정 실루엣
    ///
    /// SOLID 원칙: 단일 책임 원칙 (UI 업데이트만 담당)
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    private void UpdateDetailUI(RuneSO rune)
    {
        // RuneDatabase에서 보유 여부 또는 도감 등록 여부 확인
        bool isOwned = runeDBCache != null &&
                       runeDBCache.GetOwnedRunes() != null &&
                       runeDBCache.GetOwnedRunes().Contains(rune);

        // 도감에 등록되어 있으면 정보 공개
        bool isDiscovered = runeDBCache != null && runeDBCache.IsDiscovered(rune);

        // 보유 중이거나 도감에 등록된 경우 정보 표시
        bool showInfo = isOwned || isDiscovered;

        // 룬 아이콘 이미지 설정
        if (detailRuneImage != null && rune.icon != null)
        {
            detailRuneImage.sprite = rune.icon;
            // 정보 공개시 흰색, 미공개시 검정색
            detailRuneImage.color = showInfo ? Color.white : Color.black;
        }

        // 정보 미공개 룬은 이름을 ???로 표시
        if (detailRuneNameText != null)
            detailRuneNameText.text = showInfo ? rune.runeName : "???";

        if(detialRuneOwendText != null)
        {
            if (detailPanel.activeSelf)
            {
                if (isOwned)
                {
                    detialRuneOwendText.text = "보유 중";
                }
                else
                {
                    detialRuneOwendText.text = "미보유";
                }
            }
            else
            {
                Debug.LogWarning("[RuneCodexManager] 상세 패널이 비활성화된 상태에서 보유 여부 텍스트를 설정하려고 시도했습니다.");
            }
        }

        // 정보 미공개 룬은 등급을 ???로 표시
        if (detailRarityText != null)
        {
            if (showInfo)
            {
                string rarityKorean = GetRarityKoreanName(rune.rarity);
                detailRarityText.text = $"등급: {rarityKorean}";
                detailRarityText.color = GetRarityColor(rune.rarity);
            }
            else
            {
                detailRarityText.text = "등급: ???";
                detailRarityText.color = Color.gray;
            }
        }

        // 정보 미공개 룬은 설명을 숨김
        if (detailDescriptionText != null)
        {
            if (showInfo)
            {
                detailDescriptionText.text = string.IsNullOrEmpty(rune.description)
                    ? "설명이 없습니다."
                    : rune.description;
            }
            else
            {
                detailDescriptionText.text = "아직 발견하지 못한 룬입니다.";
            }
        }

        // 정보 미공개 룬의 등장 위치 설명 숨기기
        if(detailDropRuneText != null )
        {
            if(showInfo)
            {
                string dropKorean = GetDropKoreanName(rune.rarity);
                detailDropRuneText.text = $"획득처: {dropKorean}";
            }
            else
            {
                detailDropRuneText.text = "획득처: ???";
            }
        }


        // 필요 없는 텍스트 지우기 
        if (detailText1 != null || detailText2 != null || detailText3 != null)
        {
            detailText1.text = "";
            detailText2.text = "";
            detailText3.text = "";
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

    private string GetDropKoreanName(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return "모집";
            case RuneRarity.Rare:
                return "상점,엘리트방";
            case RuneRarity.Epic:
                return "상점,엘리트방";
            case RuneRarity.Legendary:
                return "무작위방";
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

    #region Private Methods - Utilities

    /// <summary>
    /// 기존 룬 도감 아이콘들 제거
    ///
    /// 작동 방식:
    /// - runeGridParent의 모든 자식 오브젝트 삭제
    /// - 메모리 누수 방지
    /// </summary>
    private void ClearCodex()
    {
        if (runeGridParent == null) return;

        foreach (Transform child in runeGridParent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("[RuneCodexManager] 기존 룬 아이콘 전체 삭제 완료");
    }
    public void PclearCodex() // 외부에서 호출할 함수
    {
        ClearCodex();
    }
    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그용 메서드: 현재 도감의 상태를 콘솔에 출력
    /// Inspector에서 우클릭 메뉴로 수동 호출
    ///
    /// 호출: Inspector에서 우클릭 후 Debug Codex Status 선택
    /// </summary>
    [ContextMenu("Debug Codex Status")]
    public void DebugCodexStatus()
    {
        Debug.Log("=== RuneCodexManager 상태 ===");
        Debug.Log($"RuneDatabase 캐시: {runeDBCache != null}");
        Debug.Log($"룬 그리드 부모: {runeGridParent != null}");
        Debug.Log($"룬 아이콘 프리팹: {runeIconPrefab != null}");
        Debug.Log($"상세 패널: {detailPanel != null}");

        if (runeDBCache != null)
        {
            int totalRunes = runeDBCache.GetTotalRuneCount();
            int ownedRunes = runeDBCache.GetOwnedRunes().Count;
            Debug.Log($"전체 룬 개수: {totalRunes}");
            Debug.Log($"보유 룬 개수: {ownedRunes}");
        }

        if (runeGridParent != null)
        {
            Debug.Log($"현재 표시 중인 아이콘 개수: {runeGridParent.childCount}");
        }

        if (currentDetailRune != null)
        {
            Debug.Log($"현재 상세정보 표시 중인 룬: {currentDetailRune.runeName}");
        }
    }

    #endregion
}
