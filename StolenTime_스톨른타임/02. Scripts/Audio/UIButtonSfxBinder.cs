using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSfxBinder : MonoBehaviour
{
    [SerializeField] private AudioEventId clickEventId = AudioEventId.ButtonClicked;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        _button.onClick.AddListener(PlayClickSfx);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlayClickSfx);
    }

    private void PlayClickSfx()
    {
        GameEvents.OnAudioEventRequested?.Invoke(clickEventId);
    }
}
