using UnityEngine;
using UnityEngine.InputSystem;
using InputAction = UnityEngine.InputSystem.InputAction;

namespace EditorEnhanced.Gizmo;

internal interface IGizmoInput
{
   public bool IsDragging { get; set; }
   public void OnPointerEnter();
   public void OnPointerExit();
   public void OnDrag();
   public void OnMouseClick();
   public void OnMouseRelease();
}

internal interface IGizmoScrollInput
{
   public void OnScroll(float delta);
}

public class GizmoDragInputSystem : MonoBehaviour
{
   private InputAction _clickAction;
   private IGizmoInput[] _currentGizmoDraggables;
   private IGizmoScrollInput[] _currentGizmoScrollables;
   private GameObject _currentHoveredObject;
   private LayerMask _draggableLayer;
   private Plane _dragPlane;
   private bool _isDragging;
   private Camera _mainCamera;
   private Vector3 _offset;
   private InputAction _pointerPositionAction;
   private InputAction _scrollAction;

   private void Awake()
   {
      _mainCamera = Camera.main;
      if (_mainCamera == null)
      {
         Plugin.Log.Error("Main Camera not found.");
         enabled = false;
         return;
      }

      _draggableLayer = LayerMask.GetMask("Event");

      _clickAction = new InputAction(binding: "<Mouse>/leftButton", type: InputActionType.Button);
      _clickAction.performed += OnClickPerformed;
      _clickAction.canceled += OnClickCanceled;
      _clickAction.Enable();

      _pointerPositionAction = new InputAction(
         binding: "<Mouse>/position",
         type: InputActionType.Value,
         expectedControlType: "Vector2");
      _pointerPositionAction.Enable();

      _scrollAction = new InputAction(
         binding: "<Mouse>/scroll",
         type: InputActionType.Value,
         expectedControlType: "Vector2");
      _scrollAction.performed += OnScrollPerformed;
      _scrollAction.Enable();
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
         HandleHover();
   }

   private void OnDisable()
   {
      if (_currentGizmoDraggables != null)
         foreach (var input in _currentGizmoDraggables)
            input.IsDragging = false;

      _currentGizmoDraggables = null;
      _currentGizmoScrollables = null;
      _currentHoveredObject = null;
      _isDragging = false;
      _offset = Vector3.zero;
   }

   private void OnDestroy()
   {
      if (_clickAction != null)
      {
         _clickAction.performed -= OnClickPerformed;
         _clickAction.canceled -= OnClickCanceled;
         _clickAction.Disable();
         _clickAction.Dispose();
      }

      if (_pointerPositionAction != null)
      {
         _pointerPositionAction.Disable();
         _pointerPositionAction.Dispose();
      }

      if (_scrollAction != null)
      {
         _scrollAction.performed -= OnScrollPerformed;
         _scrollAction.Disable();
         _scrollAction.Dispose();
      }
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

      foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnMouseClick();
   }

   private void OnClickCanceled(InputAction.CallbackContext context)
   {
      if (!_isDragging) return;
      _isDragging = false;
      foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnMouseRelease();
   }

   private void OnScrollPerformed(InputAction.CallbackContext context)
   {
      if (_isDragging || _currentGizmoScrollables == null) return;
      var delta = context.ReadValue<Vector2>().y;
      if (Mathf.Approximately(delta, 0f)) return;
      foreach (var scrollable in _currentGizmoScrollables) scrollable.OnScroll(delta);
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
         _currentGizmoScrollables = _currentHoveredObject.GetComponents<IGizmoScrollInput>();
         foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnPointerEnter();
      }
      else
      {
         if (_currentHoveredObject == null) return;
         foreach (var currentGizmoDraggable in _currentGizmoDraggables) currentGizmoDraggable.OnPointerExit();
         _currentHoveredObject = null;
         _currentGizmoScrollables = null;
      }
   }
}
