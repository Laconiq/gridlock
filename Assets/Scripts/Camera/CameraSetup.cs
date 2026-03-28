using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSetup : MonoBehaviour
{
    void Awake()
    {
        var cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 14f;

        transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        transform.position = transform.rotation * Vector3.back * 50f;
    }
}
