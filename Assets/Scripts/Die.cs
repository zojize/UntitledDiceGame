using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

public class Die : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public float DragZPosition = -2f;
    public float MaxSpeed = 10f;
    public float MaxForce = 15f;
    public float SlowingDistance = 1f;
    public float RotationSpeed = 1f;
    public float[] Weights = new float[6] { 1, 1, 1, 1, 1, 1 };

    public event Action<Die> OnDieSelectionChangeEvent;
    public DebugModeOptions DebugMode;
    [Flags]
    public enum DebugModeOptions
    {
        Disabled,
        Drag,
        Rest,
        Gizmos,
        Inspect,
    };

    private bool _isSelected = false;
    [HideInInspector]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnDieSelectionChangeEvent?.Invoke(this);
            UpdateHighlight();
        }
    }


    public event Action<PointerEventData, Die> OnBeginDragEvent;
    public event Action<PointerEventData, Die> OnDragEvent;
    public event Action<PointerEventData, Die> OnEndDragEvent;

    private Outline _outline;
    private Vector3 _dragOffset;
    private DiceManager _diceManager;
    private Camera _diceCamera;
    private bool _isDragging = false;
    private Rigidbody _rigidbody;
    [HideInInspector]
    public Rigidbody Rigidbody => _rigidbody;
    private Collider _collider;
    private Vector3 _targetPosition;
    private Vector3 _desiredVelocity;
    private Vector3 _steeringForce;
    private float _dragEndTime;
    private readonly Transform[] _sides = new Transform[6];
    private bool _isSimulationRunning = false;

    public void Awake()
    {
        _diceManager = transform.parent.GetComponent<DiceManager>();
        Debug.Assert(_diceManager != null, "Die must be a child of a GameObject with a DiceManager component.");
        OnBeginDragEvent += _diceManager.OnDieBeginDrag;
        OnDragEvent += _diceManager.OnDieDrag;
        OnEndDragEvent += _diceManager.OnDieEndDrag;
        OnDieSelectionChangeEvent += _diceManager.OnDieSelectionChangeDelegate;
        _diceManager.OnBeginSimulationEvent += OnSimulationBegin;
        _diceManager.OnEndSimulationEvent += OnSimulationEnd;

        _diceCamera = _diceManager.DiceCamera;
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
                _sides[sideIndex] = child;
            }
        }
    }

    public void OnDestroy()
    {
        _diceManager.SelectedDice.Remove(this);
        _diceManager.OnBeginSimulationEvent -= OnSimulationBegin;
        _diceManager.OnEndSimulationEvent -= OnSimulationEnd;
    }

    private void OnSimulationBegin(List<Die> dice)
    {
        if (!dice.Contains(this)) return;
        _isSimulationRunning = true;
        // _collider.enabled = false;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        // _rigidbody.detectCollisions = false;
    }

    private void OnSimulationEnd(List<Die> dice)
    {
        if (!dice.Contains(this)) return;
        _isSimulationRunning = false;
        // _collider.enabled = true;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        // _rigidbody.detectCollisions = true;
        Debug.Log($"linearVelocity: {_rigidbody.linearVelocity}, angularVelocity: {_rigidbody.angularVelocity}");
    }

    private void OnBeginDragHandler(PointerEventData eventData)
    {
        if (_isSimulationRunning) return;
        _isDragging = true;
        _dragOffset = transform.position - GetMouseWorldPosition(eventData);
        _rigidbody.useGravity = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag)) Debug.Log($"Begin drag: {name}");
        OnBeginDragEvent?.Invoke(eventData, this);
        OnBeginDragHandler(eventData);
    }

    public void OnDiceGroupBeginDrag(PointerEventData eventData, Die die)
    {
        if (die == this) return;
        OnBeginDragHandler(eventData);
        _dragOffset = transform.position - die.transform.position;
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

    public void OnDiceGroupDrag(PointerEventData eventData, Die die)
    {
        if (die == this) return;
        OnDragHandler(eventData);
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

    public void OnDiceGroupEndDrag(PointerEventData eventData, Die die)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Drag)) Debug.Log($"End group drag: {name}");
        if (die == this) return;
        OnEndDragHandler(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DebugMode.HasFlag(DebugModeOptions.Inspect)) Inspect();
        if (_isSimulationRunning) return;
        if (!_isDragging) IsSelected = !IsSelected;
    }

    void UpdateHighlight()
    {
        if (_outline != null)
        {
            _outline.enabled = IsSelected;
        }
    }

    private Vector3 GetMouseWorldPosition(PointerEventData eventData)
    {
        Vector3 mousePosition = eventData.position;
        mousePosition.z = -_diceCamera.transform.position.z;
        return _diceCamera.ScreenToWorldPoint(mousePosition);
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

    public int GetTopSide()
    {
        float maxDot = 0;
        int topSide = 0;
        for (int i = 0; i < 6; i++)
        {
            // the correct up direction is the one closest to Vector3.back
            float dot = Vector3.Dot(_sides[i].up, Vector3.back);
            if (dot <= maxDot) continue;
            maxDot = dot;
            topSide = i + 1;
        }
        return topSide;
    }

    private void Inspect()
    {
        Debug.Log($"Resting: {_rigidbody.IsSleeping()}, Velocity: {_rigidbody.linearVelocity}, Angular Velocity: {_rigidbody.angularVelocity}, Top Side: {GetTopSide()}");
    }

    public int Roll()
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

        return result;
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

    public void RotateToDesiredSide(int fromSide, int toSide)
    {
        if (fromSide == toSide) return;

        // Try to find direct rotation in lookup table
        if (RotationTable.TryGetValue((fromSide, toSide), out Vector3 rotation))
        {
            transform.Rotate(rotation);
            return;
        }

        // If not found, calculate inverse rotation
        if (RotationTable.TryGetValue((toSide, fromSide), out rotation))
        {
            // Invert the rotation
            transform.Rotate(-rotation);
            return;
        }

        // If neither direct nor inverse found, rotate via top face
        if (fromSide != 1)
        {
            // First rotate to top
            RotateToDesiredSide(fromSide, 1);
        }
        if (toSide != 1)
        {
            // Then rotate from top to destination
            RotateToDesiredSide(1, toSide);
        }
    }
}
