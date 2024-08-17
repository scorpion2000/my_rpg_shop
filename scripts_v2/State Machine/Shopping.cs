using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ShopGenerics;

public partial class Shopping : BaseState
{
    Brain brainSM;
    Timer shoppingTimer;
    GlobalMarket globalMarket;
    Vector2 globalMarketPos;
    Inventory brainInventory;
    Shop selectedShop;
    Item itemToBuy;
    bool globalShopping;
    List<KeyValuePair<Shop, int>> InterestedShopsToSell = new List<KeyValuePair<Shop, int>>();
    Dictionary<ShopOrder, Shop> InterestedShopsToBuy = new Dictionary<ShopOrder, Shop>();
    Dictionary<Shop, KeyValuePair<Item, int>> ShoppingList = new Dictionary<Shop, KeyValuePair<Item, int>>();
    public event Action StopShopping;

    public Shopping(Brain stateMachine, Timer _shoppingTimer) : base("Shopping", stateMachine)
    {
        brainSM = stateMachine;
        shoppingTimer = _shoppingTimer;
        globalMarket = brainSM.GetTree().Root.GetNode("World").GetNode<GlobalMarket>("GlobalMarket");
        globalMarketPos = brainSM.GetTree().Root.GetNode("World").GetNode<Node2D>("GlobalMarketPos").GlobalPosition;
        brainInventory = brainSM.GetNode<Inventory>("Inventory");
    }

    public override void Enter()
    {
        base.Enter();

        brainSM.movement.PathFinished += FindWanderPos;
        shoppingTimer.Timeout += HandleShoppingTimeout;
        shoppingTimer.Start();
        InterestedShopsToSell.Clear();

        FindWanderPos();
    }

    public override void UpdateLogic()
    {
        base.UpdateLogic();

        if (brainSM.target != null)
            brainSM.ChangeState(brainSM.combatState);
    }

    public override void Exit()
    {
        base.Exit();
        ShoppingList.Clear();
        shoppingTimer.Timeout -= HandleShoppingTimeout;
        shoppingTimer.Stop();
        StopShopping?.Invoke();
    }

    private void HandleShoppingTimeout()
    {
        brainSM.movement.PathFinished -= FindWanderPos;

        if (globalShopping)
            ExitGlobalShopping();
        
        if (brainSM.currentState != this)
            return;

        if (InterestedShopsToSell.Count == 0)
        {
            EnterGlobalShop();
            return;
        }

        GD.Print("AI can sell at " + InterestedShopsToSell.Count + " shops");
        
        InterestedShopsToSell = InterestedShopsToSell.OrderByDescending(x => x.Value).ToList();
        GD.Print(InterestedShopsToSell[0].Key.ShopFront.Name);
        selectedShop = InterestedShopsToSell[0].Key;
        EnterShop();
        InterestedShopsToSell.RemoveAt(0);
    }

    private void EnterGlobalShop()
    {
        brainSM.movement.PathFinished += DoGlobalShopping;
        EnterShop(globalMarketPos);
    }

    private void DoGlobalShopping()
    {
        brainSM.movement.PathFinished -= DoGlobalShopping;
        globalShopping = true;
        shoppingTimer.Start();
        brainSM.ToggleVisibility();
        brainSM.GlobalPosition = new Vector2(-180,190);
        SellJunkAtGlobalShop();
    }

    private void EnterShop()
    {
        brainSM.movement.PathFinished += DoShopping;
        EnterShop(selectedShop.ShopFront.GlobalPosition);
    }

    private void DoShopping()
    {
        brainSM.movement.PathFinished -= DoShopping;
        if (InterestedShopsToSell.Count > 0)
            SellJunkAtShop();
        if (ShoppingList.Count > 0)
            BuyFromShop();
        ExitShopping();
    }

    private void SellJunkAtGlobalShop()
    {
        foreach (var item in brainInventory.MobInventory)
        {
            if (item.Key.EquipmentType > 0)
                continue;
            else
                globalMarket.SellItem(brainSM, item.Key, item.Value);
        }
    }

    private void SellJunkAtShop()
    {
        foreach (var item in brainInventory.MobInventory)
        {
            if (item.Key.EquipmentType > 0)
                continue;
            else if (selectedShop.PurchaseOrders.ContainsKey(item.Key))
                selectedShop.SellItem(brainSM, item.Key, item.Value);
        }
    }

    private void BuyFromShop()
    {
        selectedShop.BuyItem(brainSM, itemToBuy, 1);
        ShoppingList.Remove(selectedShop);
    }

    private void ExitShopping()
    {
        InterestedShopsToSell.Clear();

        if (InterestedShopsToBuy.Count > 0)
        {
            if (ShoppingList.Count == 0)
                CreateShoppingList();
        }

        if (ShoppingList.Count > 0)
        {
            BuyFromNextShop();
            return;
        }
        brainSM.ChangeState(brainSM.idleState);
    }

    private void ExitGlobalShopping()
    {
        globalShopping = false;
        brainSM.GlobalPosition = brainSM.home.GlobalPosition + new Vector2(GD.RandRange(-25, 25), GD.RandRange(-25, 25));
        brainSM.ToggleVisibility();
        ExitShopping();
    }

    public void BrainFoundShop(Shop shop)
    {
        EvaluateSellingAtShop(shop);
        EvaluateBuyingFromShop(shop);
    }

    public void EnterShop(Vector2 pos)
    {
        brainSM.movement.MoveTo(pos);
    }

    private void FindWanderPos()
    {
        var wanderAreaPos = brainSM.homeZone.GlobalPosition;
        var wanderAreaEnd = brainSM.homeZone.Shape.GetRect().End;

        Random rnd = new Random();
        Vector2 wanderPosition = new Vector2(
            rnd.Next((int)wanderAreaPos.X - (int)wanderAreaEnd.X, (int)wanderAreaPos.X + (int)wanderAreaEnd.X), 
            rnd.Next((int)wanderAreaPos.Y - (int)wanderAreaEnd.Y, (int)wanderAreaPos.Y + (int)wanderAreaEnd.Y)
        );

        brainSM.movement.MoveTo(wanderPosition);
    }

    private void EvaluateSellingAtShop(Shop shop)
    {
        if (InterestedShopsToSell.Any(x => x.Key == shop))
            return;
        
        int totalPossiblePayout = 0;
        foreach (var item in brainInventory.MobInventory)
        {
            if (shop.PurchaseOrders.ContainsKey(item.Key))
            {
                int maxVolume = shop.PurchaseOrders[item.Key].MaxVolume(item.Value, shop.coins);
                totalPossiblePayout += shop.PurchaseOrders[item.Key].PayoutPrice(maxVolume);
            }
        }

        if (totalPossiblePayout > 0)
            InterestedShopsToSell.Add(new KeyValuePair<Shop, int>(shop, totalPossiblePayout));
    }

    private void EvaluateBuyingFromShop(Shop shop)
    {
        if (ShoppingList.Count != 0) return;
        if (InterestedShopsToBuy.ContainsValue(shop))
            return;
        
        foreach (var SellOrder in shop.SellOrders)
        {
            if (SellOrder.Value.Price > brainInventory.Gold)
                continue;

            if (SellOrder.Key.EquipmentType == Item.EquippableType.Weapon)
                if ((int)SellOrder.Key.Tier > (int)brainInventory.EquippedWeapon.GetItemTier)
                    InterestedShopsToBuy.Add(SellOrder.Value, shop);
            
            if (SellOrder.Key.EquipmentType == Item.EquippableType.Armor)
                if ((int)SellOrder.Key.Tier > (int)brainInventory.EquippedArmor.GetItemTier)
                    InterestedShopsToBuy.Add(SellOrder.Value, shop);

            if (SellOrder.Key.EquipmentType == Item.EquippableType.Consumable)
                if (brainInventory.EquippedPotions.GetPotionCount < 3)
                    InterestedShopsToBuy.Add(SellOrder.Value, shop);
        }
    }

    private void CreateShoppingList()
    {
        List<KeyValuePair<ShopOrder, Shop>> weapons = InterestedShopsToBuy.Where(x => x.Key.GetItem.EquipmentType == Item.EquippableType.Weapon).ToList();
        if (weapons.Count != 0)
        {
            KeyValuePair<ShopOrder, Shop> weaponToBuy = weapons.OrderBy(x => x.Key.Price).ToList()[0];
            ShoppingList.Add(weaponToBuy.Value, new KeyValuePair<Item, int>(weaponToBuy.Key.GetItem, weaponToBuy.Key.Price));
        }

        List<KeyValuePair<ShopOrder, Shop>> armors = InterestedShopsToBuy.Where(x => x.Key.GetItem.EquipmentType == Item.EquippableType.Armor).ToList();
        if (armors.Count != 0)
        {
            KeyValuePair<ShopOrder, Shop> armorToBuy = armors.OrderBy(x => x.Key.Price).ToList()[0];
            ShoppingList.Add(armorToBuy.Value, new KeyValuePair<Item, int>(armorToBuy.Key.GetItem, armorToBuy.Key.Price));
        }

        List<KeyValuePair<ShopOrder, Shop>> potions = InterestedShopsToBuy.Where(x => x.Key.GetItem.EquipmentType == Item.EquippableType.Consumable).ToList();
        if (potions.Count != 0)
        {
            KeyValuePair<ShopOrder, Shop> potionsToBuy = potions.OrderBy(x => x.Key.Price).ToList()[0];
            ShoppingList.Add(potionsToBuy.Value, new KeyValuePair<Item, int>(potionsToBuy.Key.GetItem, potionsToBuy.Key.Price));
        }

        GD.Print(brainSM.Name + " has " + ShoppingList.Count + " shops in it's shopping list");

        InterestedShopsToBuy.Clear();
    }

    private void BuyFromNextShop()
    {
        KeyValuePair<Shop, KeyValuePair<Item, int>> nextPurchase = ShoppingList.OrderBy(x => x.Value.Value).ToList()[0];
        selectedShop = nextPurchase.Key;
        if (selectedShop.SellOrders[nextPurchase.Value.Key].Price != nextPurchase.Value.Value)
        {
            ShoppingList.Remove(selectedShop);
            BuyFromNextShop();
            return;
        }

        itemToBuy = nextPurchase.Value.Key;
        EnterShop();
    }
}
