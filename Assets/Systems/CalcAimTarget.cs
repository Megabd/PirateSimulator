using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.SocialPlatforms.Impl;
using NUnit.Framework.Internal;
using TMPro;

partial struct CalcAimTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, rotation, team, sense, timer) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<ShipSenseComponent>, RefRW<RetargetTimer>>())
        {
            
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
