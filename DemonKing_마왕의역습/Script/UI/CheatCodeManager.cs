using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 치트 코드 매니저
/// InputField에 명령어를 입력하면 다양한 치트 기능 실행
///
/// 사용 방법:
/// 1. InputField를 씬에 배치
/// 2. 이 스크립트를 GameObject에 추가
/// 3. Inspector에서 InputField 연결
/// 4. 게임 실행 후 InputField에 명령어 입력 + Enter
///
/// 명령어 수정:
/// - 아래 #region 치트 명령어 목록에서 명령어 수정 가능
/// </summary>
public class CheatCodeManager : MonoBehaviour
{
    #region 치트 명령어 목록 (여기서 명령어 수정!)

    // ===== 교환소 관련 =====
    private const string CMD_OPEN_EXCHANGE = "ex";          // 일반 교환소 열기
    private const string CMD_OPEN_SECRET = "sr";              // 비밀 교환소 열기

    // ===== 재화 관련 =====
    private const string CMD_GIVE_GOLD = "gold";                  // 골드 10000 추가
    private const string CMD_GIVE_IRON = "iron";                  // 철 10000 추가
    private const string CMD_GIVE_MITHRIL = "mithril";            // 미스릴 100 추가
    private const string CMD_GIVE_DIAMOND = "diamond";            // 다이아몬드 1000 추가
    private const string CMD_GIVE_ALL = "money";         // 모든 재화 추가

    // ===== 도감 관련 =====
    private const string CMD_UNLOCK_RUNES = "allrunes";           // 모든 룬 획득 (1개씩)
    private const string CMD_UNLOCK_ENEMIES = "allenemies";       // 모든 적 도감 열기
    private const string CMD_UNLOCK_MONSTERS = "allmonsters";     // 모든 아군 유닛 상점 해금

    // ===== 노드 이동 =====
    private const string CMD_NODE_PREFIX = "node";                // node1~15: 해당 층 노드로 이동

    // ===== 디버그 =====
    private const string CMD_HELP = "help";                       // 명령어 목록 출력

    #endregion

    #region UI Components

    [Header("UI 연결")]
    [SerializeField] private InputField cheatInputField;          // 치트 입력 InputField
    [SerializeField] private Text feedbackText;                   // 피드백 메시지 Text (선택사항)

    #endregion

    #region Private Fields

    private UIManager uiManager;
    private RuneDatabase runeDB;
    private MonsterCatalog monsterCatalog;

    // 명령어 -> 실행 함수 매핑
    private Dictionary<string, System.Action> cheatCommands;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeReferences();
        SetupCheatCommands();
        SetupInputField();
    }

    void Update()
    {
        // Alt + x 키로 치트 콘솔 토글
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleCheatConsole();
        }

        // ESC 키로 치트 콘솔 닫기
        if (cheatInputField != null && cheatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideCheatConsole();
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 필요한 참조들 초기화
    /// </summary>
    private void InitializeReferences()
    {
        uiManager = FindAnyObjectByType<UIManager>();
        runeDB = RuneDatabase.Instance;
        monsterCatalog = MonsterCatalog.Instance;

        if (uiManager == null)
            Debug.LogError("[CheatCodeManager] UIManager를 찾을 수 없습니다!");

        if (runeDB == null)
            Debug.LogError("[CheatCodeManager] RuneDatabase를 찾을 수 없습니다!");

        if (monsterCatalog == null)
            Debug.LogError("[CheatCodeManager] MonsterCatalog를 찾을 수 없습니다!");
    }

    /// <summary>
    /// 치트 명령어와 실행 함수 매핑 설정
    /// </summary>
    private void SetupCheatCommands()
    {
        cheatCommands = new Dictionary<string, System.Action>
        {
            // 교환소
            { CMD_OPEN_EXCHANGE, CheatOpenExchange },
            { CMD_OPEN_SECRET, CheatOpenSecret },

            // 재화
            { CMD_GIVE_GOLD, CheatGiveGold },
            { CMD_GIVE_IRON, CheatGiveIron },
            { CMD_GIVE_MITHRIL, CheatGiveMithril },
            { CMD_GIVE_DIAMOND, CheatGiveDiamond },
            { CMD_GIVE_ALL, CheatGiveAll },

            // 도감
            { CMD_UNLOCK_RUNES, CheatUnlockAllRunes },
            { CMD_UNLOCK_ENEMIES, CheatUnlockAllEnemies },
            { CMD_UNLOCK_MONSTERS, CheatUnlockAllMonsters },

            // 디버그
            { CMD_HELP, CheatShowHelp }
        };

        Debug.Log("[CheatCodeManager] 치트 코드 시스템 초기화 완료");
    }

    /// <summary>
    /// InputField 이벤트 설정
    /// </summary>
    private void SetupInputField()
    {
        if (cheatInputField == null)
        {
            Debug.LogWarning("[CheatCodeManager] InputField가 연결되지 않았습니다!");
            return;
        }

        // Enter 키로 명령어 실행
        cheatInputField.onEndEdit.AddListener(OnCheatCodeEntered);

        // 처음엔 InputField 숨김 (` 또는 F1 키로 열기)
        cheatInputField.gameObject.SetActive(false);

        Debug.Log("[CheatCodeManager] InputField 연결 완료. ` 또는 F1 키로 치트 콘솔을 열 수 있습니다.");
    }

    #endregion

    #region 치트 코드 처리

    /// <summary>
    /// 치트 코드 입력 처리
    /// </summary>
    private void OnCheatCodeEntered(string input)
    {
        // Enter 키가 아니면 무시
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        // 입력값 정리 (소문자 변환, 공백 제거)
        string command = input.Trim().ToLower();

        if (string.IsNullOrEmpty(command))
        {
            ClearInputField();
            return;
        }

        Debug.Log($"[CheatCodeManager] 명령어 입력: {command}");

        // node1~15 명령어 처리
        if (command.StartsWith(CMD_NODE_PREFIX))
        {
            string numberPart = command.Substring(CMD_NODE_PREFIX.Length);
            if (int.TryParse(numberPart, out int nodeNumber) && nodeNumber >= 1 && nodeNumber <= 15)
            {
                CheatGoToNode(nodeNumber);
                ShowFeedback($"노드 {nodeNumber}층 전투 시작");
                ClearInputField();
                return;
            }
            else
            {
                Debug.LogWarning($"[CheatCodeManager] 잘못된 노드 번호: {command} (1~15 사이 숫자 입력)");
                ShowFeedback("node1~node15 형식으로 입력하세요");
                ClearInputField();
                return;
            }
        }

        // 명령어 실행
        if (cheatCommands.ContainsKey(command))
        {
            cheatCommands[command].Invoke();
            ShowFeedback($"치트 실행: {command}");
        }
        else
        {
            Debug.LogWarning($"[CheatCodeManager] 알 수 없는 명령어: {command}");
            ShowFeedback($"알 수 없는 명령어: {command}");
        }

        // InputField 초기화
        ClearInputField();
    }

    /// <summary>
    /// InputField 초기화
    /// </summary>
    private void ClearInputField()
    {
        if (cheatInputField != null)
        {
            cheatInputField.text = "";
            cheatInputField.ActivateInputField(); // 포커스 유지
        }
    }

    /// <summary>
    /// 피드백 메시지 표시
    /// </summary>
    private void ShowFeedback(string message)
    {
        Debug.Log($"[CheatCodeManager] {message}");

        if (feedbackText != null)
        {
            feedbackText.text = message;
            // 3초 후 메시지 지우기
            Invoke(nameof(ClearFeedback), 3f);
        }
    }

    /// <summary>
    /// 피드백 메시지 지우기
    /// </summary>
    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }

    /// <summary>
    /// 치트 콘솔 토글 (보이기/숨기기)
    /// ` (백틱) 또는 F1 키로 호출
    /// </summary>
    private void ToggleCheatConsole()
    {
        if (cheatInputField == null) return;

        bool isActive = cheatInputField.gameObject.activeSelf;

        if (isActive)
        {
            // 콘솔 숨기기
            HideCheatConsole();
        }
        else
        {
            // 콘솔 열기
            ShowCheatConsole();
        }
    }

    /// <summary>
    /// 치트 콘솔 표시
    /// </summary>
    private void ShowCheatConsole()
    {
        if (cheatInputField == null) return;

        cheatInputField.gameObject.SetActive(true);
        cheatInputField.text = "";  // 텍스트 초기화
        cheatInputField.ActivateInputField();  // 자동 포커스

        Debug.Log("[CheatCodeManager] 치트 콘솔 열림 (ESC로 닫기)");
    }

    /// <summary>
    /// 치트 콘솔 숨기기
    /// </summary>
    private void HideCheatConsole()
    {
        if (cheatInputField == null) return;

        cheatInputField.gameObject.SetActive(false);
        cheatInputField.text = "";  // 텍스트 초기화

        Debug.Log("[CheatCodeManager] 치트 콘솔 닫힘");
    }

    #endregion

    #region 치트 기능들 - 교환소

    /// <summary>
    /// 치트: 일반 교환소 열기
    /// </summary>
    private void CheatOpenExchange()
    {
        if (uiManager != null)
        {
            uiManager.OpenExchangeShop();
            Debug.Log("[CheatCodeManager] 일반 교환소 열림");
        }
    }

    /// <summary>
    /// 치트: 비밀 교환소 열기
    /// </summary>
    private void CheatOpenSecret()
    {
        if (uiManager != null)
        {
            uiManager.OpenSecretExchangeShop();
            Debug.Log("[CheatCodeManager] 비밀 교환소 열림");
        }
    }

    #endregion

    #region 치트 기능들 - 재화

    /// <summary>
    /// 치트: 골드 1000000 추가
    /// </summary>
    private void CheatGiveGold()
    {
        if (uiManager != null)
        {
            uiManager.playerGold += 1000000;
            StaticInfoManager.gold = uiManager.playerGold;
            uiManager.UpdateUI();
            Debug.Log("[CheatCodeManager] 골드 +10000");
        }
    }

    /// <summary>
    /// 치트: 철 10000 추가
    /// </summary>
    private void CheatGiveIron()
    {
        if (uiManager != null)
        {
            uiManager.playerIron += 10000;
            StaticInfoManager.iron = uiManager.playerIron;
            uiManager.UpdateUI();
            Debug.Log("[CheatCodeManager] 철 +10000");
        }
    }

    /// <summary>
    /// 치트: 미스릴 100 추가
    /// </summary>
    private void CheatGiveMithril()
    {
        if (uiManager != null)
        {
            uiManager.playerMithril += 100;
            StaticInfoManager.mis = uiManager.playerMithril;
            uiManager.UpdateUI();
            Debug.Log("[CheatCodeManager] 미스릴 +100");
        }
    }

    /// <summary>
    /// 치트: 다이아몬드 100000 추가
    /// </summary>
    private void CheatGiveDiamond()
    {
        if (uiManager != null)
        {
            uiManager.playerDiamond += 100000;
            StaticInfoManager.dia = uiManager.playerDiamond;
            uiManager.UpdateUI();
            Debug.Log("[CheatCodeManager] 다이아몬드 +1000");
        }
    }

    /// <summary>
    /// 치트: 모든 재화 추가
    /// </summary>
    private void CheatGiveAll()
    {
        CheatGiveGold();
        CheatGiveIron();
        CheatGiveMithril();
        CheatGiveDiamond();
        Debug.Log("[CheatCodeManager] 모든 재화 획득!");
    }

    #endregion

    #region 치트 기능들 - 도감

    /// <summary>
    /// 치트: 모든 룬 획득 (1개씩)
    /// </summary>
    private void CheatUnlockAllRunes()
    {
        if (runeDB == null)
        {
            Debug.LogError("[CheatCodeManager] RuneDatabase를 찾을 수 없습니다!");
            return;
        }

        // Resources/Runes 폴더에서 모든 룬 로드
        RuneSO[] allRunes = Resources.LoadAll<RuneSO>(runeDB.runesFolderPath);

        int addedCount = 0;
        foreach (var rune in allRunes)
        {
            if (rune != null && !runeDB.GetOwnedRunes().Contains(rune))
            {
                runeDB.AddRune(rune);
                addedCount++;
            }
        }

        Debug.Log($"[CheatCodeManager] 모든 룬 획득 완료! (총 {addedCount}개 추가)");
        ShowFeedback($"모든 룬 획득! ({addedCount}개)");
    }

    /// <summary>
    /// 치트: 모든 적 도감 열기
    /// </summary>
    private void CheatUnlockAllEnemies()
    {
        if (monsterCatalog == null)
        {
            Debug.LogError("[CheatCodeManager] MonsterCatalog를 찾을 수 없습니다!");
            return;
        }

        int unlockedCount = 0;
        foreach (var enemy in monsterCatalog.enemyUnits)
        {
            if (enemy != null && !enemy.encountered)
            {
                enemy.encountered = true;
                unlockedCount++;
            }
        }

        Debug.Log($"[CheatCodeManager] 모든 적 도감 열림! (총 {unlockedCount}개)");
        ShowFeedback($"모든 적 도감 열림! ({unlockedCount}개)");
    }

    /// <summary>
    /// 치트: 모든 아군 유닛 도감 해금
    /// 실제로 소유하지 않아도 도감에서 정보를 볼 수 있게 함 (hasBeenOwned = true)
    /// 상점에서도 즉시 구매 가능하도록 설정 (stage = 0)
    /// </summary>
    private void CheatUnlockAllMonsters()
    {
        if (monsterCatalog == null)
        {
            Debug.LogError("[CheatCodeManager] MonsterCatalog를 찾을 수 없습니다!");
            return;
        }

        int unlockedCount = 0;

        // 모든 일반 몬스터 도감 해금 (hasBeenOwned = true) + 상점 해금 (stage = 0)
        foreach (var monster in monsterCatalog.normalUnits)
        {
            if (monster != null)
            {
                monster.hasBeenOwned = true;  // 도감 영구 등록
                monster.stage = 0;             // 상점 즉시 해금
                unlockedCount++;
            }
        }

        // 모든 특수 몬스터 도감 해금 (hasBeenOwned = true)
        foreach (var monster in monsterCatalog.specialUnits)
        {
            if (monster != null)
            {
                monster.hasBeenOwned = true;  // 도감 영구 등록
                unlockedCount++;
            }
        }

        // UIManager가 있으면 상점 UI 갱신
        if (uiManager != null)
        {
            uiManager.UpdateUI(refreshPanels: true);
        }

        Debug.Log($"[CheatCodeManager] 모든 아군 유닛 도감 해금 완료! (총 {unlockedCount}개)");
        ShowFeedback($"모든 유닛 도감 해금! ({unlockedCount}개)");
    }

    #endregion

    #region 치트 기능들 - 디버그

    /// <summary>
    /// 치트: 명령어 목록 출력
    /// </summary>
    private void CheatShowHelp()
    {
        Debug.Log("===== 치트 명령어 목록 =====");
        Debug.Log($"교환소: {CMD_OPEN_EXCHANGE}, {CMD_OPEN_SECRET}");
        Debug.Log($"재화: {CMD_GIVE_GOLD}, {CMD_GIVE_IRON}, {CMD_GIVE_MITHRIL}, {CMD_GIVE_DIAMOND}, {CMD_GIVE_ALL}");
        Debug.Log($"도감: {CMD_UNLOCK_RUNES}, {CMD_UNLOCK_ENEMIES}, {CMD_UNLOCK_MONSTERS}");
        Debug.Log($"노드 이동: node1~node15 (해당 층으로 전투 시작)");
        Debug.Log($"도움말: {CMD_HELP}");
        Debug.Log("===========================");

        ShowFeedback("콘솔에서 명령어 목록 확인");
    }

    #endregion

    #region 치트 기능들 - 노드 이동

    /// <summary>
    /// 치트: 마왕을 특정 층 노드로 이동
    /// </summary>
    private void CheatGoToNode(int nodeNumber)
    {
        var mapGen = MapGenerator.Instance;
        var playerCtrl = GB_PlayerController.Instance;

        if (mapGen == null || !mapGen.IsReady)
        {
            Debug.LogError("[CheatCodeManager] MapGenerator가 준비되지 않았습니다. MainScene에서만 사용하세요.");
            ShowFeedback("맵이 생성되지 않음!");
            return;
        }

        if (playerCtrl == null)
        {
            Debug.LogError("[CheatCodeManager] GB_PlayerController를 찾을 수 없습니다.");
            ShowFeedback("플레이어 컨트롤러 없음!");
            return;
        }

        // 마왕을 (nodeNumber-1)층에 위치시켜서 nodeNumber층이 "다음 층"이 되도록 함
        int fromFloor = nodeNumber - 1;
        if (fromFloor < 0) fromFloor = 0;

        GB_MapNode fromNode = mapGen.GetNode(fromFloor, 0);
        if (fromNode == null)
        {
            Debug.LogError($"[CheatCodeManager] {fromFloor}층 노드를 찾을 수 없습니다.");
            ShowFeedback($"{fromFloor}층 노드 없음!");
            return;
        }

        // 마왕을 fromFloor에 위치시킴
        playerCtrl.SetStartNode(fromNode);

        // fromNode에 목표 층 노드들을 연결 (TrySelectNode의 IsNeighborSafe 체크 통과용)
        if (fromNode.connectedNodes == null)
        {
            fromNode.connectedNodes = new System.Collections.Generic.List<GB_MapNode>();
        }

        var targetNodes = new System.Collections.Generic.List<GB_MapNode>();
        for (int i = 0; i < 10; i++)
        {
            var targetNode = mapGen.GetNode(nodeNumber, i);
            if (targetNode != null)
            {
                targetNodes.Add(targetNode);
                if (!fromNode.connectedNodes.Contains(targetNode))
                {
                    fromNode.connectedNodes.Add(targetNode);
                }
            }
        }

        // MapSelectionController 가져오기
        var mapSelectCtrl = MapSelectionController.Instance;
        if (mapSelectCtrl == null)
        {
            Debug.LogError("[CheatCodeManager] MapSelectionController를 찾을 수 없습니다");
            return;
        }

        // 목표 층의 첫 번째 노드 찾아서 패널 강제 오픈
        var allNodes = Object.FindObjectsByType<GB_Node>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GB_Node firstNode = null;
        foreach (var node in allNodes)
        {
            if (node != null && node.logicNode != null && node.logicNode.floor == nodeNumber)
            {
                firstNode = node;
                break;
            }
        }

        if (firstNode != null)
        {
            // 치트 전용 메서드로 패널 오픈 (모든 체크 우회)
            mapSelectCtrl.CheatOpenNodePanel(firstNode);
            Debug.Log($"[CheatCodeManager] {nodeNumber}층 노드 패널 강제 오픈 완료");
        }
        else
        {
            Debug.LogError($"[CheatCodeManager] {nodeNumber}층 노드를 찾을 수 없습니다");
        }
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// 디버그용: 현재 상태 출력
    /// </summary>
    [ContextMenu("Debug Cheat Manager Status")]
    public void DebugStatus()
    {
        Debug.Log("=== CheatCodeManager 상태 ===");
        Debug.Log($"UIManager: {uiManager != null}");
        Debug.Log($"RuneDatabase: {runeDB != null}");
        Debug.Log($"MonsterCatalog: {monsterCatalog != null}");
        Debug.Log($"InputField: {cheatInputField != null}");
        Debug.Log($"등록된 명령어 개수: {cheatCommands?.Count ?? 0}");
    }

    #endregion
}
