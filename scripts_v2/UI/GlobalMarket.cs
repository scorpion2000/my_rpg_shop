using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ShopGenerics;

public partial class GlobalMarket : CanvasLayer
{
    [Export] PackedScene itemScene;
    [Export] VBoxContainer commonContainer;
    [Export] VBoxContainer weaponContainer;
    [Export] VBoxContainer armorContainer;
    [Export] VBoxContainer consumableContainer;
    [Export] Button searchButton;
    [Export] LineEdit searchInput;
    [Export] CheckBox autoSearch;
    [Export] Button toggleButton;
    [Export] Timer updateTimer;
    [Export] RichTextLabel timerText;

    Dictionary<string, MarketItem> marketItems = new Dictionary<string, MarketItem>();
	public Dictionary<Item, ShopOrder> PublicShoppingList = new Dictionary<Item, ShopOrder>();
	public Dictionary<Item, ShopOrder> ShoppingListOld = new Dictionary<Item, ShopOrder>();

    public override void _Ready()
    {
        SetupMarketItems();
        searchButton.Pressed += Search;
        toggleButton.Pressed += ToggleUI;
        updateTimer.Timeout += UpdateGlobalMarket;
    }

    public override void _Process(double delta)
    {
        int minuteLeft = (int)MathF.Floor((float)updateTimer.TimeLeft / 60f);
        timerText.Text = "[center]" + minuteLeft.ToString() + ":" + MathF.Floor((float)updateTimer.TimeLeft - (minuteLeft*60)).ToString();
    }

    private void SetupMarketItems()
    {
        foreach (var item in MultiplayerGameManager.AllItems)
        {
            VBoxContainer container = null;
            switch (item.Value.EquipmentType)
            {
                case Item.EquippableType.None : container = commonContainer; break;
                case Item.EquippableType.Weapon : container = weaponContainer; break;
                case Item.EquippableType.Armor : container = armorContainer; break;
                case Item.EquippableType.Consumable : container = consumableContainer; break;
                default: container = commonContainer; break;
            }

            MarketItem newItem = (MarketItem)itemScene.Instantiate();
            container.AddChild(newItem);
            newItem.SetItemInfo(item.Value);
            marketItems.Add(item.Value.ItemName, newItem);

            ShopOrder order = new ShopOrder(0, item.Value.MarketPurchasePrice, item.Value, true);
            order.UpdateVolume = item.Value.StartingVolume;
			PublicShoppingList.Add(item.Value, order);
        }
        ShoppingListOld = PublicShoppingList.ToDictionary(e => e.Key, e => e.Value);
    }

    private void ToggleUI()
    {
        Visible = !Visible;
        MultiplayerGameManager.UiOpen = Visible;
        SetProcess(Visible);
    }

    private void UpdateGlobalMarket()
    {
        foreach (var item in MultiplayerGameManager.AllItems)
        {
            ShopOrder order = PublicShoppingList[item.Value];
            order.UpdatePrice = UpdateOrderPrice(
                PublicShoppingList[item.Value],
                ShoppingListOld[item.Value].Volume,
                PublicShoppingList[item.Value].Volume
            );
            PublicShoppingList[item.Value] = order;
            ShoppingListOld[item.Value] = order;
            RpcUpdateItem(item.Value.ID, order.Price, order.Volume);
            Rpc("RpcUpdateItem", item.Value.ID, order.Price, order.Volume);
        }
        updateTimer.Start();
    }

    private int UpdateOrderPrice(ShopOrder order, int oldVolume, int newVolume)
    {
        float changePercent = 100 * ((newVolume - oldVolume) / MathF.Abs(oldVolume));
        float multiplier = 1f;
        if (changePercent <= -100)
            multiplier = 2f;
        else if (changePercent <= -40)
            multiplier = 1.45f;
        else if (changePercent < -10)
            multiplier = 1.2f;
        else if (changePercent < 0)
            multiplier = 1.1f;
        else if (changePercent == 0)
            multiplier = 1f;
        else if (changePercent >= 25)
            multiplier = 0.15f;
        else if (changePercent >= 50)
            multiplier = 0.3f;
        else if (changePercent >= 100)
            multiplier = 0.5f;

        
        if (oldVolume == 0)
            multiplier = 0.2f;

        int newPrice = (int)MathF.Ceiling(order.Price * multiplier);
        if (newPrice <= 0) newPrice = 1;

        return newPrice;
    }

    private void Search()
    {
        foreach (var item in marketItems)
        {
            item.Value.Visible = true;
            if (!item.Key.ToLower().Contains(searchInput.Text))
            {
                item.Value.Visible = false;
            }
        }
    }

    public void Search(string text)
    {
        Search();
    }

    public void AutoSearch(string text)
    {
        if (autoSearch.ButtonPressed)
            Search();
    }

	public void SellItem(Node2D buyer, Item item, int count)
	{
		int totalGoldPaid = PublicShoppingList[item].PayoutPrice(count);

		ShopOrder order = PublicShoppingList[item];
		//order.UpdatePrice = (int)MathF.Max(MathF.Floor(order.Price - item.MarketPurchasePrice / 5f), MathF.Floor(item.MarketPurchasePrice / 2));
		order.UpdateVolume = order.Volume + count;
		PublicShoppingList[item] = order;

		buyer.GetNode<Inventory>("Inventory").AddGold(totalGoldPaid);
		buyer.GetNode<Inventory>("Inventory").RemoveLoot(item, count);

        RpcUpdateItem(item.ID, item.MarketPurchasePrice, order.Volume);
        Rpc("RpcUpdateItem", item.ID, item.MarketPurchasePrice, order.Volume);
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RpcUpdateItem(int itemID, int purchasePrice, int newCount)
    {
        marketItems[MultiplayerGameManager.AllItems[itemID].ItemName].UpdateItemInfo(purchasePrice, newCount);
    }
}