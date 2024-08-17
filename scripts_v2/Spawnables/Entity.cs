public class Entity
{
    public enum spawnType
    {
        Hero,
        Monster,
        Spawner,
        Furnishing,
        ShopFront
    }
    public spawnType type;

    public virtual void Setup()
    {
        
    }
}