using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GridTrap : MonoBehaviour
{
    [SerializeField] private TrapData data;
    [Tooltip("트랩 위치를 자동 계산할지 정하는 옵션" +
        "키면 GridPosition 값을 사용 하여 그 위치에 생성" +
        "꺼두면 트랩의 씬에서의 월드위치를 보고 자동으로 계산 하여 설치함 ")]
    [SerializeField] private bool overrideGridPosition;
    [SerializeField] private Vector2Int gridPosition;

    private SpriteRenderer _spriteRenderer;
    private ITrapEffect[] _effects;
    private bool _isRevealed;
    private bool _hasTriggered;
    private bool _started;

    public TrapData Data => data;
    public Vector2Int GridPosition => gridPosition;
    public bool IsRevealed => _isRevealed;
    public bool HasTriggered => _hasTriggered;
    public bool CanTrigger => data != null && (data.repeatable || !_hasTriggered);

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Unity 버전에 따라 interface 직접 GetComponents가 애매할 수 있어,
        // MonoBehaviour 목록에서 ITrapEffect 구현체만 안전하게 골라냅니다.
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        var effects = new List<ITrapEffect>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ITrapEffect effect)
                effects.Add(effect);
        }

        _effects = effects.ToArray();
    }

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: TrapData가 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        if (!overrideGridPosition)
            gridPosition = GridSystem.Instance.WorldToGrid(transform.position);

        transform.position = GridSystem.Instance.GridToWorld(gridPosition);
        _isRevealed = data.isAlwaysVisible || !data.startsHidden;
        RefreshSprite();

        _started = true;
        TrapSystem.Instance?.Register(this);
    }

    private void OnEnable()
    {
        if (_started)
            TrapSystem.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        TrapSystem.Instance?.Unregister(this);
    }

    private void OnDisable()
    {
        TrapSystem.Instance?.Unregister(this);
    }

    public void Initialize(Vector2Int position)
    {
        TrapSystem.Instance?.Unregister(this);
        overrideGridPosition = true;
        gridPosition = position;
        if (GridSystem.Instance != null)
            transform.position = GridSystem.Instance.GridToWorld(gridPosition);
        TrapSystem.Instance?.Register(this);
    }

    public bool TryTrigger(IGridEntity activator)
    {
        if (!CanTrigger) return false;

        Reveal();

        var context = new TrapContext(
            gridPosition,
            activator,
            TrapSystem.Instance != null ? TrapSystem.Instance.CurrentFloor : 1,
            data,
            this);

        for (int i = 0; i < _effects.Length; i++)
            _effects[i]?.Execute(context);

        _hasTriggered = true;
        GameEvents.OnTrapTriggered?.Invoke(gridPosition);
        RefreshSprite();

        if (!data.repeatable && data.destroyAfterTrigger)
            gameObject.SetActive(false);

        return true;
    }

    public void Reveal()
    {
        if (_isRevealed) return;
        _isRevealed = true;
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        if (_spriteRenderer == null || data == null) return;

        if (_hasTriggered && data.triggeredSprite != null)
            _spriteRenderer.sprite = data.triggeredSprite;
        else if (_isRevealed)
            _spriteRenderer.sprite = data.visibleSprite;
        else
            _spriteRenderer.sprite = data.hiddenSprite;

        _spriteRenderer.enabled = _isRevealed || data.hiddenSprite != null;
    }
}
