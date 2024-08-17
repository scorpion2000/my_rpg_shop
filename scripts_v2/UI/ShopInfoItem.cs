using Godot;
using System;

public partial class ShopInfoItem : Control
{
	[Export] TextureRect sprite;
	[Export] RichTextLabel itemName;
	[Export] Label itemPriceText;

	public void Setup(Item item, int price)
	{
        itemName.Text = item.ItemName;
        sprite.Texture = (Texture2D)item.Sprite;
		itemPriceText.Text = price.ToString();
	}

	public void UpdatePrice(int newPrice)
	{
		itemPriceText.Text = newPrice.ToString();
	}
}
