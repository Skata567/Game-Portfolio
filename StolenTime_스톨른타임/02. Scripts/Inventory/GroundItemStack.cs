using UnityEngine;

namespace PrototypeRT
{
    /// <summary>
    /// 기존 ItemPickup을 수정하지 않고 바닥 스택 수량만 덧붙이기 위한 보조 컴포넌트입니다.
    /// YJW ItemPickup은 아이템 1개 기준이라, 인벤토리 스택 전체를 버릴 때 수량을 여기 보관합니다.
    /// </summary>
    public class GroundItemStack : MonoBehaviour
    {
        [Tooltip("이 바닥 아이템을 주웠을 때 인벤토리에 들어갈 수량입니다.")]
        [SerializeField, Min(1)] private int amount = 1;

        public int Amount => Mathf.Max(1, amount);

        /// <summary>
        /// 드랍 시점의 InventoryItem.Count를 저장합니다.
        /// 잘못된 값이 들어와도 바닥 아이템은 최소 1개로 취급합니다.
        /// </summary>
        public void SetAmount(int value)
        {
            amount = Mathf.Max(1, value);
        }
    }
}
