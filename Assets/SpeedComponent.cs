using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;


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

public struct CooldownTimer : IComponentData
{
    public float TimeLeft;
    public float MinSecs;
    public float MaxSecs;
    public uint Seed;
}

public struct PrevPosComponent : IComponentData
{
    public float3 PrePos;

    public uint Seed;
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
    public float RayCastInterval;     // how often we *want* to raycast
    public float NextRaycastTime;     // absolute time when we may raycast again
    public float ShootTimeLeft; // counts down after target acquired

    public bool HasTarget; 
    public float3 TargetPosition;
}


