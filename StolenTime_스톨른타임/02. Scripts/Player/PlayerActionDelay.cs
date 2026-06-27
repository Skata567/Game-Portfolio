using UnityEngine;

/// <summary>
/// 플레이어 행동 후 다음 행동까지의 딜레이를 관리합니다.
/// 이동/공격/아이템의 timeCost는 남은 시간을 깎는 값이 아니라 이 컴포넌트의 입력 잠금 시간으로 사용합니다.
/// </summary>
public class PlayerActionDelay : MonoBehaviour
{
    public float RemainingDelay { get; private set; }
    public bool CanAct => RemainingDelay <= 0f;

    private void Update()
    {
        if (RemainingDelay <= 0f) 
        return;


         RemainingDelay = Mathf.Max(0f, RemainingDelay - Time.deltaTime);
    }

    /// <summary>
    /// 새 행동 딜레이를 시작합니다.
    /// 이미 더 긴 딜레이가 남아 있으면 짧은 딜레이로 덮지 않습니다.
    /// </summary>
    public void StartDelay(float seconds)
    {
        RemainingDelay = Mathf.Max(RemainingDelay, Mathf.Max(0f, seconds));
    }

    public void Clear()
    {
        RemainingDelay = 0f;
    }
}
