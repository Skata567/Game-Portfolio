using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 버튼에 부착하여 클릭 시 지정된 이름의 SFX를 재생하는 스크립트.
/// 인스펙터에서 사운드 이름을 직접 지정할 수 있어 씬에 구애받지 않고 사용 가능합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("재생할 사운드 이름")]
    [Tooltip("AudioLibrary에 등록된 사운드의 정확한 이름을 입력하세요.")]
    public string soundName;

    private Button button;

    private void Start()
    {
        // 1. 이 스크립트가 부착된 게임오브젝트의 Button 컴포넌트를 가져옵니다.
        button = GetComponent<Button>();

        // 2. 버튼의 OnClick 이벤트에 PlaySound 함수를 동적으로 추가(연결)합니다.
        button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        // 3. SoundManager 싱글톤 인스턴스를 찾아 PlaySFX 함수를 호출합니다.
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(soundName);
        }
        else
        {
            Debug.LogWarning($"[UIButtonSound] SoundManager.Instance가 현재 씬에 없습니다. ({gameObject.name})");
        }
    }

    // 스크립트가 파괴될 때 등록했던 리스너를 제거하여 메모리 누수를 방지합니다.
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }
}
