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

partial struct AvoidShipCollisionSystem : ISystem
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

        foreach (var (transform, rotation, sense, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<ShipSenseComponent>>().WithEntityAccess())
        {

            float3 pos = transform.ValueRO.Position;

            //baldur kode
            float3 fwd = math.normalize(new float3(transform.ValueRO.Forward().x, 0, transform.ValueRO.Forward().z));

            var input = new PointDistanceInput
            {
                Position = pos,
                MaxDistance = 7.0f,
                Filter = new CollisionFilter
                {
                    BelongsTo = 1 << 0,
                    CollidesWith = 1 << 1,
                    GroupIndex = 0
                }
            };
            hits.Clear();
            
            if (physicsWorld.CalculateDistance(input, ref hits))
            {
                float distance = 100f;
                float3 otherPos = float3.zero;
                for (int i = 0; i < hits.Length; i++)
                {
                    var body  = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                    Entity other = body.Entity;
                    
                    if (other == entity)
                        continue;
                    if (!SystemAPI.HasComponent<ShipSenseComponent>(other))
                        continue;

                    var otherTransform = SystemAPI.GetComponent<LocalTransform>(other);
                    var otherRotation = SystemAPI.GetComponent<RotationComponent>(other);

                    float3 p = otherTransform.Position;
                    if (math.lengthsq(pos-p) < distance)
                    {
                        distance = math.lengthsq(pos-p);
                        otherPos = otherRotation.desiredPosition;
                    }
                }
                float3 newTarget = otherPos-pos;
                float2 f = new float2(fwd.x, fwd.z);
                float2 o = new float2(newTarget.x, newTarget.z); 
                float cross = f.x * o.y - f.y * o.x;
                float len = math.length(o); 
                o = o /= len;
                float rad = 0;
                if (cross > 0)
                {
                     rad = math.radians(-45.0f);
                }
                else
                {
                    rad = math.radians(45.0f);
                }
               
                float cos = math.cos(rad);
                float sin = math.sin(rad);
                float2 rot = new float2(o.x * cos - o.y * sin, o.x * sin + o.y * cos);
                float dist = math.length(otherPos - pos);
                rotation.ValueRW.desiredPosition = pos + new float3(rot.x, 0f, rot.y) * dist;
                //Debug.Log("Test");
            }

            


        }
        
        hits.Dispose();

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
