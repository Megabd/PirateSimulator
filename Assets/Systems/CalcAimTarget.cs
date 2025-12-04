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
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,              // what the RAY "is"
            CollidesWith = 1 << 1,           // what it SHOULD hit
            GroupIndex = 0
        };

        // Cache lookups once per frame instead of using SystemAPI.GetComponent inside the loop
        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var speedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true);


        foreach (var (transform, rotation, team, sense, toWorld, timer) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>, RefRO<CooldownTimer>>())
        {
            //if(timer.ValueRO.TimeLeft > 0f) continue;

            float3 pos = toWorld.ValueRO.Position;
            float3 forward = transform.ValueRO.Forward();
            float3 bestTarget = float3.zero;

            float3 origin = new float3(pos.x, pos.y, pos.z);
            float3 direction = new float3(forward.x, forward.y, forward.z);
            float maxDistance = sense.ValueRO.senseDistance;

            var rayInput = new RaycastInput
            {
                Start = origin,
                End = pos + forward * maxDistance,
                Filter = filter
            };


            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                // hit.Position, hit.SurfaceNormal, hit.RigidBodyIndex, hit.Entity, etc.
                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                if (!teamLookup.HasComponent(hitEntity) ||
                    !transformLookup.HasComponent(hitEntity) ||
                    !speedLookup.HasComponent(hitEntity))
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


