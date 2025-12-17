using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct MovementSystem : ISystem
{
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        float dt = SystemAPI.Time.DeltaTime;

        var job = new EntityMoveJob
        {
            DeltaTime = dt
        };

        if (config.ScheduleParallel)
        {
            job.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            job.Schedule();
        }

        else
        {
            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

// Moves ships forward
[BurstCompile]
public partial struct EntityMoveJob : IJobEntity
{
    public float DeltaTime;
    void Execute(Entity e, ref LocalTransform transform, ref PhysicsVelocity velocity, in ShipSense sense)
    {
        float3 upVector = transform.Forward();

        float2 forwardXZ = math.normalize(new float2(upVector.x, upVector.z));

        float2 desiredXZ = forwardXZ * sense.ShipSpeed;

        var v = velocity;

        v.Linear.x = desiredXZ.x;
        v.Linear.z = desiredXZ.y;

        velocity = v;
    }
}
