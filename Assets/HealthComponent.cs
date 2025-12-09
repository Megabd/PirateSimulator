using Unity.Entities;

public struct HealthComponent : IComponentData
{
    public int health;
    public int startingHealth;
}
