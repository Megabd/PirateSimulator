using UnityEngine;

[ExecuteAlways]
public class CameraTopDown100 : MonoBehaviour
{
    void OnEnable()
    {
        const float halfSize = 50f;

        var cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = halfSize;   // fits 100 units vertically
        transform.position = new Vector3(0f, 100f, 0f);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
