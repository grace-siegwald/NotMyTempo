using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Heat and venting rules only. WeaponOverheatHUD reads the public state.</summary>
[DisallowMultipleComponent]
public class WeaponOverheat : MonoBehaviour
{
    [Header("Heat")]
    [Tooltip("Heat capacity in ordinary shots. Increasing this allows more shots before overheating.")]
    [SerializeField, Min(1)] private int shotsBeforeOverheat = 10;
    [Tooltip("Heat per on-beat shot relative to an ordinary shot. 0.5 means half the heat.")]
    [SerializeField, Range(0f, 1f)] private float onBeatHeatMultiplier = 0.5f;
    [Tooltip("Seconds after the last shot before passive cooling begins.")]
    [SerializeField, Min(0f)] private float coolingDelay = 0.5f;
    [Tooltip("Heat units removed per second while ready. Overheated weapons must be vented.")]
    [SerializeField, Min(0f)] private float coolingPerSecond = 4f;

    [Header("Venting")]
    [Tooltip("Optional Button action in an enabled action map. Leave empty to use keyboard R. This script does not enable or disable shared actions.")]
    [SerializeField] private InputActionReference ventAction;
    [Tooltip("Successful timing presses needed to clear all heat.")]
    [SerializeField, Min(1)] private int hitsToVent = 3;
    [Tooltip("Misses allowed before venting jams and progress resets.")]
    [SerializeField, Min(1)] private int missesBeforeJam = 3;
    [Tooltip("Needle travel speed in degrees per second. Higher values make venting harder.")]
    [SerializeField, Min(1f)] private float needleSpeed = 220f;
    [Tooltip("Full angular width of the success zone in degrees. Larger values are more forgiving.")]
    [SerializeField, Range(10f, 120f)] private float targetWidth = 46f;
    [Tooltip("Seconds the weapon stays jammed before another vent attempt can begin.")]
    [SerializeField, Min(0.1f)] private float jamDelay = 1f;

    private enum VentState { Ready, Overheated, Venting, Jammed }
    private VentState state;
    private float heat;
    private float lastShotTime = float.NegativeInfinity;
    private float lastCoolingUpdate;
    private float jamEndTime;
    private float needleAngle;
    private float targetAngle;
    private int needleDirection = 1;
    private int hits;
    private int misses;

    public bool IsOverheated => state != VentState.Ready;
    public bool IsVenting => state == VentState.Venting;
    public bool IsJammed => state == VentState.Jammed;
    public bool CanFire => isActiveAndEnabled && !IsOverheated && Time.timeScale > 0f;
    public float HeatFraction => Mathf.Clamp01(heat / Mathf.Max(1, shotsBeforeOverheat));
    // Angles run clockwise from the top of the dial.
    public float NeedleAngle => needleAngle;
    public float TargetAngle => targetAngle;
    public float TargetWidth => targetWidth;
    public int Hits => hits;
    public int Misses => misses;
    public int HitsRequired => Mathf.Max(1, hitsToVent);
    public int MissLimit => Mathf.Max(1, missesBeforeJam);

    private void OnEnable()
    {
        // Disabling the weapon pauses cooling instead of applying a large catch-up.
        lastCoolingUpdate = Time.time;
    }

    public bool TryFire(bool onBeat)
    {
        if (!CanFire)
            return false;

        CoolHeat();
        heat = Mathf.Min(Mathf.Max(1, shotsBeforeOverheat),
            heat + (onBeat ? onBeatHeatMultiplier : 1f));
        lastShotTime = Time.time;

        if (heat >= Mathf.Max(1, shotsBeforeOverheat))
            state = VentState.Overheated;

        // The shot that fills the meter is still allowed.
        return true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (state == VentState.Ready)
        {
            CoolHeat();
            return;
        }

        if (state == VentState.Jammed)
        {
            if (Time.time < jamEndTime)
                return;
            state = VentState.Overheated;
        }

        // Advance even on a press frame, then reverse from that exact position.
        if (IsVenting)
            needleAngle = Mathf.Repeat(
                needleAngle + needleDirection * needleSpeed * Time.deltaTime, 360f);

        InputAction action = ventAction != null ? ventAction.action : null;
        bool pressed = action != null
            ? action.WasPressedThisFrame()
            : Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        if (pressed)
            PressVent();
    }

    private void PressVent()
    {
        needleDirection *= -1;

        if (state == VentState.Overheated)
        {
            hits = misses = 0;
            state = VentState.Venting;
            PickTarget();
            return; // First press starts venting; later presses are scored.
        }

        if (Mathf.Abs(Mathf.DeltaAngle(needleAngle, targetAngle)) <= targetWidth * 0.5f)
        {
            hits++;
            if (hits >= HitsRequired)
            {
                heat = 0f;
                state = VentState.Ready;
                lastCoolingUpdate = Time.time;
            }
            else
            {
                PickTarget();
            }
        }
        else
        {
            misses++;
            if (misses >= MissLimit)
            {
                state = VentState.Jammed;
                jamEndTime = Time.time + jamDelay;
            }
        }
    }

    private void PickTarget()
    {
        // Only the target moves. The needle angle is never reset or randomized.
        targetAngle = UnityEngine.Random.Range(0f, 360f);
    }

    private void CoolHeat()
    {
        float now = Time.time;
        float coolingStart = Mathf.Max(lastCoolingUpdate, lastShotTime + coolingDelay);
        heat = Mathf.Max(0f, heat - coolingPerSecond * Mathf.Max(0f, now - coolingStart));
        lastCoolingUpdate = now;
    }
}
