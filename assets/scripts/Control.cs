using Godot;
using System;
using System.Linq;
public partial class Control : Godot.Control
{

	[Export]
	private int port = 8911;
	[Export]
	private int max_clients = 32;
	[Export]
	private string address = "127.0.0.1";
	private ENetMultiplayerPeer peer;

	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		GD.Print(OS.GetCmdlineArgs());
		if(OS.GetCmdlineArgs().Contains("--server")){
			GD.Print("Server!");
			hostGame();
		}
	}

	private void ConnectionFailed()
	{
		GD.Print("Connection Failed");
	}


	private void ConnectedToServer()
	{
		GD.Print("Connected to Server");
		RpcId(1, "sendPlayerInformation", GetNode<LineEdit>("LineEdit").Text, Multiplayer.GetUniqueId());
	}


	private void PeerDisconected(long id)
	{
		GD.Print("Player Disconnected: " + id.ToString());
		GameManager.Players.Remove(GameManager.Players.Where(i => i.Id == id).First<PlayerInfo>());
		var players = GetTree().GetNodesInGroup("Player");
		foreach (var item in players)
		{
			if(item.Name == id.ToString()){
				item.QueueFree();
			}
		}
	}


	private void PeerConnected(long id)
	{
		GD.Print("Player Connected: " + id.ToString());
	}


	private void hostGame(){
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, max_clients);
		if (error != Error.Ok){
			GD.Print("ERROR CANNOT HOST :"+ error.ToString());
			return;
		}
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Waiting For Players!");
	}

	public void _on_host_button_down()
	{
		hostGame();
		sendPlayerInformation(GetNode<LineEdit>("LineEdit").Text, 1);
	}

	public void _on_join_button_down()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(address, port);

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Joining Game!");
	}
	public void _on_start_game_button_down()
	{
		Rpc("startGame");
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void startGame(){
		foreach (var item in GameManager.Players)
		{
			GD.Print(item.Name + " is playing");
		}
		var scene = ResourceLoader.Load<PackedScene>("res://assets/scenes/TestWorld.tscn").Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		this.Hide();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void sendPlayerInformation(string name, int id){
		PlayerInfo playerInfo = new PlayerInfo(){
			Name = name,
			Id = id
		};
		if(!GameManager.Players.Contains(playerInfo)){
			GameManager.Players.Add(playerInfo);
		}

		if (Multiplayer.IsServer()){
			foreach (var item in GameManager.Players)
			{
				Rpc("sendPlayerInformation",item.Name, item.Id);
			}
		}
	}
}
