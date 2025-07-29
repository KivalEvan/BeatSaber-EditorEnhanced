using UnityEngine;
using UnityEngine.InputSystem;
using InputAction = UnityEngine.InputSystem.InputAction;

namespace EditorEnhanced.Gizmo;

public class GizmoDragInputSystem : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector3 _offset;
    private bool _isDragging;
    private Plane _dragPlane;
    private GameObject _currentHoveredObject;
    private GizmoDraggable _currentGizmoDraggable;
    private LayerMask _draggableLayer;

    private InputAction _pointerPositionAction;
    private InputAction _clickAction;


    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Plugin.Log.Error("Main Camera not found.");
            return;
        }

        _draggableLayer = LayerMask.GetMask("Event");

        _clickAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);
        _clickAction.performed += OnClickPerformed;
        _clickAction.canceled += OnClickCanceled;
        _clickAction.Enable();

        _pointerPositionAction = new InputAction(binding: "<Mouse>/position", type: InputActionType.Value,
            expectedControlType: "Vector2");
        _pointerPositionAction.Enable();
    }

    private void OnDestroy()
    {
        if (_clickAction != null)
        {
            _clickAction.performed -= OnClickPerformed;
            _clickAction.canceled -= OnClickCanceled;
            _clickAction.Disable();
        }

        if (_pointerPositionAction != null)
        {
            _pointerPositionAction.Disable();
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        Vector2 mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, _draggableLayer)) return;
        if (hit.collider.gameObject != _currentHoveredObject) return;
        _isDragging = true;
        _dragPlane = new Plane(_mainCamera.transform.forward, transform.position);

        float distance;
        if (_dragPlane.Raycast(ray, out distance))
        {
            _offset = transform.position - ray.GetPoint(distance);
        }

        if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnBeginDrag();
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (!_isDragging) return;
        _isDragging = false;
        if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnEndDrag();
    }

    private void Update()
    {
        if (_isDragging)
        {
            Vector2 mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            float distance;
            if (_dragPlane.Raycast(ray, out distance))
            {
                transform.position = ray.GetPoint(distance) + _offset;
            }

            if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnDrag();
        }
        else
        {
            HandleHover();
        }
    }

    private void HandleHover()
    {
        Vector2 mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _draggableLayer))
        {
            if (hit.collider.gameObject == _currentHoveredObject) return;
            if (_currentHoveredObject != null)
            {
                if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnPointerExit();
            }

            _currentHoveredObject = hit.collider.gameObject;
            _currentGizmoDraggable = _currentHoveredObject.GetComponent<GizmoDraggable>();
            if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnPointerEnter();
        }
        else
        {
            if (_currentHoveredObject == null) return;
            if (_currentGizmoDraggable != null) _currentGizmoDraggable.OnPointerExit();
            _currentHoveredObject = null;
        }
    }
}