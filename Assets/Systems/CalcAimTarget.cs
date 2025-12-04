using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct CalcAimTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1u << 0,
            CollidesWith = 1u << 1,
            GroupIndex = 0
        };

        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var speedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true);

        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, rotation, team, sense, toWorld, timer, Aim) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>, RefRO<CooldownTimer>, RefRW<Aim>>())
        {
            if (timer.ValueRO.TimeLeft > 1f)
            {
                Aim.ValueRW.HasTarget = false;
                rotation.ValueRW.desiredPosition = float3.zero;
                continue;
            }

            if (Aim.ValueRW.HasTarget)
            {
                rotation.ValueRW.desiredPosition = Aim.ValueRW.TargetPosition;
                continue;
            }

            Aim.ValueRW.TimeLeft -= dt;
            if (Aim.ValueRW.TimeLeft > 0f)
                continue;
            Aim.ValueRW.TimeLeft = Aim.ValueRW.Interval;

            float3 pos = toWorld.ValueRO.Position;
            float3 forward = transform.ValueRO.Forward();
            float3 bestTarget = float3.zero;

            RaycastInput rayInput = new RaycastInput
            {
                Start = pos,
                End = pos + forward * sense.ValueRO.senseDistance,
                Filter = filter
            };

            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

                if (!teamLookup.HasComponent(hitEntity) ||
                    !transformLookup.HasComponent(hitEntity) ||
                    !speedLookup.HasComponent(hitEntity))
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }

                var teamComp = teamLookup[hitEntity];

                if (teamComp.redTeam == team.ValueRO.redTeam)
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }

                var otherTransform = transformLookup[hitEntity];
                var speed = speedLookup[hitEntity];

                float projSpeed = sense.ValueRO.cannonballSpeed;
                float3 toTarget = otherTransform.Position - pos;
                float dist = math.length(toTarget);

                if (projSpeed > 0f && dist > 0f)
                {
                    float3 moveDir = otherTransform.Forward();
                    float3 targetVel = moveDir * speed.speed;
                    float timeToHit = dist / projSpeed;
                    float3 predictedPos = otherTransform.Position + targetVel * timeToHit;
                    bestTarget = predictedPos;

                    Aim.ValueRW.HasTarget = true;
                    Aim.ValueRW.TargetPosition = bestTarget;
                }
            }

            rotation.ValueRW.desiredPosition = bestTarget;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}
