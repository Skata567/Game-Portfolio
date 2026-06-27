using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 룬 상점 관리 서비스
/// 등급별로 다른 상점에서 룬을 판매합니다
/// - 일반 상점: Common 등급 고정 4개
/// - 교환소: Rare 등급 랜덤 5개 (층수 변경 시 새로고침)
/// - 특수 교환소: Epic 등급 랜덤 3개 (특수 교환소 열릴 때 새로고침)
/// </summary>
public class RuneShopService : MonoBehaviour
{
    #region Singleton

    private static RuneShopService instance;
    public static RuneShopService Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<RuneShopService>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("RuneShopService");
                    instance = obj.AddComponent<RuneShopService>();
                    // 즉시 초기화 (Start를 기다리지 않음)
                    instance.InitializeImmediately();
                }
            }
            return instance;
        }
    }

    #endregion

    #region Fields

    [Header("룬 가격 설정 (다이아몬드)")]
    [SerializeField] private int commonRunePrice = 10;      // 커먼 룬 가격
    [SerializeField] private int rareRunePrice = 30;        // 레어 룬 가격
    [SerializeField] private int epicRunePrice = 80;        // 에픽 룬 가격

    [Header("상점 룬 개수 설정")]
    [SerializeField] private int normalShopRuneCount = 4;   // 일반 상점 커먼 룬 개수
    [SerializeField] private int exchangeShopRuneCount = 5; // 교환소 레어 룬 개수
    [SerializeField] private int secretShopRuneCount = 3;   // 특수 교환소 에픽 룬 개수

    // 현재 상점별 룬 목록
    private List<RuneSO> normalShopRunes = new List<RuneSO>();      // 일반 상점 (고정)
    private List<RuneSO> exchangeShopRunes = new List<RuneSO>();    // 교환소 (랜덤)
    private List<RuneSO> secretShopRunes = new List<RuneSO>();      // 특수 교환소 (랜덤)

    // 전체 룬 풀 (등급별)
    private List<RuneSO> allCommonRunes = new List<RuneSO>();
    private List<RuneSO> allRareRunes = new List<RuneSO>();
    private List<RuneSO> allEpicRunes = new List<RuneSO>();

    // 층수 추적용
    private int lastFloor = -1;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Awake에서 이미 초기화했는지 확인
        if (allCommonRunes.Count == 0)
        {
            InitializeImmediately();
        }
    }

    void Update()
    {
        // 층수가 변경되면 교환소 새로고침
        CheckFloorChange();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 즉시 초기화 (싱글톤 생성 시 호출)
    /// </summary>
    private void InitializeImmediately()
    {
        InitializeRunePools();
        InitializeNormalShop();
        RefreshExchangeShop();
        RefreshSecretShop();  // 특수교환소도 초기화
    }

    /// <summary>
    /// Resources/Runes 폴더에서 등급별 룬 풀 초기화
    /// </summary>
    private void InitializeRunePools()
    {
        RuneSO[] allRunes = Resources.LoadAll<RuneSO>("Runes");

        Debug.Log($"[RuneShopService] Resources.LoadAll로 로드된 총 룬 개수: {allRunes.Length}");

        allCommonRunes.Clear();
        allRareRunes.Clear();
        allEpicRunes.Clear();

        foreach (var rune in allRunes)
        {
            if (rune == null)
            {
                Debug.LogWarning("[RuneShopService] null 룬 발견!");
                continue;
            }

            // 룬의 타입 이름 확인
            string runeType = rune.GetType().Name;
            Debug.Log($"[RuneShopService] 룬 로드 시도: {rune.name} (타입: {runeType}, 이름: {rune.runeName}, ID: {rune.runeId}, 등급: {rune.rarity})");

            switch (rune.rarity)
            {
                case RuneRarity.Common:
                    allCommonRunes.Add(rune);
                    Debug.Log($"[RuneShopService] 커먼 룬 추가: {rune.runeName}");
                    break;
                case RuneRarity.Rare:
                    allRareRunes.Add(rune);
                    Debug.Log($"[RuneShopService] 레어 룬 추가: {rune.runeName}");
                    break;
                case RuneRarity.Epic:
                    allEpicRunes.Add(rune);
                    Debug.Log($"[RuneShopService] 에픽 룬 추가: {rune.runeName}");
                    break;
            }
        }

        Debug.Log($"[RuneShopService] 룬 풀 초기화 완료 - 커먼: {allCommonRunes.Count}, 레어: {allRareRunes.Count}, 에픽: {allEpicRunes.Count}");
    }

    /// <summary>
    /// 일반 상점 초기화 (커먼 등급 고정)
    /// </summary>
    private void InitializeNormalShop()
    {
        normalShopRunes.Clear();

        // 커먼 룬 전체를 일반 상점에 표시 (최대 normalShopRuneCount개)
        int count = Mathf.Min(normalShopRuneCount, allCommonRunes.Count);
        Debug.Log($"[RuneShopService] 일반 상점 초기화 시작 - normalShopRuneCount: {normalShopRuneCount}, allCommonRunes.Count: {allCommonRunes.Count}, 표시할 개수: {count}");

        for (int i = 0; i < count; i++)
        {
            normalShopRunes.Add(allCommonRunes[i]);
            Debug.Log($"[RuneShopService] 일반 상점에 추가: {allCommonRunes[i].runeName}");
        }

        Debug.Log($"[RuneShopService] 일반 상점 초기화 완료 - {normalShopRunes.Count}개 커먼 룬");
    }

    #endregion

    #region Shop Management

    /// <summary>
    /// 교환소 새로고침 (레어 등급 랜덤 5개)
    /// 층수가 변경되면 자동 호출됨
    /// 미보유 룬 우선 표시, 부족하면 보유 룬으로 채움
    /// </summary>
    public void RefreshExchangeShop()
    {
        exchangeShopRunes.Clear();

        if (allRareRunes.Count == 0)
        {
            Debug.LogWarning("[RuneShopService] 레어 등급 룬이 없습니다!");
            return;
        }

        // 보유 룬 목록 가져오기
        List<RuneSO> ownedRunes = RuneDatabase.Instance.GetOwnedRunes();

        // 미보유 레어 룬 필터링
        List<RuneSO> unownedRareRunes = allRareRunes.Where(r => !ownedRunes.Contains(r)).ToList();

        // 1단계: 미보유 룬을 랜덤으로 섞어서 우선 추가
        List<RuneSO> shuffledUnowned = unownedRareRunes.OrderBy(x => Random.value).ToList();
        int addedCount = Mathf.Min(exchangeShopRuneCount, shuffledUnowned.Count);

        for (int i = 0; i < addedCount; i++)
        {
            exchangeShopRunes.Add(shuffledUnowned[i]);
        }

        // 2단계: 설정된 개수에 미달하면 보유 룬으로 나머지 슬롯 채우기
        if (exchangeShopRunes.Count < exchangeShopRuneCount)
        {
            List<RuneSO> ownedRareRunes = allRareRunes.Where(r => ownedRunes.Contains(r)).ToList();
            List<RuneSO> shuffledOwned = ownedRareRunes.OrderBy(x => Random.value).ToList();

            int remainingSlots = exchangeShopRuneCount - exchangeShopRunes.Count;
            int fillCount = Mathf.Min(remainingSlots, shuffledOwned.Count);

            for (int i = 0; i < fillCount; i++)
            {
                exchangeShopRunes.Add(shuffledOwned[i]);
            }

            Debug.Log($"[RuneShopService] 교환소 - 미보유 룬 부족으로 보유 룬 {fillCount}개 추가됨");
        }

        Debug.Log($"[RuneShopService] 교환소 새로고침 완료 - 총 {exchangeShopRunes.Count}개 (미보유: {addedCount}개)");
    }

    /// <summary>
    /// 특수 교환소 새로고침 (에픽 등급 랜덤 3개)
    /// 층수가 변경되면 자동 호출됨
    /// 미보유 룬 우선 표시, 부족하면 보유 룬으로 채움
    /// </summary>
    public void RefreshSecretShop()
    {
        secretShopRunes.Clear();

        if (allEpicRunes.Count == 0)
        {
            Debug.LogWarning("[RuneShopService] 에픽 등급 룬이 없습니다!");
            return;
        }

        // 보유 룬 목록 가져오기
        List<RuneSO> ownedRunes = RuneDatabase.Instance.GetOwnedRunes();

        // 미보유 에픽 룬 필터링
        List<RuneSO> unownedEpicRunes = allEpicRunes.Where(r => !ownedRunes.Contains(r)).ToList();

        // 1단계: 미보유 룬을 랜덤으로 섞어서 우선 추가
        List<RuneSO> shuffledUnowned = unownedEpicRunes.OrderBy(x => Random.value).ToList();
        int addedCount = Mathf.Min(secretShopRuneCount, shuffledUnowned.Count);

        for (int i = 0; i < addedCount; i++)
        {
            secretShopRunes.Add(shuffledUnowned[i]);
        }

        // 2단계: 설정된 개수에 미달하면 보유 룬으로 나머지 슬롯 채우기
        if (secretShopRunes.Count < secretShopRuneCount)
        {
            List<RuneSO> ownedEpicRunes = allEpicRunes.Where(r => ownedRunes.Contains(r)).ToList();
            List<RuneSO> shuffledOwned = ownedEpicRunes.OrderBy(x => Random.value).ToList();

            int remainingSlots = secretShopRuneCount - secretShopRunes.Count;
            int fillCount = Mathf.Min(remainingSlots, shuffledOwned.Count);

            for (int i = 0; i < fillCount; i++)
            {
                secretShopRunes.Add(shuffledOwned[i]);
            }

            Debug.Log($"[RuneShopService] 특수 교환소 - 미보유 룬 부족으로 보유 룬 {fillCount}개 추가됨");
        }

        Debug.Log($"[RuneShopService] 특수 교환소 새로고침 완료 - 총 {secretShopRunes.Count}개 (미보유: {addedCount}개)");
    }

    /// <summary>
    /// 층수 변경 감지 및 교환소/특수교환소 자동 새로고침
    /// </summary>
    private void CheckFloorChange()
    {
        int currentFloor = StaticInfoManager.floor;

        if (lastFloor != currentFloor)
        {
            if (lastFloor != -1) // 첫 실행이 아닐 때만
            {
                Debug.Log($"[RuneShopService] 층수 변경 감지: {lastFloor} → {currentFloor}");
                RefreshExchangeShop();      // 교환소 새로고침
                RefreshSecretShop();        // 특수교환소 새로고침
            }
            lastFloor = currentFloor;
        }
    }

    #endregion

    #region Getters

    /// <summary>
    /// 일반 상점의 커먼 룬 목록 반환
    /// </summary>
    public List<RuneSO> GetNormalShopRunes()
    {
        return new List<RuneSO>(normalShopRunes);
    }

    /// <summary>
    /// 교환소의 레어 룬 목록 반환
    /// </summary>
    public List<RuneSO> GetExchangeShopRunes()
    {
        return new List<RuneSO>(exchangeShopRunes);
    }

    /// <summary>
    /// 특수 교환소의 에픽 룬 목록 반환
    /// </summary>
    public List<RuneSO> GetSecretShopRunes()
    {
        return new List<RuneSO>(secretShopRunes);
    }

    /// <summary>
    /// 룬 등급에 따른 가격 반환
    /// </summary>
    public int GetRunePrice(RuneSO rune)
    {
        if (rune == null) return 0;

        switch (rune.rarity)
        {
            case RuneRarity.Common:
                return commonRunePrice;
            case RuneRarity.Rare:
                return rareRunePrice;
            case RuneRarity.Epic:
                return epicRunePrice;
            case RuneRarity.Legendary:
                return epicRunePrice * 3; // 전설 등급은 에픽의 3배
            default:
                return 100;
        }
    }

    /// <summary>
    /// 특정 등급의 가격 반환
    /// </summary>
    public int GetPriceByRarity(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return commonRunePrice;
            case RuneRarity.Rare:
                return rareRunePrice;
            case RuneRarity.Epic:
                return epicRunePrice;
            case RuneRarity.Legendary:
                return epicRunePrice * 3;
            default:
                return 100;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 룬 구매 처리 (다이아몬드 사용)
    /// </summary>
    public bool PurchaseRune(RuneSO rune, int price)
    {
        // 튜토리얼 모드에서는 룬 상점 이용 불가
        if (TutorialManager.IsTutorialMode)
        {
            Debug.Log("[RuneShopService] 튜토리얼 모드에서는 룬 상점을 이용할 수 없습니다!");
            return false;
        }

        if (rune == null)
        {
            Debug.LogWarning("[RuneShopService] 구매할 룬이 null입니다!");
            return false;
        }

        // RuneDatabase null 체크
        if (RuneDatabase.Instance == null)
        {
            Debug.LogError("[RuneShopService] RuneDatabase.Instance가 null입니다!");
            return false;
        }

        // 이미 보유한 룬인지 확인
        var ownedRunes = RuneDatabase.Instance.GetOwnedRunes();
        if (ownedRunes == null)
        {
            Debug.LogError("[RuneShopService] GetOwnedRunes()가 null을 반환했습니다!");
            return false;
        }

        if (ownedRunes.Contains(rune))
        {
            Debug.Log($"[RuneShopService] {rune.runeName}은(는) 이미 보유하고 있습니다!");
            return false;
        }

        int currentDiamond = StaticInfoManager.dia;

        if (currentDiamond < price)
        {
            Debug.Log($"[RuneShopService] 다이아몬드 부족! 필요: {price}, 보유: {currentDiamond}");
            return false;
        }

        // 다이아몬드 차감
        StaticInfoManager.dia -= price;

        // 룬 획득
        RuneDatabase.Instance.AddRune(rune);

        Debug.Log($"[RuneShopService] {rune.runeName} 구매 완료! 가격: {price} 다이아몬드");
        return true;
    }

    #endregion
}
