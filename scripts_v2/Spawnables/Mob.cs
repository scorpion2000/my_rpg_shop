using Godot;
using System;

public partial class Mob : Entity
{
	public string MobName { get; set; }
	public bool IsHero { get; set; }
	public float LooseDistance { get; set; }
	public int Health { get; set; }
	public float MovementSpeed { get; set; }
	public int[] DropItemIDs { get; set; }
	public int[] DropItemCounts { get; set; }
	public string RunAnimFolder { get; set; }
	public string IdleAnimFolder { get; set; }
	public void SetType()
	{
		if (IsHero)
			type = spawnType.Hero;
		else
			type = spawnType.Monster;
	}
}
