using Godot;
using System;
using System.Linq;

public partial class MultiplayerController : Control
{
	[Export] int port = 12345;
	[Export] string adress = "127.0.0.1";

	ENetMultiplayerPeer peer;
	bool gameStarted = false;
	public event Action playerListUpdated;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		if (OS.GetCmdlineArgs().Contains("--server"))
		{
			HostGame();
		}
	}

	private void HostGame()
	{
		port = GetNode<LineEdit>("PortInput").Text.ToInt();
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, 8);
		if (error != Error.Ok)
		{
			GD.Print("Error: Cannot host! " + error);
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Waiting For Players!");
	}

	//Runs when the connection failed, but only on the client
    private void ConnectionFailed()
    {
		GD.Print("CONNECTION FAILED");
    }

	//runs when the connection is successful, but only on the client
    private void ConnectedToServer()
    {
		GD.Print("CONNECTED TO SERVER");
		RpcId(1, "SendPlayerInformation", GetNode<LineEdit>("LineEdit").Text, Multiplayer.GetUniqueId(), false);
    }

	//Runs when player disconnects, everywhere
    private void PeerDisconnected(long id)
    {
		GD.Print("PLAYER DISCONNECTED" + id);
		MultiplayerGameManager.Players.Remove(MultiplayerGameManager.Players.Where(i => i.id == id).First<PlayerInfo>());
		var players = GetTree().GetNodesInGroup("Player");
		foreach (var item in players)
		{
			if (item.Name == id.ToString())
				item.QueueFree();
		}
    }


	//Runs when player connects, everywhere
    private void PeerConnected(long id)
    {
		GD.Print("PLAYER CONNECTED" + id);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

	public void OnHostButtonDown()
	{
		GD.Print("Pressed The Host Button");
		HostGame();
		SendPlayerInformation(GetNode<LineEdit>("LineEdit").Text, 1, false);
	}

	public void OnJoinButtonDown()
	{
		GD.Print("Pressed The Join Button");
		port = GetNode<LineEdit>("PortInput").Text.ToInt();
		adress = GetNode<LineEdit>("IpInput").Text;

		peer = new ENetMultiplayerPeer();
		peer.CreateClient(adress, port);

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Joining Game!");
	}

	public void OnStartButtonDown()
	{
		Rpc("StartGame");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StartGame()
	{
		gameStarted = true;
		foreach (var item in MultiplayerGameManager.Players)
		{
			GD.Print(item.name + " is playing");
		}

		GD.Print("Game starting");
		var scene = ResourceLoader.Load<PackedScene>("res://Nodes/world_new.tscn").Instantiate<Node2D>();
		GetTree().Root.AddChild(scene);
		this.Hide();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void SendPlayerInformation(string _name, int _id, bool _isJip)
	{
		if (_name == "" && _id == 1)
			_name = "Host";
		else if (_name == "")
			_name = "Client " + MultiplayerGameManager.Players.Count;

		PlayerInfo playerInfo = new PlayerInfo()
		{
			name = _name,
			id = _id,
			isJip = _isJip
		};

		if (!MultiplayerGameManager.Players.Contains(playerInfo))
			MultiplayerGameManager.Players.Add(playerInfo);
		/*else
		{
			PlayerInfo self = GameManager.Players.FirstOrDefault(x => x.id == _id);
			self.isJip = _isJip;
		}*/
		if (_id == Multiplayer.GetUniqueId())
			MultiplayerGameManager.LocalPlayer = playerInfo;

		playerListUpdated?.Invoke();

		if (Multiplayer.IsServer())
		{
			foreach (var item in MultiplayerGameManager.Players)
			{
				Rpc("SendPlayerInformation", item.name, item.id, gameStarted);
			}
			if (gameStarted)
				RpcId(_id, "StartGame");
		}
	}
}
