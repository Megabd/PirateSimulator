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
}

public struct WindComponent : IComponentData 
{
    public float2 windDirection;
    public float power;
}


