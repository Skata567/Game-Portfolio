/*using System.Collections.Generic;
using UnityEngine;
using System.Linq;

이제는 필요가 없는 스크립트

public class ShopSMonsterData : MonoBehaviour
{
    private SpecialMonsterData specialmonsterData; // FindObjectOfType으로 찾은 MonsterData 캐싱
    public List<SpecialUnitStock> specialUnitStocks; // 호환성을 위해 유지

    void Start()
    {
        // MonsterData를 Start()에서 한 번만 찾아서 캐싱
        specialmonsterData = FindAnyObjectByType<SpecialMonsterData>();

        if (specialmonsterData != null)
        {
            // 원본 데이터의 참조를 그대로 사용 (복사하지 않음)
            specialUnitStocks = specialmonsterData.sUnits;
        }
        else
        {
            Debug.LogError("MonsterData를 찾을 수 없습니다!");
            specialUnitStocks = new List<SpecialUnitStock>(); // 빈 리스트로 초기화
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
        if (specialUnitStocks == null) return 0;

        return specialUnitStocks.FirstOrDefault(u => u.enemyId == enemyId)?.owned ?? 0;
    }
}*/