using System.Linq;
using Godot;

public partial class UiShopObjectController : Control
{
    [Export] public Button ShopfrontButton;

    public override void _Ready()
    {
		ShopfrontButton.Pressed += HandleShopObjectPlaceRequest;
    }

    public void HandleShopObjectPlaceRequest()
	{
		PlayerInfo playerInfo = MultiplayerGameManager.LocalPlayer;
		playerInfo.Shop.InstantiateShopObjectPlacement(Entity.spawnType.ShopFront);
		playerInfo.Shop.ShopObjectPlaced += DestroyShopFrontRequestButton;
	}

	public void DestroyShopFrontRequestButton()
	{
		//return;
		ShopfrontButton.Visible = false;
	}
}