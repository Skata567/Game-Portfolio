using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 격자 기반 플레이어의 최상위 컨트롤러(메인 브레인)입니다.
/// EntityBase를 상속받아 맵 생성 및 턴 관리에 참여하며, 
/// 입력(Input), 이동(Movement), 전투(Combat), 체력(Health), 딜레이(ActionDelay) 등
/// 하위 핵심 컴포넌트들을 통합적으로 초기화하고 참조를 제공합니다.
/// 외부에서 플레이어에게 데미지를 주거나 상태를 조회할 때 진입점 역할을 합니다.
/// </summary>
[RequireComponent(typeof(GridPlayerInput))]
[RequireComponent(typeof(GridPlayerMovement))]
[RequireComponent(typeof(GridPlayerCombat))]
[RequireComponent(typeof(GridPlayerHealth))]
[RequireComponent(typeof(PlayerActionDelay))]
[RequireComponent(typeof(PlayerLevelSystem))]
public class GridPlayer : EntityBase, IDamageable
{
    [SerializeField] private GameConfig config;

    public GameConfig Config => config;

    public GridPlayerInput Input { get; private set; }
    public GridPlayerMovement Movement { get; private set; }
    public GridPlayerCombat Combat { get; private set; }
    public GridPlayerHealth Health { get; private set; }
    public PlayerActionDelay ActionDelay { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        ResolveConfigIfMissing();

        Input = GetComponent<GridPlayerInput>();
        Movement = GetComponent<GridPlayerMovement>();
        Combat = GetComponent<GridPlayerCombat>();
        Health = GetComponent<GridPlayerHealth>();
        ActionDelay = GetComponent<PlayerActionDelay>();
        if (ActionDelay == null)
            ActionDelay = gameObject.AddComponent<PlayerActionDelay>();

        Movement.Init(this);
        Combat.Init(this);
        Health.Init(this);
        Input.Init(this);
    }

    /// <summary>
    /// 씬에 연결된 GameConfig 참조가 깨졌을 때, 에디터 플레이에서는 프로젝트 안의 GameConfig 에셋을 자동으로 찾아 연결합니다.
    /// Config가 비어 있으면 이동 딜레이 계산이 1초 기본값으로 떨어지기 때문에, 플레이 테스트 중 설정값이 무시되는 것을 막는 안전장치입니다.
    /// </summary>
    private void ResolveConfigIfMissing()
    {
        if (config != null) return;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:GameConfig");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
        }
#endif

        if (config == null)
        {
            Debug.LogWarning("GridPlayer: GameConfig가 연결되지 않아 행동 딜레이가 기본값으로 동작할 수 있습니다.");
        }
    }

    private void Start()
    {
        // 씬에 임의로 배치된 Transform 위치를 격자 논리 좌표(Vector2Int)로 변환해 스냅시킵니다.
        var startPos = GridSystem.Instance.WorldToGrid(transform.position);
        Initialize(startPos);

        // 플레이어 시작 위치를 기준으로 첫 시야 및 안개 처리를 계산합니다.
        VisionSystem.Instance.UpdateVision(GridPosition);
        
        // 60초 제한시간 카운트다운을 시작합니다.
        TimeSystem.Instance.StartTimer();
    }

    /// <summary>
    /// TurnManager가 호출하는 턴 업데이트 함수입니다.
    /// 플레이어는 몬스터와 달리 입력 기반으로 움직이므로, 이 함수 내에서 자동 행동 처리를 하지 않고 무시합니다.
    /// </summary>
    public override void OnTurnUpdate()
    {
    }

    #region IDamageable 인터페이스 위임
    // 플레이어의 체력 관리는 실제로는 GridPlayerHealth 컴포넌트가 담당하며, 
    // GridPlayer는 외부의 요청(TakeDamage 등)을 받아 Health 쪽으로 토스(위임)해주는 역할만 수행합니다.
    public int CurrentHp => Health.CurrentHp;
    public int MaxHp => Health.MaxHp;
    public bool IsDead => Health.IsDead;
    public void TakeDamage(int amount) => Health.TakeDamage(amount);
    public void Heal(int amount) => Health.Heal(amount);
}

#endregion