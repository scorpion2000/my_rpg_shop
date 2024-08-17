public class Spawner : Entity
{
    public float SpawnTimer { get; set; }
    public int MobID { get; set; }
    public int MaxMobs { get; set; }
    public string Sprite { get; set; }

    public Spawner()
	{
		type = spawnType.Spawner;
	}
}