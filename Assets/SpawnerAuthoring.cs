using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class SpawnerAuthoring : MonoBehaviour
{
    [SerializeField] public GameObject ShipPrefab;
    public float Timer;





    public class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            /*SpawnerComponent sd = default;
            sd.Prefab = GetEntity(authoring.ShipPrefab);
            sd.Timer = authoring.Timer;
            sd.TimeToNextSpawn = authoring.Timer;
            AddComponent(sd);*/

            

            // Get the entity we’re baking into (the spawner itself)
            var entity = GetEntity(TransformUsageFlags.None);

            // Convert prefab GameObject into an entity reference
            var prefabEntity = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpawnerComponent
            {
                Prefab = prefabEntity,
                Timer = authoring.Timer,
                TimeToNextSpawn = authoring.Timer
            });


            
        }
    }
}
