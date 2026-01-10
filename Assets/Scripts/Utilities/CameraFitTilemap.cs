using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitTilemap : MonoBehaviour
{
    public Renderer targetRenderer; // TilemapRenderer or SpriteRenderer

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    private void Start()
    {
        Fit();
    }

    public void Fit()
    {
        if (targetRenderer == null) return;

        Bounds b = targetRenderer.bounds;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = b.size.x / b.size.y;

        if (screenRatio >= targetRatio)
        {
            // fit height
            cam.orthographicSize = b.size.y / 2f;
        }
        else
        {
            // fit width
            float difference = targetRatio / screenRatio;
            cam.orthographicSize = b.size.y / 2f * difference;
        }

        cam.transform.position = new Vector3(
            b.center.x,
            b.center.y,
            cam.transform.position.z
        );
    }
}
