using System;

public class ShopPlacable : Entity
{
    public enum ShopPlacableType
    {
        ShopFront
    }
    public string Sprite { get; set; }
    public string PlacableType { get; set; }

    public ShopPlacable()
	{
		type = spawnType.Spawner;
	}

    public override void Setup()
    {
        type = Enum.Parse<spawnType>(PlacableType);
    }
}