using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct CalcAimTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state){}

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, rotation, team, sense, toWorld) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>>())
        {
            //old shit ass code 400 ms
            /*if (!rotation.ValueRO.desiredPosition.Equals(float3.zero))
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
          //Debug.Log("End");*/

            //40 ms nice clean code
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            if (!rotation.ValueRO.desiredPosition.Equals(float3.zero)) continue;

            float3 pos = toWorld.ValueRO.Position;
            float3 forward = transform.ValueRO.Forward();
            float3 bestTarget = float3.zero;

            float3 origin = new float3(pos.x, pos.y, pos.z);
            float3 direction = new float3(forward.x, forward.y, forward.z);
            float maxDistance = sense.ValueRO.senseDistance;

            var rayInput = new RaycastInput
            {
                Start = origin,
                End = maxDistance,
                Filter = new CollisionFilter
                {
                    BelongsTo = 1 << 0,              // what the RAY "is"
                    CollidesWith = 1 << 1,           // what it SHOULD hit
                    GroupIndex = 0
                }
            };


            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                // hit.Position, hit.SurfaceNormal, hit.RigidBodyIndex, hit.Entity, etc.
                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                if (!SystemAPI.HasComponent<TeamComponent>(hitEntity) || !SystemAPI.HasComponent<LocalTransform>(hitEntity) || !SystemAPI.HasComponent<SpeedComponent>(hitEntity))
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }

                var teamComp = SystemAPI.GetComponent<TeamComponent>(hitEntity);

                if(teamComp.redTeam == team.ValueRO.redTeam)
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }
                


                var otherpos = SystemAPI.GetComponent<LocalTransform>(hitEntity);
                var speed = SystemAPI.GetComponent<SpeedComponent>(hitEntity);
                float projSpeed = sense.ValueRO.cannonballSpeed;
                float3 toTarget = otherpos.Position - pos;
                float distSq = math.lengthsq(toTarget);
                float dist = math.sqrt(distSq);

                float3 moveDir = otherpos.Forward();
                float3 targetVel = moveDir * speed.speed;
                float timeToHit = dist / projSpeed;
                float3 predictedPos = otherpos.Position + targetVel * timeToHit;
                bestTarget = predictedPos;

            }
            rotation.ValueRW.desiredPosition = bestTarget;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}


