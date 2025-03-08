using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IDieFace
{
    // placeholder for now
}

public enum Side
{
    Top = 1,
    Right = 2,
    Front = 3,
    Back = 4,
    Left = 5,
    Bottom = 6
}


public class Die : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public float DragZPosition = -2f;
    public float MaxSpeed = 10f;
    public float MaxForce = 15f;
    public float SlowingDistance = 1f;
    public float RotationSpeed = 1f;
    public float[] Weights = new float[6] { 1, 1, 1, 1, 1, 1 };

    public DebugModeOptions DebugMode = DebugModeOptions.Gizmos;
    [Flags]
    public enum DebugModeOptions
    {
        Disabled = 0 << 0,
        Drag = 1 << 0,
        Rest = 1 << 1,
        Gizmos = 1 << 2,
        Inspect = 1 << 3,
    };

    private bool _isSelected = false;
    public bool IsSelected
    {
        get => _isSelected;
        private set
        {
            _isSelected = value;
            SelectionChangeHandler(_isSelected);
            OnSelectionChangeEvent?.Invoke(this);
        }
    }



    public static event Action<Die> OnAwakeEvent;
    public static event Action<Die> OnDisableEvent;
    public event Action<PointerEventData, Die> OnBeginDragEvent;
    public event Action<PointerEventData, Die> OnDragEvent;
    public event Action<PointerEventData, Die> OnEndDragEvent;
    public event Action<Die> OnSelectionChangeEvent;

    private Outline _outline;
    private Vector3 _dragOffset;
    // private DiceManager _diceManager;
    private bool _isDragging = false;
    private Rigidbody _rigidbody;
    [HideInInspector]
    public Rigidbody Rigidbody => _rigidbody;
    private Collider _collider;
    private Vector3 _targetPosition;
    private Vector3 _desiredVelocity;
    private Vector3 _steeringForce;
    private float _dragEndTime;
    private readonly Transform[] _sideTransforms = new Transform[6];
    private readonly IDieFace[] _faces = new IDieFace[6];
    private bool _isSimulationRunning = false;

    public void Awake()
    {
        DiceManager.OnBeginSimulationEvent += OnSimulationBegin;
        DiceManager.OnEndSimulationEvent += OnSimulationEnd;

        // DiceManager.Instance.DiceCamera = DiceManager.Instance.DiceCamera;
        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.enabled = _isSelected;
        _rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(_rigidbody != null, "Die must have a Rigidbody component.");
        _collider = GetComponent<Collider>();
        Debug.Assert(_collider != null, "Die must have a Collider component.");

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Side"))
            {
                int sideIndex = int.Parse(child.name[4..]) - 1;
                _sideTransforms[sideIndex] = child;
            }
        }

        OnAwakeEvent?.Invoke(this);
    }

    public void OnDisable()
    {
        DiceManager.OnBeginSimulationEvent -= OnSimulationBegin;
        DiceManager.OnEndSimulationEvent -= OnSimulationEnd;
        DetachDiceGroupEvents();
        OnDisableEvent?.Invoke(this);
    }

    private void OnSimulationBegin(List<Die> dice)
    {
        if (!dice.Contains(this)) return;
        _isSimulationRunning = true;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
    }

    private void OnSimulationEnd(List<Die> dice)
    {
        if (!dice.Contains(this)) return;
        _isSimulationRunning = false;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
    }

    private void OnBeginDragHandler(PointerEventData eventData)
    {
        if (_isSimulationRunning) return;
        _isDragging = true;
        // _dragOffset = transform.position - GetMouseWorldPosition(eventData);
        // _dragOffset = Vector3.zero;
        _rigidbody.useGravity = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag)) Debug.Log($"Begin drag: {name}");
        OnBeginDragHandler(eventData);
        OnBeginDragEvent?.Invoke(eventData, this);
    }

    private void OnDiceGroupBeginDrag(DiceManager.DiceGroupDragEventData eventData)
    {
        var points = Utils.GeneratePoints(2, eventData.Group.Count);
        int indexInGroup = eventData.Group.IndexOf(this);
        _dragOffset = points[indexInGroup];
        Debug.Log($"Begin group drag: {eventData.Group.Count} dice, index: {indexInGroup}, offset: {_dragOffset}");

        if (eventData.Initiator == this) return;
        OnBeginDragHandler(eventData.PointerEventData);

    }

    private void OnDragHandler(PointerEventData eventData)
    {
        if (_isSimulationRunning) return;
        if (!_isDragging) return;
        Vector3 newPosition = GetMouseWorldPosition(eventData) + _dragOffset;
        newPosition.z = DragZPosition;
        _targetPosition = newPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag))
            Debug.Log($"Drag: {name}");

        OnDragHandler(eventData);
        OnDragEvent?.Invoke(eventData, this);
    }

    private void OnDiceGroupDrag(DiceManager.DiceGroupDragEventData eventData)
    {
        if (eventData.Initiator == this) return;
        OnDragHandler(eventData.PointerEventData);
    }

    private void OnEndDragHandler(PointerEventData eventData)
    {
        if (_isSimulationRunning) return;
        _isDragging = false;
        _rigidbody.useGravity = true;
        _dragEndTime = Time.realtimeSinceStartup;
        if (DebugMode.HasFlag(DebugModeOptions.Rest))
            StartCoroutine(WaitForSleep());
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag)) Debug.Log($"End drag: {name}");
        OnEndDragHandler(eventData);
        OnEndDragEvent?.Invoke(eventData, this);
    }

    private void OnDiceGroupEndDrag(DiceManager.DiceGroupDragEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag)) Debug.Log($"End group drag: {name}");
        if (eventData.Initiator == this) return;
        OnEndDragHandler(eventData.PointerEventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Inspect)) Inspect();
        if (_isSimulationRunning) return;
        if (!_isDragging) IsSelected = !IsSelected;
    }

    public void SetHighlight(bool value)
    {
        if (_outline != null)
        {
            _outline.enabled = value;
        }
    }

    private void SelectionChangeHandler(bool isSelected)
    {
        SetHighlight(isSelected);
        if (isSelected)
        {
            DiceManager.OnDiceGroupBeginDragEvent += OnDiceGroupBeginDrag;
            DiceManager.OnDiceGroupDragEvent += OnDiceGroupDrag;
            DiceManager.OnDiceGroupEndDragEvent += OnDiceGroupEndDrag;
        }
        else
        {
            DetachDiceGroupEvents();
        }
    }

    private void DetachDiceGroupEvents()
    {
        DiceManager.OnDiceGroupBeginDragEvent -= OnDiceGroupBeginDrag;
        DiceManager.OnDiceGroupDragEvent -= OnDiceGroupDrag;
        DiceManager.OnDiceGroupEndDragEvent -= OnDiceGroupEndDrag;
    }

    private Vector3 GetMouseWorldPosition(PointerEventData eventData)
    {
        Vector3 mousePosition = eventData.position;
        mousePosition.z = -DiceManager.Instance.DiceCamera.transform.position.z;
        return DiceManager.Instance.DiceCamera.ScreenToWorldPoint(mousePosition);
    }

    private void ApplySteeringBehavior()
    {
        Vector3 toTarget = _targetPosition - transform.position;
        toTarget.z = 0; // Ignore Z for distance calculation
        float distance = toTarget.magnitude;

        // Calculate desired velocity (XY only)
        _desiredVelocity = toTarget.normalized * MaxSpeed;
        _desiredVelocity.z = 0;

        // Scale with distance for arrival behavior
        if (distance < SlowingDistance)
        {
            _desiredVelocity *= distance / SlowingDistance;
        }

        // Calculate steering force (XY only)
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        currentVelocity.z = 0;
        _steeringForce = Vector3.ClampMagnitude(_desiredVelocity - currentVelocity, MaxForce);
        _steeringForce.z = 0;

        _rigidbody.AddForce(_steeringForce, ForceMode.Force);

        // Apply rotation based on movement
        Vector3 velocity = _rigidbody.linearVelocity;
        if (velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.forward, velocity.normalized);
            Vector3 rotationAxis = targetRotation.eulerAngles.normalized;
            _rigidbody.AddTorque(RotationSpeed * velocity.magnitude * rotationAxis, ForceMode.Force);
        }
    }

    void FixedUpdate()
    {
        if (_isDragging)
        {
            // Handle Z-axis movement
            Vector3 currentPos = transform.position;
            float zDiff = DragZPosition - currentPos.z;
            if (Mathf.Abs(zDiff) > 0.01f)
            {
                currentPos.z = Mathf.Lerp(currentPos.z, DragZPosition, 0.3f);
                transform.position = currentPos;
            }

            ApplySteeringBehavior();
        }
    }

    private IEnumerator WaitForSleep()
    {
        while (!IsDiceResting())
        {
            yield return new WaitForFixedUpdate();
        }

        float duration = Time.realtimeSinceStartup - _dragEndTime;
        if (DebugMode.HasFlag(DebugModeOptions.Rest))
            Debug.Log($"Die {name} took {duration:F3}s to come to rest");
    }

    private void OnDrawGizmos()
    {
        if (_isDragging && DebugMode.HasFlag(DebugModeOptions.Gizmos))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetPosition, 1f);

            // Also visualize the steering vectors
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + _desiredVelocity.normalized * 5);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _steeringForce.normalized * 5);
        }
    }

    public bool IsDiceResting()
    {
        return _rigidbody.IsSleeping() || (_rigidbody.linearVelocity.magnitude < 0.01f && _rigidbody.angularVelocity.magnitude < 0.01f);
    }

    public Side GetTopSide()
    {
        float maxDot = 0;
        int topSide = 0;
        for (int i = 0; i < 6; i++)
        {
            // the correct up direction is the one closest to Vector3.back
            float dot = Vector3.Dot(_sideTransforms[i].up, Vector3.back);
            if (dot <= maxDot) continue;
            maxDot = dot;
            topSide = i + 1;
        }
        return (Side)topSide;
    }

    private void Inspect()
    {
        Debug.Log($"Resting: {_rigidbody.IsSleeping()}, Velocity: {_rigidbody.linearVelocity}, Angular Velocity: {_rigidbody.angularVelocity}, Top Side: {GetTopSide()}");
    }

    public Side Roll()
    {
        float totalWeight = 0;
        for (int i = 0; i < 6; i++)
        {
            totalWeight += Weights[i];
        }

        float randomValue = UnityEngine.Random.Range(0, totalWeight);
        float weightSum = 0;
        int result = 0;
        for (int i = 0; i < 6; i++)
        {
            weightSum += Weights[i];
            if (randomValue <= weightSum)
            {
                result = i + 1;
                break;
            }
        }

        return (Side)result;
    }

    // Add these rotation lookup tables
    private static readonly Dictionary<(int from, int to), Vector3> RotationTable = new()
    {
        // Rotations to get to side 1 (top)
        {(2, 1), new Vector3(0, 90, 0)},   // Right face to top
        {(3, 1), new Vector3(270, 0, 0)},  // Front face to top
        {(4, 1), new Vector3(90, 0, 0)},   // Back face to top
        {(5, 1), new Vector3(0, 270, 0)},  // Left face to top
        {(6, 1), new Vector3(180, 0, 0)}   // Bottom face to top
    };

    public void RotateToDesiredSide(Side fromSide, Side toSide)
    {
        if (fromSide == toSide) return;

        var fromIndex = (int)fromSide;
        var toIndex = (int)toSide;

        // Try to find direct rotation in lookup table
        if (RotationTable.TryGetValue((fromIndex, toIndex), out Vector3 rotation))
        {
            transform.Rotate(rotation);
            return;
        }

        // If not found, calculate inverse rotation
        if (RotationTable.TryGetValue((toIndex, fromIndex), out rotation))
        {
            // Invert the rotation
            transform.Rotate(-rotation);
            return;
        }

        // If neither direct nor inverse found, rotate via top face
        if (fromSide != Side.Top)
        {
            // First rotate to top
            RotateToDesiredSide(fromSide, Side.Top);
        }
        if (toSide != Side.Top)
        {
            // Then rotate from top to destination
            RotateToDesiredSide(Side.Top, toSide);
        }
    }

    public IDieFace GetFace(Side side)
    {
        return _faces[(int)side - 1];
    }

    public bool TrySetFace(Side side, IDieFace face)
    {
        if (_faces[(int)side - 1] != null)
            return false;

        _faces[(int)side - 1] = face;
        return true;
    }
}
