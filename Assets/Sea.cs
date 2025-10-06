using UnityEngine;

public class Sea : MonoBehaviour
{
    [SerializeField] GameObject shipPrefab;
    [SerializeField] int shipAmount = 20;

    private float hx;
    private float hy;
    void Start()
    {
        hx = transform.lossyScale.x * 0.4f;
        hy = transform.lossyScale.y * 0.4f;
        for (int i = 0; i < shipAmount; i++)
        {
            Vector3 pos = GetRandomPointInSea();
            Instantiate(shipPrefab, pos, shipPrefab.transform.rotation);
        }
    }


    public Vector3 GetRandomPointInSea()
    {
        float x = Random.Range(-hx, hx);
        float z = Random.Range(-hy, hy);
        return new Vector3(transform.position.x + x, transform.position.y + 0.1f, transform.position.z + z);
    }
}
