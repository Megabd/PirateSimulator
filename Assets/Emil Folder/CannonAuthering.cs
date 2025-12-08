using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CannonAuthoring : MonoBehaviour
{
    class Baker : Baker<CannonAuthoring>
    {
        public override void Bake(CannonAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            // TAKE THE PREFAB'S INITIAL ROTATION AS START ROTATION
            quaternion startRot = authoring.transform.rotation;

            AddComponent(entity, new RotationComponent
            {
                startRotation = startRot,
                turnSpeed = 120.0f,
                desiredPosition = float3.zero,
                maxTurnAngle = 60.0f
            });

            AddComponent(entity, new TeamComponent { redTeam = true });
            AddComponent(entity, new CanonSenseComponent { senseDistance = 20.0f, cannonballSpeed = 10.0f });
            AddComponent(entity, new PrevPosComponent { PrePos = float3.zero });
            AddComponent(entity, new Aim { RayCastTimeLeft = 0.1f, RayCastInterval = 0.1f, HasTarget = false, TargetPosition = float3.zero, ShootWarmupTime = 0.5f, ShootTimeLeft = 0.5f });
        }
    }
}
