using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct AvoidShipCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
       var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1u << 0,
            CollidesWith = 1u << 1,
            GroupIndex = 0
        };

        float dt = SystemAPI.Time.DeltaTime;

        /*
        var job = new AvoidShipCollionJob
        {
            dt = dt,
            config = config,
            transformLookup = transformLookup,
            filter = filter,
            physicsWorld = physicsWorld

        };*/

        var job = new AvoidShipCollionJobBad
        {
            dt = dt,
            config = config,
            transformLookup = transformLookup,
            filter = filter,
            physicsWorld = physicsWorld,
            CollisionWorld = collisionWorld

        };

        if (config.ScheduleParallel)
        {
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }

        else {
            state.Dependency.Complete();
            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

// Changes ship target to avoid other ships if ships are nearby
[BurstCompile]
public partial struct AvoidShipCollionJob : IJobEntity
{

    public float dt;

    [ReadOnly]
    public Config config;

    [ReadOnly]
    public ComponentLookup<LocalTransform> transformLookup;

    public CollisionFilter filter;

    [ReadOnly]
    public PhysicsWorld physicsWorld;
    void Execute(Entity e,  ref RotationComponent rotation, ref CollisionScanTimer timer, ref AvoidanceState avoidance)
    {
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        var transform = transformLookup[e];
        float3 pos = transform.Position;
        float3 fwd3 = math.normalizesafe(new float3(transform.Forward().x, 0f, transform.Forward().z));
        float2 fwd2 = new float2(fwd3.x, fwd3.z);

        timer.TimeLeft -= dt;

        bool scannedThisFrame = timer.TimeLeft <= 0f;

        if (scannedThisFrame)
        {
            timer.TimeLeft = timer.Interval;

            float probeOffset = 3f;
            float probeRadius = 3f;
            float3 probeCenter = pos + fwd3 * probeOffset;

            PointDistanceInput input = new PointDistanceInput
            {
                Position = probeCenter,
                MaxDistance = probeRadius,
                Filter = filter
            };

            hits.Clear();

            bool foundShip = physicsWorld.CalculateDistance(input, ref hits);
            float minDistSq = float.MaxValue;
            float3 closestPos = pos;

            if (foundShip)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var body = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                    Entity other = body.Entity;

                    if (other == e)
                        continue;

                    if (!transformLookup.HasComponent(other))
                        continue;

                    float3 p = transformLookup[other].Position;

                    float3 diff3 = p - pos;
                    float2 diff2 = new float2(diff3.x, diff3.z);

                    float dotValue = math.dot(fwd2, math.normalizesafe(diff2));
                    if (dotValue <= 0f)
                        continue;

                    float d = math.lengthsq(diff2);
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        closestPos = p;
                    }
                }
            }

            if (minDistSq != float.MaxValue)
            {
                float3 away = pos - closestPos;
                float2 away2 = math.normalizesafe(new float2(away.x, away.z));
                if (math.lengthsq(away2) > 0f)
                {
                    float cross = fwd2.x * away2.y - fwd2.y * away2.x;
                    float rad = cross > 0 ? math.radians(45f) : math.radians(-45f);

                    float cos = math.cos(rad);
                    float sin = math.sin(rad);

                    float2 steer2 = new float2(
                        fwd2.x * cos - fwd2.y * sin,
                        fwd2.x * sin + fwd2.y * cos
                    );

                    steer2 = math.normalizesafe(steer2);
                    float dist = math.sqrt(minDistSq) + 3f;

                    float3 target = pos + new float3(steer2.x, 0f, steer2.y) * dist;

                    avoidance.Active = true;
                    avoidance.Target = target;
                }
            }
            else
            {
                avoidance.Active = false;
            }
        }

        if (avoidance.Active)
        {
            avoidance.Target.x = math.clamp(avoidance.Target.x, -config.MapSize, config.MapSize);
            avoidance.Target.z = math.clamp(avoidance.Target.z, -config.MapSize, config.MapSize);
            rotation.desiredPosition = avoidance.Target;
        }
        hits.Dispose();
    }

}


[BurstCompile]
public unsafe partial struct AvoidShipCollionJobBad : IJobEntity
{

    public float dt;

    [ReadOnly]
    public Config config;

    [ReadOnly]
    public ComponentLookup<LocalTransform> transformLookup;

    [ReadOnly] public CollisionWorld CollisionWorld;

    public CollisionFilter filter;

    [ReadOnly]
    public PhysicsWorld physicsWorld;
    void Execute(Entity e,  ref RotationComponent rotation, ref CollisionScanTimer timer, ref AvoidanceState avoidance)
    {
        NativeList<ColliderCastHit> hits = new NativeList<ColliderCastHit>(Allocator.Temp);
        var transform = transformLookup[e];
        float3 pos = transform.Position;
        float3 fwd3 = math.normalizesafe(new float3(transform.Forward().x, 0f, transform.Forward().z));
        float2 fwd2 = new float2(fwd3.x, fwd3.z);

        timer.TimeLeft -= dt;

        bool scannedThisFrame = timer.TimeLeft <= 0f;

        if (scannedThisFrame)
        {
            timer.TimeLeft = timer.Interval;

            float probeOffset = 3f;
            float probeRadius = 3f;
            float3 probeCenter = pos + fwd3 * probeOffset;

            float3 end = pos + fwd3 * 5;
            SphereGeometry sphereGeometry = new SphereGeometry { Center = float3.zero, Radius = probeRadius*probeRadius };
            var collider = SphereCollider.Create(
            sphereGeometry,
            CollisionFilter.Default
            );

            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = (Collider*)collider.GetUnsafePtr(),
                Orientation = quaternion.identity,
                Start = pos,
                End = end
            };

            hits.Clear();

            var collector = new AllHitsCollector<ColliderCastHit>(
            maxFraction: 1.0f,
            ref hits
            );

            CollisionWorld.CastCollider(input, ref collector);

            float minDistSq = float.MaxValue;
            float3 closestPos = pos;

            for (int i = 0; i < hits.Length; i++)
                {
                    var body = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                    Entity other = body.Entity;

                    if (other == e)
                        continue;

                    if (!transformLookup.HasComponent(other))
                        continue;

                    float3 p = transformLookup[other].Position;

                    float3 diff3 = p - pos;
                    float2 diff2 = new float2(diff3.x, diff3.z);

                    float dotValue = math.dot(fwd2, math.normalizesafe(diff2));
                    if (dotValue <= 0f)
                        continue;

                    float d = math.lengthsq(diff2);
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        closestPos = p;
                    }
                }

            if (minDistSq != float.MaxValue)
            {
                float3 away = pos - closestPos;
                float2 away2 = math.normalizesafe(new float2(away.x, away.z));
                if (math.lengthsq(away2) > 0f)
                {
                    float cross = fwd2.x * away2.y - fwd2.y * away2.x;
                    float rad = cross > 0 ? math.radians(45f) : math.radians(-45f);

                    float cos = math.cos(rad);
                    float sin = math.sin(rad);

                    float2 steer2 = new float2(
                        fwd2.x * cos - fwd2.y * sin,
                        fwd2.x * sin + fwd2.y * cos
                    );

                    steer2 = math.normalizesafe(steer2);
                    float dist = math.sqrt(minDistSq) + 3f;

                    float3 target = pos + new float3(steer2.x, 0f, steer2.y) * dist;

                    avoidance.Active = true;
                    avoidance.Target = target;
                }
            }
            else
            {
                avoidance.Active = false;
            }
        }

        if (avoidance.Active)
        {
            avoidance.Target.x = math.clamp(avoidance.Target.x, -config.MapSize, config.MapSize);
            avoidance.Target.z = math.clamp(avoidance.Target.z, -config.MapSize, config.MapSize);
            rotation.desiredPosition = avoidance.Target;
        }
        hits.Dispose();
    }

}
