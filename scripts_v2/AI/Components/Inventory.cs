using Godot;
using System;
using System.Collections.Generic;
using AiComponents.Inventory;

public partial class Inventory : Node
{
    [Export] Godot.Collections.Dictionary DropTable = new Godot.Collections.Dictionary();
    public Dictionary<Item, int> ConvertedDropTable = new Dictionary<Item, int>();
    public Dictionary<Item, int> MobInventory = new Dictionary<Item, int>();
    public int Gold = 0;
    public EquippedArmor EquippedArmor;
    public EquippedWeapon EquippedWeapon;
    public EquippedPotions EquippedPotions;
    public event Action<Dictionary<Item, int>, int> InventoryUpdated;

    public override void _Ready()
    {
        ConvertDropTableToItems();
    }

    private void ConvertDropTableToItems()
    {
        foreach (var item in DropTable)
        {
            ConvertedDropTable.Add(MultiplayerGameManager.AllItems[(int)item.Key], (int)item.Value);
        }

        DropTable = null;
    }

    public void AddLoot(KeyValuePair<Item, int> item, Shop shop = null)
    {
        for (int i = 0; i < item.Value; i++)
        {
            if (!TryEquipItem(item.Key))
            {
                if (MobInventory.ContainsKey(item.Key))
                    MobInventory[item.Key] = MobInventory[item.Key] + item.Value;
                else
                    MobInventory.Add(item.Key, item.Value);
            }
        }

        InventoryUpdated?.Invoke(MobInventory, Gold);
    }

    public void RemoveLoot(Item item, int count)
    {
        MobInventory[item] = (int)MathF.Max(MobInventory[item] - count, 0);
        if (MobInventory[item] == 0)
            MobInventory.Remove(item);
    }

    public bool TryEquipItem(Item item)
    {
        if (EquippedArmor.TryEquipItem(item, null)) return true;
        if (EquippedWeapon.TryEquipItem(item, null)) return true;
        if (EquippedPotions.TryAddNewPotion(item)) return true;

        return false;
    }

    public void AddGold(int count) { Gold += count; }
    public void SubtractGold(int count) { Gold = (int)MathF.Max(Gold - count, 0); }
}