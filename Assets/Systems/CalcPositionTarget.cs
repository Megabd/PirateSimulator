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
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        float dt = SystemAPI.Time.DeltaTime;

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        //var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true); error here

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex = 0
        };

        var config = SystemAPI.GetSingleton<Config>();

        float halfWidth = config.MapSize.x * 0.5f - 10f;
        float halfHeight = config.MapSize.y * 0.5f - 10f;

        foreach (var (transform, rotation, team, sense, timer, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<ShipSenseComponent>, RefRW<CooldownTimer>>().WithEntityAccess())
        {

            timer.ValueRW.TimeLeft -= dt;
            if (timer.ValueRW.TimeLeft > 0f) continue;

            float3 pos = transform.ValueRO.Position;
            float3 currentTarget = rotation.ValueRO.desiredPosition;

            /*
            float arriveThreshold = 1f; //so it can actually reach the target
            float arriveThresholdSq = arriveThreshold * arriveThreshold;
            
            bool hasTarget = !math.all(currentTarget == float3.zero);
            if (math.distancesq(pos, currentTarget) > arriveThresholdSq) continue;*/

            //baldur kode
            float3 fwd = math.normalize(new float3(transform.ValueRO.Forward().x, 0, transform.ValueRO.Forward().z));
            float3 right = math.normalize(math.cross(new float3(0, 1, 0), fwd));

            float offset = sense.ValueRO.sampleOffset;
            float r = sense.ValueRO.sampleRadius;

            float3 s0 = pos + fwd * offset; // forward
            float3 s1 = pos - right * offset; // left
            float3 s2 = pos + right * offset; // right
            float3 s3 = pos - fwd * offset; // back

            int4 allyCounts = 0;
            bool4 hasEnemy = false;

            // one big circle
            float bigRadius = offset + r;

            var input = new PointDistanceInput
            {
                Position = pos,
                MaxDistance = bigRadius,
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

                    // same sample logic as before
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
            /*
            if (!hasEnemy.x && !hasEnemy.y && !hasEnemy.z && !hasEnemy.w)
            {
                float3 localTarget = float3.zero;
                if (ltwLookup.HasComponent(entity))
                {
                    var ltw = ltwLookup[entity].Value;
                    localTarget = math.transform(math.inverse(ltw), float3.zero);
                }
                rotation.ValueRW.desiredPosition = localTarget;
                continue;
            }
            */
            // choose best direction
            float3 chosen = s0;
            int best = -1;
            if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
            if (hasEnemy.y && allyCounts.y > best) { chosen = s1; best = allyCounts.y; }
            if (hasEnemy.z && allyCounts.z > best) { chosen = s2; best = allyCounts.z; }
            if (hasEnemy.w && allyCounts.w > best) { chosen = s3; best = allyCounts.w; }


            chosen.x = math.clamp(chosen.x, -halfWidth, halfWidth);
            chosen.z = math.clamp(chosen.z, -halfHeight, halfHeight);

            rotation.ValueRW.desiredPosition = chosen;
            //Debug.Log("New target: " + rotation.ValueRW.desiredPosition);
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(timer.ValueRW.Seed);
            timer.ValueRW.TimeLeft = rand.NextFloat(timer.ValueRW.MinSecs, timer.ValueRW.MaxSecs);
            timer.ValueRW.Seed = rand.NextUInt();


        }
        
        hits.Dispose();

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
