using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// 1. 상점에 서 고블린 사면 고블린 인벤에 들어가게 수정
// 2. 인벤 설명 끝나고 인벤 자동으로 닫히게 수정
// 3. 그후에 전투관련으로 계속 수정해야함 


/// <summary>
/// 튜토리얼 단계 정의
/// </summary>
public enum TutorialStep
{
    None = 0,
    Introduction = 1,          // 케르베라 소개
    SelectDemonLord = 2,       // 마왕 선택
    OpenRecruitPanel = 3,      // 유닛 모집 클릭
    BuyGoblins = 4,            // 고블린 5명 구매
    OpenEnhancement = 5,       // 유닛 강화 클릭
    OpenCodex = 6,             // 도감 클릭
    SelectTutorialStage = 7,   // 튜토리얼 스테이지 선택
    FormationGoblin = 8,       // 고블린 5명 편성
    FormationRune = 9,         // 룬 장착
    StartBattle = 10,          // 전투 시작
    BattleSelectUnit = 11,     // 전투: 고블린 군단 선택
    BattleRushSkill = 12,  // 전투: 달려들기 스킬
    BattleAttackSkill = 13,      // 전투: 일반공격
    Complete = 14              // 완료
}

/// <summary>
/// 튜토리얼 전용 몬스터 데이터 (임시)
/// </summary>
[System.Serializable]
public class TutorialMonsterData
{
    public string monsterId;
    public int count;

    public TutorialMonsterData(string id, int cnt)
    {
        monsterId = id;
        count = cnt;
    }
}

/// <summary>
/// 튜토리얼 매니저 - 전체 튜토리얼 시스템 총괄
/// DontDestroyOnLoad로 씬 전환에도 유지
/// </summary>
public class TutorialManager : MonoBehaviour
{
    #region 싱글톤

    private static TutorialManager instance;

    public static TutorialManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에 존재하는 TutorialManager 찾기
                instance = FindAnyObjectByType<TutorialManager>();

                // 없으면 새로 생성
                if (instance == null)
                {
                    GameObject obj = new GameObject("TutorialManager");
                    instance = obj.AddComponent<TutorialManager>();
                    DontDestroyOnLoad(obj);
                    Debug.Log("[TutorialManager] 자동 생성됨");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        Debug.Log($"[TutorialManager] Awake 호출됨! (씬: {SceneManager.GetActiveScene().name})");
        Debug.Log($"[TutorialManager] 부모 오브젝트: {(transform.parent != null ? transform.parent.name : "없음 (루트)")}");

        //  튜토리얼 완료 여부 먼저 체크 (PlayerPrefs 직접 확인)
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (tutorialCompleted == 1)
        {
            Debug.Log("[TutorialManager] 튜토리얼 이미 완료됨 - 자동 파괴");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[TutorialManager] instance 확인: {(instance != null ? "존재함" : "null")}");

        if (instance != null && instance != this)
        {
            Debug.LogWarning("[TutorialManager] 중복된 인스턴스가 감지되어 제거됩니다.");
            Debug.Log($"[TutorialManager] 기존 instance: {instance.name}, 현재 this: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 핵심: 부모가 있으면 먼저 분리! DontDestroyOnLoad는 루트 오브젝트만 가능!
        if (transform.parent != null)
        {
            Debug.LogWarning($"[TutorialManager]  부모 오브젝트({transform.parent.name})에서 분리 중...");
            transform.SetParent(null);
            Debug.Log("[TutorialManager]  루트 오브젝트로 분리 완료");
        }

        DontDestroyOnLoad(gameObject);
        Debug.Log("[TutorialManager] DontDestroyOnLoad 설정 완료");
        Debug.Log($"[TutorialManager] DontDestroyOnLoad 호출 직후 씬: {gameObject.scene.name}");

        // 씬 로드 이벤트 구독 (배틀씬 전환 감지용)
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[TutorialManager] SceneManager.sceneLoaded 이벤트 구독 완료");
    }

    void OnDestroy()
    {
        Debug.Log($"[TutorialManager] 파괴 시점 - IsTutorialMode: {IsTutorialMode}");
        Debug.Log($"[TutorialManager] 파괴 시점 - 현재 씬: {SceneManager.GetActiveScene().name}");
        Debug.Log($"[TutorialManager] 파괴 시점 - instance == this: {instance == this}");
        Debug.LogWarning("[TutorialManager] TutorialManager가 파괴되었습니다! 이후 OnSceneLoaded가 호출되지 않습니다!");

        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region 참조 및 데이터

    [Header("튜토리얼 모드 설정")]
    [Tooltip("튜토리얼 모드 활성화 여부 - 이 값이 true일 때 실제 게임 데이터가 변경되지 않음")]
    public static bool IsTutorialMode = false;

    [Header("현재 진행 상태")]
    [SerializeField] private TutorialStep currentStep = TutorialStep.None;

    [Header("튜토리얼 전용 임시 데이터")]
    [Tooltip("튜토리얼 중 구매한 가상 몬스터")]
    public List<TutorialMonsterData> tutorialMonsters = new List<TutorialMonsterData>();

    [Tooltip("튜토리얼에서 지급되는 스피드 룬")]
    public RuneSO tutorialSpeedRune;

    [Tooltip("튜토리얼 전용 편성 데이터 (unitId : count)")]
    public Dictionary<string, int> tutorialAssignments = new Dictionary<string, int>();

    [Tooltip("튜토리얼 전용 가상 재화 (실제 재화는 변경하지 않고 UI만 표시)")]
    public int tutorialGold = 0;
    public int tutorialIron = 0;
    private int initialGold = 0;  // 시작 시 재화 (복구용)
    private int initialIron = 0;

    [Tooltip("튜토리얼 구매 허용 플래그 (대화 완료 후 true)")]
    public bool canPurchase = false;

    [Header("UI 참조")]
    [Tooltip("대화 패널 - 대사 표시용")]
    public TutorialDialoguePanel dialoguePanel;

    [Tooltip("하이라이트 시스템 - 버튼 강조용")]
    public TutorialHighlight highlightSystem;

    public GameObject demonSelectPanel;

    // 대사 데이터 (각 단계별 대사)
    private Dictionary<TutorialStep, string> dialogues;

    private UIManager UIManager;
    #endregion

    #region 튜토리얼 상태 프로퍼티

    /// <summary>
    /// 현재 튜토리얼 단계
    /// </summary>
    public TutorialStep CurrentStep => currentStep;

    /// <summary>
    /// 튜토리얼 완료 여부 (PlayerPrefs에서 확인)
    /// </summary>
    public bool HasCompletedTutorial => PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

    #endregion

    #region 유니티 라이프사이클

    void Start()
    {
        Debug.Log("[TutorialManager] Start() 호출됨");
        Debug.Log($"[TutorialManager] IsTutorialMode: {IsTutorialMode}");
        Debug.Log($"[TutorialManager] currentStep: {currentStep}");

        // UI 자동 찾기 (Inspector에서 연결되지 않았을 경우)
        AutoFindUIComponents();

        // UI 연결 확인
        if (dialoguePanel == null)
        {
            Debug.LogError("[TutorialManager] DialoguePanel이 연결되지 않았습니다!");
        }
        else
        {
            Debug.Log("[TutorialManager] DialoguePanel 연결 확인됨");
        }

        if (highlightSystem == null)
        {
            Debug.LogWarning("[TutorialManager] HighlightSystem이 연결되지 않았습니다 (선택사항)");
        }
        else
        {
            Debug.Log("[TutorialManager] HighlightSystem 연결 확인됨");

            // NextButton을 항상 활성화 버튼으로 설정
            Button nextButton = FindButton("NextButton");
            if (nextButton != null)
            {
                highlightSystem.SetAlwaysActiveButton(nextButton);
                Debug.Log("[TutorialManager] NextButton을 항상 활성화 버튼으로 설정");
            }
            else
            {
                Debug.LogWarning("[TutorialManager] NextButton을 찾을 수 없습니다!");
            }
        }

        // 튜토리얼용 스피드 룬 로드 (Resources 폴더에서)
        if (tutorialSpeedRune == null)
        {
            tutorialSpeedRune = Resources.Load<RuneSO>("Runes/Common/SpeedRune");
            if (tutorialSpeedRune == null)
            {
                Debug.LogWarning("[TutorialManager] 스피드 룬을 찾을 수 없습니다!");
            }
        }

        // 대사 데이터 초기화
        InitializeDialogues();
        Debug.Log($"[TutorialManager] 대사 데이터 {dialogues.Count}개 초기화 완료");
    }

    /// <summary>
    /// UI 컴포넌트 자동 찾기
    /// Inspector에서 연결되지 않았을 경우 씬에서 검색
    /// </summary>
    private void AutoFindUIComponents()
    {
        // DialoguePanel 자동 찾기
        if (dialoguePanel == null)
        {
            dialoguePanel = FindAnyObjectByType<TutorialDialoguePanel>();
            if (dialoguePanel != null)
            {
                Debug.Log("[TutorialManager]  DialoguePanel 자동으로 찾음");
            }
        }

        // HighlightSystem 자동 찾기
        if (highlightSystem == null)
        {
            highlightSystem = FindAnyObjectByType<TutorialHighlight>();
            if (highlightSystem != null)
            {
                Debug.Log("[TutorialManager]  HighlightSystem 자동으로 찾음");
            }
        }

        // DemonSelectPanel 자동 찾기
        if (demonSelectPanel == null)
        {
            var panel = FindAnyObjectByType<DemonLordSelectPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                demonSelectPanel = panel.gameObject;
                Debug.Log($"[TutorialManager] DemonSelectPanel 자동으로 찾음: {demonSelectPanel.name}");
            }
            else
            {
                Debug.LogWarning("[TutorialManager] DemonSelectPanel을 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 대사 데이터 초기화
    /// </summary>
    private void InitializeDialogues()
    {
        dialogues = new Dictionary<TutorialStep, string>()
        {
            { TutorialStep.Introduction,                             // 이 이상 넘으면 ㅇㄷ
                "안녕하십니까, 마왕님.\n" +
                "마왕님이 전쟁을 결정하신 후 새로 발탁된\n" +
                "측근인 케르베라고 합니다.|" +
                "이후 마왕님의 곁에서 보좌하겠습니다." },

            { TutorialStep.SelectDemonLord,
                "송구스럽지만, 마왕님의 권능이 어떤 것인지\n" +
                "여쭤봐도 되겠습니까?|" +
                "죄송합니다, 측근으로서 당연히 알고 있어야\n" +
                "할 일이지만..|" +
                "이 영광스러운 마왕님의 측근이라는 자리를\n" +
                "제가 맡게 될 줄은 몰랐습니다.|" +
                "마왕님께서는 어떤 권능을 가지고 계신지요?" },

            { TutorialStep.OpenRecruitPanel,
                "그게 마왕님이 행사하시는 권능이군요.|" +
                "알려주셔서 감사합니다, 이 전쟁은\n" +
                "마왕님의 그 권능으로 시작될 것입니다.|" +
                "저는 이제 마왕군에 들어갈 유닛을 모집하러\n" +
                "가려고 합니다.|" +
                "..마왕님께서 직접 가신다고요?|" +
                "알겠습니다. \n" +
                "제가 간단한 설명을 해 드리겠습니다.|" +
                "화면 우측 상단의 모집을 눌러주시겠습니까?"},

            { TutorialStep.BuyGoblins,
                "여기가 마왕군으로 들어올 유닛을 \n" +
                "모집하는 곳입니다.|" +
                "해당 유닛의 아래쪽에 보이는 숫자가\n" +
                "1명을 영입할 때 필요한 자원입니다.|" +
                "현재 보유하신 자원은 제가 왼쪽 위에\n" +
                "보시기 쉽도록 정리해 두었습니다.|" +
                "왼쪽부터 순서대로\n" +
                "골드, 철, 다이아몬드, 미스릴입니다만,\n" +
                "모집에는 골드와 철만 사용됩니다.|" +
                "마왕님, 제가 한 번만 어느 유닛을 모집할지\n" + 
                "추천해 드려도 되겠습니까?|" +
                "자비에 감사드립니다.\n" +
                "이번만 가지고 계신 자원을 전부 소모하여\n" +
                "고블린을 5명 모집해주십시오."},

            { TutorialStep.OpenEnhancement,
                "고블린 5명을 마왕군으로 모집했습니다.|" +
                "이제 모집한 유닛들은 마왕님의 명에 따라\n" + 
                "군단에 들어가게 됩니다.|" +
                "군단의 설명 전에, 다이아몬드와 미스릴에\n" +
                "대한 설명을 마저 드리겠습니다.|" +
                "우측 상단의 강화를 눌러 주시겠습니까?"},

            { TutorialStep.OpenCodex,
                "이곳이 마왕군을 강화하는 곳입니다.|" +
                "다이아몬드를 사용해서 빈 공간을 활성화하면\n" +
                "해당 종족 전체가 강화됩니다.|" +
                "현재는 다이아몬드가 부족해서\n" +
                "강화를 하지 못하지만,|" +
                "마왕님께서 스테이지를 클리어할수록\n" +
                "인간의 자원을 약탈하여\n"+
                "더 많은 골드와 철, 다이아몬드가 모일 겁니다.|" +
                "하지만 마지막 자원인 미스릴은\n" +
                "쉽게 얻을 수 없는 희귀한 자원입니다.|" +
                "미스릴은 혼자 강력한 전투력을 자랑하는\n" +
                "유닛들을 모집 및 강화하는데에 쓰입니다만..|" +
                "강력한 적들에게서 얻거나, 광산에서 얻거나,\n"+
                "비밀 교환소에서 대가를 주고 얻는 수 밖에는\n"+
                "없습니다.|" +
                "아차, 제가 광산이나 교환소의 설명을\n" +
                "드리지 않았군요.|" +
                "제가 마왕님께서 보시기 쉽도록\n" +
                "정리해놓은 책을 드리겠습니다.|" +
                "우측 상단의 도감을 눌러 주시겠습니까?"},

            { TutorialStep.SelectTutorialStage,
                "여기에는 마왕님께서 모집하신 유닛이나,\n" +
                "마주친 적 등을 확인할 수 있습니다.|" +
                "스테이지를 나아가시면서 보실 장소,\n" +
                "그리고 전투에 필요한 정보 등도\n" +
                "정리해두었습니다.|" +
                "적의 경우에는 한 번 마주친다면 저절로\n"+
                "정보들이 기록되는 마법의 책이랍니다.|" +
                "......|" +
                "첩보부의 소식을 보고드리겠습니다.|" +
                "근처까지 접근한 인간들이 있다고 합니다.|" +
                "해당 장소로 제가 안내해 드리겠습니다.|" +
                "참, 제 도감은 마왕님께서 갖고 계시다가,\n" +
                "필요하실 때마다 봐 주십시오.|" +
                "인간들이 와 있는 지역은 저쪽입니다.\n" +
                "지도의 빨간 깃발을 눌러 출격해주시겠습니까?"},

            { TutorialStep.FormationGoblin,
                "방금처럼 마왕님이 가실 곳을 클릭하시면,\n" +
                "해당 위치의 정보가 나타납니다.|" +
                "보셨던 위치에는 인간 전사 3명이 확인됐습니다.\n" +
                "바로 마왕군을 편성하러 가보겠습니다.|" +
                "여기가 마왕군이 출격을 대기하는 곳입니다.|" +
                "좌측에는 현재 마왕군 소속의 유닛들이,\n"+
                "우측에는 유닛들이 들어갈 자리가 있습니다.|" +
                "제 1군단에 고블린 5명을 편성해서,\n" +
                "고블린 군단을 만들어 주십시오." },

            { TutorialStep.FormationRune,
                "고블린 군단이 편성되었습니다.\n" +
                "그리고, 몇 가지 정보를 보고드리겠습니다.|" +
                "먼저, 유닛들은 나눠서 편성하실 수 없습니다.|" +
                "1군단에 고블린이 들어갔다면, 2군단이나\n"+
                "3군단에는 고블린이 들어가지 않습니다.|" +
                "또한 한 군단에 여러 종류의 유닛을\n" +
                "편성하시는 것도 불가능합니다.|" +
                "즉 고블린, 리자드맨 등 두 가지 이상의\n"+
                "유닛을 한 군단에 편성 할 수 없습니다.|" +
                "이렇게나마 함께 싸우는 것도 마왕님의\n"+
                "이름 아래 모였기 때문에 가능한 일이랍니다.|" +
                "추가로, 가운데에 있는 돌은 저희 마왕군의\n" +
                "주 병기 중 하나인 룬이라고 합니다.|" +
                "강화를 최대로 마치면 한 군단당 2개까지\n" +
                "장착할 수 있으며, 각 룬은 특이한 효과를\n"+
                "적용해 줍니다.|" +
                "현재는 1군단만 룬을 1개 장착할 수 있습니다.|" +
                //"하지만 마왕님이시라면 곧 모든 군단에 룬이\n"+
                //"장착 가능하게 해주실 수 있으실 겁니다.|" +
                "이 룬은 제 가문의 가보로 내려온 룬입니다,\n" +
                "마왕님의 뜻대로 사용해 주십시오.|" +
                "룬을 한번 부대에 적용해 보시겠습니까?"},

            { TutorialStep.StartBattle,
                "이제 출정 준비가 충분히 되었습니다.\n" +
                "마왕님이 진군 명령을 내려주시면 바로\n" +
                "출정하겠습니다!" },

            { TutorialStep.BattleSelectUnit,
                "전투로 들어가기에 앞서,\n" +
                "간단한 설명을 드리겠습니다.|" +
                "전투는 턴제 방식으로 진행되며,\n" +
                "속도에 따라 선공권이 결정됩니다.|" +
                "1턴이 지날 때마다 다시 선공권이 결정되니\n"+
                "참고해 주십시오.|" +
                "전방에 인간들이 나타났습니다!|" +
                "인간들 역시 다양한 스킬을 사용 가능하니\n" +
                "주의를 기울이고 응전하시길 바랍니다!" },

/*            { TutorialStep.BattleRushSkill,
                "각 군단은 일반 공격과 스킬 2개. \n"+
                "총 세가지 행동을 할 수 있습니다.|" +
                "고블린들은 목표로 지정된 대상에게\n" +
                "더 큰 피해를 입히는 '달려들기'|" +
                "상대의 속도를 감소시키고 목표로 지정하는\n" +
                "'포위하기'를 사용할 수 있습니다.|" +
                "적을 선택하신 다음, '달려들기'를\n" +
                "사용해 보십시오." },

            { TutorialStep.BattleAttackSkill,
                "적이 공격을 버텨내고 스킬을 써서\n" +
                "체력을 조금 회복했습니다.|" +
                "군단은 체력이 줄어들수록\n" +
                "유닛의 수가 소모됩니다.|" +
                "때문에 유닛이 줄었다면 그만큼 턴이 시작될 때\n" +
                "공격력이 줄어들게 됩니다.|" +
                "하지만 그것은 아군도 똑같으니\n" +
                "주의하셔야 합니다,\n" +
                "계속해서 공격하는것을 추천드립니다!|" +
                "다시 달려들기 스킬을 사용하기에는\n" +
                "군단이 준비가 되지 않은것 같습니다.|" +
                "이번에는 일반 공격으로 적들을 공격해주세요."},*/

            { TutorialStep.Complete,
                "저희 마왕군의 영광스러운 첫 승리입니다!|" +
                "마왕님이 전장에 계시는 것만으로도\n" +
                "저희는 그 어떤 전투에서도 이길 수 있습니다.|" +
                "주변 정리가 끝났습니다.\n" +
                "저는 이제부터 마왕님을\n" +
                "전력으로 보좌하겠습니다.|" +
                "군단을 지휘하시어\n" +
                "부디 대륙을 정복하여 주십시오!" }
        };
    }

    #endregion

    #region Dialogue Helper

    /// <summary>
    /// 대사를 순차적으로 보여주는 헬퍼 함수
    /// </summary>
    /// <param name="fullDialogue">전체 대사 (|로 구분)</param>
    /// <param name="onComplete">모든 대사 완료 시 콜백</param>
    /// <param name="autoHide">마지막 대사 후 자동 숨김 여부 (기본값: true)</param>
    /// <param name="lockButtons">대화 중 버튼 잠금 여부 (기본값: false)</param>
    private void ShowDialogueSequence(string fullDialogue, System.Action onComplete, bool autoHide = true, bool lockButtons = false)
    {
        // |로 대사 나누기
        string[] dialogueParts = fullDialogue.Split('|');

        // 마지막 부분 공백 제거
        for (int i = 0; i < dialogueParts.Length; i++)
        {
            dialogueParts[i] = dialogueParts[i].Trim();
        }

        // 첫 번째 대사부터 순차적으로 표시
        ShowDialogueRecursive(dialogueParts, 0, () =>
        {
            onComplete?.Invoke();
        }, autoHide);
    }

    /// <summary>
    /// 재귀적으로 대사를 하나씩 표시
    /// </summary>
    private void ShowDialogueRecursive(string[] dialogueParts, int currentIndex, System.Action onComplete, bool autoHide)
    {
        // 모든 대사를 다 보여줬으면 완료 콜백 실행
        if (currentIndex >= dialogueParts.Length)
        {
            onComplete?.Invoke();
            return;
        }

        // 현재 대사 표시
        if (dialoguePanel != null)
        {
            // 마지막 대사인지 확인
            bool isLastDialogue = (currentIndex == dialogueParts.Length - 1);

            dialoguePanel.ShowDialogue(
                dialogueParts[currentIndex],
                () =>
                {
                    // 다음 대사로 이동
                    ShowDialogueRecursive(dialogueParts, currentIndex + 1, onComplete, autoHide);
                },
                autoHide: isLastDialogue ? autoHide : true  // 마지막 대사만 autoHide 옵션 적용, 나머지는 자동 숨김
            );
        }
        else
        {
            // dialoguePanel이 없으면 그냥 완료
            onComplete?.Invoke();
        }
    }

    #endregion

    #region Tutorial Control

    /// <summary>
    /// 튜토리얼 시작
    /// </summary>
    public void StartTutorial()
    {
        Debug.Log("[TutorialManager] 튜토리얼 시작!");

        IsTutorialMode = true;
        currentStep = TutorialStep.Introduction;

        // GameRunState 초기화 (이전에 선택한 마왕 정보 제거)
        if (GameRunState.Instance != null)
        {
            GameRunState.Instance.chosenLord = null;
            GameRunState.Instance.isLoadedSave = false;
            Debug.Log("[TutorialManager] GameRunState 초기화 완료 (chosenLord = null, isLoadedSave = false)");
        }

        // 임시 데이터 초기화
        tutorialMonsters.Clear();
        tutorialAssignments.Clear();

        // 초기 재화 설정 (마왕 능력에 따라 다름 - 여기서는 기본값)
        initialGold = StaticInfoManager.gold;
        initialIron = StaticInfoManager.iron;
        tutorialGold = 5000;
        tutorialIron = 0;

        UIManager = FindAnyObjectByType<UIManager>();
        UIManager.UpdateUI();

        Debug.Log($"[TutorialManager] 초기 재화 설정: Gold={tutorialGold}, Iron={tutorialIron}");

        // 튜토리얼 시작 시 모든 노드 비활성화 (순서 건너뛰기 방지)
        DisableAllNodes();

        // 첫 단계 시작
        ProcessCurrentStep();
    }

    /// <summary>
    /// 튜토리얼 건너뛰기
    /// </summary>
    public void SkipTutorial()
    {
        Debug.Log("[TutorialManager] 튜토리얼 건너뛰기");
        StartCoroutine(SkipTutorialCoroutine());
    }

    /// <summary>
    /// 튜토리얼 건너뛰기 코루틴 (GameRunState 초기화 후 패널 열기)
    /// </summary>
    private System.Collections.IEnumerator SkipTutorialCoroutine()
    {
        IsTutorialMode = false;
        currentStep = TutorialStep.None;

        // 기본 룬 3개 지급 (코루틴으로 딜레이 후 호출) - 발표 끝나서 비활성화
        // StartCoroutine(GrantStartingRunesDelayed());

        // 모든 노드 다시 활성화
        EnableAllNodes();

        // 완료 플래그 저장 (다음에 안 뜨도록)
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        Debug.Log("[TutorialManager]  건너뛰기 완료! PlayerPrefs 저장됨");

        // GameRunState 초기화 (마왕 선택 가능하도록)
        if (GameRunState.Instance != null)
        {
            GameRunState.Instance.chosenLord = null;
            GameRunState.Instance.isLoadedSave = false;
            Debug.Log("[TutorialManager] GameRunState 초기화 완료 (chosenLord = null, isLoadedSave = false)");
        }

        //  프레임 대기 (GameRunState 초기화가 반영될 시간 확보)
        yield return null;

        // 마왕 선택 패널 열기
        var demonLordSelectPanel = FindAnyObjectByType<DemonLordSelectPanel>(FindObjectsInactive.Include);
        if (demonLordSelectPanel != null)
        {
            Debug.Log($"[TutorialManager] DemonLordSelectPanel 찾음: {demonLordSelectPanel.name}, 활성화 상태: {demonLordSelectPanel.gameObject.activeSelf}");

            //  강제로 패널 활성화 (OnEnable 체크 무시)
            demonLordSelectPanel.gameObject.SetActive(false); // 먼저 꺼서 OnEnable 재실행
            yield return null; // 프레임 대기
            demonLordSelectPanel.gameObject.SetActive(true);  // 다시 켜기

            Debug.Log("[TutorialManager]  마왕 선택 패널 열림 (GameRunState 초기화 후)");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] DemonLordSelectPanel을 찾을 수 없습니다!");
        }

        // DontDestroyOnLoad 오브젝트 정리
        if (dialoguePanel != null)
        {
            Destroy(dialoguePanel.gameObject);
            Debug.Log("[TutorialManager] TutorialDialoguePanel 파괴");
        }

        // 싱글톤 instance를 null로 설정 (재생성 방지)
        instance = null;

        // 튜토리얼 매니저 자신도 파괴
        Destroy(gameObject);
        Debug.Log("[TutorialManager] TutorialManager 파괴 및 instance null 처리 완료");
    }


    /// <summary>
    /// 다음 단계로 진행
    /// </summary>
    public void NextStep()
    {
        Debug.Log("=================================================");
        Debug.Log($"[TutorialManager] NextStep 호출됨! 현재: {currentStep}");

        // 다음 단계로 이동
        currentStep = (TutorialStep)((int)currentStep + 1);

        Debug.Log($"[TutorialManager] 다음 단계로 이동: {currentStep}");
        Debug.Log("=================================================");

        ProcessCurrentStep();
    }

    /// <summary>
    /// 현재 단계에 맞는 처리 실행
    /// </summary>
    private void ProcessCurrentStep()
    {
        Debug.Log("=================================================");
        Debug.Log($"[TutorialManager] 🎯 ProcessCurrentStep: {currentStep}");

        // NextButton 상태 확인
        Button nextBtn = FindButton("NextButton");
        if (nextBtn != null)
        {
            Debug.Log($"[TutorialManager] NextButton 상태: active={nextBtn.gameObject.activeInHierarchy}, interactable={nextBtn.interactable}");
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] ⚠️ NextButton을 찾을 수 없습니다! (단계: {currentStep})");
        }

        // alwaysActiveButton 설정 확인
        if (highlightSystem != null)
        {
            Debug.Log($"[TutorialManager] alwaysActiveButton 설정됨: {highlightSystem != null}");
        }
        Debug.Log("=================================================");

        switch (currentStep)
        {
            case TutorialStep.Introduction:
                ShowIntroduction();
                break;

            case TutorialStep.SelectDemonLord:
                ShowSelectDemonLord();
                break;

            case TutorialStep.OpenRecruitPanel:
                ShowOpenRecruitPanel();
                break;

            case TutorialStep.BuyGoblins:
                ShowBuyGoblins();
                break;

            case TutorialStep.OpenEnhancement:
                ShowOpenEnhancement();
                break;

            case TutorialStep.OpenCodex:
                ShowOpenCodex();
                break;

            case TutorialStep.SelectTutorialStage:
                ShowSelectTutorialStage();
                break;

            case TutorialStep.FormationGoblin:
                ShowFormationGoblin();
                break;

            case TutorialStep.FormationRune:
                ShowFormationRune();
                break;

            case TutorialStep.StartBattle:
                ShowStartBattle();
                break;

/*            case TutorialStep.BattleSelectUnit:
                ShowBattleSelectUnit();
                break;

            case TutorialStep.BattleRushSkill:
                ShowBattleSurroundSkill();
                break;

            case TutorialStep.BattleAttackSkill:
                ShowBattleRushSkill();
                break;*/

            case TutorialStep.Complete:
                CompleteTutorial();
                break;
        }
    }

    #endregion

    #region Step Implementations

    /// <summary>
    /// 1단계: 대화 출력
    /// </summary>
    private void ShowIntroduction()
    {
        Debug.Log("[TutorialManager] ShowIntroduction 호출됨");

        if (dialoguePanel != null)
        {
            Debug.Log("[TutorialManager] DialoguePanel 찾음 - 대화 표시 시도");
            ShowDialogueSequence(
                dialogues[TutorialStep.Introduction],
                () => NextStep()
            );
        }
        else
        {
            Debug.LogError("[TutorialManager] DialoguePanel이 연결되지 않았습니다! Inspector에서 연결하세요!");
            NextStep();
        }
    }

    /// <summary>
    /// 2단계: 마왕 선택
    /// </summary>
    private void ShowSelectDemonLord()
    {
        Debug.Log("[TutorialManager] ShowSelectDemonLord 호출됨");

        if (dialoguePanel != null)
        {
            Debug.Log("[TutorialManager] DialoguePanel 찾음 - 대화 표시 시도");
            ShowDialogueSequence(
                dialogues[TutorialStep.SelectDemonLord],
                () =>
                {
                    // demonSelectPanel이 null이면 다시 찾기
                    if (demonSelectPanel == null)
                    {
                        var panel = FindAnyObjectByType<DemonLordSelectPanel>(FindObjectsInactive.Include);
                        if (panel != null)
                        {
                            demonSelectPanel = panel.gameObject;
                            Debug.Log($"[TutorialManager] DemonSelectPanel 재탐색 성공: {demonSelectPanel.name}");
                        }
                        else
                        {
                            Debug.LogError("[TutorialManager] DemonSelectPanel을 찾을 수 없습니다! 튜토리얼 건너뜀");
                            NextStep();
                            return;
                        }
                    }

                    demonSelectPanel.SetActive(true);
                    Debug.Log("[TutorialManager] 마왕 선택 창 활성화 (대화 창은 유지됨)");
                    Debug.Log("[TutorialManager] 마왕 선택 대기 중... (DemonLordSelectPanel에서 NextStep 호출 예정)");
                },
                autoHide: true  //  대화 창 유지!
            );
        }
        else
        {
            Debug.LogError("[TutorialManager]  DialoguePanel이 null입니다!");
            NextStep();
        }
    }

    /// <summary>
    /// 3단계: 유닛 모집 패널 열기
    /// </summary>
    private void ShowOpenRecruitPanel()
    {
        if (dialoguePanel != null)
        {

            ShowDialogueSequence(
                dialogues[TutorialStep.OpenRecruitPanel],
                () =>
                {
                    Button recruitBtn = FindButton("모집");
                    if (recruitBtn == null)
                    {
                        recruitBtn = FindButton("UnitRecruitButton");
                    }

                    if (recruitBtn != null)
                    {
                        if (highlightSystem != null)
                        {
                            highlightSystem.HighlightButton(recruitBtn);
                            EnableSpecificButton("UnitRecruitButton");
                        }

                        UnityEngine.Events.UnityAction tempAction = null;
                        tempAction = () =>
                        {
                            recruitBtn.onClick.RemoveListener(tempAction);
                            ClearHighlight();
                            Debug.Log("[TutorialManager] 모집 패널 열림");

                            // 상점 열렸을 때 닫기 버튼 비활성화
                            DisableCloseButtons();

                            NextStep();
                        };

                        recruitBtn.onClick.AddListener(tempAction);
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialManager] 모집 버튼을 찾을 수 없습니다!");
                        NextStep();
                    }
                }
            );
        }
    }

    /// <summary>
    /// 4단계: 고블린 5명 구매
    /// </summary>
    private void ShowBuyGoblins()
    {
        if (dialoguePanel != null)
        {
            // 구매 차단 (대화 전까지)
            canPurchase = false;

            ShowDialogueSequence(
                dialogues[TutorialStep.BuyGoblins],
                () =>
                {
                    // 고블린 구매 버튼 찾기 (하이라이트는 하지 않음, 자유롭게 구매하도록)
                    ClearHighlight();

                    //대화 완료 → 구매 허용
                    canPurchase = true;
                    Debug.Log("[TutorialManager] 고블린 구매 허용 (대화 완료)");

                    // 고블린 5명 구매할 때까지 대기하는 코루틴 시작
                    StartCoroutine(WaitForGoblinPurchase());
                }
            );
        }
    }

    /// <summary>
    /// 고블린 5명 구매 대기 코루틴
    /// </summary>
    private System.Collections.IEnumerator WaitForGoblinPurchase()
    {
        Debug.Log("[TutorialManager] 고블린 구매 대기 중...");

        // 고블린 5명 구매할 때까지 대기 (enemyId "101")
        while (GetTutorialMonsterCount("101") < 5)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 5마리 달성 → 즉시 구매 차단
        canPurchase = false;
        Debug.Log("[TutorialManager] 고블린 5마리 구매 완료! 구매 차단");

        yield return new WaitForSeconds(0.5f);
        NextStep();
    }

    /// <summary>
    /// 5단계: 강화 패널 열기
    /// </summary>
    private void ShowOpenEnhancement()
    {
        if (dialoguePanel != null)
        {
            ShowDialogueSequence(
                dialogues[TutorialStep.OpenEnhancement],
                () =>
                {
                    Button enhancementBtn = FindButton("강화");
                    if (enhancementBtn == null)
                    {
                        enhancementBtn = FindButton("EnhancementButton");
                    }

                    if (enhancementBtn != null)
                    {
                        if (highlightSystem != null)
                        {
                            highlightSystem.HighlightButton(enhancementBtn);
                            EnableSpecificButton("EnhancementButton");
                        }

                        UnityEngine.Events.UnityAction tempAction = null;
                        tempAction = () =>
                        {
                            enhancementBtn.onClick.RemoveListener(tempAction);
                            ClearHighlight();
                            Debug.Log("[TutorialManager] 강화 패널 열림");
                            NextStep();
                        };

                        enhancementBtn.onClick.AddListener(tempAction);
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialManager] 강화 버튼을 찾을 수 없습니다!");
                        NextStep();
                    }
                }
            );
        }
    }

    /// <summary>
    /// 6단계: 도감 패널 열기
    /// </summary>
    private void ShowOpenCodex()
    {
        Debug.Log("6단계 실행됨");
        if (dialoguePanel != null)
        {
            ShowDialogueSequence(
                dialogues[TutorialStep.OpenCodex],
                () =>
                {
                    Button codexBtn = FindButton("도감");
                    if (codexBtn == null)
                    {
                        codexBtn = FindButton("CodexButton");
                    }

                    if (codexBtn != null)
                    {
                        if (highlightSystem != null)
                        {
                            highlightSystem.HighlightButton(codexBtn);
                            EnableSpecificButton("CodexButton");
                        }

                        UnityEngine.Events.UnityAction tempAction = null;
                        tempAction = () =>
                        {
                            codexBtn.onClick.RemoveListener(tempAction);
                            ClearHighlight();
                            Debug.Log("[TutorialManager] 도감 패널 열림");

                            // 도감 열렸을 때 닫기 버튼 비활성화
                            DisableCloseButtons();

                            NextStep();
                        };

                        codexBtn.onClick.AddListener(tempAction);
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialManager] 도감 버튼을 찾을 수 없습니다!");
                        NextStep();
                    }
                }
            );
        }
    }

    /// <summary>
    /// 7단계: 튜토리얼 스테이지 선택
    /// </summary>
    private void ShowSelectTutorialStage()
    {
        if (dialoguePanel != null)
        {
            ShowDialogueSequence(
                dialogues[TutorialStep.SelectTutorialStage],
                () =>
                {
                    Debug.Log("[TutorialManager] SelectTutorialStage 대화 완료 - UI 정리 시작");

                    // 인벤토리 닫기
                    UIManager.OffInventoryTabClicked();
                    Debug.Log("[TutorialManager] 인벤토리 닫기 완료");

                    ClearHighlight();
                    Debug.Log("[TutorialManager] 하이라이트 제거 완료");

                    // 대화창 완전히 숨기기 확인
                    if (dialoguePanel != null)
                    {
                        dialoguePanel.HideDialogue();
                        Debug.Log("[TutorialManager] 대화창 강제 숨김");
                    }

                    // UI 안정화를 위한 짧은 대기 후 노드 하이라이트
                    StartCoroutine(HighlightTutorialNodeAfterDelay());
                }
            );
        }
    }

    /// <summary>
    /// 튜토리얼 노드 하이라이트 (UI 안정화 후)
    /// </summary>
    private System.Collections.IEnumerator HighlightTutorialNodeAfterDelay()
    {
        // UI가 완전히 정리될 때까지 짧은 대기
        yield return new WaitForSeconds(0.01f);

        // GB_Node 찾기 (Button이 아님!)
        GB_Node tutorialNode = FindTutorialNode();

        if (tutorialNode != null)
        {
            // 노드를 클릭 가능하도록 설정
            tutorialNode.clickable = true;

            Debug.Log($"[TutorialManager]  튜토리얼 노드 활성화 완료: {tutorialNode.name}, clickable={tutorialNode.clickable}, InstanceID={tutorialNode.GetInstanceID()}");
            Debug.Log($"[TutorialManager]  이제 맵에서 '{tutorialNode.name}' 노드를 클릭하세요!");

            // 모든 TutorialNode를 찾아서 전부 활성화 (인스턴스 불일치 방지)
            GB_Node[] allNodes = FindObjectsByType<GB_Node>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int activatedCount = 0;
            foreach (var node in allNodes)
            {
                string nodeName = node.name.ToLower();
                if (nodeName.Contains("tutorialnode") || nodeName.Contains("TutorialNode"))
                {
                    node.clickable = true;
                    activatedCount++;
                    Debug.Log($"[TutorialManager] 튜토리얼 노드 활성화: {node.name}, InstanceID={node.GetInstanceID()}");
                }
            }
            Debug.Log($"[TutorialManager] 총 {activatedCount}개의 튜토리얼 노드 활성화됨");

            // 노드 활성화 상태 계속 유지 (다른 시스템이 비활성화해도 다시 활성화)
            StartCoroutine(KeepTutorialNodeActive());

            // 노드 하이라이트 (GameObject 하이라이트 기능 사용)
            if (highlightSystem != null)
            {
                highlightSystem.HighlightGameObject(tutorialNode.gameObject);
                Debug.Log($"[TutorialManager] 튜토리얼 노드 하이라이트 완료: {tutorialNode.name}");
            }
            else
            {
                Debug.LogWarning("[TutorialManager] highlightSystem이 null입니다!");
            }

            // 노드 선택 대기 코루틴 시작
            StartCoroutine(WaitForNodeSelectionAndNext());
        }
        else
        {
            Debug.LogWarning("[TutorialManager] 튜토리얼 노드를 찾을 수 없습니다!");
            NextStep();
        }
    }

    /// <summary>
    /// 튜토리얼 노드 활성화 상태 유지 (계속 강제 활성화)
    /// </summary>
    private System.Collections.IEnumerator KeepTutorialNodeActive()
    {
        Debug.Log("[TutorialManager] 튜토리얼 노드 활성화 상태 유지 시작");

        while (currentStep == TutorialStep.SelectTutorialStage)
        {
            // 0.3초마다 모든 튜토리얼 노드를 찾아서 활성화
            GB_Node[] allNodes = FindObjectsByType<GB_Node>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var node in allNodes)
            {
                string nodeName = node.name.ToLower();
                if (nodeName.Contains("tutorialnode") || nodeName.Contains("TutorialNode"))
                {
                    if (!node.clickable)
                    {
                        node.clickable = true;
                        Debug.Log($"[TutorialManager]  노드 재활성화: {node.name}, InstanceID={node.GetInstanceID()}");
                    }
                }
            }

            yield return new WaitForSeconds(0.0000001f);
        }

        Debug.Log("[TutorialManager] 튜토리얼 노드 활성화 상태 유지 종료");
    }

    /// <summary>
    /// 노드 선택 대기 후 "진입" 버튼 하이라이트
    /// </summary>
    private System.Collections.IEnumerator WaitForNodeSelectionAndNext()
    {
        yield return StartCoroutine(WaitForNodeSelection());

        // 노드 선택 완료 후 하이라이트 해제
        ClearHighlight();
        Debug.Log("[TutorialManager] 노드 선택 완료 - 하이라이트 해제");

        // StageInfoPanel이 열릴 때까지 짧은 대기 (0.1초로 단축)
        // 패널이 열리자마자 하이라이트가 나타나도록 최소화
        yield return new WaitForSeconds(0.1f);

        // "진입" 버튼 찾기 및 하이라이트
        Button enterButton = FindButton("Enter");

        if (enterButton != null)
        {
            Debug.Log($"[TutorialManager] 진입 버튼 발견: {enterButton.name}");

            // 버튼 하이라이트
            if (highlightSystem != null)
            {
                highlightSystem.HighlightButton(enterButton);
                EnableSpecificButton(enterButton.name);
                Debug.Log("[TutorialManager] 진입 버튼 하이라이트 완료");
            }

            // 버튼 클릭 감지
            UnityEngine.Events.UnityAction tempAction = null;
            tempAction = () =>
            {
                enterButton.onClick.RemoveListener(tempAction);
                ClearHighlight();
                Debug.Log("[TutorialManager] 진입 버튼 클릭됨");

                // 다음 단계로 이동
                NextStep();

                DisableFormationTutorialButtons();
            };

            enterButton.onClick.AddListener(tempAction);
        }
        else
        {
            Debug.LogWarning("[TutorialManager] 진입 버튼을 찾을 수 없습니다! 자동으로 다음 단계로 이동");
            NextStep();
        }
    }

    /// <summary>
    /// 8단계: 고블린 5명 편성 (특별 처리 - 2단계로 나눔)
    /// </summary>
    private void ShowFormationGoblin()
    {
        // 편성 씬 진입 시 NextButton 재설정
        if (highlightSystem != null)
        {
            Button nextButton = FindButton("NextButton");
            if (nextButton != null)
            {
                highlightSystem.SetAlwaysActiveButton(nextButton);
                Debug.Log("[TutorialManager] 편성 씬에서 NextButton 재설정");
            }
        }

        if (dialoguePanel != null)
        {
            // 대사를 |로 나누기
            string[] allDialogues = dialogues[TutorialStep.FormationGoblin].Split('|');

            // 첫 번째 그룹 (인덱스 0, 1) - 편성 버튼 클릭 전
            string firstGroup = allDialogues[0];
            if (allDialogues.Length > 1 && !string.IsNullOrEmpty(allDialogues[1]))
            {
                firstGroup += "|" + allDialogues[1];
            }

            // 두 번째 그룹 (인덱스 2~끝) - 편성 버튼 클릭 후
            string secondGroup = "";
            for (int i = 2; i < allDialogues.Length; i++)
            {
                if (!string.IsNullOrEmpty(allDialogues[i].Trim()))
                {
                    if (!string.IsNullOrEmpty(secondGroup))
                        secondGroup += "|";
                    secondGroup += allDialogues[i];
                }
            }

            // 첫 번째 그룹 대사 표시 (버튼 잠금 활성화)
            ShowDialogueSequence(
                firstGroup,
                () =>
                {
                    // 편성 버튼 찾기 및 하이라이트
                    Button formationBtn = FindButton("SelectSquadSlotOne");

                    if (formationBtn != null)
                    {
                        if (highlightSystem != null)
                        {
                            highlightSystem.HighlightButton(formationBtn);
                            EnableSpecificButton("SelectSquadSlotOne");
                        }

                        UnityEngine.Events.UnityAction tempAction = null;
                        tempAction = () =>
                        {
                            formationBtn.onClick.RemoveListener(tempAction);
                            ClearHighlight();
                            Debug.Log("[TutorialManager] 편성 버튼 클릭됨");

                            // 편성 화면 진입 시 튜토리얼에 불필요한 버튼들 비활성화
                            DisableFormationTutorialButtons();

                            // 두 번째 그룹 대사 표시 (버튼 잠금 활성화)
                            ShowDialogueSequence(
                                secondGroup,
                                () =>
                                {
                                    // TODO(human): Problem 3 해결 - 1군단 클릭 후 고블린 배치 버튼 활성화
                                    // 문제: DisableFormationTutorialButtons()가 모든 버튼을 interactable=false로 만들었음
                                    // 대화가 끝나면 1군단이 자동으로 선택된 상태로 만들고,
                                    // 고블린 배치 버튼(+1, +10, -1, -10)을 활성화해야 함
                                    //
                                    // 해결 방법:
                                    // 1. SquadFormationManager.Instance.SelectSquad(0)을 호출하여 1군단 선택
                                    // 2. 이렇게 하면 OnSquadSelected 이벤트가 발생하고
                                    // 3. UnitSelectionCardUI.OnSquadSelectionChanged()가 호출되어
                                    // 4. RefreshUI() -> UpdateButtonStates()가 실행되어 버튼이 자동으로 활성화됨

                                    if (SquadFormationManager.Instance != null)
                                    {
                                        SquadFormationManager.Instance.SelectSquad(0);  // 1군단 선택
                                        Debug.Log("[TutorialManager] 1군단 자동 선택 - 고블린 배치 버튼 활성화");
                                    }

                                    // 고블린 5명 편성할 때까지 대기
                                    StartCoroutine(WaitForGoblinFormation());
                                },
                                true, // autoHide
                                true  // lockButtons
                            );
                        };

                        formationBtn.onClick.AddListener(tempAction);
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialManager] 편성 버튼을 찾을 수 없습니다!");
                        // 버튼이 없으면 바로 두 번째 그룹 표시
                        DisableFormationTutorialButtons();
                        ShowDialogueSequence(
                            secondGroup,
                            () =>
                            {
                                Button sqaud1 = FindButton("SelectSquadSlotOne");
                                Button sqaud2 = FindButton("SelectSquadSlotTwo");
                                Button sqaud3 = FindButton("SelectSquadSlotThrree");

                                if (sqaud1 != null) sqaud1.interactable = false;
                                if (sqaud2 != null) sqaud2.interactable = false;
                                if (sqaud3 != null) sqaud3.interactable = false;

                                Debug.Log("[TutorialManager] 1,2,3군단 버튼 비활성화");

                                StartCoroutine(WaitForGoblinFormation());
                            }
                        );
                    }
                }
            );
        }
    }

    /// <summary>
    /// 고블린 편성 대기 코루틴
    /// SquadFormationManager의 OnSquadsChanged 이벤트를 통해 실시간 감지
    /// </summary>
    private System.Collections.IEnumerator WaitForGoblinFormation()
    {
        Debug.Log("[TutorialManager] 고블린 편성 대기 중...");

        // SquadFormationManager가 준비될 때까지 대기
        while (SquadFormationManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 고블린 5명이 편성될 때까지 대기
        while (!IsGoblinSquadComplete())
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[TutorialManager] 고블린 5명 편성 완료!");
        NextStep();
    }

    /// <summary>
    /// 고블린 군단이 완성되었는지 확인
    /// 어느 한 군단에 고블린(101) 5명이 편성되어 있으면 true
    /// </summary>
    private bool IsGoblinSquadComplete()
    {
        if (SquadFormationManager.Instance == null) return false;

        var squads = SquadFormationManager.Instance.squads;
        if (squads == null) return false;

        // 3개 군단 중 하나라도 고블린 5명이면 완료
        foreach (var squad in squads)
        {
            if (squad != null && squad.memberIds != null && squad.memberIds.Count >= 5)
            {
                // 첫 번째 유닛 ID가 고블린(101)인지 확인
                if (squad.memberIds.Count > 0 && squad.memberIds[0] == "101")
                {
                    Debug.Log($"[TutorialManager] 고블린 군단 발견: {squad.name}, 개수: {squad.memberIds.Count}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 9단계: 룬 장착
    /// </summary>
    private void ShowFormationRune()
    {
        if (dialoguePanel != null)
        {
            ShowDialogueSequence(
                dialogues[TutorialStep.FormationRune],
                () =>
                {
                    // 룬 슬롯 하이라이트 (선택사항)
                    Button runeSlot = FindButton("RuneCard");
                    if (runeSlot != null && highlightSystem != null)
                    {
                        highlightSystem.HighlightButton(runeSlot);
                        EnableSpecificButton("RuneCard");
                    }

                    // 룬 장착 대기 코루틴 시작
                    StartCoroutine(WaitForRuneEquipped());
                }
            );
        }
    }

    /// <summary>
    /// 룬 장착 대기 코루틴
    /// SquadFormationManager의 OnRuneChanged 이벤트를 통해 실시간 감지
    /// </summary>
    private System.Collections.IEnumerator WaitForRuneEquipped()
    {
        Debug.Log("[TutorialManager] 룬 장착 대기 중...");

        // SquadFormationManager가 준비될 때까지 대기
        while (SquadFormationManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 어느 군단에든 룬이 1개 이상 장착될 때까지 대기
        while (!IsAnyRuneEquipped())
        {
            yield return new WaitForSeconds(0.5f);
        }

        ClearHighlight();
        Debug.Log("[TutorialManager] 룬 장착 완료!");
        NextStep();
    }

    /// <summary>
    /// 어느 군단에든 룬이 장착되어 있는지 확인
    /// </summary>
    private bool IsAnyRuneEquipped()
    {
        if (SquadFormationManager.Instance == null) return false;

        var squads = SquadFormationManager.Instance.squads;
        if (squads == null) return false;

        // 3개 군단 중 하나라도 룬이 장착되어 있으면 true
        foreach (var squad in squads)
        {
            if (squad != null && squad.runeSlots != null)
            {
                foreach (var runeSlot in squad.runeSlots)
                {
                    if (runeSlot != null && !string.IsNullOrEmpty(runeSlot.runeId))
                    {
                        Debug.Log($"[TutorialManager] 룬 장착 발견: {squad.name}, 룬 ID: {runeSlot.runeId}");
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 10단계: 전투 시작
    /// </summary>
    private void ShowStartBattle()
    {
        Debug.Log("[TutorialManager] ShowStartBattle 호출됨!");
        Debug.Log($"[TutorialManager] currentStep: {currentStep}");
        Debug.Log($"[TutorialManager] dialoguePanel: {(dialoguePanel != null ? "있음" : "null")}");

        if (dialoguePanel != null)
        {
            ShowDialogueSequence(
                dialogues[TutorialStep.StartBattle],
                () =>
                {
                    Debug.Log("[TutorialManager] ShowStartBattle 대화 완료 - 출정 버튼 검색 시작");

                    Button battleStartBtn = FindButton("출정");
                    Debug.Log($"[TutorialManager] '출정' 버튼 검색 결과: {(battleStartBtn != null ? "찾음" : "못 찾음")}");

                    if (battleStartBtn == null)
                    {
                        battleStartBtn = FindButton("BattleStart");
                        Debug.Log($"[TutorialManager] 'BattleStart' 버튼 검색 결과: {(battleStartBtn != null ? "찾음" : "못 찾음")}");
                    }

                    if (battleStartBtn != null)
                    {
                        Debug.Log($"[TutorialManager] 출정 버튼 발견: {battleStartBtn.name}");

                        // 대화 패널 먼저 숨기기
                        if (dialoguePanel != null)
                        {
                            dialoguePanel.HideDialogue();
                            Debug.Log("[TutorialManager] 대화 패널 숨김");
                        }

                        // 기존 onClick 리스너를 모두 제거 (자동 클릭 방지)
                        battleStartBtn.onClick.RemoveAllListeners();
                        Debug.Log("[TutorialManager] 출정 버튼의 기존 onClick 리스너 제거");

                        // 튜토리얼 전용 onClick 연결 (LoadingScene 거쳐서 이동)
                        battleStartBtn.onClick.AddListener(() =>
                        {
                            Debug.Log("[TutorialManager] 출정 버튼 클릭됨! 전투 씬으로 이동");
                            Debug.Log($"[TutorialManager] DontDestroyOnLoad 씬 확인: {gameObject.scene.name}");
                            Debug.Log($"[TutorialManager] instance 존재 여부: {instance != null}");

                            // 스테이지 정보 설정
                            var mapSelectCon = FindAnyObjectByType<MapSelectionController>();
                            if (mapSelectCon != null)
                            {
                                mapSelectCon.EnterSelectedStage();
                            }

                            //코루틴을 통해 DontDestroyOnLoad 씬 이동 확인 후 씬 전환
                            StartCoroutine(WaitForDontDestroyOnLoadThenLoadScene());
                        });
                        Debug.Log("[TutorialManager] 튜토리얼 전용 onClick 리스너 연결 완료");

                        Debug.Log("[TutorialManager] 버튼 활성화 시작...");

                        // BattleStart 버튼 활성화 (이제 클릭 가능)
                        battleStartBtn.interactable = true;
                        Debug.Log("[TutorialManager] battleStartBtn.interactable = true 완료");

                        if (highlightSystem != null)
                        {
                            Debug.Log("[TutorialManager] 하이라이트 시작...");
                            highlightSystem.HighlightButton(battleStartBtn);
                            Debug.Log("[TutorialManager] 하이라이트 완료");

                            EnableSpecificButton("BattleStart");
                            Debug.Log("[TutorialManager] EnableSpecificButton 완료");
                        }

                        // 출정 버튼은 원래 기능 그대로 사용 (씬 전환)
                        // 튜토리얼 매니저는 OnSceneLoaded에서 자동으로 전투 튜토리얼 시작
                        Debug.Log("[TutorialManager] 출정 버튼 활성화 완료 - 사용자가 클릭할 때까지 대기");
                        Debug.Log("[TutorialManager] ShowStartBattle 종료 - NextStep 호출하지 않음, 씬 전환 대기 중");

                        // NextStep()을 호출하지 않음!
                        // 사용자가 출정 버튼을 클릭하면 씬이 전환되고,
                        // OnSceneLoaded에서 전투 튜토리얼이 시작됨
                    }
                    else
                    {
                        Debug.LogError("[TutorialManager] 전투 시작 버튼을 찾을 수 없습니다!");
                        Debug.LogError("[TutorialManager] NextStep() 호출하지 않음 - 버튼을 찾을 때까지 대기");
                        // NextStep() 호출하면 튜토리얼이 종료되므로 호출하지 않음
                    }
                },
                autoHide: false
            );
        }
    }

    /// <summary>
    /// 씬 로드 감지 콜백
    /// 배틀씬으로 전환되면 전투 튜토리얼 시작 (처음만)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TutorialManager] OnSceneLoaded 호출됨!");
        Debug.Log($"[TutorialManager] 씬 이름: {scene.name}");
        Debug.Log($"[TutorialManager] IsTutorialMode: {IsTutorialMode}");
        Debug.Log($"[TutorialManager] currentStep: {currentStep}");

        // 로딩 씬은 무시
        if (scene.name == "LoadingScene")
        {
            Debug.Log("[TutorialManager] 로딩 씬 감지 - 무시");
            return;
        }

        // 튜토리얼 모드이고, StartBattle 단계에서 배틀씬으로 전환되면
        if (IsTutorialMode && currentStep == TutorialStep.StartBattle)
        {
            // 씬 이름 확인 
            string sceneName = scene.name.ToLower();
            if (sceneName.Contains("ex_battle_view"))
            {
                Debug.Log($"[TutorialManager] 배틀씬 진입 감지! ({scene.name}) 전투 튜토리얼 시작");

                // 짧은 딜레이 후 전투 튜토리얼 시작 (씬 초기화 대기)
                StartCoroutine(WaitAndStartBattleTutorial());
            }
            else
            {
                Debug.LogWarning($"[TutorialManager] 전투 씬으로 인식되지 않음: {scene.name}");
            }
        }
    }

    /// <summary>
    /// 배틀씬 로드 후 튜토리얼을 시작하고, HelpGuidPanel을 표시한 뒤 전투를 진행합니다.
    /// </summary>
    private System.Collections.IEnumerator WaitAndStartBattleTutorial()
    {
        // 0.01초 대기 (배틀씬 UI 초기화 대기)
        yield return new WaitForSeconds(0.001f);

        // 배틀씬에서 UI 컴포넌트 다시 찾기
        Debug.Log("[TutorialManager] 배틀씬 UI 컴포넌트 재검색 시작...");
        AutoFindUIComponents();

        // 전투 시작 대화 표시
        if (dialoguePanel != null)
        {
            // --- NEW MANUAL LOCKING LOGIC ---
            Debug.Log("[TutorialManager] 전투 씬 대화 시작. NextButton 외 모든 버튼 수동 잠금.");
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Button> buttonsToRestore = new List<Button>();

            foreach (var btn in allButtons)
            {
                // 이름으로 NextButton을 정확히 찾아 제외
                if (btn.name != "NextButton" && btn.interactable)
                {
                    buttonsToRestore.Add(btn);
                    btn.interactable = false;
                }
            }

            ShowDialogueSequence(
                dialogues[TutorialStep.BattleSelectUnit],
                () =>
                {
                    // --- NEW UNLOCKING LOGIC ---
                    Debug.Log("[TutorialManager] 대화 종료. 잠긴 버튼들을 다시 활성화합니다.");
                    foreach (var btn in buttonsToRestore)
                    {
                        if (btn != null) btn.interactable = true;
                    }

                    // 대화가 끝나면 HelpGuidPanel을 표시하고 자동 전투를 진행하는 코루틴 시작
                    StartCoroutine(ShowHelpPanelAndRunAutoBattle());
                },
                true,  // autoHide
                false  // lockButtons
            );
        }
        else
        {
            // 대화 패널이 없으면 바로 코루틴 시작
            StartCoroutine(ShowHelpPanelAndRunAutoBattle());
        }
    }

    private System.Collections.IEnumerator ShowHelpPanelAndRunAutoBattle()
    {
        Debug.Log("[TutorialManager] HelpGuidPanel 검색 시작...");
        GameObject helpGuidPanel = null;
        RectTransform[] allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log($"[TutorialManager] 씬에서 {allRects.Length}개의 RectTransform을 찾았습니다. HelpGuidPanel을 검색합니다:");

        foreach (var rt in allRects)
        {
            // 이름에 "Panel" 또는 "Guid"가 포함된 모든 오브젝트의 이름을 로그로 출력
            if (rt.name.ToLower().Contains("panel") || rt.name.ToLower().Contains("guid"))
            {
                Debug.Log($"[TutorialManager] - 후보 오브젝트: {rt.name}");
            }

            // 대소문자 구분 없이 이름 비교
            if (rt.name.Equals("HelpGuidPanel", System.StringComparison.OrdinalIgnoreCase))
            {
                helpGuidPanel = rt.gameObject;
                // 찾았더라도 모든 후보를 보기 위해 break하지 않음
            }
        }

        if (helpGuidPanel != null)
        {
            Debug.Log("[TutorialManager] HelpGuidPanel 찾음. 활성화합니다.");
            helpGuidPanel.SetActive(true);

            // 패널이 닫힐 때까지 대기
            while (helpGuidPanel.activeInHierarchy)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("[TutorialManager] HelpGuidPanel을 찾을 수 없습니다! 자동 전투를 바로 시작합니다.");
        }

        // 하이라이트 및 마스크 제거하여 전투가 보이도록 함
        if (highlightSystem != null)
        {
            highlightSystem.ClearHighlight();
        }

        // 이 시점에서 currentStep은 BattleSelectUnit임
        // NextStep()을 호출하지 않으므로, 더 이상의 튜토리얼 단계(스킬 사용 등)는 진행되지 않음
        // TotalManager가 승리를 감지하여 OnTutorialBattleComplete()를 호출할 때까지 전투가 자동으로 진행될 것을 기대함
        Debug.Log("[TutorialManager] 자동 전투 진행 중... 승리 조건을 기다립니다.");
    }



    /// <summary>
    /// 14단계: 튜토리얼 완료
    /// </summary>
    private void CompleteTutorial()
    {
        if (dialoguePanel != null)
        {
            // --- MANUAL LOCKING LOGIC ---
            Debug.Log("[TutorialManager] 최종 대화 시작. NextButton 외 모든 버튼 수동 잠금.");
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Button> buttonsToRestore = new List<Button>();

            foreach (var btn in allButtons)
            {
                if (btn.name != "NextButton" && btn.interactable)
                {
                    buttonsToRestore.Add(btn);
                    btn.interactable = false;
                }
            }

            ShowDialogueSequence(
                dialogues[TutorialStep.Complete],
                () =>
                {
                    // --- MANUAL UNLOCKING LOGIC ---
                    Debug.Log("[TutorialManager] 최종 대화 종료. 잠긴 버튼들을 다시 활성화합니다.");
                    foreach (var btn in buttonsToRestore)
                    {
                        if (btn != null) btn.interactable = true;
                    }
                    // --- END UNLOCKING LOGIC ---

                    // 튜토리얼에서 얻은 룬과 고블린은 실제 데이터로 이전
                    TransferTutorialRewards();

                    // 재화는 초기 상태로 복구
                    RestoreTutorialResources();

                    // 데이터 리셋 및 종료
                    ResetTutorialData();
                    IsTutorialMode = false;

                    // 닫기 버튼 활성화
                    EnableCloseButtons();

                    // 모든 노드 다시 활성화
                    EnableAllNodes();

                    // 승리 창 활성화
                    GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                    GameObject winWindow = System.Array.Find(allObjects, obj => obj.name == "Win_Window");
                    if (winWindow != null && !winWindow.activeSelf)
                    {
                        winWindow.SetActive(true);
                    }

                    // 타이틀 스테이지 버튼 활성화
                    EnableSpecificButton("StageButton");
                    EnableSpecificButton("TitleButton");

                    // 완료 플래그 저장
                    PlayerPrefs.SetInt("TutorialCompleted", 1);
                    PlayerPrefs.Save();

                    // 플레이어 층수를 2층으로 변경
                    MovePlayerToFloor1();

                    Debug.Log("[TutorialManager]  튜토리얼 완료! PlayerPrefs 저장됨");
                    Debug.Log($"[TutorialManager] TutorialCompleted 값: {PlayerPrefs.GetInt("TutorialCompleted", 0)}");

                    // 코루틴으로 정리 및 씬 전환 (순서 보장)
                    StartCoroutine(CleanupAndReturnToTitle());

                }
            );
        }
    }

    /// <summary>
    /// 튜토리얼 오브젝트 정리 (현재 씬에 유지)
    /// </summary>
    private System.Collections.IEnumerator CleanupTutorialObjects()
    {
        Debug.Log("[TutorialManager] 튜토리얼 오브젝트 정리 시작 (씬 유지)...");

        // 1. DontDestroyOnLoad 오브젝트 파괴
        if (dialoguePanel != null)
        {
            Destroy(dialoguePanel.gameObject);
            Debug.Log("[TutorialManager] TutorialDialoguePanel 파괴");
        }

        if (highlightSystem != null)
        {
            Destroy(highlightSystem.gameObject);
            Debug.Log("[TutorialManager] TutorialHighlight 파괴");
        }

        // 2. 프레임 대기 (Destroy 실행 보장)
        yield return new WaitForEndOfFrame();

        // 3. TutorialManager 자신도 파괴
        Destroy(gameObject);
        Debug.Log("[TutorialManager] TutorialManager 파괴 완료 - 정상 게임 모드로 전환됨");
    }

    /// <summary>
    /// 튜토리얼 정리 및 타이틀로 복귀 (코루틴) - 정식 튜토리얼 완료 시 사용
    /// </summary>
    private System.Collections.IEnumerator CleanupAndReturnToTitle()
    {
        Debug.Log("[TutorialManager] 정리 시작...");

        // 1. DontDestroyOnLoad 오브젝트 파괴
        if (dialoguePanel != null)
        {
            Destroy(dialoguePanel.gameObject);
            Debug.Log("[TutorialManager] TutorialDialoguePanel 파괴 명령");
        }

        // 2. TutorialManager 자신도 파괴
        Destroy(gameObject);
        Debug.Log("[TutorialManager] TutorialManager 파괴 명령");

        // 3. 프레임 대기 (Destroy 실행 보장)
        yield return new WaitForEndOfFrame();
        yield return null;

        Debug.Log("[TutorialManager] 정리 완료, 타이틀 씬으로 이동 중...");

        // 4. 타이틀로 복귀
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// DontDestroyOnLoad 씬으로 이동이 완료될 때까지 기다린 후 씬 전환
    /// 이렇게 하지 않으면 LoadSceneMode.Single이 TutorialManager를 파괴할 수 있음
    /// </summary>
    private System.Collections.IEnumerator WaitForDontDestroyOnLoadThenLoadScene()
    {
        Debug.Log("[TutorialManager] DontDestroyOnLoad 씬 이동 대기 중...");
        Debug.Log($"[TutorialManager] 현재 씬: {gameObject.scene.name}");

        // DontDestroyOnLoad 씬으로 이동할 때까지 대기 (최대 10프레임)
        int maxFrames = 10;
        int frameCount = 0;

        while (gameObject.scene.name != "DontDestroyOnLoad" && frameCount < maxFrames)
        {
            yield return null;
            frameCount++;
            Debug.Log($"[TutorialManager] 대기 중... ({frameCount}/{maxFrames}) 현재 씬: {gameObject.scene.name}");
        }

        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.Log("[TutorialManager] DontDestroyOnLoad 씬으로 이동 완료!");
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] DontDestroyOnLoad 씬 이동 실패! 현재: {gameObject.scene.name}");
            Debug.LogWarning("[TutorialManager] 그래도 씬 전환 시도...");
        }

        // 이제 안전하게 씬 전환
        Debug.Log("[TutorialManager] LoadingSceneManager를 통해 배틀씬으로 이동 (로딩 씬 거침)");
        LoadingSceneManager.LoadScene("ex_Battle_View");
    }

    #endregion

    #region Tutorial Data Management

    /// <summary>
    /// 튜토리얼 전용 몬스터 추가
    /// </summary>
    /// <param name="enemyId">몬스터 ID (UnitStock의 enemyId)</param>
    /// <param name="count">추가할 개수</param>
    public void AddTutorialMonster(string enemyId, int count)
    {
        if (string.IsNullOrEmpty(enemyId)) return;

        // 기존에 있으면 카운트 증가
        var existing = tutorialMonsters.Find(m => m.monsterId == enemyId);
        if (existing != null)
        {
            existing.count += count;
        }
        else
        {
            tutorialMonsters.Add(new TutorialMonsterData(enemyId, count));
        }

        Debug.Log($"[TutorialManager] 튜토리얼 몬스터 추가: {enemyId} x{count}");
    }

    /// <summary>
    /// 튜토리얼 전용 몬스터 카운트 조회
    /// </summary>
    public int GetTutorialMonsterCount(string unitId)
    {
        var data = tutorialMonsters.Find(m => m.monsterId == unitId);
        return data?.count ?? 0;
    }

    /// <summary>
    /// 튜토리얼 전용 편성 설정
    /// </summary>
    public void SetTutorialAssignment(string unitId, int count)
    {
        tutorialAssignments[unitId] = count;
        Debug.Log($"[TutorialManager] 튜토리얼 편성: {unitId} x{count}");
    }

    /// <summary>
    /// 튜토리얼 전용 편성 조회
    /// </summary>
    public int GetTutorialAssignment(string unitId)
    {
        return tutorialAssignments.TryGetValue(unitId, out int count) ? count : 0;
    }

    /// <summary>
    /// 튜토리얼 전용 재화 차감
    /// </summary>
    /// <param name="gold">차감할 골드</param>
    /// <param name="iron">차감할 철</param>
    /// <returns>성공 여부 (재화가 부족하면 false)</returns>
    public bool DeductTutorialResources(int gold, int iron)
    {
        // 재화 부족 체크
        if (tutorialGold < gold || tutorialIron < iron)
        {
            Debug.LogWarning($"[TutorialManager] 재화 부족! 필요: Gold={gold}, Iron={iron}, 보유: Gold={tutorialGold}, Iron={tutorialIron}");
            return false;
        }

        tutorialGold -= gold;
        tutorialIron -= iron;

        Debug.Log($"[TutorialManager] 재화 차감: Gold-{gold}, Iron-{iron} → 남은 재화: Gold={tutorialGold}, Iron={tutorialIron}");
        return true;
    }

    /// <summary>
    /// 튜토리얼 보상 실제 데이터로 이전 (고블린, 룬)
    /// </summary>
    private void TransferTutorialRewards()
    {
        // 고블린은 구매 시 이미 실제 인벤토리에 추가되었으므로 중복 추가 방지
        // MonsterPurchaseService에서 AssignmentManager.AddOwned() 호출함
        var goblinCount = GetTutorialMonsterCount("101");
        if (goblinCount > 0)
        {
            Debug.Log($"[TutorialManager] 고블린 {goblinCount}마리는 이미 인벤토리에 추가되어 있습니다 (구매 시 추가됨)");
        }

        // 튜토리얼 룬을 실제 인벤토리에 추가
        if (tutorialSpeedRune != null)
        {
            RuneDatabase.Instance?.AddRune(tutorialSpeedRune);
            Debug.Log($"[TutorialManager] 튜토리얼 룬 보상 확인: {tutorialSpeedRune.runeName}");
        }

        Debug.Log("[TutorialManager] 튜토리얼 보상 확인 완료 (고블린 및 룬은 유지됨)");
    }

    /// <summary>
    /// 튜토리얼 재화 복구 (초기 재화로 되돌림)
    /// </summary>
    private void RestoreTutorialResources()
    {
        // 초기 재화로 복구
        StaticInfoManager.gold = initialGold;
        StaticInfoManager.iron = initialIron;

        Debug.Log($"[TutorialManager] 재화 복구: Gold={initialGold}, Iron={initialIron}");
    }

    /// <summary>
    /// 튜토리얼 데이터 리셋
    /// </summary>
    private void ResetTutorialData()
    {
        tutorialMonsters.Clear();
        tutorialAssignments.Clear();
        tutorialSpeedRune = null;
        currentStep = TutorialStep.None;

        // 재화는 리셋하지 않음 (StaticInfoManager에 복구해야 함)
        tutorialGold = 0;
        tutorialIron = 0;

        Debug.Log("[TutorialManager] 튜토리얼 데이터 리셋 완료");
    }

    #endregion

    #region UI Control

    /// <summary>
    /// 특정 이름의 버튼 하이라이트
    /// </summary>
    private void HighlightButtonByName(string buttonName)
    {
        if (highlightSystem != null)
        {
            highlightSystem.HighlightButton(buttonName);
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] 하이라이트 시스템이 없습니다! (버튼: {buttonName})");
        }
    }

    /// <summary>
    /// 하이라이트 해제
    /// </summary>
    private void ClearHighlight()
    {
        if (highlightSystem != null)
        {
            highlightSystem.ClearHighlight();
        }
    }

    /// <summary>
    /// 버튼 찾기 헬퍼 메서드
    /// </summary>
    private Button FindButton(string buttonName)
    {
        // 모든 버튼 검색 (비활성화된 것도 포함)
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            // 정확한 이름 매칭
            if (button.name.Equals(buttonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }

            // 부분 매칭
            if (button.name.ToLower().Contains(buttonName.ToLower()))
            {
                return button;
            }
        }

        Debug.LogWarning($"[TutorialManager] 버튼을 찾을 수 없습니다: {buttonName}");
        return null;
    }

    /// <summary>
    /// Transform의 전체 경로를 반환하는 헬퍼 메서드
    /// </summary>
    private string GetFullPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    /// <summary>
    /// 튜토리얼 노드 찾기 헬퍼 메서드
    /// GB_Node는 Button이 아니므로 별도 메서드로 검색
    /// 활성화된 노드 + DontDestroyOnLoad 씬의 노드만 검색
    /// </summary>
    private GB_Node FindTutorialNode()
    {
        // 모든 GB_Node 검색 (비활성화된 것도 포함)
        GB_Node[] allNodes = FindObjectsByType<GB_Node>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log($"[TutorialManager] 전체 노드 개수: {allNodes.Length}");

        //  활성화된 튜토리얼 노드만 필터링
        List<GB_Node> activeTutorialNodes = new List<GB_Node>();

        // 디버그: 모든 노드 이름 출력 + 활성화 상태 + 씬 정보
        foreach (var node in allNodes)
        {
            string nodeName = node.name.ToLower();
            bool isActive = node.gameObject.activeInHierarchy;
            string sceneName = node.gameObject.scene.name;

            Debug.Log($"[TutorialManager] 노드 발견: {node.name}, active={isActive}, 씬={sceneName}, InstanceID={node.GetInstanceID()}");

            // 튜토리얼 노드 이름 패턴 확인
            if (nodeName.Contains("tutorialnode"))
            {
                //  활성화 체크
                if (!isActive)
                {
                    Debug.LogWarning($"[TutorialManager] 튜토리얼 노드 발견했지만 비활성화 상태! (이름: {node.name}, 씬: {sceneName}) - 스킵");
                    continue;
                }

                //  DontDestroyOnLoad 씬 체크 (선호)
                if (sceneName == "DontDestroyOnLoad")
                {
                    Debug.Log($"[TutorialManager] DontDestroyOnLoad 씬의 활성화된 튜토리얼 노드 찾음: {node.name}");
                    activeTutorialNodes.Add(node);
                }
                // MainScene도 허용 (초기 생성 시)
                else if (sceneName == "MainScene")
                {
                    Debug.Log($"[TutorialManager] MainScene의 활성화된 튜토리얼 노드 찾음: {node.name}");
                    activeTutorialNodes.Add(node);
                }
                else
                {
                    Debug.LogWarning($"[TutorialManager] 튜토리얼 노드가 잘못된 씬에 있음: {node.name}, 씬={sceneName} - 스킵");
                }
            }
        }

        //  우선순위: DontDestroyOnLoad 씬의 노드 > MainScene 노드
        GB_Node selectedNode = null;

        foreach (var node in activeTutorialNodes)
        {
            if (node.gameObject.scene.name == "DontDestroyOnLoad")
            {
                selectedNode = node;
                break; // DontDestroyOnLoad 씬 노드 발견 시 즉시 선택
            }
        }

        // DontDestroyOnLoad에 없으면 MainScene 노드 선택
        if (selectedNode == null && activeTutorialNodes.Count > 0)
        {
            selectedNode = activeTutorialNodes[0];
        }

        if (selectedNode != null)
        {
            Debug.Log($"[TutorialManager] 최종 선택된 튜토리얼 노드: {selectedNode.name}, 씬={selectedNode.gameObject.scene.name}, InstanceID={selectedNode.GetInstanceID()}");
            return selectedNode;
        }

        Debug.LogError("[TutorialManager]  활성화된 튜토리얼 노드를 찾을 수 없습니다!");
        Debug.LogError($"[TutorialManager] 총 {allNodes.Length}개 노드 중 활성화된 튜토리얼 노드 0개");
        return null;
    }

    /// <summary>
    /// 노드 선택 대기 코루틴
    /// MapSelectionController에서 노드가 선택될 때까지 대기
    /// </summary>
    private System.Collections.IEnumerator WaitForNodeSelection()
    {
        Debug.Log("[TutorialManager] 튜토리얼 노드 선택 대기 중...");

        // MapSelectionController가 노드를 선택할 때까지 대기
        while (true)
        {
            // MapSelectionController의 SelectedNode 확인
            if (MapSelectionController.Instance != null &&
                MapSelectionController.Instance.SelectedNode != null)
            {
                string nodeName = MapSelectionController.Instance.SelectedNode.name.ToLower();
                if (nodeName.Contains("tutorialnode"))
                {
                    Debug.Log("[TutorialManager] 튜토리얼 노드 선택됨! - StageInfoPanel은 유지 (Prepare 실행 필요)");

                    if(highlightSystem != null)
                    {
                        highlightSystem.RestoreNodeSortingOrder();
                    }
                    else
                    {
                        Debug.Log("[TutorialManager] highlightSystem이 null 입니다.");
                    }
                    yield break; // 코루틴 종료하고 다음 단계로
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 상점/인벤토리 닫기 버튼 비활성화
    /// 튜토리얼 중에는 닫기 버튼으로 빠져나갈 수 없도록
    /// </summary>
    /// <param name="exceptions">비활성화에서 제외할 버튼 이름에 포함될 문자열 목록</param>
    private void DisableCloseButtons(List<string> exceptions = null)
    {
        // 모든 버튼 검색 (비활성화된 것도 포함)
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            string buttonNameLower = button.name.ToLower();
            bool isException = false;

            // 예외 목록 확인
            if (exceptions != null)
            {
                foreach (string ex in exceptions)
                {
                    if (buttonNameLower.Contains(ex.ToLower()))
                    {
                        isException = true;
                        break;
                    }
                }
            }

            if (isException)
            {
                Debug.Log($"[TutorialManager] 예외 처리된 버튼: {button.name} (활성 상태 유지)");
                continue; // 예외 목록에 있으면 비활성화하지 않고 건너뜀
            }

            // 기존 닫기 버튼 패턴 확인 + MonsterSellButton 추가
            if (buttonNameLower.Contains("close") || buttonNameLower.Contains("exit") || buttonNameLower == "x" ||
                buttonNameLower.Contains("offshop") || buttonNameLower.Contains("offinventory") ||
                buttonNameLower.Contains("monstersellbutton") || buttonNameLower.Contains("sellbutton"))
            {
                button.interactable = false;
                Debug.Log($"[TutorialManager] 버튼 비활성화: {button.name}");
            }
        }
    }

    /// <summary>
    /// 닫기 버튼 다시 활성화 (튜토리얼 완료 시)
    /// </summary>
    private void EnableCloseButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var button in allButtons)
        {
            string name = button.name.ToLower();
            if (name.Contains("close") || name.Contains("exit") || name == "x" ||
                name.Contains("offshop") || name.Contains("offinventory") ||
                name.Contains("monstersellbutton") || name.Contains("sellbutton"))
            {
                button.interactable = true;
                Debug.Log($"[TutorialManager] 버튼 활성화: {button.name}");
            }
        }
    }

    /// <summary>
    /// 이름으로 특정 버튼 하나만 활성화
    /// </summary>
    /// <param name="buttonName">활성화할 버튼 이름</param>
    public void EnableSpecificButton(string buttonName)
    {
        Button btn = FindButton(buttonName);
        if (btn != null)
        {
            btn.interactable = true;
            Debug.Log($"[TutorialManager] 특정 버튼 활성화: {btn.name}");
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] 활성화할 버튼을 찾지 못했습니다: {buttonName}");
        }
    }

    /// <summary>
    /// 이름으로 특정 버튼 하나만 비활성화
    /// </summary>
    /// <param name="buttonName">비활성화할 버튼 이름</param>
    public void DisableSpecificButton(string buttonName)
    {
        Button btn = FindButton(buttonName);
        if (btn != null)
        {
            btn.interactable = false;
            Debug.Log($"[TutorialManager] 특정 버튼 비활성화: {btn.name}");
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] 비활성화할 버튼을 찾지 못했습니다: {buttonName}");
        }
    }

    /// <summary>
    /// BattleStart 버튼 비활성화
    /// 편성 화면 진입 시 호출하여 룬 장착 전까지 출정 차단
    /// </summary>
    private void DisableBattleStartButton()
    {
        Button battleStartBtn = FindButton("출정");
        if (battleStartBtn == null)
        {
            battleStartBtn = FindButton("BattleStart");
        }

        if (battleStartBtn != null)
        {
            battleStartBtn.interactable = false;
            Debug.Log("[TutorialManager] BattleStart 버튼 비활성화 (룬 장착 전까지 차단)");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] BattleStart 버튼을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 편성 화면 진입 시 튜토리얼에 불필요한 버튼들 비활성화
    /// SelectSquadSlot2, SelectSquadSlot3, BattleStart 버튼을 잠금
    /// </summary>
    private void DisableFormationTutorialButtons()
    {
        Button slot1Btn = FindButton("SelectSquadSloetOne");
        if (slot1Btn != null)
        {
            slot1Btn.interactable = false;
        }
        // SelectSquadSlot2 비활성화
        Button slot2Btn = FindButton("SelectSquadSlotTwo");
        if (slot2Btn != null)
        {
            slot2Btn.interactable = false;
            Debug.Log("[TutorialManager] SelectSquadSlotTwo 버튼 비활성화");
        }

        // SelectSquadSlot3 비활성화
        Button slot3Btn = FindButton("SelectSquadSlotThree");
        if (slot3Btn != null)
        {
            slot3Btn.interactable = false;
            Debug.Log("[TutorialManager] SelectSquadSlotThree 버튼 비활성화");
        }

        // BattleStart 비활성화
        Button battleStartBtn = FindButton("BattleStart");
        if (battleStartBtn == null)
        {
            battleStartBtn = FindButton("BattleStart");
        }

        if (battleStartBtn != null)
        {
            battleStartBtn.interactable = false;
            Debug.Log("[TutorialManager] BattleStart 버튼 비활성화");
        }
    }

    #endregion

    #region Battle Support

    /// <summary>
    /// 튜토리얼 전투 중인지 확인
    /// </summary>
    public bool IsInBattle()
    {
        return currentStep == TutorialStep.BattleSelectUnit ||
               currentStep == TutorialStep.BattleRushSkill ||
               currentStep == TutorialStep.BattleAttackSkill;
    }

    /// <summary>
    /// 튜토리얼 전투에서 적이 자동으로 죽어야 하는지 체크
    /// 전투 시스템에서 호출
    /// </summary>
    public bool ShouldEnemiesDieInTutorial(int currentTurn, bool playerHasAttacked)
    {
        if (!IsTutorialMode) return false;
        if (!IsInBattle()) return false;

        // 3턴 이상이고 플레이어가 공격했으면 적 사망
        return currentTurn >= 3 && playerHasAttacked;
    }

    /// <summary>
    /// 튜토리얼 전투 완료 시 호출
    /// </summary>
    public void OnTutorialBattleComplete()
    {
        if (IsTutorialMode)
        {
            Debug.Log("[TutorialManager] 튜토리얼 전투 완료 감지 - 임시 완료 처리 시작");

            // 임시로 튜토리얼 완료 처리 (대화 없이 바로 종료)
            CompleteTutorial();
        }
    }

    #endregion

    #region Node Control

    /// <summary>
    /// 모든 노드 비활성화 (튜토리얼 순서 건너뛰기 방지)
    /// </summary>
    private void DisableAllNodes()
    {
        // 씬에 있는 모든 GB_Node 찾기
        GB_Node[] allNodes = FindObjectsByType<GB_Node>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var node in allNodes)
        {
            if (node != null)
            {
                node.clickable = false;
            }
        }

        Debug.Log($"[TutorialManager] {allNodes.Length}개 노드 비활성화 (클릭 차단)");
    }

    /// <summary>
    /// 모든 노드 다시 활성화 (튜토리얼 완료 또는 건너뛰기 시)
    /// </summary>
    private void EnableAllNodes()
    {
        Debug.Log("[TutorialManager] 노드 제어권을 MapSelectionController로 반환");

        // MapSelectionController에게 노드 관리를 다시 맡김
        // UpdateInteractables()가 게임 로직에 맞게 노드를 활성화할 것임
        if (MapSelectionController.Instance != null)
        {
            MapSelectionController.Instance.UpdateInteractables();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] MapSelectionController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 기본 룬 3개 지급 (딜레이 후 호출 - RuneDatabase 초기화 대기)
    /// </summary>
    private System.Collections.IEnumerator GrantStartingRunesDelayed()
    {
        Debug.Log("[TutorialManager] 기본 룬 지급 준비 중 (RuneDatabase 초기화 대기)...");

        // RuneDatabase가 초기화될 때까지 대기
        int maxWaitFrames = 10;
        int waitedFrames = 0;

        while (RuneDatabase.Instance == null && waitedFrames < maxWaitFrames)
        {
            yield return null;
            waitedFrames++;
        }

        if (RuneDatabase.Instance == null)
        {
            Debug.LogError("[TutorialManager] RuneDatabase를 찾을 수 없습니다! (10프레임 대기 후에도 없음)");
            yield break;
        }

        // 룬 지급
        GrantStartingRunes();
    }

    /// <summary>
    /// 기본 룬 3개 지급 (튜토리얼 완료/스킵 시 호출)
    /// </summary>
    private void GrantStartingRunes()
    {
        var runeDB = RuneDatabase.Instance;
        if (runeDB == null)
        {
            Debug.LogError("[TutorialManager] RuneDatabase를 찾을 수 없습니다!");
            return;
        }

        // 지급할 기본 룬 ID (Common 등급 3개)
        string[] startingRuneIds = new string[]
        {
            "rune_power",   // 힘의 룬
            "rune_health",  // 생명의 룬
            "rune_speed"    // 속도의 룬
        };

        int addedCount = 0;
        foreach (string runeId in startingRuneIds)
        {
            var rune = runeDB.FindByRuneId(runeId);
            if (rune != null)
            {
                runeDB.AddRune(rune);
                addedCount++;
                Debug.Log($"[TutorialManager] 기본 룬 지급: {rune.runeName}");
            }
            else
            {
                Debug.LogWarning($"[TutorialManager] 기본 룬을 찾을 수 없음: {runeId}");
            }
        }

        Debug.Log($"[TutorialManager] 기본 룬 {addedCount}개 지급 완료");
    }

    /// <summary>
    /// 튜토리얼 완료 후 플레이어를 2층(floor 1)의 첫 번째 노드로 이동
    /// </summary>
    private void MovePlayerToFloor1()
    {
        Debug.Log("[TutorialManager] 플레이어를 2층(floor 1)으로 이동 시작");

        // MapGenerator 확인
        if (MapGenerator.Instance == null)
        {
            Debug.LogError("[TutorialManager] MapGenerator.Instance가 null입니다! 플레이어를 2층으로 이동할 수 없습니다.");
            return;
        }

        // GB_PlayerController 확인
        if (GB_PlayerController.Instance == null)
        {
            Debug.LogError("[TutorialManager] GB_PlayerController.Instance가 null입니다! 플레이어를 2층으로 이동할 수 없습니다.");
            return;
        }

        // floor 1의 첫 번째 노드 찾기
        GB_MapNode floor1FirstNode = MapGenerator.Instance.GetNode(1, 0);

        if (floor1FirstNode == null)
        {
            Debug.LogError("[TutorialManager] floor 1의 첫 번째 노드를 찾을 수 없습니다!");
            return;
        }

        // 플레이어를 floor 1로 이동
        GB_PlayerController.Instance.SetStartNode(floor1FirstNode);
        Debug.Log($"[TutorialManager] 플레이어를 floor 1 노드로 이동 완료: {floor1FirstNode.nodeObject?.name}");

        // runStarted 플래그 설정 (이미 게임이 시작된 상태로 설정)
        StaticInfoManager.runStarted = true;

        // 노드 상태 업데이트 (1층 노드 비활성화, 2층 노드만 활성화)
        if (MapSelectionController.Instance != null)
        {
            MapSelectionController.Instance.UpdateInteractables();
            Debug.Log("[TutorialManager] 노드 상태 업데이트 완료 - 1층 노드는 이제 클릭 불가, 2층 노드만 클릭 가능");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] MapSelectionController.Instance가 null입니다!");
        }
    }

    #endregion
}
