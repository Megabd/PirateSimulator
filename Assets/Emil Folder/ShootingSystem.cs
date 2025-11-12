using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Shoot before transform systems so new balls don't flash at origin for a frame
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ShootingSystem : ISystem
{
    private float timer;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0f) return;
        timer = 2.0f;

        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();

        var ballXform = em.GetComponentData<LocalTransform>(config.CannonBallPrefab);

        foreach (var (shipTag, cannons) in
                 SystemAPI.Query<RefRO<ShipAuthoring.Ship>, DynamicBuffer<ShipAuthoring.CannonElement>>())
        {
            for (int i = 0; i < cannons.Length; i++)
            {
                var cannonEntity = cannons[i].Cannon;
                var cannonLTW = em.GetComponentData<LocalToWorld>(cannonEntity);

                var ball = em.Instantiate(config.CannonBallPrefab);

                // spawn at cannon position
                ballXform.Position = cannonLTW.Position;
                em.SetComponentData(ball, ballXform);

                // baldur shit ass asset, i fucking hate u, i will find and molest you, god damnit
                var dir = cannonLTW.Up;


                em.SetComponentData(ball, new CannonBalls
                {
                    Velocity = math.normalize(dir) * 10f,
                    Lifetime = 0f
                });
            }
        }
    }
}
