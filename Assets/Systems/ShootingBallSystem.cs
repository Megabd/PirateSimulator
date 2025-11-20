using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

partial struct ShootingBallSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();

        var ballXform = em.GetComponentData<LocalTransform>(config.CannonBallPrefab);
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform,
             rotation,
             coolDownTimer,
             canonSense,
             worldPos,
             prevPos)
         in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<RotationComponent>,
                RefRW<CooldownTimer>,
                RefRO<CanonSenseComponent>,
                RefRO<LocalToWorld>,
                RefRW<PrevPosComponent>>())
        {


            float3 currentPos = worldPos.ValueRO.Position;
            float3 cannonVel = (currentPos - prevPos.ValueRO.PrePos) / dt; // world-space velocity
            prevPos.ValueRW.PrePos = currentPos; // store for next frame

            coolDownTimer.ValueRW.TimeLeft -= dt;
            if (coolDownTimer.ValueRW.TimeLeft > 0f) continue;
            if (rotation.ValueRO.desiredPosition.x == 0 && rotation.ValueRO.desiredPosition.y == 0 && rotation.ValueRO.desiredPosition.z == 0) continue;

            float3 origin = currentPos;
            float3 dir = math.normalize(worldPos.ValueRO.Forward);

            var ball = em.Instantiate(config.CannonBallPrefab);

            // spawn at cannon position
            ballXform.Position = origin;
            ballXform.Rotation = transform.ValueRO.Rotation; // local rotation of cannon entity
            em.SetComponentData(ball, ballXform);


            // ball inherits cannon velocity + its own shooting speed
            em.SetComponentData(ball, new CannonBalls
            {
                Velocity = cannonVel + dir * 10f,
                Lifetime = 0f
            });

            rotation.ValueRW.desiredPosition = float3.zero;

            // Reset cooldown with randomness
            var rand = new Unity.Mathematics.Random(coolDownTimer.ValueRW.Seed);
            coolDownTimer.ValueRW.TimeLeft =
                rand.NextFloat(coolDownTimer.ValueRW.MinSecs, coolDownTimer.ValueRW.MaxSecs);
            coolDownTimer.ValueRW.Seed = rand.NextUInt();
        }


    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
