using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static UnityEngine.Rendering.STP;

partial struct CalcPositionTarget : ISystem
{
    CollisionFilter filter;
    public Unity.Mathematics.Random rand;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex = 0
        };
        rand = new Unity.Mathematics.Random(255555211);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        float dt = SystemAPI.Time.DeltaTime;

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);

        var config = SystemAPI.GetSingleton<Config>();

        var job =
        new CalcPositionTargetJob
        {
            dt = dt,
            config = config,
            filter = filter,
            physicsWorld = physicsWorld,
            teamLookup = teamLookup,
            transformLookup = transformLookup,
            rand = rand
        };

        if (config.ScheduleParallel)
        {
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }

        else {
            job.Run();
        /*foreach (var (transform, rotation, team, timer) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRW<CooldownTimer>>())
        {
            timer.ValueRW.TimeLeft -= dt;
            if (timer.ValueRW.TimeLeft > 0f) 
            {
                continue;
            }
            // Positions samples

            float3 pos = transform.ValueRO.Position;
            float3 fwd = math.normalize(new float3(transform.ValueRO.Forward().x, 0, transform.ValueRO.Forward().z));
            float3 right = math.normalize(math.cross(math.forward(), fwd));

            float offset = ShipConfig.SenseOffset;
            float3 s0 = pos + fwd * offset; // forward
            float3 s1 = pos - right * offset; // left
            float3 s2 = pos + right * offset; // right
            float3 s3 = pos - fwd * offset; // back

            int4 allyCounts = 0;
            bool4 hasEnemy = false;

            float r = ShipConfig.SenseRadius;
            float r2 = r * r;

            foreach (var (otherTransform, health, otherTeam) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<HealthComponent>, RefRO<TeamComponent>>())
            {
                float3 p = otherTransform.ValueRO.Position;
                float3 d0 = p - s0; if (math.lengthsq(d0) <= r2) { allyCounts.x += otherTeam.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.x |= otherTeam.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d1 = p - s1; if (math.lengthsq(d1) <= r2) { allyCounts.y += otherTeam.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.y |= otherTeam.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d2 = p - s2; if (math.lengthsq(d2) <= r2) { allyCounts.z += otherTeam.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.z |= otherTeam.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d3 = p - s3; if (math.lengthsq(d3) <= r2) { allyCounts.w += otherTeam.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.w |= otherTeam.ValueRO.redTeam != team.ValueRO.redTeam; }
            }
            float3 chosen = s0;
            int best = -1;
            if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
            if (hasEnemy.y && allyCounts.y > best) {chosen = s1; best = allyCounts.y; }
            if (hasEnemy.z && allyCounts.z > best) {chosen = s2; best = allyCounts.z; }
            if (hasEnemy.w && allyCounts.w > best) {chosen = s3; best = allyCounts.w; }

            rotation.ValueRW.desiredPosition = chosen;
            //Unity.Mathematics.Random rand = new Unity.Mathematics.Random(timer.ValueRW.Seed);
            timer.ValueRW.TimeLeft = rand.NextFloat(5f, 10f);
            }*/
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}


// Calculates the target position for the ships. Every few seconds, it checkes enemies and allies in 4 directions, choosing one with most allies, but at least one enemies.
[BurstCompile]
public partial struct CalcPositionTargetJob : IJobEntity
{
    public float dt;
    public Unity.Mathematics.Random rand;
    public CollisionFilter filter;
    public Config config;

    [ReadOnly]
    public PhysicsWorld physicsWorld;
    [ReadOnly]
    public ComponentLookup<TeamComponent> teamLookup;
    [ReadOnly]
    public ComponentLookup<LocalTransform> transformLookup;

    void Execute(Entity e,  ref RotationComponent rotation, ref LocalToWorld toWorld, ref CooldownTimer timer)
    {
        timer.TimeLeft -= dt;
        if (timer.TimeLeft > 0f)
        {
            return;
        }
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        var transform = transformLookup[e];
        var team = teamLookup[e];
        float3 pos = transform.Position;
        float3 currentTarget = rotation.desiredPosition;

        float3 fwd = math.normalize(new float3(transform.Forward().x, 0, transform.Forward().z));
        float3 right = math.normalize(math.cross(new float3(0, 1, 0), fwd));

        float offset = ShipConfig.SenseOffset;

        float3 s0 = pos + fwd * offset; // forward
        float3 s1 = pos - right * offset; // left
        float3 s2 = pos + right * offset; // right
        float3 s3 = pos - fwd * offset; // back



        int4 allyCounts = 0;
        bool4 hasEnemy = false;

        var input = new PointDistanceInput
        {
            Position = pos,
            MaxDistance = ShipConfig.SenseRadius,
            Filter = filter
        };
        hits.Clear();
            
        if (physicsWorld.CalculateDistance(input, ref hits))
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var body  = physicsWorld.Bodies[hits[i].RigidBodyIndex];
                Entity other = body.Entity;
                
                if (other == e)
                {
                    continue;
                }

                if (!transformLookup.HasComponent(other) || !teamLookup.HasComponent(other))
                {
                    continue;
                }
                var otherTransform = transformLookup[other];
                var otherTeam = teamLookup[other];

                float3 p = otherTransform.Position;

                float3 d0 = p - s0;
                bool3 check = d0 <= float3.zero;
                if (check.x && check.y && check.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.x += ally ? 1 : -1;
                    hasEnemy.x |= !ally;
                }

                float3 d1 = p - s1;
                bool3 check1 = d1 <= float3.zero;
                if (check1.x && check1.y && check1.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.y += ally ? 1 : -1;
                    hasEnemy.y |= !ally;
                }

                float3 d2 = p - s2;
                bool3 check2 = d2 <= float3.zero;
                if (check2.x && check2.y && check2.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.z += ally ? 1 : -1;
                    hasEnemy.z |= !ally;
                }

                float3 d3 = p - s3;
                bool3 check3 = d3 <= float3.zero;
                if (check3.x && check3.y && check3.z)
                {
                    bool ally = otherTeam.redTeam == team.redTeam;
                    allyCounts.w += ally ? 1 : -1;
                    hasEnemy.w |= !ally;
                }
            }
                // choose best direction
                float3 chosen = new float3(0f, 0f, 1f);

                int best = -1;
                if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
                if (hasEnemy.y && allyCounts.y > best) { chosen = s1; best = allyCounts.y; }
                if (hasEnemy.z && allyCounts.z > best) { chosen = s2; best = allyCounts.z; }
                if (hasEnemy.w && allyCounts.w > best) { chosen = s3; best = allyCounts.w; }
                chosen.x = math.clamp(chosen.x, -config.MapSize, config.MapSize);
                chosen.z = math.clamp(chosen.z, -config.MapSize, config.MapSize);
                rotation.desiredPosition = chosen;
                timer.TimeLeft = rand.NextFloat(5f, 10f);
                }
                hits.Dispose();
        }     
}