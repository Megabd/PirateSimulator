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
                turnSpeed = 60.0f,
                desiredPosition = float3.zero,
                maxTurnAngle = 60.0f
            });

            AddComponent(entity, new TeamComponent { redTeam = true });
            AddComponent(entity, new CooldownTimer { TimeLeft = 1.0f, MinSecs = 1.0f, MaxSecs = 3.0f, Seed = 1 });
            AddComponent(entity, new CanonSenseComponent { senseDistance = 10.0f, cannonballSpeed = 3.0f });
            AddComponent(entity, new PrevPosComponent { PrePos = float3.zero });
            AddComponent(entity, new Aim { TimeLeft = 0.1f, Interval = 0.1f, HasTarget = false, TargetPosition = float3.zero });
        }
    }
}
