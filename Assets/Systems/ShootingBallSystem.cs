using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

partial struct ShootingBallSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, rotation, coolDownTimer, cannonBall) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRW<CooldownTimer>, RefRO<CannonComponent>>())
        {
            coolDownTimer.ValueRW.TimeLeft -= dt;
            if (coolDownTimer.ValueRW.TimeLeft > 0f)
                continue; // not ready to shoot yet

            float3 origin = transform.ValueRO.Position;
            float3 dir = transform.ValueRO.Up();          // your "barrel" direction

            float speed = cannonBall.ValueRO.projectileSpeed;

            // SpawnCannonball(origin, dir * speed);

            // Reset rotation target after shooting
            rotation.ValueRW.desiredPosition = float3.zero;

            // Reset cooldown with randomness
            var rand = new Random(coolDownTimer.ValueRW.Seed);
            coolDownTimer.ValueRW.TimeLeft = rand.NextFloat(coolDownTimer.ValueRW.MinSecs, coolDownTimer.ValueRW.MaxSecs);
            coolDownTimer.ValueRW.Seed = rand.NextUInt();

        }

    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
