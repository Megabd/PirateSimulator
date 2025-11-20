using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

public struct SpeedComponent : IComponentData
{
    public float speed;
}

public struct RotationComponent : IComponentData
{
    public float turnSpeed;
    // Position we want to turn towards
    public float3 desiredPosition;
    public float maxTurnAngle;
    public quaternion startRotation;
}

public struct WindComponent : IComponentData 
{
    public float2 windDirection;
    public float power;
}

public struct TeamComponent : IComponentData
{
    public bool redTeam;
}

public struct ShipSenseComponent : IComponentData
{
    public float sampleOffset;
    public float sampleRadius;
}

public struct CanonSenseComponent : IComponentData
{
    public float senseDistance;
    public float cannonballSpeed;

}

public struct CooldownTimer : IComponentData
{
    public float TimeLeft;
    public float MinSecs;
    public float MaxSecs;
    public uint Seed;
}

public struct CannonComponent : IComponentData
{
    public Entity projectilePrefab;
    public float projectileSpeed;
    public float lifeTime;
}

public struct PrevPosComponent : IComponentData
{
    public float3 PrePos;
}






