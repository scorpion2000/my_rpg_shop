using Godot;
using System;
using System.Linq;

[Obsolete]
public partial class CraftingUiController : Control, IInteractable
{
	[Export] BoxContainer[] craftReqBoxes;
	[Export] GridContainer craftableContainer;
	[Export] PackedScene craftableItem;
	[Export] Label craftableSelectionLabel;
	[Export] TextureRect craftableSelectionTexture;
	[Export] SpinBox volumeInput;
	[Export] Button simpleCraftButton;
	[Export] int[] craftableItems = new int[]{};
	bool canPlayerInteract = true;
	Shop shop;
	Item craftableSelection;
	public void ExitButton() { InteractUI(MultiplayerGameManager.LocalPlayer, null); }

    public bool GetCanPlayerInteract { get { return canPlayerInteract; } }

    public override void _Ready()
    {
		simpleCraftButton.Pressed += HandleCrafting;
		/*if (MultiplayerGameManager.AllItems.Count == 0)
        	MultiplayerGameManager.ItemsReady += CreateCraftableItems;
		else
			CreateCraftableItems();*/
    }

	private void CreateCraftableItems()
	{
		foreach (int itemID in craftableItems)
		{
			CraftableItem itemUI = craftableItem.Instantiate<CraftableItem>();
			itemUI.ButtonSetup(MultiplayerGameManager.AllItems[itemID]);
			itemUI.CraftableItemSelection += HandleItemSelection;
			craftableContainer.AddChild(itemUI);
		}
	}

	private void HandleItemSelection(int itemID)
	{
		craftableSelection = MultiplayerGameManager.AllItems[itemID];
		craftableSelectionLabel.Text = craftableSelection.ItemName;
		craftableSelectionTexture.Texture = (Texture2D)craftableSelection.Sprite;

		for (int i = 0; i < craftableSelection.craftingRecipie.Length; i++)
		{
			craftReqBoxes[i].GetChild<Label>(0).Text = MultiplayerGameManager.AllItems[craftableSelection.craftingRecipie[i]].ItemName;
			craftReqBoxes[i].GetChild<TextureRect>(1).Texture = (Texture2D)MultiplayerGameManager.AllItems[craftableSelection.craftingRecipie[i]].Sprite;
		}

		if (craftableSelection.craftingRecipie.Length < 3)
		{
			for (int i = craftableSelection.craftingRecipie.Length; i < 3; i++)
			{
				craftReqBoxes[i].GetChild<Label>(0).Text = "";
				craftReqBoxes[i].GetChild<TextureRect>(1).Texture = null;
			}
		}
	}

	private void HandleCrafting()
	{
		if (craftableSelection == null || (int)volumeInput.Value <= 0)
			return;

		if (Multiplayer.IsServer())
			RpcServerHandleCrafting(MultiplayerGameManager.LocalPlayer.id, craftableSelection.ID, (int)volumeInput.Value);
		else
			RpcId(1, "RpcServerHandleCrafting", MultiplayerGameManager.LocalPlayer.id, craftableSelection.ID, (int)volumeInput.Value);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcServerHandleCrafting(int playerID, int itemID, int volume)
	{
		Shop shop = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == playerID).Shop;
		Item item = MultiplayerGameManager.AllItems[itemID];
		int craftingCheck = 0;

		foreach (int craftingItemID in item.craftingRecipie)
		{
			if (shop.ShopInventory.ContainsKey(MultiplayerGameManager.AllItems[craftingItemID]))
				craftingCheck++;
		}

		if (craftingCheck == item.craftingRecipie.Length)
			CraftItem(shop, item, volume);
	}

	private void CraftItem(Shop shop, Item item, int volume)
	{
		if (!Multiplayer.IsServer())
			return;
		
		shop.RpcAnyModifyInventory(item.ID, volume);
		foreach (var itemID in item.craftingRecipie)
		{
			shop.RpcAnyModifyInventory(itemID, -volume);
		}
	}

    public void InteractUI(PlayerInfo playerInfo, Node2D interactionNode = null)
    {
		if (Visible) Visible = false; else Visible = true;
        MultiplayerGameManager.ToggleUiOpen();
		shop = (Shop)interactionNode;
    }
}
