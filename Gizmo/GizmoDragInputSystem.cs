using UnityEngine;
using UnityEngine.InputSystem;
using InputAction = UnityEngine.InputSystem.InputAction;

namespace EditorEnhanced.Gizmo;

internal interface IGizmoInput
{
    public void OnPointerEnter();
    public void OnPointerExit();
    public void OnDrag();
    public void OnBeginDrag();
    public void OnEndDrag();
}

public class GizmoDragInputSystem : MonoBehaviour
{
    private InputAction _clickAction;
    private IGizmoInput[] _currentGizmoDraggables;
    private GameObject _currentHoveredObject;
    private LayerMask _draggableLayer;
    private Plane _dragPlane;
    private bool _isDragging;
    private Camera _mainCamera;
    private Vector3 _offset;

    private InputAction _pointerPositionAction;

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

    private void Update()
    {
        if (_isDragging)
        {
            var mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
            var ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            float distance;
            if (_dragPlane.Raycast(ray, out distance)) transform.position = ray.GetPoint(distance) + _offset;

            foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnDrag();
        }
        else
        {
            HandleHover();
        }
    }

    private void OnDestroy()
    {
        if (_clickAction != null)
        {
            _clickAction.performed -= OnClickPerformed;
            _clickAction.canceled -= OnClickCanceled;
            _clickAction.Disable();
        }

        if (_pointerPositionAction != null) _pointerPositionAction.Disable();
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        var mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
        var ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, _draggableLayer)) return;
        if (hit.collider.gameObject != _currentHoveredObject) return;
        _isDragging = true;
        _dragPlane = new Plane(_mainCamera.transform.forward, transform.position);

        float distance;
        if (_dragPlane.Raycast(ray, out distance)) _offset = transform.position - ray.GetPoint(distance);

        foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnBeginDrag();
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (!_isDragging) return;
        _isDragging = false;
        foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnEndDrag();
    }

    private void HandleHover()
    {
        var mouseScreenPos = _pointerPositionAction.ReadValue<Vector2>();
        var ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _draggableLayer))
        {
            if (hit.collider.gameObject == _currentHoveredObject) return;
            if (_currentHoveredObject != null)
                foreach (var currentGizmoDraggable in _currentGizmoDraggables)
                    currentGizmoDraggable.OnPointerExit();

            _currentHoveredObject = hit.collider.gameObject;
            _currentGizmoDraggables = _currentHoveredObject.GetComponents<IGizmoInput>();
            foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnPointerEnter();
        }
        else
        {
            if (_currentHoveredObject == null) return;
            foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnPointerExit();
            _currentHoveredObject = null;
        }
    }
}