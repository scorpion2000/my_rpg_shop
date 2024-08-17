using Godot;
using System;
using System.Collections.Generic;

public partial class ShopFront : CanvasLayer
{
	[Export] PackedScene ShopFrontSearchItem;
	[Export] PackedScene ShopFrontItem;
	[Export] Control SearchScrollContainer;
	[Export] Control PurchasesScrollContainer;
	[Export] Control SalesScrollContainer;
	[Export] LineEdit searchSearchInput;
	[Export] CheckBox searchAutoSearch;
	[Export] Button searchSearchButton;
	[Export] LineEdit frontSearchInput;
	[Export] CheckBox frontAutoSearch;
	[Export] Button frontSearchButton;
	[Export] Button exitButton;

	private Dictionary<string, Control> searchItems = new Dictionary<string, Control>();
	private Dictionary<string, Control> purchaseItems = new Dictionary<string, Control>();
	private Dictionary<string, Control> saleItems = new Dictionary<string, Control>();

    public override void _Ready()
    {
		CreateSearchItems();
		searchSearchButton.Pressed += delegate{Search(searchSearchInput, searchItems);};
		searchSearchInput.TextChanged += s => {AutoSearch(s, searchAutoSearch, searchSearchInput, searchItems);};
		searchSearchInput.TextSubmitted += s => {Search(s, searchSearchInput, searchItems);};
		
		frontSearchButton.Pressed += delegate{Search(frontSearchInput, purchaseItems, saleItems);};
		frontSearchInput.TextChanged += s => {AutoSearch(s, frontAutoSearch, frontSearchInput, purchaseItems, saleItems);};
		frontSearchInput.TextSubmitted += s => {Search(s, frontSearchInput, purchaseItems, saleItems);};

		exitButton.Pressed += Toggle;
    }

	public void HandleItemAddition(int itemID, bool isPurchase)
	{
		if (
			(isPurchase && purchaseItems.ContainsKey(MultiplayerGameManager.AllItems[itemID].ItemName)) ||
			(!isPurchase && saleItems.ContainsKey(MultiplayerGameManager.AllItems[itemID].ItemName))
		) {
			GD.PrintS("Player attemped to add existing item to shopfront",MultiplayerGameManager.LocalPlayer,itemID);
			return;
		}
		ShopFrontItem frontItem = (ShopFrontItem)ShopFrontItem.Instantiate();
		frontItem.Setup(MultiplayerGameManager.AllItems[itemID], this, isPurchase);
		if (isPurchase)
		{
			purchaseItems.Add(MultiplayerGameManager.AllItems[itemID].ItemName, frontItem);
			PurchasesScrollContainer.AddChild(frontItem);
		}
		else
		{
			saleItems.Add(MultiplayerGameManager.AllItems[itemID].ItemName, frontItem);
			SalesScrollContainer.AddChild(frontItem);
		}
		MultiplayerGameManager.CreateShopItem(itemID, 0, 0, isPurchase, MultiplayerGameManager.LocalPlayer.id);
	}

	public void HandleItemDeletion(ShopFrontItem item, bool isPurchase, int itemID)
	{
		if (isPurchase)
			purchaseItems.Remove(item.ItemName);
		else
			saleItems.Remove(item.ItemName);

		MultiplayerGameManager.CreateShopItem(itemID, 0, 0, isPurchase, MultiplayerGameManager.LocalPlayer.id);
	}

	public void HandleItemEdit(int itemID, int volume, int price, bool buyOrder)
	{
		MultiplayerGameManager.CreateShopItem(itemID, volume, price, buyOrder, MultiplayerGameManager.LocalPlayer.id);
	}

	public void Toggle()
	{
		Visible = !Visible;
		MultiplayerGameManager.UiOpen = Visible;
	}

	private void CreateSearchItems()
	{
		foreach (var item in MultiplayerGameManager.AllItems)
		{
			ShopFrontSearchItem searchItem = (ShopFrontSearchItem)ShopFrontSearchItem.Instantiate();
			searchItem.Setup(this, item.Key, item.Value);
			SearchScrollContainer.AddChild(searchItem);
			searchItems.Add(item.Value.ItemName, searchItem);
		}
	}

    private void Search(LineEdit lineEdit, Dictionary<string, Control> dictionary, Dictionary<string, Control> secondDictionary = null)
    {
        foreach (var item in dictionary)
        {
            item.Value.Visible = true;
            if (!item.Key.ToLower().Contains(lineEdit.Text))
            {
                item.Value.Visible = false;
            }
        }

		if (secondDictionary == null)
			return;

		foreach (var item in secondDictionary)
        {
            item.Value.Visible = true;
            if (!item.Key.ToLower().Contains(lineEdit.Text))
            {
                item.Value.Visible = false;
            }
        }
    }

    public void Search(string text, LineEdit lineEdit, Dictionary<string, Control> dictionary, Dictionary<string, Control> secondDictionary = null)
    {
        Search(lineEdit, dictionary, secondDictionary);
    }

    public void AutoSearch(string text, CheckBox checkBox, LineEdit lineEdit, Dictionary<string, Control> dictionary, Dictionary<string, Control> secondDictionary = null)
    {
        if (checkBox.ButtonPressed)
            Search(lineEdit, dictionary, secondDictionary);
    }
}
