using Godot;

public partial class Item : Node
{
    public enum ItemTier
    {
        Junk = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        VeryRare = 4,
        Legendary = 5
    }
    public enum EquippableType
    {
        None = 0,
        Armor = 1,
        Weapon = 2,
        Consumable = 3
    }
    [Export] public string ItemName { get; set; }
    [Export] public int ID { get; set; }
    [Export] public int MarketPurchasePrice { get; set; }
    [Export] public EquippableType EquipmentType { get; set; }
    [Export] public int EquipmentValue { get; set; }
    [Export] public int Durability { get; set; }
    [Export] public int DamageProtection { get; set; }
    [Export] public int BaseDamage { get; set; }
    [Export] public ItemTier Tier { get; set; }
    [Export] public Texture Sprite = new ImageTexture();
    [Export] public Texture EquippedSprite = new ImageTexture();
    [Export] public int[] craftingRecipie { get; set; }
    [Export] public int StartingVolume { get; set; }
    public string spriteName { get; set; }

    public void SetTexture()
    {
        Sprite = GD.Load<Texture>($"res://{spriteName}");
    }
}