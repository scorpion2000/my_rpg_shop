namespace ShopGenerics
{
    public struct ShopOrder
    {
        int volume;
        int price;
        bool finitePurchase;
        Item item;
        public event System.Action<Item> Emptied;
        public int Volume { get {return volume; } }
        public int Price { get {return price; } }
        public Item GetItem { get { return item; } }
        public int UpdateVolume
        { 
            set
            {
                volume = value;
                if (volume < 0 && !finitePurchase) Godot.GD.PrintErr("Shop order volume is below 0!");
                if (volume <= 0 && !finitePurchase) Emptied.Invoke(item);
            }
        }
        public int UpdatePrice {set { price = value; } }

        public ShopOrder(int _volume, int _price, Item _item, bool _finitePurchase)
        {
            volume = _volume;
            price = _price;
            item = _item;
            finitePurchase = _finitePurchase;
            Emptied = null;
        }

        public int PayoutPrice(int _volume)
        {
            if (finitePurchase)
                return _volume * price;

            return (int)System.MathF.Min(_volume, volume) * price;
        }

        public int PayoutVolume(int _volume)
        {
            if (finitePurchase)
                return _volume;

            return (int)System.MathF.Min(_volume, volume);
        }

        public int MaxVolume(int _volume, int _price)
        {
            if (finitePurchase)
                return _volume;
            
            return (int)System.MathF.Min(System.MathF.Floor(_price / price), _volume);
        }
    }
}