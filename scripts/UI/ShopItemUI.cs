using Godot;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Collections.Generic;
using ShopGenerics;

[Obsolete]
public partial class ShopItemUI : Node
{
    [Export] LineEdit newBuyVolume;
    [Export] LineEdit newBuyPrice;
    [Export] LineEdit newSellVolume;
    [Export] LineEdit newSellPrice;
    public int buyOrder;
    public int buyPrice;
    public int sellOrder;
    public int sellPrice;
    public int itemID;
    int pos;
    TradeManager parent;

    public override void _Ready()
    {
        parent = GetParent().GetParent().GetParent<TradeManager>();
        parent.TradeManagerClosed += ShopItemUpdate;
        parent.TradeManagerOpened += UpdateItemValues;
    }
    public void SimpleNumberInputFilter(string input, string nodePath)
	{
        if (input.Any(char.IsLetter))
            pos = -1;
        else
            return;

		string result = Regex.Replace(input, "[^0123456789]", "");
        pos += GetNode<LineEdit>(nodePath).CaretColumn;
        GetNode<LineEdit>(nodePath).Text = result;
        GetNode<LineEdit>(nodePath).CaretColumn = pos;
	}

    private void UpdateItemValues(Shop shop)
    {
        Item item = MultiplayerGameManager.AllItems[itemID];
        if (shop.PurchaseOrders.ContainsKey(item))
        {
            newBuyVolume.Text = shop.PurchaseOrders[item].Volume.ToString();
            newBuyPrice.Text = shop.PurchaseOrders[item].Price.ToString();
        }
        else
            newBuyVolume.Text = "0";    //If we don't find it, the volume went down to 0, and it got deleted

        if (shop.SellOrders.ContainsKey(item))
        {
            newSellVolume.Text = shop.SellOrders[item].Volume.ToString();
            newSellPrice.Text = shop.SellOrders[item].Price.ToString();
        }
        else
            newSellVolume.Text = "0";   //If we don't find it, the volume went down to 0, and it got deleted
    }

    public void SetupItem(Item item)
    {
        GetNode<TextureRect>("Item Texture").Texture = (Texture2D)item.Sprite;
        GetNode<Label>("Item Name").Text = item.ItemName;
        itemID = item.ID;
    }

    public void ShopItemUpdate()
    {
        if (newBuyVolume.Text == "") newBuyVolume.Text = "0";
        if (newBuyPrice.Text == "") newBuyPrice.Text = "0";
        if (newSellVolume.Text == "") newSellVolume.Text = "0";
        if (newSellPrice.Text == "") newSellPrice.Text = "0";

        if (Multiplayer.IsServer())
        {
            RpcServerUpdateItem(newBuyVolume.Text.ToInt(), newBuyPrice.Text.ToInt(), true, MultiplayerGameManager.LocalPlayer.id);
            RpcServerUpdateItem(newSellVolume.Text.ToInt(), newSellPrice.Text.ToInt(), false, MultiplayerGameManager.LocalPlayer.id);
        }
        else
        {
            RpcId(1, "RpcServerUpdateItem", newBuyVolume.Text.ToInt(), newBuyPrice.Text.ToInt(), true, MultiplayerGameManager.LocalPlayer.id);
            RpcId(1, "RpcServerUpdateItem", newSellVolume.Text.ToInt(), newSellPrice.Text.ToInt(), false, MultiplayerGameManager.LocalPlayer.id);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcServerUpdateItem(int volume, int value, bool buyOrder, int playerID)
    {
        //This is where we shouold create validation, if the need arises
        parent.CreateShopItem(itemID, volume, value, buyOrder, playerID);
    }
}
