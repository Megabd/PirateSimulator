using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

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
        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();

        var ballXform = em.GetComponentData<LocalTransform>(config.CannonBallPrefab);
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform,
             rotation,
             canonSense,
             aim,
             worldPos,
             prevPos)
         in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<RotationComponent>,
                RefRO<CanonSenseComponent>,
                RefRW<Aim>,
                RefRO<LocalToWorld>,
                RefRW<PrevPosComponent>>())
        {


            float3 currentPos = worldPos.ValueRO.Position;
            float3 cannonVel = (currentPos - prevPos.ValueRO.PrePos) / dt; // world-space velocity
            prevPos.ValueRW.PrePos = currentPos; // store for next frame

            // No target, no warmup, no shot
            if (!aim.ValueRO.HasTarget)
            {
                aim.ValueRW.ShootTimeLeft = 0f;
                continue;
            }

            // Count down the warmup timer
            aim.ValueRW.ShootTimeLeft -= dt;
            if (aim.ValueRW.ShootTimeLeft > 0f)
            {
                // still winding up, don't shoot yet
                continue;
            }

            var ball = em.Instantiate(config.CannonBallPrefab);

            // spawn at cannon position
            ballXform.Position = currentPos;
            ballXform.Rotation = transform.ValueRO.Rotation; // local rotation of cannon entity
            em.SetComponentData(ball, ballXform);


            // ball inherits cannon velocity + its own shooting speed
            float3 dir = math.normalize(worldPos.ValueRO.Forward);
            em.SetComponentData(ball, new CannonBalls
            {
                Velocity = cannonVel + dir * canonSense.ValueRO.cannonballSpeed,
                Lifetime = 0f,
                Radius = 0.5f //canonball hitbox
            });

            aim.ValueRW.HasTarget = false;
            rotation.ValueRW.desiredPosition = float3.zero;
        }


    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
