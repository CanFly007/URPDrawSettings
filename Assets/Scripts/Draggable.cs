using UnityEngine;

public class Draggable : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private float zCoordinate;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isDragging && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            int layerMask = 1 << LayerMask.NameToLayer("Player");

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                // Check if the hit object is this GameObject
                if (hit.collider.gameObject == gameObject)
                {
                    isDragging = true;
                    zCoordinate = mainCamera.WorldToScreenPoint(transform.position).z;
                    offset = transform.position - GetMouseWorldPos();
                }
            }
        }

        if (isDragging)
        {
            if (Input.GetMouseButton(0))
            {
                transform.position = GetMouseWorldPos() + offset;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoordinate;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}