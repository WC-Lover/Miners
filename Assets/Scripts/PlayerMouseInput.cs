using UnityEngine;

public class PlayerMouseInput : MonoBehaviour
{

    public static PlayerMouseInput Instance;
    private Camera mainCamera;
    public Vector3 lastWorldPosition;

    private void Awake()
    {
        Instance = this;

        lastWorldPosition = Vector3.zero; 
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Get mouse position in screen coordinates
            Vector3 mousePos = Input.mousePosition;

            // Convert screen coordinates to world coordinates
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;

            // Maybe add layer mask to avoid pressing on top of buildings or resources with -> Y != 0
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the hit object is your plane
                if (hit.collider.gameObject.TryGetComponent<MeshCollider>(out var meshCollider))
                {
                    lastWorldPosition = hit.point;
                }
            }
        }
    }

}
