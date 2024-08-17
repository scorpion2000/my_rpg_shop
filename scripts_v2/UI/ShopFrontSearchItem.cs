using Godot;

public partial class ShopFrontSearchItem : Control
{
    [Export] Button purchaseButton;
    [Export] Button sellButton;
    [Export] TextureRect sprite;
    [Export] RichTextLabel itemName;
    public int itemID;
    public ShopFront shopFront;

    public void Setup(ShopFront _shopFront, int _itemID, Item item)
    {
        this.shopFront = _shopFront;
        this.itemID = _itemID;

        sprite.Texture = (Texture2D)item.Sprite;
        itemName.Text = "[center]" + item.ItemName;

        sellButton.Pressed += delegate{shopFront.HandleItemAddition(itemID, false);};
        purchaseButton.Pressed += delegate{shopFront.HandleItemAddition(itemID, true);};
    }

    /*private void ShopItemAddition(bool isPurchase)
    {
        shopFront.HandleItemAddition(itemID, isPurchase);
    }*/
}