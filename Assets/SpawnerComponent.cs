using UnityEngine;
using Unity.Entities;

    public struct SpawnerComponent : IComponentData
    {
        public Entity Prefab;
        public float Timer;
        public float TimeToNextSpawn;

    }
