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
        foreach (var (transform, rotation, coolDownTimer, CanonSense, WorldPos) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRW<CooldownTimer>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>>())
        {
            coolDownTimer.ValueRW.TimeLeft -= dt;
            //Debug.Log(coolDownTimer.ValueRW.TimeLeft);
            if (coolDownTimer.ValueRW.TimeLeft > 0f)
                continue; // not ready to shoot yet

            float3 origin = WorldPos.ValueRO.Position;
            float3 dir = transform.ValueRO.Forward();          // your "barrel" direction

            //float speed = cannonBall.ValueRO.projectileSpeed;

            //var cannonLTW = em.GetComponentData<LocalToWorld>(cannonEntity);

            var ball = em.Instantiate(config.CannonBallPrefab);

                // spawn at cannon position
            ballXform.Position = origin;
            em.SetComponentData(ball, ballXform);

                // baldur shit ass asset, i fucking hate u, i will find and molest you, god damnit
                //var dir = cannonLTW.Up;
                


                em.SetComponentData(ball, new CannonBalls
                {
                    Velocity = math.normalize(dir) * 10f,
                    Lifetime = 0f
                });

            // Reset rotation target after shooting
            //rotation.ValueRW.desiredPosition = float3.zero;

            // Reset cooldown with randomness
            var rand = new Unity.Mathematics.Random(coolDownTimer.ValueRW.Seed);
            coolDownTimer.ValueRW.TimeLeft = rand.NextFloat(coolDownTimer.ValueRW.MinSecs, coolDownTimer.ValueRW.MaxSecs);
            coolDownTimer.ValueRW.Seed = rand.NextUInt();
            //Debug.Log("Testing");

        }

    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
