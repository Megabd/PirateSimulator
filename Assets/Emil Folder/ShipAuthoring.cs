using Unity.Entities;
using UnityEngine;

public class ShipAuthoring : MonoBehaviour
{
    public GameObject[] Cannons;
    public GameObject Mast;
    public GameObject Sail;

    class Baker : Baker<ShipAuthoring>
    {
        public override void Bake(ShipAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);


            var buffer = AddBuffer<CannonElement>(entity);

            foreach (var cannonGo in authoring.Cannons)
            {
                if (cannonGo == null) continue;
                buffer.Add(new CannonElement
                {
                    Cannon = GetEntity(cannonGo, TransformUsageFlags.Dynamic)
                });
            }

            AddComponent(entity, new Ship
            {
                Mast = GetEntity(authoring.Mast, TransformUsageFlags.Dynamic),
                Sail = GetEntity(authoring.Sail, TransformUsageFlags.Dynamic)
            });
        }
}

// A component that will be added to the root entity of every tank.
    public struct CannonElement : IBufferElementData
    {
        public Entity Cannon;
    }

    public struct Ship : IComponentData
    {
        public Entity Mast;
        public Entity Sail;
    }
}