using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.SocialPlatforms.Impl;
using NUnit.Framework.Internal;
using TMPro;
using static UnityEngine.UI.Image;
using UnityEngine;

partial struct CalcAimTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, rotation, team, sense, toWorld) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>>())
        {
            if (!rotation.ValueRO.desiredPosition.Equals(float3.zero))
            {
                continue;
            }

            float3 pos = toWorld.ValueRO.Position;
            float senseDistSq = sense.ValueRO.senseDistance * sense.ValueRO.senseDistance;
            float projSpeed = sense.ValueRO.cannonballSpeed;
            float3 bestTarget = float3.zero;
            float bestDistSq = float.MaxValue;
            float3 forward = transform.ValueRO.Forward();


            foreach (var (otherTransform, speed, otherTeam, shipSense, othertoWorld) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SpeedComponent>, RefRO<TeamComponent>, RefRO<ShipSenseComponent>, RefRO<LocalToWorld>> ())
            {

                bool isEnemy = otherTeam.ValueRO.redTeam != team.ValueRO.redTeam;
                float3 otherPos = otherTransform.ValueRO.Position;

                if (!isEnemy)
                    continue; // same team, ignore  
                              // Not in range, ignore, or too close  
                float3 toTarget = otherPos - pos;
                float distSq = math.lengthsq(toTarget);
                if (distSq < 1e-6f || distSq > senseDistSq)
                    continue;

                float3 dir = toTarget * math.rsqrt(distSq);
                float dot = math.dot(forward, dir);
                // if not in front, ignore  
                if (dot < 0.95)
                    continue;

                // Found an enemy in range, see if best  
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    // Predictive aim, no wind or own movement considered  
                    float dist = math.sqrt(distSq);
                    float3 moveDir = otherTransform.ValueRO.Forward();
                    float3 targetVel = moveDir * speed.ValueRO.speed;
                    float timeToHit = dist / projSpeed;
                    float3 predictedPos = otherPos + targetVel * timeToHit;
                    bestTarget = predictedPos;
                }
            }
            // Rotate to best target found (or back to 0 if none)  
            rotation.ValueRW.desiredPosition = bestTarget;
          //Debug.Log("End");
          
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
