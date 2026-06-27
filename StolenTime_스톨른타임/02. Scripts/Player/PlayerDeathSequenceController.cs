using System.Collections;
using PrototypeRT;
using UnityEngine;

/// <summary>
/// 플레이어 사망 또는 제한시간 종료 시 카메라 줌, 암전, 입력 차단, 게임오버 UI를 순서대로 실행한다.
/// 씬 전환 전까지 한 번만 재생되어야 하므로 GameEvents를 구독하고 내부 중복 실행 플래그로 보호한다.
/// </summary>
public class PlayerDeathSequenceController : MonoBehaviour
{
    private const string InputBlockSource = "PlayerDeathSequence";

    [Header("필수 참조")]
    [Tooltip("사망 연출의 중심이 되는 플레이어입니다. 비워두면 실행 시 씬에서 GridPlayer를 자동으로 찾습니다.")]
    [SerializeField] private GridPlayer player;

    [Tooltip("암전 오버레이보다 위에 올릴 플레이어 스프라이트입니다. 비워두면 Player 오브젝트의 SpriteRenderer를 사용합니다.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Tooltip("사망 시 플레이어를 따라가며 줌인할 카메라 팔로우 컴포넌트입니다. 비워두면 CameraFollow.Instance 또는 씬 검색으로 찾습니다.")]
    [SerializeField] private CameraFollow cameraFollow;

    [Tooltip("암전 오버레이 크기와 위치를 맞출 기준 카메라입니다. 비워두면 Main Camera를 사용하고, 없으면 CameraFollow가 붙은 카메라를 사용합니다.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("GAME OVER 텍스트, 음성 이벤트, 메인 메뉴/재시작 버튼 표시를 담당하는 UI 뷰입니다. 비워두면 비활성 오브젝트까지 포함해 자동 검색합니다.")]
    [SerializeField] private DeathGameOverView gameOverView;

    [Header("연출 시간")]
    [Tooltip("사망 포커스 때 카메라 Orthographic Size를 어디까지 줄일지 정합니다. 값이 작을수록 플레이어가 더 크게 보입니다.")]
    [SerializeField, Min(0.01f)] private float cameraZoomSize = 2.2f;

    [Tooltip("카메라가 플레이어에게 줌인하는 데 걸리는 시간입니다. CameraFollow의 사망 포커스 모드에 전달됩니다.")]
    [SerializeField, Min(0.01f)] private float cameraZoomSeconds = 0.7f;

    [Tooltip("사망 애니메이션이 끝났다고 보고 GAME OVER UI를 시작하기 전까지 기다리는 시간입니다. 애니메이션 이벤트가 없어서 시간값으로 대기합니다.")]
    [SerializeField, Min(0f)] private float deathAnimationWaitSeconds = 1.2f;

    [Tooltip("화면 암전 오버레이가 목표 알파까지 페이드되는 시간입니다. 0이면 즉시 암전됩니다.")]
    [SerializeField, Min(0f)] private float overlayFadeSeconds = 0.7f;

    [Header("암전 오버레이")]
    [Tooltip("사망 연출 중 화면을 얼마나 어둡게 덮을지 정합니다. 0은 투명, 1은 완전 검정입니다.")]
    [SerializeField, Range(0f, 1f)] private float darkOverlayAlpha = 0.75f;

    [Tooltip("런타임에 생성되는 검은 오버레이의 Sorting Order입니다. 플레이어보다 낮고 대부분의 월드 스프라이트보다 높게 둡니다.")]
    [SerializeField] private int darkOverlaySortingOrder = 9000;

    [Tooltip("사망 연출 중 플레이어 스프라이트를 임시로 올릴 Sorting Order입니다. darkOverlaySortingOrder보다 커야 플레이어가 암전 위에 보입니다.")]
    [SerializeField] private int playerDeathSortingOrder = 9010;

    [Tooltip("암전 오버레이와 플레이어를 강제로 올릴 Sorting Layer 이름입니다. 비워두면 플레이어의 현재 Sorting Layer를 사용합니다.")]
    [SerializeField] private string darkOverlaySortingLayerName;

    private bool _isPlaying;
    private int _originalPlayerSortingOrder;
    private string _originalPlayerSortingLayerName;
    private SpriteRenderer _darkOverlayRenderer;
    private Coroutine _sequenceRoutine;

    private void Awake()
    {
        ResolveReferences();
        CreateDarkOverlay();
        SetDarkOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnTimeUp += OnTimeUp;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnTimeUp -= OnTimeUp;
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
    }

    private void OnDestroy()
    {
        KeyBindingManager.SetUiInputBlock(InputBlockSource, false);
    }

    private void LateUpdate()
    {
        UpdateDarkOverlayTransform();
    }

    private void OnPlayerDied()
    {
        if (_isPlaying)
            return;

        _sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private void OnTimeUp()
    {
        if (_isPlaying)
            return;

        _sequenceRoutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _isPlaying = true;
        ResolveReferences();
        KeyBindingManager.SetUiInputBlock(InputBlockSource, true);
        TimeSystem.Instance?.StopTimer();

        RaisePlayerAboveOverlay();

        if (cameraFollow != null && player != null)
            cameraFollow.BeginDeathFocus(player.transform, cameraZoomSize, cameraZoomSeconds);

        if (gameOverView != null)
            gameOverView.ShowOverlay(0f);

        yield return FadeDarkOverlay(darkOverlayAlpha, overlayFadeSeconds);

        if (deathAnimationWaitSeconds > 0f)
            yield return new WaitForSecondsRealtime(deathAnimationWaitSeconds);

        if (gameOverView != null)
        {
            gameOverView.PlayGameOverSequence();
            yield return new WaitForSecondsRealtime(gameOverView.GameOverRevealSeconds);
        }
        else
        {
            Debug.LogWarning("PlayerDeathSequenceController: GameOverView가 연결되지 않아 버튼 UI를 표시할 수 없습니다.");
        }

        _sequenceRoutine = null;
    }

    private IEnumerator FadeDarkOverlay(float targetAlpha, float duration)
    {
        if (_darkOverlayRenderer == null)
            yield break;

        float startAlpha = _darkOverlayRenderer.color.a;
        if (duration <= 0f)
        {
            SetDarkOverlayAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDarkOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetDarkOverlayAlpha(targetAlpha);
    }

    private void ResolveReferences()
    {
        if (player == null)
            player = FindFirstObjectByType<GridPlayer>();

        if (playerSpriteRenderer == null && player != null)
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        if (cameraFollow == null)
            cameraFollow = CameraFollow.Instance != null ? CameraFollow.Instance : FindFirstObjectByType<CameraFollow>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null && cameraFollow != null)
            targetCamera = cameraFollow.GetComponent<Camera>();

        if (gameOverView == null)
            gameOverView = FindFirstObjectByType<DeathGameOverView>(FindObjectsInactive.Include);
    }

    private void RaisePlayerAboveOverlay()
    {
        if (playerSpriteRenderer == null)
            return;

        _originalPlayerSortingOrder = playerSpriteRenderer.sortingOrder;
        _originalPlayerSortingLayerName = playerSpriteRenderer.sortingLayerName;

        if (!string.IsNullOrWhiteSpace(darkOverlaySortingLayerName))
            playerSpriteRenderer.sortingLayerName = darkOverlaySortingLayerName;

        playerSpriteRenderer.sortingOrder = playerDeathSortingOrder;
    }

    public void RestorePlayerSorting()
    {
        if (playerSpriteRenderer == null)
            return;

        playerSpriteRenderer.sortingLayerName = _originalPlayerSortingLayerName;
        playerSpriteRenderer.sortingOrder = _originalPlayerSortingOrder;
    }

    private void CreateDarkOverlay()
    {
        if (_darkOverlayRenderer != null)
            return;

        GameObject overlay = new GameObject("RuntimeDeathDarkOverlay");
        overlay.transform.SetParent(transform, false);

        _darkOverlayRenderer = overlay.AddComponent<SpriteRenderer>();
        _darkOverlayRenderer.sprite = CreateOverlaySprite();
        _darkOverlayRenderer.color = new Color(0f, 0f, 0f, 0f);
        _darkOverlayRenderer.sortingOrder = darkOverlaySortingOrder;

        if (!string.IsNullOrWhiteSpace(darkOverlaySortingLayerName))
            _darkOverlayRenderer.sortingLayerName = darkOverlaySortingLayerName;
        else if (playerSpriteRenderer != null)
            _darkOverlayRenderer.sortingLayerName = playerSpriteRenderer.sortingLayerName;
    }

    private static Sprite CreateOverlaySprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "RuntimeDeathDarkOverlayTexture",
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void SetDarkOverlayAlpha(float alpha)
    {
        if (_darkOverlayRenderer == null)
            return;

        Color color = _darkOverlayRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        _darkOverlayRenderer.color = color;
        _darkOverlayRenderer.enabled = alpha > 0f;
        UpdateDarkOverlayTransform();
    }

    private void UpdateDarkOverlayTransform()
    {
        if (_darkOverlayRenderer == null || targetCamera == null)
            return;

        Transform overlayTransform = _darkOverlayRenderer.transform;
        Vector3 cameraPosition = targetCamera.transform.position;
        overlayTransform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;
        overlayTransform.localScale = new Vector3(halfWidth * 2.2f, halfHeight * 2.2f, 1f);
    }
}
