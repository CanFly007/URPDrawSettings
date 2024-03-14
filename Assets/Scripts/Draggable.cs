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
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            int layerMask = 1 << LayerMask.NameToLayer("Player");

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                if (hit.collider.gameObject.GetComponent<Draggable>() != null)
                {
                    isDragging = true;
                    zCoordinate = mainCamera.WorldToScreenPoint(hit.collider.gameObject.transform.position).z;
                    offset = hit.collider.gameObject.transform.position - GetMouseWorldPos();
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
                // Drop the object
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