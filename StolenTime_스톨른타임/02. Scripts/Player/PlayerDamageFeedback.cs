using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어가 피해를 받았을 때 SpriteRenderer 알파를 짧게 깜빡여 피격 피드백을 보여줍니다.
/// GridPlayerHealth.OnDamaged 이벤트를 구독하며, 사망 상태에서는 피격 연출을 새로 시작하지 않습니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(GridPlayer))]
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("피격 깜빡임")]
    [Tooltip("피격 한 번에 투명해졌다 돌아오는 횟수입니다.")]
    [SerializeField, Min(1)] private int blinkCount = 3;

    [Tooltip("투명 상태와 원래 상태를 각각 유지하는 시간입니다. 값이 작을수록 빠르게 깜빡입니다.")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.05f;

    [Tooltip("깜빡일 때 적용할 알파값입니다. 0에 가까울수록 더 투명해집니다.")]
    [SerializeField, Range(0f, 1f)] private float hitAlpha = 0.25f;

    private GridPlayer _player;
    private SpriteRenderer _spriteRenderer;
    private PlayerStatusVisual _statusVisual;
    private Coroutine _flashCoroutine;
    private Color _baseColor;
    private bool _isSubscribed;

    private void Awake()
    {
        _player = GetComponent<GridPlayer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _statusVisual = GetComponent<PlayerStatusVisual>();
        _baseColor = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (_statusVisual != null)
            _statusVisual.Refresh();
        else if (_spriteRenderer != null)
            _spriteRenderer.color = _baseColor;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    /// <summary>
    /// GridPlayer 초기화 순서 때문에 OnEnable 시점에 Health가 아직 없을 수 있어,
    /// OnEnable과 Start 양쪽에서 안전하게 한 번만 구독을 시도합니다.
    /// </summary>
    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (_player == null)
            _player = GetComponent<GridPlayer>();

        if (_player == null || _player.Health == null)
            return;

        _player.Health.OnDamaged += OnDamaged;
        _isSubscribed = true;
    }

    /// <summary>
    /// 오브젝트 비활성화/파괴 시 이벤트 구독을 해제해 중복 호출과 누수 가능성을 막습니다.
    /// </summary>
    private void Unsubscribe()
    {
        if (!_isSubscribed || _player == null || _player.Health == null) return;

        _player.Health.OnDamaged -= OnDamaged;
        _isSubscribed = false;
    }

    private void OnDamaged(int damage)
    {
        if (_spriteRenderer == null || _player.Health.IsDead) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        Color restoreColor = _spriteRenderer.color;

        // 알파만 바꿔서 피격 인지는 주되, 위치/조작/애니메이션 흐름은 건드리지 않습니다.
        for (int i = 0; i < blinkCount; i++)
        {
            Color hitColor = restoreColor;
            hitColor.a = hitAlpha;
            _spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(blinkInterval);

            _spriteRenderer.color = restoreColor;
            yield return new WaitForSeconds(blinkInterval);
        }

        if (_statusVisual != null)
            _statusVisual.Refresh();
        else
            _spriteRenderer.color = restoreColor;

        _flashCoroutine = null;
    }
}
