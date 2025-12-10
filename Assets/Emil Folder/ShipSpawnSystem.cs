using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public partial struct ShipSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();
        int count = math.max(0, config.ShipCount);
        double elapsedTimeD = SystemAPI.Time.ElapsedTime;
        float elapsedTime = (float)elapsedTimeD;
        var entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.Instantiate(config.ShipPrefab, entities);
        var rng = Unity.Mathematics.Random.CreateFromIndex(1337u);
        uint seed = 1;
        bool team = true;
        for (int i = 0; i < count; i++)
        {
            if (team)
            {
                team = false;
            }
            else
            {
                team = true;
            }
            float2 xz = rng.NextFloat2(
                new float2(-SeaConfig.halfWidth, -SeaConfig.halfHeight),
                new float2(SeaConfig.halfWidth, SeaConfig.halfHeight));

            var pos = new float3(xz.x, 0f, xz.y);
            var tilt = quaternion.Euler(math.radians(90f), 0f, 0f);
            var yaw = quaternion.RotateY(0);
            quaternion fix = math.mul(yaw, tilt);
            em.SetComponentData(entities[i],
                LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
            em.SetComponentData(entities[i],
                new RotationComponent {turnSpeed = 60.0f, desiredPosition = new float3(0.0f, 0.0f, 0.0f), maxTurnAngle = 360.0f, startRotation = quaternion.identity});
            em.SetComponentData(entities[i], new TeamComponent { redTeam = team });
            em.SetComponentData(entities[i],
                new CooldownTimer { TimeLeft = 1.0f, MinSecs = 5.0f, MaxSecs = 15.0f, Seed = seed });

            //ship color
            var ship = em.GetComponentData<ShipAuthoring.Ship>(entities[i]);
            var sailEntity = ship.Sail;
            if (!em.HasComponent<URPMaterialPropertyBaseColor>(sailEntity))
            {
                em.AddComponent<URPMaterialPropertyBaseColor>(sailEntity);
            }
            float4 sailColor = team
                ? new float4(1f, 0f, 0f, 1f)   // red
                : new float4(0f, 0f, 1f, 1f);  // blue
            em.SetComponentData(sailEntity, new URPMaterialPropertyBaseColor { Value = sailColor });



            var cannonBuffer = em.GetBuffer<ShipAuthoring.CannonElement>(entities[i]);
            int j = 0;

            // Apply team component to each cannon entity
            foreach (var ele in cannonBuffer)
            {
                quaternion lol;
                quaternion test;
                if (j < 3)
                {
                    test = quaternion.Euler(0, 0, math.radians(-90f));
                    lol = math.mul(fix, test);
                }
                else
                {
                    test = quaternion.Euler(0, 0f, math.radians(-90f));
                    lol = math.mul(fix, test);
                }

                // Set team on the cannon
                em.SetComponentData(ele.Cannon, new TeamComponent { redTeam = team });

                // stagger Aim per cannon instance
                if (em.HasComponent<Aim>(ele.Cannon))
                {
                    var aim = em.GetComponentData<Aim>(ele.Cannon);

                    float rand01 = rng.NextFloat();                  // 0..1
                    aim.NextRaycastTime = elapsedTime + aim.RayCastInterval * rand01;

                    em.SetComponentData(ele.Cannon, aim);
                }

                j++;
            }

            seed += 1;
            //Debug.Log(team);
        }

        entities.Dispose();
    }
}
