using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathGameOverView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Image darkOverlay;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;

    [Header("Scene Flow")]
    [SerializeField] private MoveScene moveScene;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float gameOverRevealSeconds = 1f;
    [SerializeField] private Vector2 hiddenTextOffset = new Vector2(0f, -24f);
    [SerializeField] private bool playGameOverVoice = true;

    private Vector2 _gameOverShownPosition;
    private Coroutine _showRoutine;
    private bool _initialized;

    public float GameOverRevealSeconds => gameOverRevealSeconds;

    private void Awake()
    {
        bool wasInitializedBeforeAwake = _initialized;
        EnsureInitialized();

        if (!wasInitializedBeforeAwake)
            HideImmediate();
    }

    private void OnDestroy()
    {
        if (_initialized)
            UnregisterButtons();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>();
        if (moveScene == null)
            moveScene = FindFirstObjectByType<MoveScene>();

        if (gameOverText != null)
            _gameOverShownPosition = gameOverText.rectTransform.anchoredPosition;

        RegisterButtons();
        _initialized = true;
    }

    public void HideImmediate()
    {
        EnsureInitialized();

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        SetRootVisible(false);
        SetOverlayAlpha(0f);
        SetGameOverAlpha(0f);
        SetButtonsVisible(false);
    }

    public void ShowOverlay(float alpha)
    {
        EnsureInitialized();
        SetRootVisible(true);
        SetOverlayAlpha(alpha);
        SetGameOverAlpha(0f);
        SetButtonsVisible(false);
    }

    public void PlayGameOverSequence()
    {
        EnsureInitialized();
        SetRootVisible(true);

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        SetRootVisible(true);
        SetButtonsVisible(false);

        if (playGameOverVoice)
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.GameOverVoice);

        float elapsed = 0f;
        while (elapsed < gameOverRevealSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / gameOverRevealSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            SetGameOverAlpha(eased);
            if (gameOverText != null)
                gameOverText.rectTransform.anchoredPosition = Vector2.Lerp(_gameOverShownPosition + hiddenTextOffset, _gameOverShownPosition, eased);

            yield return null;
        }

        SetGameOverAlpha(1f);
        if (gameOverText != null)
            gameOverText.rectTransform.anchoredPosition = _gameOverShownPosition;

        SetButtonsVisible(true);
        _showRoutine = null;
    }

    private void OnMainMenuClicked()
    {
        if (moveScene != null)
        {
            moveScene.StartMainMenu();
            return;
        }

        SceneManager.LoadScene(0);
    }

    private void OnRestartClicked()
    {
        if (moveScene != null)
        {
            moveScene.StartNewGame();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RegisterButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void UnregisterButtons()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    private void SetRootVisible(bool visible)
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        gameObject.SetActive(visible);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (darkOverlay == null) return;
        Color color = darkOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        darkOverlay.color = color;
        darkOverlay.enabled = alpha > 0f;
    }

    private void SetGameOverAlpha(float alpha)
    {
        if (gameOverText == null) return;
        Color color = gameOverText.color;
        color.a = Mathf.Clamp01(alpha);
        gameOverText.color = color;
        gameOverText.enabled = alpha > 0f;
    }

    private void SetButtonsVisible(bool visible)
    {
        SetButtonVisible(mainMenuButton, visible);
        SetButtonVisible(restartButton, visible);
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button == null) return;
        button.gameObject.SetActive(visible);
        button.interactable = visible;
    }
}
