using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ShootingSystem : ISystem
{
    private float timer;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0)
        {
            return;
        }
        timer = 0.3f;

        var config = SystemAPI.GetSingleton<Config>();

        var ballTransform = state.EntityManager.GetComponentData<LocalTransform>(config.CannonBallPrefab);

        foreach (var (tank, transform, color) in
                 SystemAPI.Query<RefRO<Ship>, RefRO<LocalToWorld>, RefRO<URPMaterialPropertyBaseColor>>())
        {
            Entity cannonBallEntity = state.EntityManager.Instantiate(config.CannonBallPrefab);

            state.EntityManager.SetComponentData(cannonBallEntity, color.ValueRO);

            var cannonTransform = state.EntityManager.GetComponentData<LocalToWorld>(tank.ValueRO.Cannon);
            ballTransform.Position = cannonTransform.Position;

            state.EntityManager.SetComponentData(cannonBallEntity, ballTransform);

            state.EntityManager.SetComponentData(cannonBallEntity, new CannonBall
            {
                Velocity = math.normalize(cannonTransform.Up) * 12.0f
            });
        }
    }
}