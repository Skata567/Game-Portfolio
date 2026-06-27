using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 적군 도감 관리자
/// 조우한 적만 정보 표시, 미조우 적은 검정 실루엣
/// 인벤토리 UI와 토글 방식으로 작동 (하나 켜지면 하나 꺼짐)
/// </summary>
public class EnemyBestiaryManager : MonoBehaviour
{
    #region UI Components

    [Header("적군 도감 Grid (스크롤뷰 내부)")]
    [SerializeField] private Transform bestiaryGridParent; // 적군 아이콘 Grid Parent
    [SerializeField] private GameObject enemyIconPrefab; // 적군 아이콘 프리팹

    [Header("상세 정보 패널 (인벤토리와 공유)")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailMonsterImage;
    [SerializeField] private Text detailNameText;
    [SerializeField] private Text detailHPText;
    [SerializeField] private Text detailDamageText;
    [SerializeField] private Text detailSpeedText;
    [SerializeField] private Text detailRaceText;
    [SerializeField] private Text detailCostText;
    [SerializeField] private Text detailOwnedText;
    [SerializeField] private Text detailDescriptionText;

    #endregion

    #region Private Fields

    private MonsterCatalog catalogCache;
    private UnitStock currentDetailEnemy;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeServices();
        InitializeUI();
    }

    #endregion

    #region 초기화 메서드들

    /// <summary>
    /// 필요한 서비스 초기화
    /// </summary>
    private void InitializeServices()
    {
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogError("[EnemyBestiaryManager] MonsterCatalog 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// UI 초기 설정
    /// </summary>
    private void InitializeUI()
    {
        // 상세 패널은 인벤토리와 공유하므로 초기화만
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    #endregion

    #region Public Methods - 적군 도감 UI 업데이트

    /// <summary>
    /// 적군 도감 UI 전체 갱신
    /// </summary>
    public void UpdateBestiaryUI()
    {
        ClearGrid();

        if (catalogCache == null || !catalogCache.IsReady)
        {
            Debug.LogWarning("[EnemyBestiaryManager] MonsterCatalog이 준비되지 않았습니다.");
            return;
        }

        // 적군 리스트 가져오기 (code 순 정렬)
        var enemies = catalogCache.enemyUnits.OrderBy(e => e.enemyId).ToList();

        Debug.Log($"[EnemyBestiaryManager] 적군 데이터 개수: {enemies.Count}");

        if (enemies.Count == 0)
        {
            Debug.LogError("[EnemyBestiaryManager] MonsterCatalog.enemyUnits가 비어있습니다! JSON에서 적군 데이터를 불러오지 못했습니다.");
            return;
        }

        foreach (var enemy in enemies)
        {
            Debug.Log($"[EnemyBestiaryManager] 적군 생성 중: {enemy.displayName} (ID: {enemy.enemyId}, 아이콘: {enemy.icon != null})");
            CreateEnemyIcon(enemy);
        }

        Debug.Log($"[EnemyBestiaryManager] 총 {enemies.Count}개 적 아이콘 생성 완료");

        // 첫 번째 적 자동 선택하여 상세 정보 표시
        if (enemies.Count > 0)
        {
            ShowEnemyDetail(enemies[0]);
            Debug.Log($"[EnemyBestiaryManager] 첫 번째 적 자동 선택: {enemies[0].displayName}");
        }
    }

    #endregion

    #region Private Methods - 적 아이콘 생성

    /// <summary>
    /// 적군 아이콘 생성
    /// </summary>
    private void CreateEnemyIcon(UnitStock enemy)
    {
        if (enemyIconPrefab == null)
        {
            Debug.LogError("[EnemyBestiaryManager] Enemy Icon Prefab이 null입니다! Inspector에서 설정해주세요.");
            return;
        }

        if (bestiaryGridParent == null)
        {
            Debug.LogError("[EnemyBestiaryManager] Bestiary Grid Parent가 null입니다! Inspector에서 설정해주세요.");
            return;
        }

        GameObject iconObj = Instantiate(enemyIconPrefab, bestiaryGridParent);
        var iconController = iconObj.GetComponent<InventoryMonsterIcon>();

        if (iconController != null)
        {
            // InventoryMonsterIcon 재사용 (encountered 필드로 미조우 처리)
            iconController.SetupEnemy(enemy, this);
        }
        else
        {
            Debug.LogError($"[EnemyBestiaryManager] 프리팹에 InventoryMonsterIcon 컴포넌트가 없습니다! 프리팹: {enemyIconPrefab.name}");
        }
    }

    #endregion

    #region Public Methods - 상세 정보 표시

    /// <summary>
    /// 적 상세 정보 표시
    /// </summary>
    public void ShowEnemyDetail(UnitStock enemy)
    {
        if (detailPanel == null)
        {
            Debug.LogError("[EnemyBestiaryManager] ❌ Detail Panel이 null입니다! Inspector에서 연결해주세요.");
            return;
        }

        if (enemy == null)
        {
            Debug.LogWarning("[EnemyBestiaryManager] 적 데이터가 null입니다.");
            return;
        }

        // 미조우 적도 상세 정보 표시 (단, ???로 표시)
        Debug.Log($"[EnemyBestiaryManager] 적 상세 정보 표시: {enemy.displayName} (조우: {enemy.encountered})");

        currentDetailEnemy = enemy;
        UpdateDetailUI(enemy);
        detailPanel.SetActive(true);
    }

    /// <summary>
    /// 상세 정보 패널 숨김
    /// </summary>
    public void HideDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        currentDetailEnemy = null;
    }

    #endregion

    #region Private Methods - 상세 정보 UI 업데이트

    /// <summary>
    /// 상세 정보 UI 갱신
    /// </summary>
    private void UpdateDetailUI(UnitStock enemy)
    {
        bool isEncountered = enemy.encountered;

        // 이미지
        if (detailMonsterImage != null && enemy.icon != null)
        {
            detailMonsterImage.sprite = enemy.icon;
            // 조우한 적: 정상 표시, 미조우: 검정 실루엣
            detailMonsterImage.color = isEncountered ? Color.white : Color.black;
        }

        // 이름
        if (detailNameText != null)
            detailNameText.text = isEncountered ? enemy.displayName : "???";

        // 체력
        if (detailHPText != null)
            detailHPText.text = isEncountered ? $"체력: {enemy.hp}" : "체력: ???";

        // 공격력
        if (detailDamageText != null)
            detailDamageText.text = isEncountered ? $"공격력: {enemy.damage}" : "공격력: ???";

        // 최대 속도
        if (detailSpeedText != null)
            detailSpeedText.text = isEncountered ? $"최대 속도: {enemy.minSpeed} ~ {enemy.maxSpeed}" : "최대 속도: ???";


        // 종족 (적군은 모두 Human)
        if (detailRaceText != null)
        {
            Debug.Log($"[EnemyBestiaryManager] 적군 카테고리: '{enemy.category}' (이름: {enemy.displayName})");
            switch(enemy.category)
            {
                case "전사":
                    detailRaceText.text = isEncountered ? "종족: 전사" : "종족: ???";
                    detailRaceText.color = isEncountered ? Color.white : Color.gray; //흰색
                    break;
                case "궁수":
                    detailRaceText.text = isEncountered ? "종족: 궁수" : "종족: ???";
                    detailRaceText.color = isEncountered ? new Color(0.3f, 0.8f, 0.3f) : Color.gray; //녹색
                    break;
                case "마법사":
                    detailRaceText.text = isEncountered ? "종족: 마법사" : "종족: ???";
                    detailRaceText.color = isEncountered ? new Color(0.4f, 0.8f, 1.0f) : Color.gray; //하늘색
                    break;
                case "도적":
                    detailRaceText.text = isEncountered ? "종족: 도적" : "종족: ???";
                    detailRaceText.color = isEncountered ? new Color(1.0f, 0.84f, 0.0f) : Color.gray; //황금색
                    break;
                case "해적":
                    detailRaceText.text = isEncountered ? "종족: 해적" : "종족: ???";
                    detailRaceText.color = isEncountered ? new Color(0.1f, 0.3f, 0.9f) : Color.gray; //파란색
                    break;
                default:
                    detailRaceText.text = isEncountered ? $"종족: {enemy.category}" : "종족: ???";
                    detailRaceText.color = isEncountered ? new Color(0.5f, 0.2f, 0.8f) : Color.gray; //보라색
                    break;

            }
        }

        // 코스트 (적군은 코스트 없음, 공격 횟수로 대체 가능)
        if (detailCostText != null)
            detailCostText.text = isEncountered ? $"공격 횟수: {enemy.attackCount}" : "공격 횟수: ???";

        // 보유수 (적군은 보유 개념 없음, 조우 횟수로 대체 가능)
        if (detailOwnedText != null)
            detailOwnedText.text = ""; // 적군은 보유수 표시 안 함

        // 설명
        if (detailDescriptionText != null)
            detailDescriptionText.text = isEncountered ?
                (string.IsNullOrEmpty(enemy.ex) ? "적군입니다." : enemy.ex) :
                "아직 조우하지 못한 적입니다.";
    }

    #endregion

    #region Private Methods - 유틸리티

    /// <summary>
    /// 그리드 초기화
    /// </summary>
    private void ClearGrid()
    {
        if (bestiaryGridParent == null) return;

        foreach (Transform child in bestiaryGridParent)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion
}
