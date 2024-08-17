using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TradeManager : Control, IInteractable
{
    [Export] GridContainer grid;
    [Export] PackedScene tradeItem;
	[Export] PackedScene shopInventoryItem;
	[Export] GridContainer inventoryContainer;
    private bool canPlayerInteract = true;
    public bool GetCanPlayerInteract { get { return canPlayerInteract; } }
    public event Action TradeManagerClosed;
    public event Action<Shop> TradeManagerOpened;
    public event Action<int, int, int, bool, int> ItemUpdated;
	public void ExitButton() { InteractUI(MultiplayerGameManager.LocalPlayer, null); }
    Dictionary<Item, Control> shopInventoryItems = new Dictionary<Item, Control>();

    public override void _Ready()
    {
        if (MultiplayerGameManager.AllItems.Count == 0)
        	MultiplayerGameManager.ItemsReady += Setup;
		else
			Setup();
    }

    private void Setup()
    {
        foreach (var item in MultiplayerGameManager.AllItems)
        {
            ShopItemUI newItemUI = tradeItem.Instantiate<ShopItemUI>();
            grid.AddChild(newItemUI);
            newItemUI.SetupItem(item.Value);
            //newItemUI.ItemUpdated += HandleUpdatedItem;
        }
    }

    public void CreateShopItem(int itemID, int volume, int value, bool buyOrder, int playerID)
    {
        ItemUpdated?.Invoke(itemID, volume, value, buyOrder, playerID);
    }

    public void InteractUI(PlayerInfo playerInfo, Node2D interactionNode = null)
    {
		if (Visible) Visible = false; else Visible = true;
        MultiplayerGameManager.ToggleUiOpen();
        if (!Visible) TradeManagerClosed?.Invoke();
        if (Visible) TradeManagerOpened?.Invoke((Shop)interactionNode);
    }

    public void CreateInventoryItem(Item item, int count)
    {
        if (shopInventoryItems.ContainsKey(item))
        {
            if (count <= 0)
            {
                shopInventoryItems[item].QueueFree();
                shopInventoryItems.Remove(item);
            }
            else
            {
                shopInventoryItems[item].GetChild<Label>(2).Text = count.ToString();
            }
        }
        else
        {
            Control inventoryItem = shopInventoryItem.Instantiate<Control>();
            inventoryItem.GetChild<TextureRect>(0).Texture = (Texture2D)item.Sprite;
            inventoryItem.GetChild<Label>(1).Text = item.ItemName;
            inventoryItem.GetChild<Label>(2).Text = count.ToString();
            inventoryContainer.AddChild(inventoryItem);
            shopInventoryItems.Add(item, inventoryItem);
        }
    }
}