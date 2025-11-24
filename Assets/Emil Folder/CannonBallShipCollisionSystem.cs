using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CannonBallSystem))] // after movement
public partial struct CannonBallShipCollisionSystem : ISystem
{
    private ComponentLookup<ShipAuthoring.Ship> _shipLookup;
    private ComponentLookup<HealthComponent> _healthLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CannonBalls>();
        state.RequireForUpdate<PhysicsWorldSingleton>();

        _shipLookup = state.GetComponentLookup<ShipAuthoring.Ship>(false);
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _shipLookup.Update(ref state);
        _healthLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        new CannonBallShipHitJob
        {
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            DeltaTime = SystemAPI.Time.DeltaTime,
            PhysicsWorld = physicsWorldSingleton,
            ShipLookup = _shipLookup,
            HealthLookup = _healthLookup,
            Damage = 1
        }.Schedule();
    }
}

[BurstCompile]
public partial struct CannonBallShipHitJob : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;

    public PhysicsWorldSingleton PhysicsWorld;
    public ComponentLookup<ShipAuthoring.Ship> ShipLookup;
    public ComponentLookup<HealthComponent> HealthLookup;

    public int Damage;

    void Execute(Entity e, ref CannonBalls ball, in LocalTransform xform)
    {
        // Approximate this frame’s travel as one ray step
        float3 start = xform.Position - ball.Velocity * DeltaTime;
        float3 end = xform.Position;

        // Don’t bother when almost not moving
        if (math.lengthsq(end - start) < 0.0001f)
            return;

        var input = new RaycastInput
        {
            Start = start,
            End = end,
            Filter = CollisionFilter.Default
        };

        if (!PhysicsWorld.CastRay(input, out var hit))
            return;

        var target = hit.Entity;

        // Only react to ships
        if (ShipLookup.HasComponent(target) && HealthLookup.HasComponent(target))
        {
            var health = HealthLookup[target];
            health.health -= Damage;
            HealthLookup[target] = health;

            ECB.DestroyEntity(e);
        }
    }
}