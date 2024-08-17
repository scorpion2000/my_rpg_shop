using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public partial class MultiplayerGameManager : Node
{
	public static PlayerInfo LocalPlayer;
	public static List<PlayerInfo> Players = new List<PlayerInfo>();
	public static List<Node2D> SpawnedLairs = new List<Node2D>();
	public static List<Node2D> SpawnedMobs = new List<Node2D>();
	public static List<Node2D> SpawnedShopPlacables = new List<Node2D>();
	public static Dictionary<int, Spawner> AllLairs = new Dictionary<int, Spawner>();
	public static Dictionary<int, Mob> AllMobs = new Dictionary<int, Mob>();
	public static Dictionary<int, Item> AllItems = new Dictionary<int, Item>();
	public static Dictionary<int, ShopPlacable> AllShopPlacables = new Dictionary<int, ShopPlacable>();
	public static bool UiOpen = false;
	public static event Action<Brain> HeroSpawned;
	public static event Action<Node2D> LairSpawned;
	public static event Action ItemsReady;
	public static event Action<int, int, int, bool, int> ShopItemAdded;
	public static event Action<int, int, bool> ShopItemModified;
	public static void ToggleUiOpen() {UiOpen = !UiOpen;}
	public static void HeroSpawnedEvent(Brain hero) { HeroSpawned?.Invoke((Brain)hero); }

	public override void _Ready()
	{
		var file = Godot.FileAccess.Open("res://json/items.json", Godot.FileAccess.ModeFlags.Read);
		string values = file.GetAsText();
		AllItems = JsonSerializer.Deserialize<Dictionary<int, Item>>(values);
		foreach (var item in AllItems)
		{
			item.Value.SetTexture();
			item.Value.ID = item.Key;
		}

		file = Godot.FileAccess.Open("res://json/mobs.json", Godot.FileAccess.ModeFlags.Read);
		values = file.GetAsText();
		AllMobs = JsonSerializer.Deserialize<Dictionary<int, Mob>>(values);
		foreach (var item in AllMobs)
		{
			item.Value.SetType();
			//GD.Print(item.Value.MobName);
		}

		file = Godot.FileAccess.Open("res://json/spawners.json", Godot.FileAccess.ModeFlags.Read);
		values = file.GetAsText();
		AllLairs = JsonSerializer.Deserialize<Dictionary<int, Spawner>>(values);
		/*foreach (var item in AllLairs)
		{
			GD.Print(item.Value);
		}*/
		file = Godot.FileAccess.Open("res://json/shopPlacable.json", Godot.FileAccess.ModeFlags.Read);
		values = file.GetAsText();
		AllShopPlacables = JsonSerializer.Deserialize<Dictionary<int, ShopPlacable>>(values);
		foreach (var item in AllShopPlacables)
		{
			item.Value.Setup();
		}
	}

	public static void AddLair(Node2D lair)
	{
		if (!SpawnedLairs.Contains(lair))
			SpawnedLairs.Add(lair);
	}

	public static void RemoveLair(Node2D lair)
	{
		if (SpawnedLairs.Contains(lair))
			SpawnedLairs.Remove(lair);
	}

	public static void CreateShopItem(int itemID, int volume, int price, bool buyOrder, int playerID)
	{
		ShopItemAdded?.Invoke(itemID, volume, price, buyOrder, playerID);
	}

	public static void HandleShopItemModified(int itemID, int newVolume, bool buyOrder)
	{
		ShopItemModified?.Invoke(itemID, newVolume, buyOrder);
	}
}
