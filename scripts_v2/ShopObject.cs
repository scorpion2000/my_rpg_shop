using Godot;
using System;
using System.Linq;

public partial class ShopObject : CharacterBody2D, IInteractable
{
    [Export] string InteractableNodeName;
    [Export] AnimationPlayer animPlayer;
    [Export] Sprite2D interactionSprite;
	[Export] public bool canPlayerInteract;
    ShopFront playerShopUI;
	Area2D placableArea;
    bool inPlacableArea = false;
	bool toggled;
    public event Action<ShopObject> Placed;
    public event Action<ShopObject> PlacementCancelled;
	public bool GetCanPlayerInteract { get { return canPlayerInteract; } }

    public override void _Ready()
    {
        playerShopUI = GetTree().Root.GetNode("World").GetNode<ShopFront>("PlayerShopfront");
        ShopPlacable entity = MultiplayerGameManager.AllShopPlacables.First(x => x.Value.type == Entity.spawnType.ShopFront).Value;
        PlacableSetup(entity);
    }

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();

        if (Input.IsActionJustPressed("Click"))
        {
            if (inPlacableArea)
                HandlePlacement();
        }

        if (Input.IsActionJustPressed("RClick"))
            HandlePlacementCancel();
    }

    public void PlacableSetup(ShopPlacable entity)
    {
        GetNode<Sprite2D>("Sprite2D").Texture = GD.Load<Texture2D>($"res://{entity.Sprite}");
    }

    public void SetPlacableArea(Area2D _placableArea) {
        placableArea = _placableArea;
        placableArea.BodyEntered += HandleEnteringPlaceArea;
        placableArea.BodyExited += HandleExitingPlaceArea;
        GetNode<Sprite2D>("Sprite2D").SelfModulate = new Color("ff0000");
    }

    private void HandleEnteringPlaceArea(Node2D body)
    {
        if (body == this)
            inPlacableArea = true;
        
        GetNode<Sprite2D>("Sprite2D").SelfModulate = new Color("00ff00");
    }

    private void HandleExitingPlaceArea(Node2D body)
    {
        if (body == this)
            inPlacableArea = false;
        
        GetNode<Sprite2D>("Sprite2D").SelfModulate = new Color("ff0000");
    }

    private void HandlePlacement()
    {
        GetNode<Sprite2D>("Sprite2D").SelfModulate = new Color("ffffff");
        Placed?.Invoke(this);
        SetProcess(false);
        InputPickable = true;
    }

    private void HandlePlacementCancel()
    {
        PlacementCancelled?.Invoke(this);
        QueueFree();
    }

    public void EnableInputInfo(int playerID)
    {
        if (playerID == 1)
            canPlayerInteract = true;
        else
            RpcId(playerID, "RpcClientEnableInteraction");
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
        //interactableObject.InteractUI(MultiplayerGameManager.LocalPlayer, MultiplayerGameManager.LocalPlayer.Shop);
        playerShopUI.Toggle();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcClientEnableInteraction()
    {
        canPlayerInteract = true;
    }
}
