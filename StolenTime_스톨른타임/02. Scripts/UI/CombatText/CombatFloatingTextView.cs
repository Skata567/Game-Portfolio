using TMPro;
using UnityEngine;

/// <summary>
/// 월드에 생성된 전투 숫자 하나를 담당합니다.
/// 숫자를 위로 움직이고, 시간이 지날수록 알파를 낮춰 서서히 사라지게 합니다.
/// </summary>
public class CombatFloatingTextView : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("데미지/회복 숫자를 표시할 TextMeshPro 컴포넌트입니다. 비워두면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text text;

    [Header("Motion")]
    [Tooltip("숫자가 완전히 사라질 때까지 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float lifetime = 0.75f;

    [Tooltip("lifetime 동안 위로 올라갈 월드 거리입니다.")]
    [SerializeField] private Vector3 riseOffset = new Vector3(0f, 0.8f, 0f);

    [Tooltip("카메라를 바라보게 할지 여부입니다. 월드 텍스트가 옆으로 누워 보이면 켜두는 것이 좋습니다.")]
    [SerializeField] private bool faceCamera = true;

    private Vector3 _startPosition;
    private Color _startColor = Color.white;
    private float _elapsed;

    private void Awake()
    {
        // 텍스트를 담을 TextMeshPro 컴포넌트가 인스펙터에서 수동으로 참조되지 않았다면,
        // 자식 오브젝트들 중에서 자동으로 찾아서 가져옵니다.
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>
    /// 스폰된 텍스트 뷰를 특정 메시지, 색상, 크기, 수명, 그리고 연출용 오프셋으로 설정하는 초기화 메서드입니다.
    /// </summary>
    public void Init(string message, Color color, float fontSize, float duration, Vector3 moveOffset)
    {
        // Awake 시점에 찾지 못했거나 초기화되지 않은 경우를 대비하여 텍스트 컴포넌트 참조를 다시 확인합니다.
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        // 연출 설정 값들을 로컬 필드에 기록합니다.
        lifetime = Mathf.Max(0.01f, duration);
        riseOffset = moveOffset;
        _startPosition = transform.position; // 시작할 때의 월드 위치 기록
        _elapsed = 0f;                       // 경과 시간 초기화

        if (text == null) return;

        // TODO(UI): 데미지 숫자 폰트는 프리팹의 TMP_Text.font에서 교체하세요.
        // 이 코드는 런타임 표시 내용, 색상, 크기만 바꿔서 프리팹 스타일을 최대한 보존합니다.
        // 폰트 위치나 외곽선 같은 GUI 작업은 CombatFloatingTextSpawner의 floatingTextPrefab에 연결한 프리팹에서 하는 게 제일 편합니다.
        text.text = message;
        text.color = color;
        text.fontSize = fontSize;
        _startColor = color; // 페이드 아웃 연출을 시작할 때의 원본 색상을 기록합니다.
    }

    public void ApplyFontIfMissing(TMP_FontAsset fallbackFont)
    {
        if (fallbackFont == null)
            return;

        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        if (text == null || text.font != null)
            return;

        // 런타임 생성 텍스트는 TMP 기본 폰트가 비어 있을 수 있어 스포너의 기본 폰트를 주입한다.
        // 프리팹에서 이미 폰트를 정한 경우에는 디자이너 설정을 보존한다.
        text.font = fallbackFont;
    }

    private void LateUpdate()
    {
        // 카메라를 바라보는 설정(faceCamera)이 켜져 있다면 월드상의 텍스트 회전을 메인 카메라 방향과 일치시킵니다. (빌보드 효과)
        if (faceCamera && Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        // 경과 시간을 갱신하고 진행률(0~1)을 계산합니다.
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / lifetime);

        // 시간이 지남에 따라 텍스트의 월드 위치를 시작 지점으로부터 목표 상승 오프셋 방향으로 부드럽게 이동시킵니다.
        transform.position = _startPosition + riseOffset * progress;

        // 시간이 지남에 따라 텍스트의 투명도(알파 값)를 서서히 줄여 페이드 아웃시킵니다.
        if (text != null)
        {
            Color color = _startColor;
            color.a = 1f - progress;
            text.color = color;
        }

        // 수명이 모두 끝나서 진행률이 100%가 되면 게임 오브젝트를 소멸시킵니다.
        if (progress >= 1f)
            Destroy(gameObject);
    }
}
