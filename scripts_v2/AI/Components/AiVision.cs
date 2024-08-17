using Godot;
using System.Linq;
using System.Collections.Generic;
using System;

public partial class AiVision : CharacterBody2D
{
    [Export] public float looseDistance = 150;
    CharacterBody2D parent;
	Node2D target;
	Area2D visionArea;
	Brain parentBrain;
	public event Action<Node2D> VisibleTargetChanged;
	public event Action<List<Node2D>> ShopsSeen;

    public override void _Ready()
    {
		if (!Multiplayer.IsServer())
			QueueFree();

		visionArea = GetParent<CharacterBody2D>().GetNode<Area2D>("VisionCircle");
		if (GetParent() is Brain brain)
			parentBrain = brain;
		else
			QueueFree();
    }

    public void OnVisionTimeout()
    {
        SearchForBodies();
    }

	private bool CanSeeBody(Node2D body)
	{
        if (body.GetNode<Health>("Health").GetHealth == 0 || body == null || !IsInstanceValid(body))
            return false;

		var spaceState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, body.GlobalPosition);
		query.Exclude = new Godot.Collections.Array<Rid> { GetParent<CharacterBody2D>().GetRid() };
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var result = spaceState.IntersectRay(query);

		if (!result.ContainsKey("collider"))
			return false;

		if ((Node2D)result["collider"] == body && GlobalPosition.DistanceTo((Vector2)result["position"]) < looseDistance)
			return true;
		
		return false;
	}

    private void SearchForBodies()
	{
		if (target != null)
		{
			if (!CanSeeBody(target))
			{
                target = null;
				VisibleTargetChanged?.Invoke(null);
				return;
			}
			return;
		}

		List<Node2D> hostileBodies = new List<Node2D>();
		List<Node2D> shops = new List<Node2D>();
		List<Node2D> physicsBodies = visionArea.GetOverlappingBodies().ToList();
		if (physicsBodies.Contains(GetParent<Node2D>()))
			physicsBodies.Remove(GetParent<Node2D>());

		foreach (Node2D body in physicsBodies)
		{
			if (body is Brain bodyBrain)
            	if (bodyBrain.team != parentBrain.team)
					hostileBodies.Add(body);
			if (body is Shop shop)
				shops.Add(body);
		}

		if (hostileBodies.Count == 0)
		{
			ShopsSeen?.Invoke(shops);
			return;
		}

        foreach (var body in hostileBodies)
        {
			if (CanSeeBody(body))
            {
		        target = body;
				if (target.GetNode("Health") is Health health)
					if (health.GetHealth <= 0) continue;
				VisibleTargetChanged?.Invoke(target);
                break;
            }
        }
	}
}