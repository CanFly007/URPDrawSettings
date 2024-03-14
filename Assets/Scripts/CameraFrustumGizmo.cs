using UnityEngine;

[ExecuteInEditMode]
public class CameraFrustumGizmo : MonoBehaviour
{
    public Color frustumColor = Color.green;
    public float frustumLineDuration = 0.02f;

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void OnDrawGizmos()
    {
        if (_camera == null)
            return;

        Gizmos.color = frustumColor;
        Matrix4x4 temp = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        if (_camera.orthographic)
        {
            float spread = _camera.farClipPlane - _camera.nearClipPlane;
            float center = (_camera.farClipPlane + _camera.nearClipPlane) * 0.5f;
            Gizmos.DrawWireCube(new Vector3(0, 0, center), new Vector3(_camera.orthographicSize * 2 * _camera.aspect, _camera.orthographicSize * 2, spread));
        }
        else
        {
            Gizmos.DrawFrustum(Vector3.zero, _camera.fieldOfView, _camera.farClipPlane, _camera.nearClipPlane, _camera.aspect);
        }

        Gizmos.matrix = temp;
    }

    void Update()
    {
        DrawFrustum();
    }

    private void DrawFrustum()
    {
        Vector3[] frustumCorners = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        Vector3 worldSpaceCorner1 = _camera.transform.TransformVector(frustumCorners[0]);
        Vector3 worldSpaceCorner2 = _camera.transform.TransformVector(frustumCorners[1]);
        Vector3 worldSpaceCorner3 = _camera.transform.TransformVector(frustumCorners[2]);
        Vector3 worldSpaceCorner4 = _camera.transform.TransformVector(frustumCorners[3]);

        Debug.DrawLine(_camera.transform.position, worldSpaceCorner1, frustumColor, frustumLineDuration);
        Debug.DrawLine(_camera.transform.position, worldSpaceCorner2, frustumColor, frustumLineDuration);
        Debug.DrawLine(_camera.transform.position, worldSpaceCorner3, frustumColor, frustumLineDuration);
        Debug.DrawLine(_camera.transform.position, worldSpaceCorner4, frustumColor, frustumLineDuration);
        Debug.DrawLine(worldSpaceCorner1, worldSpaceCorner2, frustumColor, frustumLineDuration);
        Debug.DrawLine(worldSpaceCorner2, worldSpaceCorner3, frustumColor, frustumLineDuration);
        Debug.DrawLine(worldSpaceCorner3, worldSpaceCorner4, frustumColor, frustumLineDuration);
        Debug.DrawLine(worldSpaceCorner4, worldSpaceCorner1, frustumColor, frustumLineDuration);
    }
}