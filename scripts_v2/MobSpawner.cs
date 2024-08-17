using Godot;
using System;

public partial class MobSpawner : Node2D
{
	int mobID;
	int maxMobs;
	int spawnedMobs;
	int killedMobs;
	Timer spawnTimer;
	CollisionShape2D spawnArea;
	JipSyncronyzer jipSyncronyzer;
	public event Action SpawnerFinished;
	public event Action SpawnDestroyed;

    public override void _Ready()
    {
        spawnTimer = GetNode<Timer>("SpawnTimer");
		spawnArea = GetNode("SpawnArea").GetNode<CollisionShape2D>("CollisionShape2D");
		jipSyncronyzer = GetTree().Root.GetNode("World").GetNode<JipSyncronyzer>("JipSyncHandler");

		if (!Multiplayer.IsServer())
		{
			spawnTimer.Stop();
			spawnTimer.QueueFree();

			return;
		}
    }

	public void EntitySetup(Spawner entity)
	{
		spawnTimer.WaitTime = entity.SpawnTimer;
		mobID = entity.MobID;
		maxMobs = entity.MaxMobs;
        GetNode<Sprite2D>("Sprite2D").Texture = GD.Load<Texture2D>($"res://{entity.Sprite}");
	}

	private void CreateMob()
	{
		Node2D mob = jipSyncronyzer.CreateSpawnable(mobID, GetRandomSpawnPos(), this, spawnArea);
		mob.GetNode<Health>("Health").Died += HandleMobDeath;
		spawnedMobs++;

		if (spawnedMobs >= maxMobs)
			GetNode<Timer>("SpawnTimer").Stop();
	}

	private Vector2 GetRandomSpawnPos()
	{
		var lairAreaPos = spawnArea.GlobalPosition;
		var lairAreaEnd = spawnArea.Shape.GetRect().End;

		Random rnd = new Random();
		Vector2 rndSpawnPos = new Vector2(
			rnd.Next((int)lairAreaPos.X - (int)lairAreaEnd.X, (int)lairAreaPos.X + (int)lairAreaEnd.X), 
			rnd.Next((int)lairAreaPos.Y - (int)lairAreaEnd.Y, (int)lairAreaPos.Y + (int)lairAreaEnd.Y)
		);

		return rndSpawnPos;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void DestroySelf()
	{
		MultiplayerGameManager.RemoveLair(this);

		if (Multiplayer.IsServer())
		{
			spawnTimer.Stop();
			SpawnDestroyed?.Invoke();
		}
		
		QueueFree();
	}

	public void OnSpawnTimerTimeout()
	{
		CreateMob();
	}

	private void HandleMobDeath(Node2D killer)
	{
		killedMobs++;
		if (killedMobs >= maxMobs && Multiplayer.IsServer())
		{
			//CLIENTS ARE TRACKING MOB DEATHS, MUST FIX LATER
			DestroySelf();
			Rpc("DestroySelf");
		}
	}
}
