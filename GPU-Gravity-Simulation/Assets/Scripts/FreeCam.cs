using UnityEngine;

public class FreeCam : MonoBehaviour
{
    Vector3 dragOrigin;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetMouseButton(0))
        {
            Vector3 diff = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position += diff;
        }
        
        Camera.main.orthographicSize = Mathf.Max(0.1f, Camera.main.orthographicSize - Input.mouseScrollDelta.y);
    }
}