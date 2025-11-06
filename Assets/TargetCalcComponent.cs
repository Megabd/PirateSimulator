using Unity.Entities;

public struct TargetCalcComponent : IComponentData
{
    float senseRadius;          // who counts as nearby
    float sampleOffset;
    float targetChangeTimer;
    float timer;
    float fixedY;

    //Rewrote target vector into 3 floats, might be a smarter way to handle this.
    float targetX;
    float targetY;
    float targetZ; 
}
