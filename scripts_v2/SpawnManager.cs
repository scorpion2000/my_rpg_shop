using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SpawnManager : Node
{
	public List<int> heroIDs = new List<int>();
	public List<int> MobLairs = new List<int>();
	JipSyncronyzer jipSyncronyzer;
	int maxMobLairCount = 2;
	int currentHeroeCount;
	int maxHeroeCount = 4;
	List<CollisionShape2D> lairSpawnAreas = new List<CollisionShape2D>();

    public override void _Ready()
    {
		GD.Print(MultiplayerGameManager.AllMobs.Count);
		foreach (var item in MultiplayerGameManager.AllMobs)
			if (item.Value.IsHero)
				heroIDs.Add(item.Key);

		foreach (var item in MultiplayerGameManager.AllLairs)
			MobLairs.Add(item.Key);

		jipSyncronyzer = GetParent().GetNode<JipSyncronyzer>("JipSyncHandler");

		foreach (var item in GetTree().GetNodesInGroup("MobAreas"))
		{
			lairSpawnAreas.Add(item.GetNode<CollisionShape2D>("CollisionShape2D"));
		}
    }

    public void OnHeroTimer()
	{
		if (currentHeroeCount >= maxHeroeCount)
			return;
		
		CreateHeroNpc(currentHeroeCount);
		currentHeroeCount++;
	}

	private void CreateHeroNpc(int count)
	{
		GD.Print(jipSyncronyzer);
		Node2D hero = jipSyncronyzer.CreateSpawnable(heroIDs[0], GetParent().GetNode<Node2D>("Home").GlobalPosition, GetParent().GetNode<Node2D>("Home"), GetParent().GetNode<Node2D>("Home").GetNode("HomeWanderArea").GetNode<CollisionShape2D>("WanderAreaColl"));

		MultiplayerGameManager.HeroSpawnedEvent((Brain)hero);
		//MultiplayerGameManager.AllMobs.Add(hero);
	}

	public void OnMobLairTimer()
	{
		if (MultiplayerGameManager.SpawnedLairs.Count() >= maxMobLairCount)
			return;

		CollisionShape2D lairSpawnArea = lairSpawnAreas[(int)GD.RandRange(0, lairSpawnAreas.Count() - 1)];

		Vector2 spawnPos = new Vector2(
			(float)GD.RandRange(lairSpawnArea.GlobalPosition.X - lairSpawnArea.Shape.GetRect().End.X, lairSpawnArea.GlobalPosition.X + lairSpawnArea.Shape.GetRect().End.X),
			(float)GD.RandRange(lairSpawnArea.GlobalPosition.Y - lairSpawnArea.Shape.GetRect().End.Y, lairSpawnArea.GlobalPosition.Y + lairSpawnArea.Shape.GetRect().End.Y)
		);
		
		Random rnd = new Random();
		Node2D lair = jipSyncronyzer.CreateSpawnable(MobLairs[rnd.Next(0, MobLairs.Count)], spawnPos, null, null);
		MultiplayerGameManager.AddLair(lair);
	}
}
