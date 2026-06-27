using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StatusController의 현재 상태 목록을 읽어서 플레이어 색, 상태 아이콘, 상태 이펙트를 표시합니다.
/// 실제 데미지/쿨타임 계산은 StatusController가 담당하고, 이 스크립트는 보여주기만 담당합니다.
/// </summary>
[RequireComponent(typeof(StatusController))]
public class PlayerStatusVisual : MonoBehaviour
{
    [Header("대상 참조")]
    [Tooltip("색을 바꿀 플레이어 SpriteRenderer입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer playerRenderer;

    [Tooltip("상태 아이콘을 표시할 UI Image입니다. 비워두면 아이콘 표시는 생략합니다.")]
    [SerializeField] private Image statusIconImage;

    [Tooltip("상태 이펙트 프리팹을 붙일 위치입니다. 비워두면 이 오브젝트 위치에 생성합니다.")]
    [SerializeField] private Transform effectAnchor;

    [Header("기본 색")]
    [Tooltip("아무 상태도 없을 때 플레이어에게 되돌릴 색입니다.")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("켜면 시작할 때 SpriteRenderer의 현재 색을 정상 색으로 기억합니다. 보스처럼 기본 색이 흰색이 아닌 대상에 사용합니다.")]
    [SerializeField] private bool captureInitialColor = true;

    [Header("상태별 표시")]
    [Tooltip("상태별 색, 아이콘, 이펙트 설정입니다. 위에 있는 항목일수록 여러 상태가 겹쳤을 때 우선 표시됩니다.")]
    [SerializeField] private StatusVisualEntry[] visuals =
    {
        new(StatusType.Burn, new Color(1f, 0.45f, 0.05f, 1f), true),
        new(StatusType.Bleed, new Color(1f, 0.05f, 0.05f, 1f), true),
        new(StatusType.Chill, new Color(0.25f, 0.65f, 1f, 1f), true),
        new(StatusType.Slow, new Color(1f, 1f, 1f, 0.55f), true),
        new(StatusType.Cripple, new Color(1f, 1f, 1f, 0.55f), true),
        new(StatusType.Hunger, Color.white, false),
        new(StatusType.Float, Color.white, false),
        new(StatusType.Haste, Color.white, false)
    };

    private readonly List<StatusType> _activeStatuses = new();
    private StatusController _statusController;
    private GameObject _activeEffect;

    public static PlayerStatusVisual EnsureFor(GameObject target)
    {
        if (target == null)
            return null;

        // 이미 비주얼 컴포넌트가 있으면 중복으로 붙이지 않습니다.
        PlayerStatusVisual visual = target.GetComponent<PlayerStatusVisual>();
        if (visual != null)
            return visual;

        // SpriteRenderer가 없는 순수 로직 오브젝트에는 색 변경 비주얼을 붙여도 의미가 없습니다.
        // 보스처럼 스프라이트가 자식에 있는 경우까지 허용하기 위해 InChildren으로 검사합니다.
        if (target.GetComponentInChildren<SpriteRenderer>() == null)
            return null;

        // StatusController.ApplyStatus에서 호출되므로, 상태이상이 처음 걸리는 순간 자동으로 표시 기능이 붙습니다.
        // 몬스터/보스 프리팹마다 수동으로 PlayerStatusVisual을 붙이지 않아도 됩니다.
        return target.AddComponent<PlayerStatusVisual>();
    }

    private void Awake()
    {
        _statusController = GetComponent<StatusController>();

        if (playerRenderer == null)
            playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<SpriteRenderer>();

        // 보스 Phase 색상처럼 기본 색이 흰색이 아닐 수 있으므로 시작 색을 정상 색으로 기억합니다.
        // 상태이상이 끝났을 때 무조건 흰색으로 돌아가면 기존 연출을 덮어버릴 수 있습니다.
        if (captureInitialColor && playerRenderer != null)
            normalColor = playerRenderer.color;

        if (effectAnchor == null)
            effectAnchor = transform;
    }

    private void OnEnable()
    {
        if (_statusController != null)
            _statusController.OnStatusChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (_statusController != null)
            _statusController.OnStatusChanged -= Refresh;
    }

    /// <summary>
    /// 현재 활성 상태 중 표시 우선순위가 가장 높은 항목 하나를 골라 색/아이콘/이펙트를 갱신합니다.
    /// 여러 아이콘을 동시에 보여주고 싶으면 이 함수를 확장해서 아이콘 슬롯 리스트에 뿌리면 됩니다.
    /// </summary>
    public void Refresh()
    {
        if (_statusController == null) return;

        _statusController.GetActiveStatusTypes(_activeStatuses);

        if (!TryGetTopVisual(out StatusVisualEntry visual))
        {
            ApplyColor(normalColor);
            ApplyIcon(null);
            ApplyEffect(null);
            return;
        }

        ApplyColor(visual.ChangePlayerColor ? visual.PlayerColor : normalColor);
        ApplyIcon(visual.Icon);
        ApplyEffect(visual.EffectPrefab);
    }

    private bool TryGetTopVisual(out StatusVisualEntry visual)
    {
        for (int i = 0; i < visuals.Length; i++)
        {
            for (int j = 0; j < _activeStatuses.Count; j++)
            {
                if (visuals[i].StatusType != _activeStatuses[j]) continue;

                visual = visuals[i];
                return true;
            }
        }

        visual = default;
        return false;
    }

    private void ApplyColor(Color color)
    {
        if (playerRenderer != null)
            playerRenderer.color = color;
    }

    private void ApplyIcon(Sprite icon)
    {
        if (statusIconImage == null) return;

        statusIconImage.sprite = icon;
        statusIconImage.enabled = icon != null;
    }

    private void ApplyEffect(GameObject effectPrefab)
    {
        if (_activeEffect != null)
        {
            Destroy(_activeEffect);
            _activeEffect = null;
        }

        if (effectPrefab == null) return;

        _activeEffect = Instantiate(effectPrefab, effectAnchor);
        _activeEffect.transform.localPosition = Vector3.zero;
    }

    [Serializable]
    private struct StatusVisualEntry
    {
        public StatusType StatusType;
        public Color PlayerColor;
        public bool ChangePlayerColor;
        public Sprite Icon;
        public GameObject EffectPrefab;

        public StatusVisualEntry(StatusType statusType, Color playerColor, bool changePlayerColor)
        {
            StatusType = statusType;
            PlayerColor = playerColor;
            ChangePlayerColor = changePlayerColor;
            Icon = null;
            EffectPrefab = null;
        }
    }
}
