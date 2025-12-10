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

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1u << 0,
            CollidesWith = 1u << 1,
            GroupIndex = 0
        };

        float dt = SystemAPI.Time.DeltaTime;

        float halfWidth = config.MapSize.x * 0.5f - 10f;
        float halfHeight = config.MapSize.y * 0.5f - 10f;

        if (config.ScheduleParallel)
        {
            new AvoidShipCollionJob
            {
                dt = dt,
                halfWidth = halfWidth,
                halfHeight = halfHeight,
                transformLookup = transformLookup,
                filter = filter,
                physicsWorld = physicsWorld

            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new AvoidShipCollionJob
            {
                dt = dt,
                halfWidth = halfWidth,
                halfHeight = halfHeight,
                transformLookup = transformLookup,
                filter = filter,
                physicsWorld = physicsWorld
            }.Schedule();
        }

        else {
        foreach (var (transform, rotation, timer, avoidance, entity)
            in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<RotationComponent>,
                RefRW<CollisionScanTimer>,
                RefRW<AvoidanceState>>()
                .WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            float3 fwd3 = math.normalizesafe(new float3(transform.ValueRO.Forward().x, 0f, transform.ValueRO.Forward().z));
            float2 fwd2 = new float2(fwd3.x, fwd3.z);

            timer.ValueRW.TimeLeft -= dt;

            bool scannedThisFrame = timer.ValueRW.TimeLeft <= 0f;

            if (scannedThisFrame)
            {
                timer.ValueRW.TimeLeft = timer.ValueRW.Interval;

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

                        if (other == entity)
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

                        avoidance.ValueRW.Active = true;
                        avoidance.ValueRW.Target = target;
                    }
                }
                else
                {
                    avoidance.ValueRW.Active = false;
                }
            }

            if (avoidance.ValueRW.Active)
            {
                avoidance.ValueRW.Target.x = math.clamp(avoidance.ValueRW.Target.x, -halfWidth, halfWidth);
                avoidance.ValueRW.Target.z = math.clamp(avoidance.ValueRW.Target.z, -halfHeight, halfHeight);
                rotation.ValueRW.desiredPosition = avoidance.ValueRW.Target;
            }
        }
        }

        hits.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}


[BurstCompile]
public partial struct AvoidShipCollionJob : IJobEntity
{

    public float dt;

    public float halfWidth;
    public float halfHeight;
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
                avoidance.Target.x = math.clamp(avoidance.Target.x, -halfWidth, halfWidth);
                avoidance.Target.z = math.clamp(avoidance.Target.z, -halfHeight, halfHeight);
                rotation.desiredPosition = avoidance.Target;
            }
            hits.Dispose();
        }

    }
