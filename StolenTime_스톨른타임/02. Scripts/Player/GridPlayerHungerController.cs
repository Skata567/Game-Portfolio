using UnityEngine;

/// <summary>
/// 플레이어의 배고픔(Hunger) 수치를 시간에 따라 감소시키고, 구간에 따른 체력 증감 틱을 관리하는 전담 컴포넌트입니다.
/// </summary>
public class GridPlayerHungerController : MonoBehaviour
{
    private NYHPlayerStats _stats;
    private GridPlayerHealth _health;
    private StatusController _statusController;

    private float _hungerDrainTimer = 0f;
    private float _healthTickTimer = 0f;

    private void Start()
    {
        _stats = GetComponent<NYHPlayerStats>();
        if (_stats == null) _stats = NYHPlayerStats.Instance;

        _health = GetComponent<GridPlayerHealth>();
        _statusController = GetComponent<StatusController>();

        if (_stats != null)
        {
            _stats.OnStatsChanged += HandleHungerState;
            HandleHungerState(); // 초기 상태 갱신
        }
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnStatsChanged -= HandleHungerState;
        }
    }

    private void Update()
    {
        // 시간이 흐르지 않으면(예: Safe Room) 배고픔도 닳지 않습니다.
        if (TimeSystem.Instance == null || !TimeSystem.Instance.IsRunning) return;
        if (_stats == null || _health == null || _health.IsDead) return;

        float dt = Time.deltaTime;

        // 1. 배고픔 수치 자연 감소 (10초에 1)
        _hungerDrainTimer += dt;
        if (_hungerDrainTimer >= 10f)
        {
            _hungerDrainTimer -= 10f;
            if (_stats.Hunger > 0)
            {
                _stats.AddStat(NYHStatType.Hunger, -1);
            }
        }

        // 2. 배고픔 상태에 따른 체력 틱 처리
        _healthTickTimer += dt;
        int currentHunger = _stats.Hunger;

        if (currentHunger == 0) // 굶주림: 5초마다 데미지 1
        {
            if (_healthTickTimer >= 5f)
            {
                _healthTickTimer -= 5f;
                _health.TakeDamage(1); 
            }
        }
        else if (currentHunger <= 3) // 배고픔: 10초마다 체력 회복 1
        {
            if (_healthTickTimer >= 10f)
            {
                _healthTickTimer -= 10f;
                _health.Heal(1);
            }
        }
        else // 보통: 5초마다 체력 회복 1
        {
            if (_healthTickTimer >= 5f)
            {
                _healthTickTimer -= 5f;
                _health.Heal(1);
            }
        }
    }

    private void HandleHungerState()
    {
        if (_stats == null || _statusController == null) return;

        // 배고픔이 0이 되면 상태이상(Hunger)을 적용하여 이동 속도를 늦춥니다.
        if (_stats.Hunger == 0)
        {
            if (!_statusController.IsHungry)
            {
                _statusController.ApplyStatus(new StatusRequest(null, Vector2Int.zero, StatusType.Hunger, 9999, 0, null));
            }
        }
        else
        {
            // 배고픔 수치가 1 이상 회복되면 굶주림 상태이상을 해제합니다.
            if (_statusController.IsHungry)
            {
                _statusController.TryRemoveStatus(StatusType.Hunger, true);
            }
        }
    }
}
