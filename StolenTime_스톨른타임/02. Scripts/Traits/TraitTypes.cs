using System;
using UnityEngine;

public enum TraitTier
{
    // GDD 기준으로 레벨 보상 포인트가 티어별로 따로 지급되므로 enum으로 분리한다.
    // 같은 특성 포인트 하나로 전부 올리게 만들면 1티어 포인트로 3티어 특성을 찍는 문제가 생긴다.
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}

/// <summary>
/// 특성을 코드에서 구분하기 위한 고정 ID입니다.
/// 화면에 보이는 이름/설명은 LocalizationManager가 담당하므로, 여기 enum 이름은 저장/효과 연결 안정성을 우선합니다.
/// </summary>
public enum TraitId
{
    // 저장/조회용 ID는 씬 오브젝트 이름과 맞춘다.
    // 디자이너가 Inspector에서 버튼을 연결할 때 같은 이름을 고르면 되도록 하기 위해서다.
    Appraisal,
    Healthy_Body,
    Survivalist,
    Physical,
    Greed,
    Lightfoot,
    Trinket,
    HeavyBlow,
    EagleEye
}

/// <summary>
/// 특성 하나의 UI 표시 데이터입니다.
/// 1차 구현에서는 MonoBehaviour의 배열로 관리하고, 나중에 특성 수가 늘어나면 ScriptableObject로 옮기기 쉬운 형태로 둡니다.
/// </summary>
[Serializable]
public class TraitData
{
    // ScriptableObject까지 만들기 전 단계라 직렬화 가능한 데이터 클래스로 둔다.
    // 이후 특성 수가 늘어나면 이 구조를 그대로 SO로 옮겨도 UI/컨트롤러 쪽 변경이 작다.
    [Header("식별 정보")]
    [Tooltip("이 특성이 어떤 효과 코드와 연결되는지 정하는 고정 ID입니다. 표시 이름을 바꿔도 ID는 가능하면 유지하세요.")]
    public TraitId id;

    [Tooltip("이 특성을 올릴 때 소비할 포인트 티어입니다. Lv2~4는 Tier1, Lv5~7은 Tier2, Lv8~10은 Tier3 포인트를 줍니다.")]
    public TraitTier tier;

    [Header("상세 UI 표시")]
    [Tooltip("상세 패널에 표시할 아이콘입니다. 비워두면 TraitUIController가 오른쪽 버튼 아이콘을 대신 사용합니다.")]
    public Sprite icon;

    // 이름/설명 텍스트는 LocalizationManager의 키로 조회한다 (TraitController.GetTraitDisplayText 참고).
}
