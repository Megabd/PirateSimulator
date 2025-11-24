using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class CameraSpectator : MonoBehaviour
{
    private Camera cam;

    private World world;
    private EntityManager entityManager;
    private EntityQuery shipQuery;

    private Entity followedShip = Entity.Null;
    private bool followTeamRed = false;

    enum Mode { TopDown, FollowRed, FollowBlue }
    private Mode mode = Mode.TopDown;

    public float followDistance = 20f;
    public float followHeight = 10f;
    public float smoothSpeed = 3f;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        SetTopDownView();
    }

    void TrySetupWorld()
    {
        if (world != null && world.IsCreated) return;

        world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        entityManager = world.EntityManager;

        shipQuery = entityManager.CreateEntityQuery(
            typeof(ShipAuthoring.Ship),
            typeof(LocalTransform),
            typeof(TeamComponent)
        );
    }

    void Update()
    {
        TrySetupWorld();

        if (world == null || !world.IsCreated)
            return;

        HandleInput();

        switch (mode)
        {
            case Mode.TopDown:
                HandleTopDown();
                break;

            case Mode.FollowRed:
            case Mode.FollowBlue:
                HandleFollow();
                break;
        }
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            mode = Mode.FollowBlue;
            followTeamRed = false;
            PickRandomShip();
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            mode = Mode.FollowRed;
            followTeamRed = true;
            PickRandomShip();
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            mode = Mode.TopDown;
            followedShip = Entity.Null;
            SetTopDownView();
        }
    }

    void HandleTopDown()
    {
        cam.orthographic = true;
        cam.orthographicSize = 50f;

        transform.position = Vector3.Lerp(
            transform.position,
            new Vector3(0f, 100f, 0f),
            Time.deltaTime * 5f
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(90f, 0f, 0f),
            Time.deltaTime * 5f
        );
    }

    void HandleFollow()
    {
        if (followedShip == Entity.Null ||
            !entityManager.Exists(followedShip) ||
            !entityManager.HasComponent<LocalTransform>(followedShip))
        {
            PickRandomShip();
            return;
        }

        cam.orthographic = false;

        var transformData = entityManager.GetComponentData<LocalTransform>(followedShip);

        float3 shipPos = transformData.Position;
        quaternion shipRot = transformData.Rotation;

        float3 forward = math.mul(shipRot, new float3(0, 0, 1));

        float3 targetPos =
            shipPos
            - forward * followDistance
            + new float3(0, followHeight, 0);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt((Vector3)shipPos);
    }


    void PickRandomShip()
    {
        if (world == null || !world.IsCreated)
            return;

        using var entities = shipQuery.ToEntityArray(Allocator.Temp);

        if (entities.Length == 0)
        {
            followedShip = Entity.Null;
            return;
        }

        var candidates = new System.Collections.Generic.List<Entity>();

        foreach (var ship in entities)
        {
            if (!entityManager.Exists(ship)) continue;
            if (!entityManager.HasComponent<LocalTransform>(ship)) continue;

            var team = entityManager.GetComponentData<TeamComponent>(ship);
            if (team.redTeam == followTeamRed)
                candidates.Add(ship);
        }

        if (candidates.Count == 0)
        {
            followedShip = Entity.Null;
            return;
        }

        followedShip = candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }



    void SetTopDownView()
    {
        cam.orthographic = true;
        cam.orthographicSize = 50f;
        transform.position = new Vector3(0f, 100f, 0f);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
