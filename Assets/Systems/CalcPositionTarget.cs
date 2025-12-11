using NUnit.Framework.Internal;
using System.Security.Principal;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Windows;

partial struct CalcPositionTarget : ISystem
{
    CollisionFilter filter;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex = 0
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        float dt = SystemAPI.Time.DeltaTime;

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        //var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true); error here

        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel)
        {
            state.Dependency = new CalcPositionTargetJob
            {
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                teamLookup = teamLookup,
                transformLookup = transformLookup
            }.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = new CalcPositionTargetJob
            {
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                teamLookup = teamLookup,
                transformLookup = transformLookup
            }.Schedule(state.Dependency);

        }

        else {
            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            foreach (var (transform, rotation, team, timer, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRW<CooldownTimer>>().WithEntityAccess())
            {

                var lcTimer = timer.ValueRW;
                lcTimer.TimeLeft -= dt;
                if (lcTimer.TimeLeft > 0f){
                    timer.ValueRW = lcTimer;
                    continue; 
                }
                var lcTransform = transform.ValueRO;

                float3 pos = lcTransform.Position;
                float3 currentTarget = rotation.ValueRO.desiredPosition;

                float3 fwd = math.normalize(new float3(lcTransform.Forward().x, 0, lcTransform.Forward().z));
                float3 right = math.normalize(math.cross(new float3(0, 1, 0), fwd));

                float offset = ShipConfig.SenseOffset;

                float3 s0 = pos + fwd * offset; // forward
                float3 s1 = pos - right * offset; // left
                float3 s2 = pos + right * offset; // right
                float3 s3 = pos - fwd * offset; // back

                int4 allyCounts = 0;
                bool4 hasEnemy = false;

                var input = new PointDistanceInput
                {
                    Position = pos,
                    MaxDistance = ShipConfig.SenseRadius,
                    Filter = filter
                };
                hits.Clear();
            
                if (physicsWorld.CalculateDistance(input, ref hits))
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var body  = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                        Entity other = body.Entity;
                    
                        if (other == entity) continue;

                        if (!transformLookup.HasComponent(other) || !teamLookup.HasComponent(other))
                        {
                            continue;
                        }

                        var otherTransform = transformLookup[other];
                        var otherTeam = teamLookup[other];

                        float3 p = otherTransform.Position;

                        // same sample logic 
                        float3 d0 = p - s0;
                        bool3 check = d0 <= float3.zero;
                        if (check.x && check.y && check.z)
                        {
                            bool ally = otherTeam.redTeam == team.ValueRO.redTeam;
                            allyCounts.x += ally ? 1 : -1;
                            hasEnemy.x |= !ally;
                        }

                        float3 d1 = p - s1;
                        bool3 check1 = d1 <= float3.zero;
                        if (check1.x && check1.y && check1.z)
                        {
                            bool ally = otherTeam.redTeam == team.ValueRO.redTeam;
                            allyCounts.y += ally ? 1 : -1;
                            hasEnemy.y |= !ally;
                        }

                        float3 d2 = p - s2;
                        bool3 check2 = d2 <= float3.zero;
                        if (check2.x && check2.y && check2.z)
                        {
                            bool ally = otherTeam.redTeam == team.ValueRO.redTeam;
                            allyCounts.z += ally ? 1 : -1;
                            hasEnemy.z |= !ally;
                        }

                        float3 d3 = p - s3;
                        bool3 check3 = d3 <= float3.zero;
                        if (check3.x && check3.y && check3.z)
                        {
                            bool ally = otherTeam.redTeam == team.ValueRO.redTeam;
                            allyCounts.w += ally ? 1 : -1;
                            hasEnemy.w |= !ally;
                        }
                    }
                }

                // choose best direction
                float3 chosen = s0;
                int best = -1;
                if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
                if (hasEnemy.y && allyCounts.y > best) { chosen = s1; best = allyCounts.y; }
                if (hasEnemy.z && allyCounts.z > best) { chosen = s2; best = allyCounts.z; }
                if (hasEnemy.w && allyCounts.w > best) { chosen = s3; best = allyCounts.w; }


                chosen.x = math.clamp(chosen.x, -SeaConfig.halfWidth, SeaConfig.halfWidth);
                chosen.z = math.clamp(chosen.z, -SeaConfig.halfHeight, SeaConfig.halfHeight);

                rotation.ValueRW.desiredPosition = chosen;
                Unity.Mathematics.Random rand = new Unity.Mathematics.Random(lcTimer.Seed);
                lcTimer.TimeLeft = rand.NextFloat(lcTimer.MinSecs, lcTimer.MaxSecs);
                lcTimer.Seed = rand.NextUInt();
                timer.ValueRW = lcTimer;


            }
            hits.Dispose();
        }
        

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}



[BurstCompile]
public partial struct CalcPositionTargetJob : IJobEntity
{
    public float dt;
    public CollisionFilter filter;

    [ReadOnly]
    public PhysicsWorld physicsWorld;

    [ReadOnly]
    public ComponentLookup<TeamComponent> teamLookup;
    [ReadOnly]
    public ComponentLookup<LocalTransform> transformLookup;

    

    void Execute(Entity e,  ref RotationComponent rotation, ref CooldownTimer timer)
    {
        timer.TimeLeft -= dt;
        if (timer.TimeLeft > 0f)
        {
            return;
        }
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        var transform = transformLookup[e];
        var team = teamLookup[e];
        float3 pos = transform.Position;
        float3 currentTarget = rotation.desiredPosition;

        //baldur kode
        float3 fwd = math.normalize(new float3(transform.Forward().x, 0, transform.Forward().z));
        float3 right = math.normalize(math.cross(new float3(0, 1, 0), fwd));

        float offset = ShipConfig.SenseOffset;

        float3 s0 = pos + fwd * offset; // forward
        float3 s1 = pos - right * offset; // left
        float3 s2 = pos + right * offset; // right
        float3 s3 = pos - fwd * offset; // back

        int4 allyCounts = 0;
        bool4 hasEnemy = false;

        var input = new PointDistanceInput
        {
            Position = pos,
            MaxDistance = ShipConfig.SenseRadius,
            Filter = filter
        };
        hits.Clear();
            
        if (physicsWorld.CalculateDistance(input, ref hits))
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var body  = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                Entity other = body.Entity;
                
                if (other == e)
                {
                    continue;
                }

                if (!transformLookup.HasComponent(other) || !teamLookup.HasComponent(other))
                {
                    continue;
                }
                var otherTransform = transformLookup[other];
                var otherTeam = teamLookup[other];

                float3 p = otherTransform.Position;

                // same sample logic as before
                float3 d0 = p - s0;
                bool3 check = d0 <= float3.zero;
                if (check.x && check.y && check.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.x += ally ? 1 : -1;
                    hasEnemy.x |= !ally;
                }

                float3 d1 = p - s1;
                bool3 check1 = d1 <= float3.zero;
                if (check1.x && check1.y && check1.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.y += ally ? 1 : -1;
                    hasEnemy.y |= !ally;
                }

                float3 d2 = p - s2;
                bool3 check2 = d2 <= float3.zero;
                if (check2.x && check2.y && check2.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.z += ally ? 1 : -1;
                    hasEnemy.z |= !ally;
                }

                float3 d3 = p - s3;
                bool3 check3 = d3 <= float3.zero;
                if (check3.x && check3.y && check3.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.w += ally ? 1 : -1;
                    hasEnemy.w |= !ally;
                }
            }
                // choose best direction
                float3 chosen = s0;
                int best = -1;
                if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
                if (hasEnemy.y && allyCounts.y > best) { chosen = s1; best = allyCounts.y; }
                if (hasEnemy.z && allyCounts.z > best) { chosen = s2; best = allyCounts.z; }
                if (hasEnemy.w && allyCounts.w > best) { chosen = s3; best = allyCounts.w; }

                rotation.desiredPosition = chosen;
                //Debug.Log("New target: " + rotation.ValueRW.desiredPosition);
                Unity.Mathematics.Random rand = new Unity.Mathematics.Random(timer.Seed);
                timer.TimeLeft = rand.NextFloat(timer.MinSecs, timer.MaxSecs);
                timer.Seed = rand.NextUInt();
                }
                hits.Dispose();
        }     
}