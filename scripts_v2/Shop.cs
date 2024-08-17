using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ShopGenerics;

public partial class Shop : StaticBody2D, IInteractable
{
    [Export] AnimationPlayer animPlayer;
    [Export] Sprite2D interactionSprite;
	[Export] public bool canPlayerInteract;
	[Export] PackedScene shopFrontScene;
    ShopInfoPanel shopInfoPanel;
	JipSyncronyzer jipSyncronyzer;
	bool toggled;
	public string ShopName = "Empty Shop";
	public PlayerInfo ShopOwner;
	public ShopObject ShopFront;
	public int coins = 100;
	public Label CoinDisplay;
	public TradeManager TradeManager;
	public Dictionary<Item, ShopOrder> PurchaseOrders = new Dictionary<Item, ShopOrder>();
	public Dictionary<Item, ShopOrder> SellOrders = new Dictionary<Item, ShopOrder>();
	public Dictionary<Item, int> ShopInventory = new Dictionary<Item, int>();
	public event Action ShopObjectPlaced;
	public bool GetCanPlayerInteract { get { return canPlayerInteract; } }
	public int UpdateCoins { set {coins = value; if (ShopOwner == MultiplayerGameManager.LocalPlayer) CoinDisplay.Text = coins.ToString(); } }

    public override void _Ready()
    {
		jipSyncronyzer = GetTree().Root.GetNode("World").GetNode<JipSyncronyzer>("JipSyncHandler");
		shopInfoPanel = GetTree().Root.GetNode("World").GetNode<ShopInfoPanel>("ShopInfoPanel");
		CoinDisplay = GetTree().Root.GetNode("World").GetNode("ParallaxBackground").GetNode("CoinDisplay").GetNode<Label>("Money");
		TradeManager = GetParent().GetParent().GetNode("ParallaxBackground").GetNode<TradeManager>("TradeManager");

		MultiplayerGameManager.ShopItemAdded += RpcAnyCreateShopOrder;

		PlayerInfo self = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == Multiplayer.GetUniqueId());
        if (self.isJip)
            RpcId(1, "RpcServerRequestShopInfo", Multiplayer.GetUniqueId());

		if (Multiplayer.IsServer())
			TradeManager.ItemUpdated += RpcAnyCreateShopOrder;
    }

    public void InstantiateShopObjectPlacement(Entity.spawnType type)
	{
		ShopObject shopObject = null;
		if (type == Entity.spawnType.ShopFront)
		{
			shopObject = (ShopObject)shopFrontScene.Instantiate();
			shopObject.SetMeta("ID", MultiplayerGameManager.AllShopPlacables.First(x => x.Value.type == Entity.spawnType.ShopFront).Key);
		}
		shopObject.SetPlacableArea(GetNode<Area2D>("PlacableArea"));
		shopObject.Placed += HandleShopObjectPlacement;
		shopObject.PlacementCancelled += HandleShopObjectPlacementCancel;
		GetTree().Root.GetNode("World").AddChild(shopObject);
	}

	private void HandleShopObjectPlacementCancel(ShopObject shopObject)
	{
		shopObject.Placed -= HandleShopObjectPlacement;
		shopObject.PlacementCancelled -= HandleShopObjectPlacementCancel;
	}

	private void HandleShopObjectPlacement(ShopObject shopObject)
	{
		shopObject.Placed -= HandleShopObjectPlacement;
		shopObject.PlacementCancelled -= HandleShopObjectPlacementCancel;

		if (Multiplayer.IsServer())
			RpcServerCreateShopObject(shopObject.GlobalPosition, (int)shopObject.GetMeta("ID"), ShopOwner.id);
		else
			RpcId(1, "RpcServerCreateShopObject", shopObject.GlobalPosition, (int)shopObject.GetMeta("ID"), ShopOwner.id);

		shopObject.QueueFree();
	}

	private void HandleOrderEmpied(Item order)
	{
		if (PurchaseOrders.ContainsKey(order))
			PurchaseOrders.Remove(order);
		else if (SellOrders.ContainsKey(order))
			SellOrders.Remove(order);
	}

	public void ToggleInteractionAnim(bool toggle)
    {
		if (!canPlayerInteract)
			return;
		
		if (toggled != toggle) toggled = toggle;

        if (toggled)
        {
            interactionSprite.Visible = true;
            animPlayer.Play("Interaction");
        }
        else
        {
            interactionSprite.Visible = false;
            animPlayer.Stop();
        }
    }

    public void Interact()
    {
        shopInfoPanel.Open(this);
    }

	public void ChangeShopOwner(PlayerInfo playerInfo)
	{
		ShopOwner = playerInfo;
		if (ShopOwner == MultiplayerGameManager.LocalPlayer) CoinDisplay.Text = coins.ToString();
		Rpc("RpcClientChangeShopOwner", ShopOwner.id);
	}

	public void ClientJIPShopOrderSync(int playerID)
	{
		foreach (var item in PurchaseOrders)
			RpcId(playerID, "RpcAnyCreateShopOrder", item.Key.ID, item.Value.Volume, item.Value.Price, true, ShopOwner.id);
		foreach (var item in SellOrders)
			RpcId(playerID, "RpcAnyCreateShopOrder", item.Key.ID, item.Value.Volume, item.Value.Price, false, ShopOwner.id);
	}

	public void BuyItem(Node2D buyer, Item item, int volume)
	{
		if (!SellOrders.ContainsKey(item))
			return;

		int maxVolume = SellOrders[item].MaxVolume(volume, ((Brain)buyer).GetNode<Inventory>("Inventory").Gold);
		int totalGoldToPay = SellOrders[item].PayoutPrice(maxVolume);

		buyer.GetNode<Inventory>("Inventory").SubtractGold(totalGoldToPay);
		buyer.GetNode<Inventory>("Inventory").AddLoot(new KeyValuePair<Item, int>(item, maxVolume), this);

		//UpdateCoins = coins + totalGoldToPay;
		RpcAnyUpdateCoins(totalGoldToPay);

		ShopOrder order = SellOrders[item];
		int newVolume = SellOrders[item].Volume - maxVolume;
		order.UpdateVolume = newVolume;
		if (SellOrders.ContainsKey(item))
			SellOrders[item] = order;

		RpcAnyModifyInventory(item.ID, -maxVolume);
		Rpc("RpcClientUpdateOrderVolume", item.ID, newVolume, true);
	}

	public void SellItem(Node2D buyer, Item item, int volume)
	{
		int maxVolume = PurchaseOrders[item].MaxVolume(volume, coins);
		int totalGoldPaid = PurchaseOrders[item].PayoutPrice(maxVolume);

		buyer.GetNode<Inventory>("Inventory").AddGold(totalGoldPaid);
		buyer.GetNode<Inventory>("Inventory").RemoveLoot(item, maxVolume);

		//UpdateCoins = coins - totalGoldPaid;
		RpcAnyUpdateCoins(-totalGoldPaid);

		ShopOrder order = PurchaseOrders[item];
		int newVolume = PurchaseOrders[item].Volume - maxVolume;
		order.UpdateVolume = newVolume;
		if (PurchaseOrders.ContainsKey(item))
			PurchaseOrders[item] = order;

		RpcAnyModifyInventory(item.ID, maxVolume);
		Rpc("RpcClientUpdateOrderVolume", item.ID, newVolume, true);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcAnyUpdateCoins(int value)
	{
		UpdateCoins = coins + value;

		if (Multiplayer.IsServer())
			if (ShopOwner.id != 1) RpcId(ShopOwner.id, "RpcAnyUpdateCoins", value);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcClientUpdateOrderVolume(int itemID, int newVolume, bool buyOrder)
	{
		Item item = MultiplayerGameManager.AllItems[itemID];

		if (buyOrder && PurchaseOrders.ContainsKey(item))
		{
			ShopOrder order = PurchaseOrders[item];
			order.UpdateVolume = newVolume;
			PurchaseOrders[item] = order;
		}
		else if (!buyOrder && SellOrders.ContainsKey(item))
		{
			ShopOrder order = SellOrders[item];
			order.UpdateVolume = newVolume;
			SellOrders[item] = order;
		}

		//Every client has to update a shop's order. But only the shop's owner has to update
		//the shopfront UI
		if (ShopOwner == MultiplayerGameManager.LocalPlayer)
			MultiplayerGameManager.HandleShopItemModified(itemID, newVolume, buyOrder);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcAnyModifyInventory(int itemID, int volume)
	{
		Item item = MultiplayerGameManager.AllItems[itemID];
		if (!ShopInventory.ContainsKey(item))
			ShopInventory.Add(item, volume);
		else
			ShopInventory[item] += volume;

		if (Multiplayer.GetUniqueId() == ShopOwner.id)
			TradeManager.CreateInventoryItem(item, ShopInventory[item]);

		if (ShopInventory[item] < 0)
			GD.PrintErr("Error: '"+ Name +"' inventory item volume is less than 0!");

		if (ShopInventory[item] <= 0)
			ShopInventory.Remove(item);

		if (Multiplayer.IsServer())
			if (ShopOwner.id != 1)
				RpcId(ShopOwner.id, "RpcAnyModifyInventory", itemID, volume);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcClientChangeShopName(string newName, bool serverRequest = false)
	{
		if (Multiplayer.IsServer())
		{
			RpcServerChangeShopName(newName);
			return;
		}

		if (!serverRequest)
			RpcId(1, "RpcServerChangeShopName", newName);
		else
			ShopName = newName;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcClientChangeShopOwner(int id)
	{
		ShopOwner = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == id);
		if (ShopOwner == MultiplayerGameManager.LocalPlayer) CoinDisplay.Text = coins.ToString();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcServerChangeShopName(string newName)
	{
		ShopName = newName;
		Rpc("RpcClientChangeShopName", ShopName, true);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcServerCreateShopObject(Vector2 pos, int spawnableID, int playerID)
	{
		ShopObject _shopObject = (ShopObject)jipSyncronyzer.CreateSpawnable(spawnableID, pos, null, null, false);
		if (MultiplayerGameManager.AllShopPlacables[spawnableID].type == Entity.spawnType.ShopFront)
			ShopFront = _shopObject;
		_shopObject.EnableInputInfo(playerID);
		ShopObjectPlaced?.Invoke();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcServerRequestShopInfo(int id)
	{
		if (ShopOwner == null)
			return;

		RpcId(id, "RpcClientChangeShopName", ShopName, true);
		RpcId(id, "RpcClientChangeShopOwner", ShopOwner.id);
		ClientJIPShopOrderSync(id);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RpcAnyCreateShopOrder(int itemID, int volume, int price, bool buyOrder, int playerID)
	{
		PlayerInfo player = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == playerID);
		Item item = MultiplayerGameManager.AllItems[itemID];
		if (ShopOwner != player)
			return;
		
		Dictionary<Item, ShopOrder> ShopOrders = buyOrder ? PurchaseOrders : SellOrders;

		if (volume <= 0)
		{
			if (ShopOrders.ContainsKey(item))
				HandleOrderEmpied(item);
			
			return;
		}

		ShopOrder order = new ShopOrder(volume, price, item, false);
		order.Emptied += HandleOrderEmpied;

		if (ShopOrders.ContainsKey(item))
			ShopOrders[item] = order;
		else
			ShopOrders.Add(item, order);

		if (Multiplayer.IsServer())
			Rpc("RpcAnyCreateShopOrder", itemID, volume, price, buyOrder, playerID);
	}
}