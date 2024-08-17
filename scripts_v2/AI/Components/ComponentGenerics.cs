namespace AiComponents
{
    namespace Inventory
    {
        using System.Collections.Generic;
        using System.Linq;

        public struct EquippedArmor
        {
            Item item;
            int durability;
            Shop creator;

            public Item GetItem { get { return item; } }
            public Shop GetCreator { get { return creator; } }
            public int GetItemTier { get { if (item != null) return (int)item.Tier; else return -1; } }
            public int GetDurability { get { if (item != null) return durability; else return 0; } }
            public int GetDamageProtection { get { if (item != null) return item.DamageProtection; else return 0; } }

            public bool TryEquipItem(Item _item, Shop _creator)
            {
                if (_item.EquipmentType != Item.EquippableType.Armor) return false;
                if (item == null) { UpdateItem(_item, _creator); return true; }
                if (_item.Tier > item.Tier) { UpdateItem(_item, _creator); return true; }
                else return false;
            }

            public void UpdateItem(Item _item, Shop _creator)
            {
                item = _item;
                durability = _item.Durability;
                creator = _creator;
            }

            public void DegradeDurability(float value, float multiplier)
            {
                durability -= (int)System.MathF.Ceiling(value * multiplier);
                if (GetDurability <= 0)
                    RemoveItem();
            }

            public void RemoveItem()
            {
                item = null;
                durability = 0;
                creator = null;
            }
        }

        public struct EquippedWeapon
        {
            Item item;
            int durability;
            Shop creator;

            public Item GetItem { get { return item; } }
            public int GetItemTier { get { if (item != null) return (int)item.Tier; else return -1; } }
            public Shop GetCreator { get { return creator; } }
            public int GetDurability { get { if (item != null) return durability; else return 0; } }
            public int GetBaseDamage { get { if (item != null) return item.BaseDamage; else return 1; } }

            public bool TryEquipItem(Item _item, Shop _creator)
            {
                if (_item.EquipmentType != Item.EquippableType.Weapon) return false;
                if (item == null) { UpdateItem(_item, _creator); return true; }
                if (_item.Tier > item.Tier) { UpdateItem(_item, _creator); return true; }
                else return false;
            }

            public void UpdateItem(Item _item, Shop _creator)
            {
                item = _item;
                durability = _item.Durability;
                creator = _creator;
            }

            public void DegradeDurability(float value, float multiplier)
            {
                durability -= (int)System.MathF.Ceiling(value * multiplier);
                if (GetDurability <= 0)
                    RemoveItem();
            }

            public void RemoveItem()
            {
                item = null;
                durability = 0;
                creator = null;
            }
        }

        public struct EquippedPotions
        {
            Dictionary<Item, int> potions;

            public EquippedPotions()
            {
                potions = new Dictionary<Item, int>();
            }

            public int GetPotionCount { get { return potions.Sum(x => x.Value); } }
            
            public bool TryAddNewPotion(Item item)
            {
                if (item.EquipmentType != Item.EquippableType.Consumable) return false;

                EquipPotion(item);
                return true;
            }

            public void EquipPotion(Item item)
            {
                if (potions.ContainsKey(item))
                    potions[item] += 1;
                else
                    potions.Add(item, 1);
            }
        }
    }
}