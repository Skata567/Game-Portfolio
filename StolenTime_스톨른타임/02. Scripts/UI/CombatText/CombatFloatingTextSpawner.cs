using TMPro;
using UnityEngine;

/// <summary>
/// 전투 결과 이벤트를 받아 캐릭터 머리 위에 데미지/회복/빗나감 숫자를 생성합니다.
/// 씬에 빈 오브젝트로 하나 배치하면 동작하며, 폰트와 위치를 꾸미고 싶으면 전용 프리팹을 연결하면 됩니다.
/// </summary>
public class CombatFloatingTextSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("전투 숫자 프리팹입니다. TMP_Text와 CombatFloatingTextView가 있으면 그대로 사용합니다. 비워두면 기본 월드 텍스트를 런타임 생성합니다.")]
    [SerializeField] private CombatFloatingTextView floatingTextPrefab;

    [Tooltip("프리팹을 쓰지 않고 런타임 텍스트를 만들 때 적용할 TMP 폰트입니다. 프리팹 텍스트에 이미 폰트가 있으면 덮어쓰지 않습니다.")]
    [SerializeField] private TMP_FontAsset fallbackFontAsset;

    [Header("Position")]
    [Tooltip("대상 Transform 위치에서 머리 위로 올릴 월드 오프셋입니다. 숫자가 너무 낮거나 높으면 이 값을 조정하세요.")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.9f, 0f);

    [Tooltip("여러 숫자가 동시에 뜰 때 완전히 겹치지 않도록 좌우로 흔드는 범위입니다.")]
    [SerializeField, Min(0f)] private float randomHorizontalJitter = 0.15f;

    [Header("Style")]
    [Tooltip("일반 데미지 숫자 색상입니다.")]
    [SerializeField] private Color damageColor = Color.white;

    [Tooltip("치명타 데미지 숫자 색상입니다.")]
    [SerializeField] private Color criticalDamageColor = Color.red;

    [Tooltip("빗나감 텍스트 색상입니다.")]
    [SerializeField] private Color missColor = Color.gray;

    [Tooltip("회복 숫자 색상입니다.")]
    [SerializeField] private Color healColor = Color.green;

    [Tooltip("골드 획득 텍스트 색상입니다.")]
    [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0.1f);

    [Tooltip("일반 데미지, 회복, 빗나감 텍스트 크기입니다.")]
    [SerializeField, Min(1f)] private float normalFontSize = 3f;

    [Tooltip("치명타 텍스트 크기입니다.")]
    [SerializeField, Min(1f)] private float criticalFontSize = 4f;

    [Header("Motion")]
    [Tooltip("숫자가 사라질 때까지 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float lifetime = 0.75f;

    [Tooltip("숫자가 사라지는 동안 위로 올라갈 거리입니다.")]
    [SerializeField] private Vector3 riseOffset = new Vector3(0f, 0.8f, 0f);

    private void OnEnable()
    {
        // 전투 결과(데미지, 회복 등) 발생 시 실행될 Spawn 메서드를 글로벌 게임 이벤트 허브에 등록합니다.
        GameEvents.OnCombatTextRequested += Spawn;
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되거나 삭제될 때 메모리 누수를 방지하기 위해 이벤트를 해제합니다.
        GameEvents.OnCombatTextRequested -= Spawn;
    }

    /// <summary>
    /// 전투 텍스트 생성 요청 이벤트를 수신했을 때 호출되어 텍스트를 월드 상에 스폰합니다.
    /// </summary>
    /// <param name="request">출력할 메시지, 위치, 텍스트 종류가 담긴 요청 데이터</param>
    private void Spawn(CombatTextRequest request)
    {
        // 캐릭터 발밑이나 중심 위치에 오프셋(spawnOffset)을 더하고 좌우 흔들림(Jitter)을 보정하여 스폰 위치를 정합니다.
        Vector3 position = request.WorldPosition + spawnOffset + GetJitter();
        CombatFloatingTextView view = CreateView(position);
        if (view == null) return;

        // 데미지 종류(일반, 치명타, 회복, 미스 등)에 맞는 텍스트 색상을 설정합니다.
        Color color = GetColor(request.Type);
        
        // 치명타의 경우 좀 더 큰 폰트 크기를 사용하고, 그 외에는 일반 크기를 사용합니다.
        float fontSize = request.Type == CombatTextType.CriticalDamage ? criticalFontSize : normalFontSize;
        
        // 생성된 텍스트 뷰 컴포넌트를 수치 메시지와 연출 속성으로 초기화합니다.
        view.Init(request.Text, color, fontSize, lifetime, riseOffset);
    }

    /// <summary>
    /// 지정된 위치에 텍스트 뷰를 실제로 생성하여 반환합니다.
    /// 프리팹이 있다면 그것을 생성하고, 없으면 런타임에 빈 게임오브젝트를 만들어 TextMeshPro 컴포넌트를 주입합니다.
    /// </summary>
    private CombatFloatingTextView CreateView(Vector3 position)
    {
        // 1. 디자이너가 인스펙터에 프리팹을 지정해 두었다면, 해당 프리팹을 월드에 복제하여 반환합니다.
        if (floatingTextPrefab != null)
        {
            CombatFloatingTextView prefabView = Instantiate(floatingTextPrefab, position, Quaternion.identity);
            prefabView.ApplyFontIfMissing(fallbackFontAsset);
            return prefabView;
        }

        // TODO(UI): 데미지 숫자 프리팹/폰트/위치 변경 지점입니다.
        // 나중에 폰트, 외곽선, 그림자, 애니메이션을 꾸미려면 TMP 프리팹을 만들고 floatingTextPrefab에 연결하세요.
        // 프리팹을 쓰면 이 런타임 생성 코드는 백업용으로만 남고, 실제 GUI 수정은 프리팹에서 처리하면 됩니다.
        
        // 2. 프리팹이 없는 경우 시스템이 중단되지 않도록 런타임에 동적으로 임시 텍스트 오브젝트를 조립합니다.
        GameObject root = new GameObject("CombatFloatingText");
        root.transform.position = position;

        // TextMeshPro 컴포넌트를 달고 화면 정렬 상태와 레이어를 설정합니다.
        TextMeshPro text = root.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.sortingOrder = 100; // 게임 내 다른 스프라이트나 오브젝트보다 무조건 앞쪽에 보이도록 설정
        text.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄로만 쭉 나오게 강제
        text.font = fallbackFontAsset;

        // 애니메이션 연출을 담당할 CombatFloatingTextView를 붙여서 반환합니다.
        CombatFloatingTextView view = root.AddComponent<CombatFloatingTextView>();
        view.ApplyFontIfMissing(fallbackFontAsset);
        return view;
    }

    /// <summary>
    /// 여러 텍스트가 정방향에 겹쳐서 표시되어 가독성이 떨어지는 현상을 방지하기 위해 좌우 무작위 오프셋(흔들림)을 생성합니다.
    /// </summary>
    private Vector3 GetJitter()
    {
        if (randomHorizontalJitter <= 0f)
            return Vector3.zero;

        return new Vector3(Random.Range(-randomHorizontalJitter, randomHorizontalJitter), 0f, 0f);
    }

    /// <summary>
    /// 전투 숫자 종류(Type)에 맞춰 사전 정의된 인스펙터 컬러 설정을 매핑하여 반환합니다.
    /// </summary>
    private Color GetColor(CombatTextType type)
    {
        return type switch
        {
            CombatTextType.CriticalDamage => criticalDamageColor,
            CombatTextType.Miss => missColor,
            CombatTextType.Heal => healColor,
            CombatTextType.Gold => goldColor,
            _ => damageColor // 일반 데미지 및 기타
        };
    }
}
