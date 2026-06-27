using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShopMonsterData : MonoBehaviour
{
    private AssignmentManager monsterData; // FindObjectOfType으로 찾은 MonsterData 캐싱
    public List<UnitStock> unitStocks; // 호환성을 위해 유지

    void Start()
    {
        // MonsterData를 Start()에서 한 번만 찾아서 캐싱
        monsterData = FindAnyObjectByType<AssignmentManager>();

        if (monsterData != null)
        {
            // 원본 데이터의 참조를 그대로 사용 (복사하지 않음)
            unitStocks = monsterData.unitStocks;
        }
        else
        {
            Debug.LogError("MonsterData를 찾을 수 없습니다!");
            unitStocks = new List<UnitStock>(); // 빈 리스트로 초기화
        }
    }

    /// <summary>
    /// 특정 몬스터의 보유 수량을 반환하는 메서드
    /// </summary>
    /// <param name="enemyId">조회할 몬스터 ID</param>
    /// <returns>보유 수량 (없으면 0)</returns>
    /// 
    /// 사용 예시:
    /// int owned = shopMonsterData.GetOwned("goblin");
    /// Debug.Log($"고블린 보유량: {owned}");
    public int GetOwned(string enemyId)
    {
        if (unitStocks == null) return 0;

        return unitStocks.FirstOrDefault(u => u.enemyId == enemyId)?.owned ?? 0;
    }
}