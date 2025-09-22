using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float stickSensitivity = 1000f;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    private Vector2 lookInput;
    private Vector2 virtualScreenPos;

    private void Start()
    {
        InitializeCursorController();
    }

    private void OnDestroy()
    {
        lookAction.action.Disable();
    }

    private void Update()
    {
        UpdateCursorPosition();
    }

    private void InitializeCursorController()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        virtualScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        lookAction.action.Enable();
    }

    private void UpdateCursorPosition()
    {
        // Read input
        lookInput = lookAction.action.ReadValue<Vector2>();

        if (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
        {
            virtualScreenPos = Mouse.current.position.ReadValue();
        }
        else
        {
            virtualScreenPos += lookInput * stickSensitivity * Time.deltaTime;
        }

        // Clamp to camera viewport
        virtualScreenPos = ClampToCameraView(virtualScreenPos);

        // Raycast to determine cursor world position
        Ray ray = mainCamera.ScreenPointToRay(virtualScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            transform.position = hit.point;
        }
    }

    private Vector2 ClampToCameraView(Vector2 screenPos)
    {
        Vector2 viewportPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );

        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);

        return new Vector2(
            viewportPos.x * Screen.width,
            viewportPos.y * Screen.height
        );
    }

    public Vector3 GetCursorWorldPosition()
    {
        return transform.position;
    }
}
