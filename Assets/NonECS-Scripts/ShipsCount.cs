using TMPro;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class ShipsCountUI : MonoBehaviour
{
    TextMeshProUGUI textField;

    EntityQuery shipQuery;

    void Start()
    {

        textField = GetComponent<TextMeshProUGUI>();

        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        // Build a query that matches ALL ship entities .WithAll<TeamComponent>()
        shipQuery = new EntityQueryBuilder(Allocator.Temp)
            .Build(entityManager);
    }

    void FixedUpdate()
    {
        int count = shipQuery.CalculateEntityCount();
        textField.text = $"Ships: {count}";
    }
}
