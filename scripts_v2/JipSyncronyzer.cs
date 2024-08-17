using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class JipSyncronyzer : Node
{
    [Export] PackedScene playerScene;
	[Export] PackedScene HeroType;
	[Export] PackedScene MonsterType;
	[Export] PackedScene SpawnerType;
	[Export] PackedScene ShopFront;
    //[Export] Godot.Collections.Dictionary spawnableDictionary = new Godot.Collections.Dictionary();
	Dictionary<int, Entity> spawnableDictionary = new Dictionary<int, Entity>();
    Dictionary<int, Node2D> spawnedObjects = new Dictionary<int, Node2D>();   //<creationID, spawnableID>
    int creationID;
    int index;
    public event Action<Brain> MobSpawned;

    public override void _Ready()
    {
		SpawnableDictionarySetup();

        foreach (PlayerInfo item in MultiplayerGameManager.Players)
		{
			SpawnPlayer(item.id);
			index++;

			item.hasCharacter = true;
		}

        if (Multiplayer.IsServer())
            return;
        
        Node root = GetTree().Root.GetNode("World");
        root.GetNode("SpawnManager").GetNode<Timer>("HeroSpawnCountdown").Stop();
        root.GetNode("SpawnManager").GetNode<Timer>("HeroSpawnCountdown").QueueFree();
        root.GetNode("SpawnManager").GetNode<Timer>("MonsterLairCountdown").Stop();
        root.GetNode("SpawnManager").GetNode<Timer>("MonsterLairCountdown").QueueFree();
        Rpc("SpawnPlayer", Multiplayer.GetUniqueId());	//For JIP Players. They need to tell playing players that they should exist in their games

        if (MultiplayerGameManager.LocalPlayer.isJip)
            RpcId(1, "RpcServerSyncWithClient", Multiplayer.GetUniqueId());
    }

	private void SpawnableDictionarySetup()
	{
		foreach (var item in MultiplayerGameManager.AllMobs)
			spawnableDictionary.Add(item.Key, item.Value);
		foreach (var item in MultiplayerGameManager.AllLairs)
			spawnableDictionary.Add(item.Key, item.Value);
		foreach (var item in MultiplayerGameManager.AllShopPlacables)
			spawnableDictionary.Add(item.Key, item.Value);
	}

    private Node2D InstantiateSpawnable(int spawnableID, Vector2 spawnPos, Node2D homeNode, CollisionShape2D nodeArea, int _creationID, bool runProcess)
    {
		Node2D spawnable = null;
		if (spawnableDictionary[spawnableID].type == Entity.spawnType.Hero)
        	spawnable = HeroType.Instantiate<Node2D>();
		if (spawnableDictionary[spawnableID].type == Entity.spawnType.Monster)
        	spawnable = MonsterType.Instantiate<Node2D>();
		if (spawnableDictionary[spawnableID].type == Entity.spawnType.Spawner)
        	spawnable = SpawnerType.Instantiate<Node2D>();
		if (spawnableDictionary[spawnableID].type == Entity.spawnType.ShopFront)
        	spawnable = ShopFront.Instantiate<Node2D>();
		
        spawnable.SetMeta("creationID", _creationID);
        spawnable.GlobalPosition = spawnPos;
		GetTree().Root.GetNode<Node2D>("World").AddChild(spawnable);
        if (spawnable is Brain brain)
        {
			brain.EntitySetup((Mob)spawnableDictionary[spawnableID], spawnableID);
		    brain.Spawn(spawnPos, homeNode, nodeArea);
            MobSpawned?.Invoke(brain);
        }

		if (spawnable is MobSpawner spawner)
		{
			spawner.EntitySetup((Spawner)spawnableDictionary[spawnableID]);
		}

		spawnable.SetMeta("ID", spawnableID);
        spawnable.SetProcess(runProcess);

        return spawnable;
    }

    public Node2D CreateSpawnable(int spawnableID, Vector2 spawnPos, Node2D homeNode, CollisionShape2D nodeArea, bool runProcess = true)
	{
        Node2D mob = InstantiateSpawnable(spawnableID, spawnPos, homeNode, nodeArea, creationID, runProcess);
        spawnedObjects.Add(creationID, mob);
        Rpc("RpcClientCreateSpawnable", creationID, spawnableID, spawnPos, runProcess);
        creationID++;
        return mob;
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcClientCreateSpawnable(int _creationID, int spawnableID, Vector2 spawnPos, bool runProcess)
    {
        Node2D mob = InstantiateSpawnable(spawnableID, spawnPos, null, null, _creationID, runProcess);
        if (mob.HasNode("MultiplayerSynchronizer"))
        {
            mob.GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetVisibilityFor(1, true);
            RpcId(1, "RpcServerClientSyncRequest", _creationID, Multiplayer.GetUniqueId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcServerClientSyncRequest(int _creationID, int playerID)
    {
        spawnedObjects[_creationID].GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetVisibilityFor(playerID, true);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcServerSyncWithClient(int playerID)
    {
        GD.Print("Sync request JIP: " + playerID);
        //Clients specifically call this for the server ONLY, but just in case I mess up later
        if (!Multiplayer.IsServer())
            return;

        foreach (var item in spawnedObjects)
        {
            if (item.Value == null || !IsInstanceValid(item.Value))
                continue;
            GD.Print("Server sending " + item.Value.Name + " for JIP client: " + playerID);
            int _creationID = item.Key;
            int spawnableID = (int)item.Value.GetMeta("ID");
            Vector2 spawnPos = item.Value.GlobalPosition;
            RpcId(playerID, "RpcClientCreateSpawnable", _creationID, spawnableID, spawnPos, item.Value.IsProcessing());
        }
    }
    
	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void SpawnPlayer(long id)
	{
		PlayerInfo playerInfo = MultiplayerGameManager.Players.Where(x => x.id == (int)id).ToList()[0];
		if (playerInfo.hasCharacter)
			return;

		PlayerController currentPlayer = playerScene.Instantiate<PlayerController>();
		currentPlayer.Name = playerInfo.id.ToString();
		currentPlayer.GetNode<Label>("PlayerName").Name = (playerInfo.name);
		AddChild(currentPlayer);
		foreach (Node2D spawnPoint in GetTree().GetNodesInGroup("PlayerSpawnPoints"))
		{
			if (int.Parse(spawnPoint.Name) == index)
				currentPlayer.GlobalPosition = spawnPoint.GlobalPosition;
		}
		if (playerInfo.id != Multiplayer.GetUniqueId())
			currentPlayer.GetNode<Camera2D>("Camera2D").QueueFree();

		if (playerInfo.id == Multiplayer.GetUniqueId())
			playerInfo.localPlayerCharacter = currentPlayer;
		else
		{
			currentPlayer.GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetVisibilityFor(playerInfo.id, true);
			RpcId(playerInfo.id, "RpcClientSyncRequest", Multiplayer.GetUniqueId());
		}

		if (Multiplayer.IsServer())
			RpcServerRequestShop(id);
		else
			RpcId(1, "RpcServerRequestShop", id);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcServerRequestShop(long id)
	{
		//PlayerInfo playerInfo = MultiplayerGameManager.Players.Where(x => x.id == (int)id).ToList()[0];
		PlayerInfo playerInfo = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == (int)id);

		int index = 0;
		if (playerInfo.Shop != null)
		{
			foreach (Shop item in GetParent().GetNode("Shops").GetChildren())
			{
				if (item == playerInfo.Shop)
				{
					Rpc("RpcClientUpdatePlayerInfo", id, index);
					return;
				}
				index++;
			}
		}

		foreach (Shop item in GetParent().GetNode("Shops").GetChildren())
		{
			if (item.ShopOwner == null)
			{
				item.ChangeShopOwner(playerInfo);
				item.RpcServerChangeShopName(playerInfo.name + "'s Shop");
				playerInfo.Shop = item;
				break;
			}
			index++;
		}

		Rpc("RpcClientUpdatePlayerInfo", id, index);
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcClientSyncRequest(int syncRequestId)
	{
		PlayerInfo playerInfo = MultiplayerGameManager.Players.FirstOrDefault(x => x.id == Multiplayer.GetUniqueId());
		playerInfo.localPlayerCharacter.GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetVisibilityFor(syncRequestId, true);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcClientUpdatePlayerInfo(int playerID, int shopID)
	{
		PlayerInfo playerInfo = MultiplayerGameManager.Players.Where(x => x.id == (int)playerID).ToList()[0];
		playerInfo.Shop = (Shop)GetParent().GetNode("Shops").GetChildren()[shopID];
	}
}