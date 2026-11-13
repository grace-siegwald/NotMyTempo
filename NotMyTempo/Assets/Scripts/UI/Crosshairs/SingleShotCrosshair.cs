using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair for the Single Shot.
/// Each shot kicks the ticks inward and shortens them, then they ease back to idle.
/// </summary>
public class SingleShotCrosshair : ArchetypeCrosshair
{
    [Header("References")]
    [SerializeField] private SingleShotArchetype archetype;
    [Tooltip("The ticks that kick in on each shot. Their direction comes from where they sit in the editor")]
    [SerializeField] private Image[] ticks;
    [SerializeField] private Image centerDot;

    [Header("Shot Kick")]
    [Tooltip("Distance from center, in pixels, while idle")]
    [SerializeField] private float idleSpread = 11f;
    [Tooltip("Distance from center, in pixels, right as a shot fires")]
    [SerializeField] private float shotSpread = 8f;
    [Tooltip("Tick length right as a shot fires, as a fraction of their editor length. 0.5 = half as long")]
    [Range(0.1f, 1f)]
    [SerializeField] private float shotLengthScale = 0.5f;
    [Tooltip("Seconds to ease back to idle after a shot")]
    [SerializeField] private float kickDuration = 0.12f;

    [Header("Colors")]
    [SerializeField] private Color crosshairColor = Color.white;

    private Vector2[] tickDirections;
    private Vector2[] tickRestingSizes;
    private float kickEndTime;

    protected override FireArchetype Archetype { get { return archetype; } }

    // 1 the moment a shot fires, fading to 0 over kickDuration
    private float KickAmount
    {
        get { return Mathf.Clamp01((kickEndTime - Time.time) / kickDuration); }
    }

    protected override void Awake()
    {
        base.Awake();
        tickDirections = GetDirectionsFromCenter(ticks);
        tickRestingSizes = GetRestingSizes(ticks);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        archetype.Fired += HandleFired;
    }

    protected override void OnDisable()
    {
        archetype.Fired -= HandleFired;
        kickEndTime = 0f;
        base.OnDisable();
    }

    protected override void Update()
    {
        base.Update();

        float kick = KickAmount;
        Color color = ApplyFizzleTint(crosshairColor);

        float spread = Mathf.Lerp(idleSpread, shotSpread, kick);
        PlacePieces(ticks, tickDirections, spread, color);

        float lengthScale = Mathf.Lerp(1f, shotLengthScale, kick);
        ScalePieceLengths(ticks, tickRestingSizes, lengthScale);

        centerDot.color = color;
    }

    private void HandleFired()
    {
        kickEndTime = Time.time + kickDuration;
    }
}
