using Godot;

public class PlayerInfo
{
    //Basic info
    public string name;
    public int id;
    public bool isJip = false;

    //Ingame info
    public bool hasCharacter = false;
    public Node2D localPlayerCharacter; //Note: This variable is local, and isn't synced across the server
    public Shop Shop;
}