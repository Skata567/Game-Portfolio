using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬 도감 아이콘 컨트롤러 (몬스터 도감과 동일한 패턴)
/// 각 룬 아이콘의 버튼을 통해 클릭 시 상세정보 표시
/// 미획득 룬은 검정 실루엣으로 표시
///
/// 주요 기능:
/// - 2-Layer Silhouette Pattern: 미획득 룬 검정 실루엣 처리
/// - RuneDatabase 연동: 보유 여부 자동 확인
/// - 클릭 시 상세 정보 패널 표시
///
/// 작동 원리:
/// - runeIconImage: 항상 룬 아이콘 표시 (베이스)
/// - runeIconImage2: 획득 전 검정색, 획득 후 흰색 (실루엣 레이어)
/// </summary>
public class RuneCodexIcon : MonoBehaviour
{
    #region UI 컴포넌트들

    [Header("UI 요소들")]
    [SerializeField] private Image runeIconImage;        // 룬 아이콘 (1번째, 베이스)
    [SerializeField] private Image runeIconImage2;       // 룬 아이콘 (2번째, 실루엣 레이어)
    [SerializeField] private Button iconButton;          // 클릭 가능한 버튼

    #endregion

    #region Private Fields

    private RuneSO runeData;                             // 저장된 룬 데이터
    private RuneCodexManager codexManager;               // 룬 도감 매니저 참조
    private RuneDatabase runeDBCache;                    // 캐시된 RuneDatabase 참조 (성능 최적화)

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        FindUIComponents();
        InitializeRuneDatabaseReference();
    }

    #endregion

    #region 초기화

    /// <summary>
    /// RuneDatabase 참조를 초기화하는 메서드
    /// 싱글톤 인스턴스 캐싱으로 성능 최적화
    /// </summary>
    private void InitializeRuneDatabaseReference()
    {
        runeDBCache = RuneDatabase.Instance;

        if (runeDBCache == null)
        {
            Debug.LogError("[RuneCodexIcon] RuneDatabase 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// UI 컴포넌트들을 자동으로 찾는 메서드
    /// Inspector에서 할당되지 않은 경우 자동으로 찾아서 연결
    ///
    /// 작동 방식:
    /// - GetComponent/GetComponentInChildren을 사용하여 컴포넌트 자동 검색
    /// - null 체크로 중복 초기화 방지
    /// - 검정 실루엣 레이어는 "SilhouetteImage" 이름으로 검색
    ///
    /// 호출: Awake()에서 자동 호출됨
    /// </summary>
    private void FindUIComponents()
    {
        if (runeIconImage == null)
            runeIconImage = GetComponent<Image>();

        // 검정 실루엣 레이어 찾기 (이름으로 검색)
        if (runeIconImage2 == null)
        {
            // 방법 1: 이름으로 찾기 (프리팹에 "SilhouetteImage" 이름 필요)
            Transform silhouetteTransform = transform.Find("SilhouetteImage");
            if (silhouetteTransform != null)
            {
                runeIconImage2 = silhouetteTransform.GetComponent<Image>();
            }
            else
            {
                // 방법 2: 모든 Image 중 마지막 것 (검정 레이어가 보통 맨 위/마지막)
                Image[] images = GetComponentsInChildren<Image>(true); // includeInactive = true
                if (images.Length >= 2)
                {
                    // 마지막 Image를 검정 레이어로 가정
                    runeIconImage2 = images[images.Length - 1];
                    Debug.Log($"[RuneCodexIcon] 검정 레이어를 자동 검색: {images.Length}개 Image 중 마지막 사용");
                }
            }
        }

        if (iconButton == null)
            iconButton = GetComponent<Button>();
    }

    #endregion

    #region Public Setup Methods

    /// <summary>
    /// 룬 데이터를 설정하고 UI를 업데이트하는 메서드
    ///
    /// 작동 방식:
    /// 1. 이전 데이터 초기화
    /// 2. 새 룬 데이터 설정
    /// 3. UI 업데이트 (획득 여부에 따라 실루엣 처리)
    /// 4. 버튼 이벤트 연결
    ///
    /// 사용:
    /// var iconController = iconObject.GetComponent<RuneCodexIcon>();
    /// iconController.SetupRune(runeData, codexManager);
    /// </summary>
    /// <param name="rune">표시할 룬 데이터</param>
    /// <param name="manager">룬 도감 매니저</param>
    public void SetupRune(RuneSO rune, RuneCodexManager manager)
    {
        ClearData();

        runeData = rune;
        codexManager = manager;

        UpdateUI();
        SetupButton();

       /* Debug.Log($"[RuneCodexIcon] 룬 설정 완료: {rune?.runeName}");*/
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 이전 데이터를 모두 초기화하는 메서드
    /// 새로운 룬 설정 전 이전 상태와의 충돌 방지
    /// </summary>
    public void ClearData()
    {
        runeData = null;
        codexManager = null;
    }

    /// <summary>
    /// 룬 데이터에 따라 UI를 업데이트하는 메서드
    /// 획득 여부에 따라 검정 실루엣 또는 정상 표시
    ///
    /// 작동 원리:
    /// 1. RuneDatabase에서 소유 여부 또는 도감 등록 여부 확인
    /// 2. 아이콘 이미지 설정 (1번째 - 항상 표시)
    /// 3. 실루엣 레이어 색상 설정 (2번째 - 정보 미공개시 검정)
    /// 4. 이름 텍스트 설정 (정보 미공개시 "???" 표시)
    /// </summary>
    private void UpdateUI()
    {
        if (runeData == null)
        {
            Debug.LogWarning("[RuneCodexIcon] 룬 데이터가 null입니다!");
            return;
        }

        // RuneDatabase에서 소유 여부 확인
        bool isOwned = runeDBCache != null &&
                       runeDBCache.GetOwnedRunes() != null &&
                       runeDBCache.GetOwnedRunes().Contains(runeData);

        // 도감에 등록되어 있으면 정보 공개
        bool isDiscovered = runeDBCache != null && runeDBCache.IsDiscovered(runeData);

        // 보유 중이거나 도감에 등록된 경우 정보 표시
        bool showInfo = isOwned || isDiscovered;

        // 아이콘 이미지 설정 (1번째 이미지 - 항상 표시)
        if (runeIconImage != null && runeData.icon != null)
        {
            runeIconImage.sprite = runeData.icon;
        }
        else if (runeIconImage != null)
        {
            Debug.LogWarning($"[RuneCodexIcon] 아이콘이 없는 룬: {runeData.runeName}");
        }

        // 2번째 이미지 설정 (정보 미공개시 검정색 실루엣)
        if (runeIconImage2 != null)
        {
            if (runeData.icon != null)
            {
                runeIconImage2.sprite = runeData.icon;
            }

            // 정보 공개시 흰색, 미공개시 검정색
            runeIconImage2.color = showInfo ? Color.white : Color.black;
        }
    }

    /// <summary>
    /// 버튼 이벤트를 설정하는 메서드
    /// 클릭 시 룬 상세정보 창을 표시
    ///
    /// 작동 원리:
    /// 1. 기존 이벤트 리스너 제거
    /// 2. 룬 클릭 이벤트 리스너 추가
    /// 3. RuneCodexManager에 룬 데이터 전달
    /// </summary>
    private void SetupButton()
    {
        if (iconButton == null)
        {
            Debug.LogWarning("[RuneCodexIcon] 버튼이 null입니다!");
            return;
        }

        // 이전 이벤트 리스너 제거 (메모리 누수 방지)
        iconButton.onClick.RemoveAllListeners();

        // 룬 데이터가 유효한지 확인
        if (runeData != null)
        {
            if (codexManager == null)
            {
                Debug.LogWarning("[RuneCodexIcon] 룬 도감 매니저가 null입니다!");
                return;
            }

            iconButton.onClick.AddListener(() => {
                Debug.Log($"[RuneCodexIcon] 룬 클릭: {runeData.runeName}");
                codexManager.ShowRuneDetail(runeData);
            });
        }
        else
        {
            Debug.LogWarning("[RuneCodexIcon] 올바른 룬 데이터가 설정되지 않았습니다!");
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// UI를 강제로 다시 업데이트하는 메서드
    /// 외부에서 룬 획득 후 UI 갱신 시 호출
    ///
    /// 사용:
    /// iconController.RefreshUI(); // 룬 획득 시 호출
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }

    /// <summary>
    /// 현재 룬의 이름을 반환하는 메서드
    /// 디버그 용도로 사용
    /// </summary>
    public string GetRuneName()
    {
        return runeData?.runeName ?? "알 수 없음";
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그용 메서드: 현재 아이콘의 상태를 콘솔에 출력
    /// Inspector에서 우클릭 메뉴로 수동 호출
    ///
    /// 호출: Inspector에서 우클릭 후 Debug Icon Status 선택
    /// </summary>
    [ContextMenu("Debug Icon Status")]
    public void DebugIconStatus()
    {
        Debug.Log("=== RuneCodexIcon 상태 ===");
        Debug.Log($"이름: {GetRuneName()}");
        Debug.Log($"아이콘 이미지: {runeIconImage != null}");
        Debug.Log($"실루엣 이미지: {runeIconImage2 != null}");
        Debug.Log($"버튼: {iconButton != null}");
        Debug.Log($"도감 매니저: {codexManager != null}");
        Debug.Log($"RuneDatabase 캐시: {runeDBCache != null}");

        if (runeData != null)
        {
            Debug.Log($"아이콘 스프라이트: {runeData.icon != null}");
            Debug.Log($"ID: {runeData.runeId}");
            Debug.Log($"등급: {runeData.rarity}");

            if (runeDBCache != null)
            {
                bool isOwned = runeDBCache.GetOwnedRunes().Contains(runeData);
                Debug.Log($"보유 여부: {isOwned}");
            }
        }
    }

    #endregion
}
