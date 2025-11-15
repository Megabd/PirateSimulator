using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

public class CannonAuthoring : MonoBehaviour
{
    class Baker : Baker<CannonAuthoring>
    {
        public override void Bake(CannonAuthoring authoring) {
        var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new RotationComponent { turnSpeed = 60.0f, desiredPosition = new float3(1.0f, 0.0f, 0.0f) });
        AddComponent(entity, new TeamComponent { redTeam = true });
        AddComponent(entity, new CooldownTimer { TimeLeft = 1.0f, MinSecs = 1.0f, MaxSecs = 3.0f, Seed = 1 });
        AddComponent(entity, new CanonSenseComponent { senseDistance = 1.0f, cannonballSpeed = 3.0f });
        //Debug.Log("here?");
    }
    }
}