using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 몬스터 구매 서비스를 관리하는 서비스 클래스
/// 플레이어가 몬스터를 구매하고 인벤토리에 추가하는 로직을 담당
/// FindObjectOfType로 Start()에서 한 번만 호출하여 순환 초기화
/// </summary>
public class MonsterPurchaseService : MonoBehaviour
{

    private AssignmentManager monsterData;

    private bool isInitialized = false; // 초기화 여부


    void Start()
    {
        Debug.Log("=== MonsterPurchaseService Start 시작 ===");
        monsterData = FindAnyObjectByType<AssignmentManager>();

        isInitialized = true;

        if (monsterData == null)
        {
            Debug.LogError("MonsterData를 찾을 수 없습니다.");
        }
        else
        {
            // JSON 로드 후 유닛 정보 확인
            Debug.Log($"[MonsterPurchaseService] MonsterData 초기화 완료 - " +
                     $"총 {monsterData.unitStocks.Count}개 유닛, " +
                     $"현재 보유: {monsterData.unitStocks.Sum(u => u.owned)}마리");
        }
    }

    /// <summary>
    /// 유효한 MonsterData를 반환하는 안전 메서드 (재시도 포함)
    /// 왜 반환값이 없거나 파괴된 경우 다시 찾아서 반환
    /// </summary>
    /// <returns>유효한 MonsterData 또는 null</returns>
    private AssignmentManager GetValidMonsterData()
    {
        // 현재 참조가 유효한지 확인
        if (monsterData != null)
            return monsterData;

        // 참조가 없거나 경우 다시 찾기 시도
        Debug.LogWarning("[MonsterPurchaseService] MonsterData 참조가 유실되어 다시 찾는 중...");

        // Singleton 방식 시도
        monsterData = AssignmentManager.Instance;

        // Singleton이 없으면 직접 찾기
        if (monsterData == null)
        {
            monsterData = FindAnyObjectByType<AssignmentManager>();
        }

        if (monsterData == null)
        {
            Debug.LogError("[MonsterPurchaseService] MonsterData를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log("[MonsterPurchaseService] MonsterData 재연결 완료");
        }

        return monsterData;
    }


    /// <summary>
    /// 몬스터 구매
    /// </summary>
    /// if(result.success) Debug.Log("구매 성공!");
    /// 이게 보이면?
    public (bool success, string message, int newGoldAmount, int newIronAmount)
    PurchaseMonster(string enemyId, int quantity, int playerGold, int playerIron)
    {
        // 몬스터 데이터 먼저 가져오기 (공통)
        var currentMonsterData = GetValidMonsterData();
        if (currentMonsterData == null)
        {
            return (false, "몬스터 데이터를 찾을 수 없습니다.", playerGold, playerIron);
        }

        // 대상 몬스터 찾기 (공통)
        var targetMonster = currentMonsterData.unitStocks.FirstOrDefault(u => u.enemyId == enemyId);
        if (targetMonster == null)
        {
            return (false, $"ID '{enemyId}'인 몬스터를 찾을 수 없습니다.", playerGold, playerIron);
        }

        //  비용 계산 (튜토리얼/일반 모드 공통)
        int totalGoldCost = targetMonster.price_Gold * quantity;
        int totalIronCost = targetMonster.price_Iron * quantity;

        // 튜토리얼 모드에서는 재화만 별도 관리, 인벤토리는 실제로 추가
        if (TutorialManager.IsTutorialMode)
        {
            //  튜토리얼 중 고블린 구매 단계에서는 고블린(101)만 구매 가능
            if (TutorialManager.Instance.CurrentStep == TutorialStep.BuyGoblins)
            {
                // 구매 허용 플래그 체크 (대화 완료 전까지 구매 차단)
                if (!TutorialManager.Instance.canPurchase)
                {
                    Debug.Log($"[MonsterPurchaseService] 튜토리얼: 대화를 먼저 완료하세요!");
                    return (false, "대화를 먼저 완료하세요!", playerGold, playerIron);
                }

                if (enemyId != "101")
                {
                    Debug.Log($"[MonsterPurchaseService] 튜토리얼 모드: 고블린만 구매 가능합니다!");
                    return (false, "튜토리얼에서는 고블린만 구매할 수 있습니다!", playerGold, playerIron);
                }

                // 5마리 이상 구매 차단
                int currentCount = TutorialManager.Instance.GetTutorialMonsterCount("101");
                if (currentCount >= 5)
                {
                    Debug.Log($"[MonsterPurchaseService] 튜토리얼: 고블린은 5마리까지만 구매 가능합니다!");
                    return (false, "고블린은 5마리까지만 구매 가능합니다!", playerGold, playerIron);
                }
            }

            // 튜토리얼 재화에서 차감 시도
            bool deductSuccess = TutorialManager.Instance.DeductTutorialResources(totalGoldCost, totalIronCost);
            if (!deductSuccess)
            {
                return (false, "재화가 부족합니다!", playerGold, playerIron);
            }

            // 실제 인벤토리에 추가 (UI에 표시되도록)
            AssignmentManager.Instance.AddOwned(enemyId, quantity);

            // 도감 획득 기록 설정 (한 번이라도 소유하면 도감에 영구 등록)
            targetMonster.hasBeenOwned = true;

            // 튜토리얼 매니저에 카운트 추가 (진행 추적용)
            TutorialManager.Instance.AddTutorialMonster(targetMonster.enemyId, quantity);

            // 유닛 변경 이벤트 발생
            MonsterCatalog.Instance?.NotifyUnitsChanged();

            // UI 업데이트 (재화 표시)
            var uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateUI();
            }

            Debug.Log($"[MonsterPurchaseService] 튜토리얼 모드: {targetMonster.displayName} {quantity}마리 구매 완료 (인벤토리 추가됨, Gold-{totalGoldCost}, Iron-{totalIronCost})");
            return (true, $"{targetMonster.displayName} {quantity}마리를 구매했습니다!", playerGold, playerIron);
        }

        //캐시된 데이터 검증 (순환 초기화)
        if (!isInitialized || monsterData == null)
        {
            return (false, "몬스터 데이터가 초기화되지 않았습니다.", playerGold, playerIron);
        }

        int currentStage = StaticInfoManager.floor;
        Debug.Log($" 구매 시도 - 현재 스테이지: {currentStage}");

        //스테이지 잠금 확인
        if (targetMonster.stage > currentStage)
        {
            return (false,
                   $"{targetMonster.displayName}은(는) 스테이지 {targetMonster.stage}까지 잠금됩니다. (현재: {currentStage})",
                   playerGold, playerIron);
        }

        // 비용 확인

        if (playerGold < totalGoldCost)
        {
            return (false, $"골드가 부족합니다. 필요: {totalGoldCost:N0}, 보유: {playerGold:N0}",
                   playerGold, playerIron);
        }

        if (playerIron < totalIronCost)
        {
            return (false, $"철이 부족합니다. 필요: {totalIronCost:N0}, 보유: {playerIron:N0}",
                   playerGold, playerIron);
        }

        // 구매 처리
        AssignmentManager.Instance.AddOwned(enemyId, quantity);

        // 도감 획득 기록 설정 (한 번이라도 소유하면 도감에 영구 등록)
        targetMonster.hasBeenOwned = true;

        int newGold = playerGold - totalGoldCost;
        int newIron = playerIron - totalIronCost;

        StaticInfoManager.gold = newGold;
        StaticInfoManager.iron = newIron;

        // 유닛 변경 이벤트 발생
        MonsterCatalog.Instance?.NotifyUnitsChanged();

        Debug.Log($" 구매 완료: {targetMonster.displayName} {quantity}마리");
        return (true, $"{targetMonster.displayName} {quantity}마리를 구매했습니다!", newGold, newIron);
    }


    /// <summary>
    /// 플레이어 보유 몬스터 개수를 조회하는 메서드
    /// </summary>
    /// <param name="enemyId">조회할 몬스터 ID</param>
    /// <returns>보유 개수 (없으면 0 반환)</returns>
    public int GetOwnedMonsterCount(string enemyId)
    {
        var currentData = GetValidMonsterData();
        if (currentData == null) return 0;

        return monsterData.unitStocks.FirstOrDefault(u => u.enemyId == enemyId)?.owned ?? 0;
    }

    /// <summary>
    /// 보유 중인 모든 몬스터 반환
    /// </summary>
    /// <returns>보유 개수가 0보다 큰 몬스터들의 리스트</returns>
    public List<UnitStock> GetInventory()
    {
        var currentData = GetValidMonsterData();
        if (currentData == null) return new List<UnitStock>();

        // 보유 개수가 0보다 큰 몬스터 반환 (인벤토리 필터링)
        return monsterData.unitStocks.Where(u => u.owned > 0).ToList();
    }

    /// <summary>
    /// 상점에 표시할 몬스터 목록 반환 - 스테이지별 몬스터 필터!
    /// 현재 스테이지에서 구매 가능한 몬스터를 반환
    /// UI에서 상점 아이콘 생성 시 이 메서드 사용
    /// </summary>
    /// <returns>현재 스테이지에서 구매 가능한 몬스터 리스트</returns>
    public List<UnitStock> GetShopItems()
    {
        var currentData = GetValidMonsterData();
        if (currentData == null) return new List<UnitStock>();

        int currentStage = StaticInfoManager.floor;

        // LINQ를 이용해 스테이지별 잠금을 몬스터를 필터링
        var availableMonsters = monsterData.unitStocks
            .Where(monster => monster.stage <= currentStage)
            .OrderBy(monster => int.Parse(monster.enemyId))  // enemyId 숫자 순서로 정렬 (101 -> 102 -> 103...)
            .ToList();

        return availableMonsters;
    }
    /// <summary>
    /// 잠금 확인
    /// </summary>
    /// <param name="enemyId"></param>
    /// <returns></returns>
    public bool IsMonsterLocked(string enemyId)
    {
        if (!isInitialized || monsterData == null)
        {
            Debug.LogWarning($"[IsMonsterLocked] 초기화되지 않음 - enemyId: {enemyId}");
            return true;
        }

        var monster = monsterData.unitStocks.FirstOrDefault(u => u.enemyId == enemyId);
        if (monster == null)
        {
            Debug.LogWarning($"[IsMonsterLocked] 몬스터를 찾을 수 없음 - enemyId: {enemyId}");
            return true;
        }

        int currentStage = StaticInfoManager.floor;
        bool isLocked = monster.stage > currentStage;


        return isLocked;
    }

    /// <summary>
    /// 잠금 몬스터들 미리보기 (향후추가)
    /// 현재 스테이지에서 잠긴 몬스터들의 회색아이콘 표시하고 싶을 때 사용
    /// </summary>
    /// <param name="previewCount">미리보기로 표시할 개수</param>
    /// <returns>정렬된 잠긴 몬스터들</returns>
    public List<UnitStock> GetLockedMonstersPreview(int previewCount = 3)
    {
        if (monsterData == null)
            return new List<UnitStock>();

        int currentStage = StaticInfoManager.floor;

        // 현재 스테이지에서 잠긴 몬스터들 중 가장 가까운 것들
        var lockedMonsters = monsterData.unitStocks
            .Where(monster => monster.stage > currentStage)
            .OrderBy(monster => monster.stage)
            .Take(previewCount)
            .ToList();

        return lockedMonsters;
    }


    /// <summary>
    /// 몬스터를 판매하는 메서드 (인벤토리에서 사용)
    /// 미편성 유닛을 먼저 판매하고, 부족하면 편성된 유닛을 제거
    /// </summary>
    /// <param name="enemyId">판매할 몬스터 ID</param>
    /// <param name="quantity">판매할 개수</param>
    /// <returns>판매 성공 여부와 메시지, 획득 골드를 포함한 튜플</returns>
    public (bool success, string message, int goldEarned, int ironEarned) SellMonster(string enemyId, int quantity)
    {
        if (monsterData == null)
        {
            return (false, "몬스터 데이터가 초기화되지 않았습니다.", 0, 0);
        }

        var targetMonster = monsterData.unitStocks.FirstOrDefault(u => u.enemyId == enemyId);
        if (targetMonster == null)
        {
            return (false, $"ID '{enemyId}'인 몬스터를 찾을 수 없습니다.", 0, 0);
        }

        if (targetMonster.owned < quantity)
        {
            return (false, $"보유 개수가 부족합니다. 보유: {targetMonster.owned}, 판매 시도: {quantity}", 0, 0);
        }

        // 편성된 유닛 수량 계산
        var formationManager = SquadFormationManager.Instance;
        int totalAssigned = 0;

        if (formationManager != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var squad = formationManager.squads[i];
                if (squad.memberIds.Count > 0 && squad.memberIds[0] == enemyId)
                {
                    totalAssigned += squad.memberIds.Count;
                }
            }
        }

        // 미편성 수량 계산
        int unassigned = targetMonster.owned - totalAssigned;

        Debug.Log($"[MonsterPurchaseService] 판매 분석 - 보유: {targetMonster.owned}, 편성: {totalAssigned}, 미편성: {unassigned}, 판매 시도: {quantity}");

        // 판매 가격은 구매 가격의 절반으로 설정 (일반적인 게임 경제)
        int sellGoldPrice = targetMonster.price_Gold / 2;
        int sellIronPrice = targetMonster.price_Iron / 2;
        int totalEarned = sellGoldPrice * quantity;
        int totalIronEarned = sellIronPrice * quantity;

        // 보유 수량 차감
        targetMonster.owned -= quantity;

        // 편성 조정
        if (quantity <= unassigned)
        {
            // 미편성만 판매 - 편성 변경 불필요
            Debug.Log($"[MonsterPurchaseService] 미편성 유닛만 판매 ({quantity}마리)");
        }
        else
        {
            // 미편성 전부 + 편성에서도 제거 필요
            int needToRemoveFromFormation = quantity - unassigned;
            Debug.Log($"[MonsterPurchaseService] 편성에서 {needToRemoveFromFormation}마리 제거 필요");

            if (formationManager != null)
            {
                // 각 부대에서 순서대로 제거
                for (int i = 0; i < 3 && needToRemoveFromFormation > 0; i++)
                {
                    var squad = formationManager.squads[i];
                    if (squad.memberIds.Count > 0 && squad.memberIds[0] == enemyId)
                    {
                        int currentCount = squad.memberIds.Count;
                        int removeCount = Mathf.Min(needToRemoveFromFormation, currentCount);

                        Debug.Log($"[MonsterPurchaseService] {squad.name}에서 {removeCount}마리 제거 (현재: {currentCount})");

                        // memberIds에서 뒤에서부터 제거
                        squad.memberIds.RemoveRange(squad.memberIds.Count - removeCount, removeCount);

                        needToRemoveFromFormation -= removeCount;

                        // 부대가 비었으면 완전히 초기화 (룬도 제거)
                        if (squad.memberIds.Count == 0)
                        {
                            formationManager.ClearSquadUnit(i);
                            Debug.Log($"[MonsterPurchaseService] {squad.name} 완전히 초기화 (룬 제거)");
                        }
                    }
                }

                // 편성 변경 이벤트 발동 (UI 새로고침)
                formationManager.ReFreshUI();
            }
        }

        // 유닛 변경 이벤트 발생
        MonsterCatalog.Instance?.NotifyUnitsChanged();

        Debug.Log($"[MonsterPurchaseService] 판매 완료 - 최종 보유: {targetMonster.owned}마리");
        return (true, $"{targetMonster.displayName} {quantity}마리를 판매했습니다!", totalEarned, totalIronEarned);
    }

    /// <summary>
    /// 특정 몬스터의 전체 정보를 반환하는 메서드
    /// </summary>
    /// <param name="enemyId">조회할 몬스터 ID</param>
    /// <returns>몬스터 정보 (없으면 null)</returns>
    ///
    public UnitStock GetMonsterInfo(string enemyId)
    {
        var currentData = GetValidMonsterData();
        if (currentData == null) return null;

        return monsterData.unitStocks.FirstOrDefault(u => u.enemyId == enemyId);
    }
}
