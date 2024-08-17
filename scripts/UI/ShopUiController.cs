using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Obsolete]
public partial class ShopUiController : Control, IInteractable
{
	[Export] PackedScene ShopItem;
	[Export] GridContainer container;
	Panel shopInfo;
	Panel shopSettings;
	LineEdit shopNameInput;
	Shop lastInteractShop;
	bool canPlayerInteract = true;
	Dictionary<Item, int[]> shopItemDisplay = new Dictionary<Item, int[]>();
	List<Control> shopItems = new List<Control>();
    public bool GetCanPlayerInteract { get { return canPlayerInteract; } }
	public void ExitButton() { InteractUI(MultiplayerGameManager.Players.Where(x => x.id == Multiplayer.GetUniqueId()).ToList()[0], lastInteractShop); }
	public void InteractUI(PlayerInfo playerInfo, Node2D interactionNode = null)
	{
		lastInteractShop = (Shop)interactionNode;

		if (Visible) Visible = false; else Visible = true;
		ToggleSetting(lastInteractShop.ShopOwner == playerInfo);
        MultiplayerGameManager.ToggleUiOpen();

		if (Visible) ProcessShopOrders(lastInteractShop);
	}

    public override void _Ready()
    {
		shopInfo = GetNode<Panel>("ShopInfo");
		shopSettings = GetNode<Panel>("ShopSettings");
		shopNameInput = shopSettings.GetNode("ShopNameEdit").GetNode<LineEdit>("LineEdit");
    }

	private void ToggleSetting(bool toggle)
	{
		shopSettings.GetNode<Control>("ShopNameEdit").Visible = toggle;
		shopSettings.GetNode<Label>("ShopName").Visible = !toggle;

		if (toggle)
			shopNameInput.Text = lastInteractShop.ShopName;
		else
			shopSettings.GetNode<Label>("ShopName").Text = lastInteractShop.ShopName;
	}

	private void ProcessShopOrders(Shop shop)
	{
		GD.Print(shop.PurchaseOrders.Count);
		shopItemDisplay.Clear();

		foreach (var item in shop.PurchaseOrders)
		{
			if (!shopItemDisplay.ContainsKey(item.Key))
				shopItemDisplay.Add(item.Key, new int [] {item.Value.Volume, item.Value.Price, 0, 0});
			else
			{
				int[] volumePrice = shopItemDisplay[item.Key];
				volumePrice[0] = item.Value.Volume; volumePrice[1] = item.Value.Price;
				shopItemDisplay[item.Key] = volumePrice;
			}
		}

		foreach (var item in shop.SellOrders)
		{
			if (!shopItemDisplay.ContainsKey(item.Key))
				shopItemDisplay.Add(item.Key, new int [] {0, 0, item.Value.Volume, item.Value.Price});
			else
			{
				int[] volumePrice = shopItemDisplay[item.Key];
				volumePrice[2] = item.Value.Volume; volumePrice[3] = item.Value.Price;
				shopItemDisplay[item.Key] = volumePrice;
			}
		}

		CreateItemDisplays();
	}

	private void CreateItemDisplays()
	{
		foreach (Control item in shopItems)
			item.QueueFree();
		
		shopItems.Clear();

		foreach (var item in shopItemDisplay)
		{
			Control shopItem = ShopItem.Instantiate<Control>();
			shopItem.GetChild<Label>(0).Text = item.Key.ItemName;
			for (int i = 0; i < 4; i++)
			{
				shopItem.GetChild<Label>(i + 1).Text = item.Value[i].ToString();
			}
			container.AddChild(shopItem);
			shopItems.Add(shopItem);
		}
	}
}
