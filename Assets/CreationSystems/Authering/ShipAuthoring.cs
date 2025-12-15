using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ShipAuthoring : MonoBehaviour
{
    public GameObject[] Cannons;
    public GameObject Mast;
    public GameObject Sail;
    public GameObject EDBoat;


    class Baker : Baker<ShipAuthoring>
    {

        public override void Bake(ShipAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            var buffer = AddBuffer<CannonElement>(entity);

            foreach (var cannonGo in authoring.Cannons)
            {
                if (cannonGo == null) continue;
                var cannon = GetEntity(cannonGo, TransformUsageFlags.Dynamic);
                buffer.Add(new CannonElement
                {
                    Cannon = cannon
                });
            }

            AddComponent(entity, new Ship
            {
                Mast = GetEntity(authoring.Mast, TransformUsageFlags.Dynamic),
                Sail = GetEntity(authoring.Sail, TransformUsageFlags.Dynamic),
                EDBoat = GetEntity(authoring.EDBoat, TransformUsageFlags.Dynamic)
            });
            AddComponent(entity, new RotationComponent { turnSpeed = 60.0f, desiredPosition = new float3(1.0f, 0.0f, 1.0f), maxTurnAngle = 360.0f});
            AddComponent(entity, new HealthComponent { health = 50, startingHealth = 50});
            AddComponent(entity, new TeamComponent { redTeam = true });
            AddComponent(entity, new CooldownTimer { TimeLeft = 1.0f });
            AddComponent(entity, new CollisionScanTimer{ TimeLeft = 2f, Interval = 2f});
            AddComponent(entity, new AvoidanceState{Active = false,Target = float3.zero});
        }
    }

    public struct CannonElement : IBufferElementData
    {
        public Entity Cannon;
    }

    public struct Ship : IComponentData
    {
        public Entity Mast;
        public Entity Sail;
        public Entity EDBoat;
    }
}