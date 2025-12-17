using Unity.Entities;
using Unity.Mathematics;


public struct RotationComponent : IComponentData
{
    public float turnSpeed;
    // Position we want to turn towards
    public float3 desiredPosition;
    public float maxTurnAngle;
    public quaternion startRotation;
}

public struct TeamComponent : IComponentData
{
    public bool redTeam;
}

public struct CooldownTimer : IComponentData
{
    public float TimeLeft;
}

public struct PrevPosComponent : IComponentData
{
    public float3 PrePos;
}

public struct CollisionScanTimer : IComponentData
{
    public float TimeLeft;
    public float Interval;
}

public struct AvoidanceState : IComponentData
{
    public bool Active;
    public float3 Target;
}

public struct Aim : IComponentData
{
    public float NextRaycastTime;
    public float ShootTimeLeft; 

    public bool HasTarget; 
    public float3 TargetPosition;
}

public struct HealthComponent : IComponentData
{
    public int health;
    public int startingHealth;
}

public struct ShipSense : IComponentData
{
    public float ShipSpeed;
    public float SenseRadius;
    public float SenseOffset;
}

public struct CannonData : IComponentData
{
    public float SenseDistance;
    public float CannonballSpeed;
    public float CannonballLifeTime;
    public float ShootWarmupTime;
}

public struct CannonBalls : IComponentData
{
    public float3 Velocity;
    public float Lifetime;
    public float Radius;
}