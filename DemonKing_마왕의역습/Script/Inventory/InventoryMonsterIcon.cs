using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 몬스터 아이콘 컨트롤러 (MonsterCatalog 호환 버전)
/// 각 몬스터 아이콘의 버튼을 통해 클릭 시 상세정보 표시
/// 일반 몬스터와 특수 몬스터를 모두 올바르게 처리
///
/// 주요 개선사항:
/// - SpecialUnitStock 사용, UnitStock으로 통합
/// - MonsterCatalog를 통해 특수 메타 정보 조회
/// - FindAnyObjectByType 사용으로 성능 최적화
/// - SOLID 원칙 준수로 코드 재사용성 극대화
/// </summary>
public class InventoryMonsterIcon : MonoBehaviour
{
    #region Private Fields

    [Header("UI 요소들")]
    [SerializeField] private Image monsterIconImage;     // 몬스터 아이콘 (1번째)
    [SerializeField] private Image monsterIconImage2;    // 몬스터 아이콘 (2번째, 미획득시 검정)
    [SerializeField] private Text ownedCountText;        // 보유수 텍스트
    [SerializeField] private Button iconButton;          // 클릭 가능한 버튼

    private UnitStock monsterData;                       // 저장된 몬스터 데이터
    private InventoryUIManager inventoryManager;         // 인벤토리 매니저 참조
    private EnemyBestiaryManager bestiaryManager;        // 적군 도감 매니저 참조
    private MonsterCatalog.SpecialMeta specialMeta;      // 특수 몬스터 메타데이터

    // 현재 몬스터의 타입을 저장하기 위한 플래그
    private bool isSpecialMonster = false;
    private bool isEnemyMonster = false;

    // 캐시된 MonsterCatalog 참조 (성능 최적화)
    private MonsterCatalog catalogCache;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        FindUIComponents();
        InitializeCatalogReference();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// MonsterCatalog 참조를 초기화하는 메서드
    /// 씬 전환 시 유지되는 싱글톤 인스턴스 캐싱
    ///
    /// 작동 방식:
    /// 1. MonsterCatalog.Instance로 싱글톤 접근
    /// 2. 캐싱하여 반복 접근 시 성능 최적화
    /// 3. null 체크로 안정성 확보
    /// </summary>
    private void InitializeCatalogReference()
    {
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogError("[InventoryMonsterIcon] MonsterCatalog 인스턴스를 찾을 수 없습니다!");
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
        if (monsterIconImage == null)
            monsterIconImage = GetComponent<Image>();

        // 검정 실루엣 레이어 찾기 (이름으로 검색)
        if (monsterIconImage2 == null)
        {
            // 방법 1: 이름으로 찾기 (프리팹에 "SilhouetteImage" 이름 필요)
            Transform silhouetteTransform = transform.Find("SilhouetteImage");
            if (silhouetteTransform != null)
            {
                monsterIconImage2 = silhouetteTransform.GetComponent<Image>();
            }
            else
            {
                // 방법 2: 모든 Image 중 마지막 것 (검정 레이어가 보통 맨 위/마지막)
                Image[] images = GetComponentsInChildren<Image>(true); // includeInactive = true
                if (images.Length >= 2)
                {
                    // 마지막 Image를 검정 레이어로 가정
                    monsterIconImage2 = images[images.Length - 1];
                    Debug.Log($"[InventoryMonsterIcon] 검정 레이어를 자동 검색: {images.Length}개 Image 중 마지막 사용");
                }
            }
        }

        if (ownedCountText == null)
            ownedCountText = GetComponentInChildren<Text>();

        if (iconButton == null)
            iconButton = GetComponent<Button>();
    }

    #endregion

    #region Public Setup Methods

    /// <summary>
    /// 일반 몬스터 데이터를 설정하고 UI를 업데이트하는 메서드
    ///
    /// 작동 방식:
    /// 1. 이전 데이터 초기화
    /// 2. 일반 몬스터로 플래그 설정
    /// 3. UI 업데이트 및 버튼 이벤트 연결
    ///
    /// 사용:
    /// var iconController = iconObject.GetComponent<InventoryMonsterIcon>();
    /// iconController.SetupMonster(normalMonsterData, inventoryUIManager);
    /// </summary>
    /// <param name="monster">일반 몬스터 데이터</param>
    /// <param name="manager">인벤토리 매니저</param>
    public void SetupMonster(UnitStock monster, InventoryUIManager manager)
    {
        // SOLID 원칙: 단일 책임 원칙을 위해 각 메서드가 하나의 책임만 담당
        ClearData();

        monsterData = monster;
        inventoryManager = manager;
        isSpecialMonster = false;
        specialMeta = null; // 일반 몬스터는 특수 메타 없음

        UpdateUI();
        SetupButton();

        /*Debug.Log($"[InventoryMonsterIcon] 일반 몬스터 설정 완료: {monster?.displayName}");*/
    }

    /// <summary>
    /// 특수 몬스터 데이터를 설정하고 UI를 업데이트하는 메서드
    ///
    /// 작동 방식:
    /// 1. 이전 데이터 초기화
    /// 2. MonsterCatalog에서 특수 메타 정보 조회
    /// 3. 특수 몬스터로 플래그 설정
    /// 4. UI 업데이트 및 버튼 이벤트 연결
    ///
    /// 사용:
    /// var iconController = iconObject.GetComponent<InventoryMonsterIcon>();
    /// iconController.SetupSpecialMonster(specialMonsterData, inventoryUIManager);
    /// </summary>
    /// <param name="monster">특수 몬스터 데이터 (UnitStock 타입으로 통일)</param>
    /// <param name="manager">인벤토리 매니저</param>
    public void SetupSpecialMonster(UnitStock monster, InventoryUIManager manager)
    {
        ClearData();

        monsterData = monster;
        inventoryManager = manager;
        isSpecialMonster = true;

        // MonsterCatalog에서 특수 메타 정보 조회
        if (catalogCache != null)
        {
            specialMeta = catalogCache.GetSpecialMeta(monster.enemyId);
        }
        else
        {
            Debug.LogWarning($"[InventoryMonsterIcon] MonsterCatalog가 없어 특수 메타를 가져올 수 없습니다: {monster.enemyId}");
            specialMeta = new MonsterCatalog.SpecialMeta { price = 0, shop = true };
        }

        UpdateUI();
        SetupButton();

       /* Debug.Log($"[InventoryMonsterIcon] 특수 몬스터 설정 완료: {monster?.displayName}");*/
    }

    /// <summary>
    /// 적군 데이터를 설정하고 UI를 업데이트하는 메서드
    ///
    /// 작동 방식:
    /// 1. 이전 데이터 초기화
    /// 2. 적군으로 플래그 설정
    /// 3. encountered 필드로 미조우 처리
    /// 4. UI 업데이트 및 버튼 이벤트 연결
    ///
    /// 사용:
    /// var iconController = iconObject.GetComponent<InventoryMonsterIcon>();
    /// iconController.SetupEnemy(enemyData, bestiaryManager);
    /// </summary>
    /// <param name="enemy">적군 데이터</param>
    /// <param name="manager">적군 도감 매니저</param>
    public void SetupEnemy(UnitStock enemy, EnemyBestiaryManager manager)
    {
        ClearData();

        monsterData = enemy;
        bestiaryManager = manager;
        isEnemyMonster = true;

        UpdateUI();
        SetupButton();

        Debug.Log($"[InventoryMonsterIcon] 적군 설정 완료: {enemy?.displayName} (조우: {enemy.encountered})");
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 이전 데이터를 모두 초기화하는 메서드
    /// 새로운 몬스터 설정 전 이전 상태와의 충돌 방지
    ///
    /// 작동 방식:
    /// - 모든 데이터 필드를 null로 초기화
    /// - 플래그를 기본값으로 복원
    ///
    /// SOLID 원칙: 개방-폐쇄 원칙을 위해 확장 가능한 구조 유지
    /// </summary>
    private void ClearData()
    {
        monsterData = null;
        inventoryManager = null;
        bestiaryManager = null;
        isSpecialMonster = false;
        isEnemyMonster = false;
        specialMeta = null;
    }

    /// <summary>
    /// 몬스터 타입에 따른 올바른 UI를 업데이트하는 메서드
    /// 일반 몬스터, 특수 몬스터, 적군을 구분하여 처리
    /// 미획득 유닛/미조우 적군은 검정 실루엣으로 표시
    ///
    /// 작동 원리:
    /// 1. 몬스터 데이터의 타입 확인
    /// 2. 해당 타입의 데이터가 유효한지 검증
    /// 3. 획득/조우 여부에 따라 아이콘 이미지와 보유수 텍스트 업데이트
    ///
    /// SOLID 원칙: 리스코프 치환 원칙을 위해 타입별 처리 분리
    /// </summary>
    private void UpdateUI()
    {
        if (monsterData == null)
        {
            Debug.LogWarning("[InventoryMonsterIcon] 몬스터 데이터가 null입니다!");
            return;
        }

        // 적군은 encountered 필드, 아군은 owned 필드 사용 여기서판정
        bool isVisible = isEnemyMonster ? monsterData.encountered : (monsterData.owned > 0 || monsterData.hasBeenOwned);
        Debug.Log($"[InventoryMonsterIcon] {monsterData.displayName} - owned: {monsterData.owned}, hasBeenOwned: {monsterData.hasBeenOwned}, isVisible: {isVisible}");


        // 아이콘 이미지 설정 (1번째 이미지 - 항상 표시)
        if (monsterIconImage != null && monsterData.icon != null)
        {
            monsterIconImage.sprite = monsterData.icon;
        }
        else if (monsterIconImage != null)
        {
            Debug.LogWarning($"[InventoryMonsterIcon] 아이콘이 없는 몬스터: {monsterData.displayName}");
        }

        // 2번째 이미지 설정 (미획득/미조우시 검정색)
        if (monsterIconImage2 != null)
        {
            if (monsterData.icon != null)
            {
                monsterIconImage2.sprite = monsterData.icon;
            }

            // 미획득/미조우시 검정색, 획득/조우시 흰색
            monsterIconImage2.color = isVisible ? Color.white : Color.black;
            Debug.Log($"[InventoryMonsterIcon] 현재 몬스터 아이콘 : {isVisible}");
        }

        // 보유수 텍스트 설정 (적군은 보유수 표시 안 함)
        if (ownedCountText != null)
        {
            if (isEnemyMonster)
            {
                // 적군은 조우 여부만 표시 (선택적)
                ownedCountText.text = monsterData.encountered ? $"{monsterData.displayName}" : "???";            
            }
            else
            {
                // 아군은 보유수 표시
                ownedCountText.text = monsterData.owned.ToString();
            }
        }

        // 특수 몬스터일 경우 추가 UI 처리 (예: 테두리 색상 등)
        if (isSpecialMonster)
        {
            UpdateSpecialMonsterVisuals();
        }
    }

    /// <summary>
    /// 특수 몬스터 시각적 효과를 업데이트하는 메서드
    /// 일반 몬스터와 구분하기 위한 시각적 표현
    ///
    /// 작동 방식:
    /// - 특수 몬스터에게 테두리나 색상 효과 추가
    /// - 상점 타입에 따른 추가 메시지 표시
    /// </summary>
    private void UpdateSpecialMonsterVisuals()
    {
        // 특수 몬스터 시각적 효과 (예: 테두리 색상 변경)
        if (monsterIconImage != null)
        {
            // 예시: 테두리를 특별한 색상으로 표시 가능
            // monsterIconImage.color = Color.yellow; // 주석
        }
    }

    /// <summary>
    /// 버튼 이벤트를 몬스터 타입에 따라 올바르게 설정하는 메서드
    /// 클릭 시 해당 타입의 상세정보 창을 표시
    ///
    /// 작동 원리:
    /// 1. 기존 이벤트 리스너 제거
    /// 2. 현재 몬스터 타입에 맞는 이벤트 리스너 추가
    /// 3. 올바른 데이터를 해당 매니저에 전달
    ///
    /// SOLID 원칙: 의존성 역전 원칙을 위해 추상화된 인터페이스
    /// </summary>
    private void SetupButton()
    {
        if (iconButton == null)
        {
            Debug.LogWarning("[InventoryMonsterIcon] 버튼이 null입니다!");
            return;
        }

        // 이전 이벤트 리스너 제거 (메모리 누수 방지)
        iconButton.onClick.RemoveAllListeners();

        // 몬스터 데이터가 유효한지 확인
        if (monsterData != null)
        {
            if (isEnemyMonster)
            {
                // 적군 클릭 이벤트
                if (bestiaryManager == null)
                {
                    Debug.LogWarning("[InventoryMonsterIcon] 적군 도감 매니저가 null입니다!");
                    return;
                }

                iconButton.onClick.AddListener(() => {
                    Debug.Log($"[InventoryMonsterIcon] 적군 클릭: {monsterData.displayName}");
                    bestiaryManager.ShowEnemyDetail(monsterData);
                });
            }
            else if (isSpecialMonster)
            {
                // 특수 몬스터 클릭 이벤트
                if (inventoryManager == null)
                {
                    Debug.LogWarning("[InventoryMonsterIcon] 인벤토리 매니저가 null입니다!");
                    return;
                }

                iconButton.onClick.AddListener(() => {
                    Debug.Log($"[InventoryMonsterIcon] 특수 몬스터 클릭: {monsterData.displayName}");
                    inventoryManager.ShowSpecialMonsterDetail(monsterData, specialMeta);
                });
            }
            else
            {
                // 일반 몬스터 클릭 이벤트
                if (inventoryManager == null)
                {
                    Debug.LogWarning("[InventoryMonsterIcon] 인벤토리 매니저가 null입니다!");
                    return;
                }

                iconButton.onClick.AddListener(() => {
                    Debug.Log($"[InventoryMonsterIcon] 일반 몬스터 클릭: {monsterData.displayName}");
                    inventoryManager.ShowMonsterDetail(monsterData);
                });
            }
        }
        else
        {
            Debug.LogWarning("[InventoryMonsterIcon] 올바른 몬스터 데이터가 설정되지 않았습니다!");
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// 보유수 업데이트 메서드 (구매/판매 시 호출)
    /// 외부에서 몬스터 데이터가 변경되었을 때 UI를 갱신
    ///
    /// 작동 방식:
    /// - 현재 몬스터의 owned 속성을 가져와 텍스트에 반영
    /// - null 체크로 안정성 확보
    ///
    /// 사용:
    /// iconController.UpdateOwnedCount(); // 구매/판매 시 호출
    /// </summary>
    public void UpdateOwnedCount()
    {
        if (ownedCountText != null && monsterData != null)
        {
            ownedCountText.text = monsterData.owned.ToString();
        }
    }

    /// <summary>
    /// 현재 몬스터가 특수 몬스터인지 확인하는 메서드
    /// 로직이나 외부에서 타입을 확인할 때 사용
    ///
    /// 사용:
    /// if (iconController.IsSpecialMonster())
    /// {
    ///     // 특수 몬스터 처리 로직
    /// }
    /// </summary>
    /// <returns>특수 몬스터면 true, 일반 몬스터면 false</returns>
    public bool IsSpecialMonster()
    {
        return isSpecialMonster;
    }

    /// <summary>
    /// 현재 몬스터의 이름을 반환하는 메서드
    /// 디버그 용도로 사용
    ///
    /// 사용:
    /// string monsterName = iconController.GetMonsterName();
    /// Debug.Log($"현재 몬스터: {monsterName}");
    /// </summary>
    /// <returns>몬스터 이름, 설정되지 않았으면 "알 수 없음"</returns>
    public string GetMonsterName()
    {
        return monsterData?.displayName ?? "알 수 없음";
    }

    /// <summary>
    /// 현재 몬스터의 특수 메타 정보를 반환하는 메서드
    /// 특수 몬스터의 가격이나 상점 타입 정보가 필요할 때 사용
    ///
    /// 사용:
    /// var meta = iconController.GetSpecialMeta();
    /// if (meta != null)
    /// {
    ///     Debug.Log($"미스릴 가격: {meta.price}");
    /// }
    /// </summary>
    /// <returns>특수 메타 정보, 일반 몬스터거나 없으면 null</returns>
    public MonsterCatalog.SpecialMeta GetSpecialMeta()
    {
        return isSpecialMonster ? specialMeta : null;
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그용 메서드: 현재 아이콘의 상태를 콘솔에 출력
    /// Inspector에서 우클릭 메뉴로 수동 호출
    ///
    /// 작동 방식:
    /// - 현재 몬스터의 모든 정보 필드를 콘솔에 출력
    /// - 버그 해결 및 문제를 파악할 때 유용
    ///
    /// 호출: Inspector에서 우클릭 후 Debug Icon Status 선택
    /// </summary>
    [ContextMenu("Debug Icon Status")]
    public void DebugIconStatus()
    {
        Debug.Log("=== InventoryMonsterIcon 상태 ===");
        Debug.Log($"타입: {(isSpecialMonster ? "특수" : "일반")}");
        Debug.Log($"이름: {GetMonsterName()}");
        Debug.Log($"아이콘 이미지: {monsterIconImage != null}");
        Debug.Log($"보유수 텍스트: {ownedCountText != null}");
        Debug.Log($"버튼: {iconButton != null}");
        Debug.Log($"인벤토리 매니저: {inventoryManager != null}");
        Debug.Log($"MonsterCatalog 캐시: {catalogCache != null}");

        if (monsterData != null)
        {
            Debug.Log($"아이콘 스프라이트: {monsterData.icon != null}");
            Debug.Log($"보유 개수: {monsterData.owned}");
            Debug.Log($"ID: {monsterData.enemyId}");
        }

        if (isSpecialMonster && specialMeta != null)
        {
            Debug.Log($"특수 메타 - 가격: {specialMeta.price}, 상점: {(specialMeta.shop ? "일반" : "비밀")}");
        }
    }

    #endregion
}
