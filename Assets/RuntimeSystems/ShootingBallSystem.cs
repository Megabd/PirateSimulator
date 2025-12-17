using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RotationSystem))]
partial struct ShootingBallSystem : ISystem
{
    private Unity.Mathematics.Random rand;
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        rand = new Unity.Mathematics.Random(1);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var ballXform = SystemAPI.GetComponent<LocalTransform>(config.CannonBallPrefab);
        float dt = SystemAPI.Time.DeltaTime;

        var job = new ShootingBallJob
        {
            dt = dt,
            config = config,
            ecb = ecb,
            ballXform = ballXform
        };

        if (config.ScheduleParallel)
        {
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }

        else
        {
            job.Run();
        }

    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

// Counts a warmup timer, then shoots cannonball from cannons position, and resets target.
[BurstCompile]
public partial struct ShootingBallJob : IJobEntity
{
    public float dt;

    public Config config;

    public EntityCommandBuffer.ParallelWriter ecb;
    public LocalTransform ballXform;
    void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity e, in LocalTransform transform, ref RotationComponent rotation, ref Aim aim, in LocalToWorld worldPos, ref PrevPosComponent prevPos, in CannonData data)
    {
        float3 currentPos = worldPos.Position;
        float3 cannonVel = (currentPos - prevPos.PrePos) / dt; 
        prevPos.PrePos = currentPos; // store for next frame

        // No target, no warmup, no shot
        if (!aim.HasTarget)
        {
            aim.ShootTimeLeft = 0f;
            return;
        }

        // Count down the warmup timer
        aim.ShootTimeLeft -= dt;
        if (aim.ShootTimeLeft > 0f)
        {
            // still winding up, dont shoot yet
            return;
        }
            
        var ball = ecb.Instantiate(entityInQueryIndex, config.CannonBallPrefab);

        // spawn at cannon position
        ballXform.Position = currentPos;
        ballXform.Rotation = transform.Rotation; 
        ecb.SetComponent(entityInQueryIndex, ball, ballXform);


        // ball inherits cannon velocity + its own shooting speed
        float3 dir = math.normalize(worldPos.Forward);
        ecb.SetComponent(entityInQueryIndex, ball, new CannonBalls
        {
            Velocity = cannonVel + dir * data.CannonballSpeed,
            Lifetime = data.CannonballLifeTime,
            Radius = 0.5f //canonball hitbox
        });

        aim.HasTarget = false;
        rotation.desiredPosition = float3.zero;
    }
}
