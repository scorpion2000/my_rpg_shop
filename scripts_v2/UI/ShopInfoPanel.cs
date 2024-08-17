using System.Collections.Generic;
using Godot;
using ShopGenerics;

public partial class ShopInfoPanel : CanvasLayer
{
    [Export] PackedScene shopItemScene;
    [Export] VBoxContainer purchasesBox;
    [Export] VBoxContainer salesBox;
    [Export] Control shopNameEdit;
    [Export] Label shopNameLabel;
    [Export] LineEdit shopNameLineEdit;
    [Export] Button shopNameSetButton;
    [Export] Button closeButton;
    Shop activeShop = null;
    Dictionary<Item, ShopInfoItem> purchaseItems = new Dictionary<Item, ShopInfoItem>();
    Dictionary<Item, ShopInfoItem> saleItems = new Dictionary<Item, ShopInfoItem>();

    public override void _Ready()
    {
        shopNameSetButton.Pressed += ChangeShopName;
        closeButton.Pressed += Close;
    }

    private void NameEditSetup()
    {
        bool isOwner = activeShop.ShopOwner == MultiplayerGameManager.LocalPlayer;
        shopNameLabel.Visible = !isOwner;
        shopNameEdit.Visible = isOwner;

        shopNameLineEdit.Text = activeShop.ShopName;
        shopNameLabel.Text = activeShop.ShopName;
    }

    private void ChangeShopName()
    {
        if (activeShop.ShopOwner != MultiplayerGameManager.LocalPlayer)
        {
            GD.PrintErr("Warning: " + MultiplayerGameManager.LocalPlayer.id + " tried to edit " + activeShop.ShopOwner.id + "'s shop!");
            return;
        }

        activeShop.RpcClientChangeShopName(shopNameLineEdit.Text);
    }

    private void ShopItemsSetup()
    {
        ItemsAgainstActiveShop(purchaseItems, true);
        ItemsAgainstActiveShop(saleItems, false);

        CreateNewItems(purchaseItems, true);
        CreateNewItems(saleItems, false);
    }

    private void ItemsAgainstActiveShop(Dictionary<Item, ShopInfoItem> dictionary, bool purchaseOrder)
    {
        foreach (var item in dictionary)
        {
            if ((purchaseOrder && activeShop.PurchaseOrders.ContainsKey(item.Key)) || (!purchaseOrder && activeShop.SellOrders.ContainsKey(item.Key)))
                item.Value.UpdatePrice(purchaseOrder ? activeShop.PurchaseOrders[item.Key].Price : activeShop.SellOrders[item.Key].Price);
            else
            {
                dictionary.Remove(item.Key);
                item.Value.QueueFree();
            }
        }
    }

    private void CreateNewItems(Dictionary<Item, ShopInfoItem> dictionary, bool purchaseOrder)
    {
        GD.Print(activeShop.PurchaseOrders.Count);
        foreach (KeyValuePair<Item, ShopOrder> item in purchaseOrder ? activeShop.PurchaseOrders : activeShop.SellOrders)
        {
            if (dictionary.ContainsKey(item.Key))
                continue;
            
            ShopInfoItem shopItem = (ShopInfoItem)shopItemScene.Instantiate();
            shopItem.Setup(item.Key, item.Value.Price);
            if (purchaseOrder)
                purchasesBox.AddChild(shopItem);
            else
                salesBox.AddChild(shopItem);

            dictionary.Add(item.Key, shopItem);
        }
    }

    public void Open(Shop shop)
    {
        Visible = !Visible;
        MultiplayerGameManager.UiOpen = Visible;
        activeShop = shop;
        NameEditSetup();
        ShopItemsSetup();
    }

    public void Close()
    {
        Visible = !Visible;
        MultiplayerGameManager.UiOpen = Visible;
    }
}