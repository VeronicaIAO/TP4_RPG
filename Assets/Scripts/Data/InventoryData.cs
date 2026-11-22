using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemData item;
        [Min(0)] public int amount = 1;
    }

    [CreateAssetMenu(menuName = "TP RPG/Inventory", fileName = "Inventory")]
    public class InventoryData : ScriptableObject
    {
        public List<ItemStack> items = new List<ItemStack>();
    }
}
