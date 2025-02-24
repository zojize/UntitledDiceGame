using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { get; private set; }
    public Camera DiceCamera;
    public Slider Slider;
    public bool DebugMode = false;

    public record DiceGroupDragEventData
    {
        public PointerEventData PointerEventData { get; init; }
        public List<Die> Group { get; init; }
        public Die Initiator { get; init; }
    }

    public static event Action<DiceGroupDragEventData> OnDiceGroupBeginDragEvent;
    public static event Action<DiceGroupDragEventData> OnDiceGroupDragEvent;
    public static event Action<DiceGroupDragEventData> OnDiceGroupEndDragEvent;

    public static event Action<List<Die>> OnBeginSimulationEvent;
    public static event Action<List<Die>> OnSimulationEvent;
    public static event Action<List<Die>> OnEndSimulationEvent;

    private static Die _groupDragInitiator;
    private static readonly List<Die> SelectedDice = new();

    private static int MAX_SIMULATION_FRAMES;
    private const float JITTER_POSITION_THRESHOLD = 0.01f;
    private const float JITTER_ROTATION_THRESHOLD = 0.1f;
    private int _desiredSide = 1;

    private GameObject _diePrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }

        _diePrefab = Resources.Load<GameObject>("Prefabs/Die");

        var existingDice = Component.FindObjectsByType<Die>(FindObjectsSortMode.None);

        foreach (var die in existingDice)
        {
            OnDieAwake(die);
        }
        Die.OnAwakeEvent += OnDieAwake;
        Die.OnDisableEvent += OnDieDisable;

        // Set maximum simulation frames equal to 10 seconds of simulation
        MAX_SIMULATION_FRAMES = (int)(1f / Time.fixedDeltaTime) * 10;
        Slider.onValueChanged.AddListener((val) => _desiredSide = (int)val);
    }

    private static void OnDieAwake(Die die)
    {
        die.OnSelectionChangeEvent += OnDieSelectionChange;
        die.OnBeginDragEvent += OnDieBeginDrag;
        die.OnDragEvent += OnDieDrag;
        die.OnEndDragEvent += OnDieEndDrag;
    }

    private static void OnDieDisable(Die die)
    {
        die.OnSelectionChangeEvent -= OnDieSelectionChange;
        die.OnBeginDragEvent -= OnDieBeginDrag;
        die.OnDragEvent -= OnDieDrag;
        die.OnEndDragEvent -= OnDieEndDrag;

        SelectedDice.Remove(die);
    }


    public static void AddDie()
    {
        GameObject dieObject = Instantiate(Instance._diePrefab, Instance.transform);
        dieObject.transform.position = new Vector3(
            UnityEngine.Random.Range(0, -15),
            UnityEngine.Random.Range(0, 10),
            -15
        );
    }

    public static void RemoveDie()
    {
        if (Instance.transform.childCount > 0)
        {
            for (int i = Instance.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = Instance.transform.GetChild(i);
                if (child.TryGetComponent<Die>(out var die))
                {
                    Destroy(child.gameObject);
                    break;
                }
            }
        }
    }

    public static void OnDieSelectionChange(Die die)
    {
        if (die.IsSelected)
        {
            if (!SelectedDice.Contains(die))
            {
                SelectedDice.Add(die);
            }
        }
        else
        {
            SelectedDice.Remove(die);
        }
    }


    public static void OnDieBeginDrag(PointerEventData eventData, Die die)
    {

        if (!SelectedDice.Contains(die))
        {
            return;
        }

        _groupDragInitiator = die;

        OnDiceGroupBeginDragEvent?.Invoke(new DiceGroupDragEventData
        {
            PointerEventData = eventData,
            Group = SelectedDice,
            Initiator = _groupDragInitiator,
        });
    }

    public static void OnDieDrag(PointerEventData eventData, Die die)
    {
        if (!SelectedDice.Contains(die))
        {
            return;
        }

        OnDiceGroupDragEvent?.Invoke(new DiceGroupDragEventData
        {
            PointerEventData = eventData,
            Group = SelectedDice,
            Initiator = _groupDragInitiator,
        });
    }

    private static List<List<DiceRollState>> _simulationResult;
    private static int[] _finalSides;
    private static bool _isSimulationRunning;
    private static float _simulationStart;

    public static void OnDieEndDrag(PointerEventData eventData, Die die)
    {
        if (!SelectedDice.Contains(die))
        {
            return;
        }

        OnDiceGroupEndDragEvent?.Invoke(new DiceGroupDragEventData
        {
            PointerEventData = eventData,
            Group = SelectedDice,
            Initiator = _groupDragInitiator,
        });

        _groupDragInitiator = null;

        BeginSimulation();
    }

    private static void BeginSimulation()
    {
        _isSimulationRunning = true;

        var startTime = Time.realtimeSinceStartup;
        var (result, sides) = SimulateDiceRoll(SelectedDice);
        var duration = Time.realtimeSinceStartup - startTime;
        if (Instance.DebugMode) Debug.Log($"Simulation result for {SelectedDice.Count()} dice: {result.Count} frames, took {duration:F3}s");

        _simulationResult = result;
        _finalSides = sides;
        _simulationStart = Time.realtimeSinceStartup;

        OnBeginSimulationEvent?.Invoke(SelectedDice);
    }

    void Update()
    {
        if (_isSimulationRunning)
        {
            float timeSinceStart = Time.realtimeSinceStartup - _simulationStart;
#pragma warning disable UNT0004 // intentional use of Time.fixedDeltaTime
            int frameIndex = Mathf.FloorToInt(timeSinceStart / Time.fixedDeltaTime);
#pragma warning restore UNT0004

            // TODO: Implement interpolation between frames
            if (frameIndex < _simulationResult.Count)
            {
                var frame = _simulationResult[frameIndex];
                for (int i = 0; i < frame.Count; i++)
                {
                    var state = frame[i];
                    var die = SelectedDice.ElementAt(i);
                    die.transform.SetPositionAndRotation(state.Position, state.Rotation);
                    die.RotateToDesiredSide(_finalSides[i], _desiredSide);
                }
                OnSimulationEvent?.Invoke(SelectedDice);
            }
            else
            {
                _isSimulationRunning = false;
                if (DebugMode) Debug.Log("Simulation playback complete");
                OnEndSimulationEvent?.Invoke(SelectedDice);
            }
        }
    }


    private static (List<List<DiceRollState>> result, int[] sides) SimulateDiceRoll(IEnumerable<Die> dice)
    {
        List<List<DiceRollState>> result = new();
        List<DiceRollState> initialStates = new();
        var diceArray = dice.ToArray();
        var sides = Enumerable.Repeat(0, diceArray.Length).ToArray();

        // Store current physics state
        var originalSimMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;

        // Record initial states and prepare for simulation
        foreach (var die in diceArray)
        {
            var rb = die.Rigidbody;
            var state = new DiceRollState
            {
                Position = die.transform.position,
                Rotation = die.transform.rotation,
                Velocity = rb.linearVelocity,
                AngularVelocity = rb.angularVelocity
            };
            initialStates.Add(state);
        }

        bool allResting = false;
        int frameCount = 0;

        while (frameCount < MAX_SIMULATION_FRAMES && !allResting)
        {
            List<DiceRollState> frameState = new();
            int restingCount = 0;

            foreach (var die in diceArray)
            {
                var rb = die.Rigidbody;
                var state = new DiceRollState
                {
                    Position = die.transform.position,
                    Rotation = die.transform.rotation,
                    Velocity = rb.linearVelocity,
                    AngularVelocity = rb.angularVelocity
                };
                frameState.Add(state);

                // Check if die is actually at rest
                if (die.IsDiceResting())
                {
                    restingCount++;
                }
            }

            result.Add(frameState);
            allResting = restingCount == initialStates.Count;

            Physics.Simulate(Time.fixedDeltaTime);
            frameCount++;
        }

        // Restore initial states
        for (int i = 0; i < initialStates.Count; i++)
        {
            var die = diceArray.ElementAt(i);
            var initialState = initialStates[i];
            var rb = die.Rigidbody;

            sides[i] = die.GetTopSide();
            die.transform.SetPositionAndRotation(initialState.Position, initialState.Rotation);
            rb.linearVelocity = initialState.Velocity;
            rb.angularVelocity = initialState.AngularVelocity;
        }



        int stableFrames = FindStableSequenceLength(result);
        float stableTime = stableFrames * Time.fixedDeltaTime;

        if (stableFrames > 0)
        {
            if (Instance.DebugMode) Debug.Log($"Found {stableFrames} stable frames ({stableTime:F2}s) at end of simulation - removing stable sequence");
            result.RemoveRange(result.Count - stableFrames, stableFrames);
        }
        else if (!allResting)
        {
            if (Instance.DebugMode) Debug.LogWarning($"Dice simulation stopped after {frameCount} frames without reaching rest state");
        }

        // Restore physics state
        Physics.simulationMode = originalSimMode;

        return (result, sides);
    }

    private static int FindStableSequenceLength(List<List<DiceRollState>> records)
    {
        if (records.Count < 2) return 0;

        var lastFrame = records.Last();
        int stableFrames = 0;

        // Start from the second-to-last frame and go backwards
        for (int i = records.Count - 2; i >= 0; i--)
        {
            var frame = records[i];
            bool frameIsStable = true;

            for (int j = 0; j < frame.Count; j++)
            {
                var deltaPos = (frame[j].Position - lastFrame[j].Position).magnitude;
                var deltaRot = Quaternion.Angle(frame[j].Rotation, lastFrame[j].Rotation);

                if (deltaPos > JITTER_POSITION_THRESHOLD || deltaRot > JITTER_ROTATION_THRESHOLD)
                {
                    frameIsStable = false;
                    break;
                }
            }

            if (!frameIsStable) break;
            stableFrames++;
        }

        return stableFrames;
    }

    private record DiceRollState
    {
        public Vector3 Position { get; init; }
        public Quaternion Rotation { get; init; }
        public Vector3 Velocity { get; init; }
        public Vector3 AngularVelocity { get; init; }
    }
}
