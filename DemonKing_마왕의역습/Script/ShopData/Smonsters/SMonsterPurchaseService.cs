using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 특수 몬스터 구매 서비스 (MonsterCatalog 호환)
/// 특수 몬스터 구매/판매 등 로직 담당
/// </summary>
public class SMonsterPurchaseService : MonoBehaviour
{
    #region Private Fields

    private MonsterCatalog catalogCache;
    private List<UnitStock> normalShopCache;
    private List<UnitStock> secretShopCache;
    private bool cacheValid = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeCatalog();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// MonsterCatalog 초기화 및 캐시 설정
    /// </summary>
    private void InitializeCatalog()
    {
        catalogCache = MonsterCatalog.Instance;

        if (catalogCache == null)
        {
            Debug.LogError("[SMonsterPurchaseService] MonsterCatalog 인스턴스를 찾을 수 없습니다!");
            return;
        }

        if (catalogCache.IsReady)
        {
            InitializeCache();
        }
        else
        {
            catalogCache.OnReady += InitializeCache;
        }

        Debug.Log($"[SMonsterPurchaseService] 초기화 완료 - 특수 몬스터 {catalogCache.specialUnits.Count}개");
    }

    /// <summary>
    /// 캐시 초기화
    /// </summary>
    private void InitializeCache()
    {
        InvalidateCache();
        UpdateCache();

        Debug.Log($"[SMonsterPurchaseService] 캐시 초기화 완료");
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// 캐시 무효화
    /// </summary>
    private void InvalidateCache()
    {
        cacheValid = false;
        normalShopCache = null;
        secretShopCache = null;
    }

    /// <summary>
    /// 캐시 업데이트 (shop 필드에 따라 분류)
    /// </summary>
    private void UpdateCache()
    {
        if (cacheValid && normalShopCache != null && secretShopCache != null)
            return;

        if (!ValidateCatalog())
        {
            normalShopCache = new List<UnitStock>();
            secretShopCache = new List<UnitStock>();
            cacheValid = true;
            return;
        }

        normalShopCache = new List<UnitStock>();
        secretShopCache = new List<UnitStock>();

        foreach (var unit in catalogCache.specialUnits)
        {
            var meta = catalogCache.GetSpecialMeta(unit.enemyId);
            if (meta.shop)
                normalShopCache.Add(unit);
            else
                secretShopCache.Add(unit);
        }

        // enemyId 숫자 순서로 정렬 (201 -> 202 -> 203...)
        normalShopCache = normalShopCache.OrderBy(monster => int.Parse(monster.enemyId)).ToList();
        secretShopCache = secretShopCache.OrderBy(monster => int.Parse(monster.enemyId)).ToList();

        cacheValid = true;
    }

    /// <summary>
    /// MonsterCatalog 유효성 검사
    /// </summary>
    private bool ValidateCatalog()
    {
        if (catalogCache == null)
        {
            catalogCache = MonsterCatalog.Instance;
            if (catalogCache == null)
            {
                Debug.LogError("[SMonsterPurchaseService] MonsterCatalog 재연결 실패");
                return false;
            }
        }

        return catalogCache.IsReady && catalogCache.specialUnits != null;
    }

    #endregion

    #region Public Methods - Shop Items

    /// <summary>
    /// 일반 교환소 아이템 조회 (shop = true)
    /// </summary>
    public List<UnitStock> GetNormalShopItems()
    {
        UpdateCache();
        return new List<UnitStock>(normalShopCache);
    }

    /// <summary>
    /// 비밀 교환소 아이템 조회 (shop = false)
    /// </summary>
    public List<UnitStock> GetSecretShopItems()
    {
        UpdateCache();
        return new List<UnitStock>(secretShopCache);
    }

    /// <summary>
    /// 상점 타입별 아이템 조회
    /// </summary>
    public List<UnitStock> GetShopItemsByType(bool isNormalShop)
    {
        return isNormalShop ? GetNormalShopItems() : GetSecretShopItems();
    }

    /// <summary>
    /// 전체 특수 몬스터 상점 아이템 조회
    /// </summary>
    public List<UnitStock> GetShopItems()
    {
        if (!ValidateCatalog())
            return new List<UnitStock>();

        return catalogCache.specialUnits.ToList();
    }

    #endregion

    #region Public Methods - Purchase

    /// <summary>
    /// 특수 몬스터 구매 (미스릴 소모, 최대 1개)
    /// </summary>
    public (bool success, string message, int newMithrilAmount) PurchaseMonster(string enemyId, int quantity, int playerMithril)
    {
        // 튜토리얼 모드에서는 특수 몬스터 구매 불가
        if (TutorialManager.IsTutorialMode)
        {
            Debug.Log("[SMonsterPurchaseService] 튜토리얼 모드에서는 특수 몬스터를 구매할 수 없습니다!");
            return (false, "튜토리얼 모드에서는 특수 몬스터를 구매할 수 없습니다.", playerMithril);
        }

        if (!ValidateCatalog())
        {
            return (false, "특수 몬스터 데이터를 찾을 수 없습니다.", playerMithril);
        }

        var targetMonster = catalogCache.specialUnits.FirstOrDefault(u => u.enemyId == enemyId);
        if (targetMonster == null)
        {
            return (false, $"ID '{enemyId}'인 특수 몬스터를 찾을 수 없습니다.", playerMithril);
        }

        // 이미 소유 체크 (특수 몬스터는 1개 제한)
        if (targetMonster.owned >= 1)
        {
            return (false, $"{targetMonster.displayName}은(는) 이미 보유하고 있습니다.", playerMithril);
        }

        var meta = catalogCache.GetSpecialMeta(enemyId);
        int totalCost = meta.price * quantity;

        // 미스릴 부족 체크
        if (playerMithril < totalCost)
        {
            return (false, $"미스릴이 부족합니다. 필요: {totalCost}, 보유: {playerMithril}", playerMithril);
        }

        // 구매 처리
        targetMonster.owned = 1; // 특수 몬스터는 최대 1개
        Debug.Log($"[SMonsterPurchaseService] 구매: {targetMonster.displayName}, owned={targetMonster.owned}, enemyId={targetMonster.enemyId}");

        // 도감 획득 기록 설정 (한 번이라도 소유하면 도감에 영구 등록)
        targetMonster.hasBeenOwned = true;

        int newMithril = playerMithril - totalCost;

        StaticInfoManager.mis = newMithril;

        // 캐시 무효화 (상점 UI 갱신용)
        InvalidateCache();

        // 유닛 변경 이벤트 발생
        MonsterCatalog.Instance?.NotifyUnitsChanged();

        return (true, $"{targetMonster.displayName}을(를) 성공적으로 구매했습니다!", newMithril);
    }

    #endregion

    #region Public Methods - Sell

    /// <summary>
    /// 특수 몬스터 판매
    /// </summary>
    public (bool success, string message, int mithrilEarned) SellMonster(string enemyId, int quantity)
    {
        if (!ValidateCatalog())
        {
            return (false, "특수 몬스터 데이터가 초기화되지 않았습니다.", 0);
        }

        var targetMonster = catalogCache.specialUnits.FirstOrDefault(u => u.enemyId == enemyId);
        if (targetMonster == null)
        {
            return (false, $"ID '{enemyId}'인 특수 몬스터를 찾을 수 없습니다.", 0);
        }

        if (targetMonster.owned < 1)
        {
            return (false, $"{targetMonster.displayName}을(를) 보유하고 있지 않습니다.", 0);
        }

        var meta = catalogCache.GetSpecialMeta(enemyId);
        int sellPrice = meta.price / 2; // 판매 가격은 구매 가격의 절반

        targetMonster.owned = 0; // 특수 몬스터는 전체 판매

        //군단에서 해당 유닛 제거 (판매한 유닛이 군단에 배치되어 있으면 제거)
        var formationManager = SquadFormationManager.Instance;
        if (formationManager != null)
        {
            // 3개 군단을 순회하면서 해당 유닛이 있으면 제거
            for (int i = 0; i < 3; i++)
            {
                var squad = formationManager.squads[i];
                if (squad.memberIds.Count > 0 && squad.memberIds[0] == enemyId)
                {
                    formationManager.ClearSquadUnit(i);
                    Debug.Log($"[SMonsterPurchaseService] {targetMonster.displayName}을(를) {squad.name}에서 제거 (판매)");
                }
            }
        }

        // 유닛 변경 이벤트 발생
        MonsterCatalog.Instance?.NotifyUnitsChanged();

        return (true, $"{targetMonster.displayName}을(를) 판매했습니다!", sellPrice);
    }

    #endregion

    #region Public Methods - Query

    /// <summary>
    /// 특수 몬스터 보유 여부 확인
    /// </summary>
    public bool IsMonsterAlreadyOwned(string enemyId)
    {
        if (!ValidateCatalog()) return false;

        var targetMonster = catalogCache.specialUnits.FirstOrDefault(u => u.enemyId == enemyId);
        return targetMonster != null && targetMonster.owned >= 1;
    }

    /// <summary>
    /// 보유 개수 조회
    /// </summary>
    public int GetOwnedMonsterCount(string enemyId)
    {
        if (!ValidateCatalog()) return 0;

        return catalogCache.specialUnits.FirstOrDefault(u => u.enemyId == enemyId)?.owned ?? 0;
    }

    /// <summary>
    /// 보유중인 특수 몬스터 목록 조회
    /// </summary>
    public List<UnitStock> GetInventory()
    {
        if (!ValidateCatalog())
            return new List<UnitStock>();

        return catalogCache.specialUnits.Where(u => u.owned > 0).ToList();
    }

    /// <summary>
    /// 상점 타입별 보유 몬스터 조회
    /// </summary>
    public List<UnitStock> GetInventoryByShopType(bool isNormalShop)
    {
        UpdateCache();
        var targetList = isNormalShop ? normalShopCache : secretShopCache;
        return targetList.Where(monster => monster.owned > 0).ToList();
    }

    /// <summary>
    /// 특수 몬스터 전체 정보 조회
    /// </summary>
    public UnitStock GetMonsterInfo(string enemyId)
    {
        if (!ValidateCatalog()) return null;

        return catalogCache.specialUnits.FirstOrDefault(u => u.enemyId == enemyId);
    }

    /// <summary>
    /// 특수 몬스터의 상점 타입 조회
    /// </summary>
    public bool? GetMonsterShopType(string enemyId)
    {
        if (!ValidateCatalog()) return null;

        var meta = catalogCache.GetSpecialMeta(enemyId);
        return meta.shop;
    }

    /// <summary>
    /// 상점별 아이템 개수 조회
    /// </summary>
    public (int normalShopCount, int secretShopCount) GetShopItemCounts()
    {
        UpdateCache();
        return (normalShopCache.Count, secretShopCache.Count);
    }

    #endregion

    #region Public Methods - Meta Info

    /// <summary>
    /// 특수 몬스터 메타 정보 조회
    /// </summary>
    public MonsterCatalog.SpecialMeta GetSpecialMeta(string enemyId)
    {
        if (!ValidateCatalog())
            return new MonsterCatalog.SpecialMeta { price = 0, shop = true };

        return catalogCache.GetSpecialMeta(enemyId);
    }

    #endregion

    #region Unity Lifecycle - Cleanup

    private void OnDestroy()
    {
        if (catalogCache != null)
        {
            catalogCache.OnReady -= InitializeCache;
        }
    }

    #endregion
}
