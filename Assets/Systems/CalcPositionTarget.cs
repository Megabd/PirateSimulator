using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.SocialPlatforms.Impl;
using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;

partial struct CalcPositionTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, rotation, team, sense, timer) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<ShipSenseComponent>, RefRW<CooldownTimer>>())
        {
            timer.ValueRW.TimeLeft -= dt;
            if (timer.ValueRW.TimeLeft > 0f) continue;

            float3 pos = transform.ValueRO.Position;
            float3 fwd = math.normalize(new float3(transform.ValueRO.Up().x, 0, transform.ValueRO.Up().z));
            float3 right = math.normalize(math.cross(math.up(), fwd));

            float offset = sense.ValueRO.sampleOffset;

            // Positions samples
            float3 s0 = pos + fwd * offset; // forward
            float3 s1 = pos - right * offset; // left
            float3 s2 = pos + right * offset; // right
            float3 s3 = pos - fwd * offset; // back

            int4 allyCounts = 0;
            bool4 hasEnemy = false;
            float r = sense.ValueRO.sampleRadius;
            float r2 = r * r;
            // Replace with physics hit //This also sees cannons currently, might wanna fix this?
            foreach (var (otherTransform, otherTeamComponent) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<TeamComponent>>())
            {
                float3 p = otherTransform.ValueRO.Position;
                float3 d0 = p - s0; if (math.lengthsq(d0) <= r2) { allyCounts.x += otherTeamComponent.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.x |= otherTeamComponent.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d1 = p - s1; if (math.lengthsq(d1) <= r2) { allyCounts.y += otherTeamComponent.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.y |= otherTeamComponent.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d2 = p - s2; if (math.lengthsq(d2) <= r2) { allyCounts.z += otherTeamComponent.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.z |= otherTeamComponent.ValueRO.redTeam != team.ValueRO.redTeam; }
                float3 d3 = p - s3; if (math.lengthsq(d3) <= r2) { allyCounts.w += otherTeamComponent.ValueRO.redTeam == team.ValueRO.redTeam ? 1 : -1; hasEnemy.w |= otherTeamComponent.ValueRO.redTeam != team.ValueRO.redTeam; }
            }

            float3 chosen = s0;
            int best = -1;

            //Debug.Log("Allies: "+ allyCounts);
            //Debug.Log("Enemies: " + hasEnemy);

            if (hasEnemy.x && allyCounts.x > best) { chosen = s0; best = allyCounts.x; }
            else if (hasEnemy.y && allyCounts.y > best) {chosen = s1; best = allyCounts.y; }
            else if (hasEnemy.z && allyCounts.z > best) {chosen = s2; best = allyCounts.z; }
            else if (hasEnemy.w && allyCounts.w > best) {chosen = s3; best = allyCounts.w; }

            rotation.ValueRW.desiredPosition = chosen;

            //Debug.Log(chosen);

            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(timer.ValueRW.Seed);
            timer.ValueRW.TimeLeft = rand.NextFloat(timer.ValueRW.MinSecs, timer.ValueRW.MaxSecs);
            timer.ValueRW.Seed = rand.NextUInt();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
