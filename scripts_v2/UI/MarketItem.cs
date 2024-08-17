using Godot;

public partial class MarketItem : Control
{
    [Export] TextureRect texture;
    [Export] RichTextLabel itemName;
    [Export] RichTextLabel purchasePrice;
    [Export] RichTextLabel sellPrice;
    [Export] RichTextLabel itemVolume;

    int oldPrice;

    public void SetItemInfo(Item item)
    {
        texture.Texture = (Texture2D)item.Sprite;
        itemName.Text = "[center]" + item.ItemName;

        itemVolume.Text = "[center][" + item.StartingVolume + "]";
        purchasePrice.Text = "[center]Buy: " + item.MarketPurchasePrice.ToString();
        sellPrice.Text = "[center]Sell: " + (item.MarketPurchasePrice * 2).ToString();
        oldPrice = item.MarketPurchasePrice;
    }

    public void UpdateItemInfo(int _purchasePrice, int _itemVolume)
    {
        if (_purchasePrice != oldPrice)
            GD.Print(itemName.Text + " price changed from " + oldPrice.ToString() + " to " + _purchasePrice.ToString());
        purchasePrice.Text = "[center]Buy: " + _purchasePrice.ToString();
        sellPrice.Text = "[center]Sell: " + (_purchasePrice * 2).ToString();
        itemVolume.Text = "[center][" + _itemVolume.ToString() + "]";
        oldPrice = _purchasePrice;
    }
}