using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair for the Charge Shot.
/// A ring fills clockwise while charging and finishes exactly on the first beat the player
/// can release on. At that moment the ticks snap together into a solid + to say "release now".
/// </summary>
public class ChargeCrosshair : ArchetypeCrosshair
{
    [Header("References")]
    [SerializeField] private ChargeShotArchetype archetype;
    [Tooltip("Image Type = Filled, Fill Method = Radial 360, Fill Origin = Top. Fills as the charge builds")]
    [SerializeField] private Image chargeRing;
    [Tooltip("The ticks that snap together when the charge is full. Their direction comes from where they sit in the editor")]
    [SerializeField] private Image[] ticks;
    [Tooltip("Center dot. Hidden once the charge is full, because the joined ticks cover it")]
    [SerializeField] private Image centerDot;

    [Header("Tick Spread")]
    [Tooltip("Distance from center, in pixels, while idle or charging")]
    [SerializeField] private float openSpread = 13f;
    [Tooltip("How far the ticks have inched in just before the charge is full. Keep it a few pixels outside Closed Spread so there's still a visible snap")]
    [SerializeField] private float nearlyFullSpread = 9f;
    [Tooltip("Distance from center, in pixels, when full. Half a tick's length makes the ticks meet in a solid +")]
    [SerializeField] private float closedSpread = 5f;
    [Tooltip("How fast the ticks snap together. Higher = snappier")]
    [SerializeField] private float snapSpeed = 30f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color chargingColor = Color.white;
    [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.85f);

    private Vector2[] tickDirections;
    private float currentSpread;

    protected override FireArchetype Archetype { get { return archetype; } }

    protected override void Awake()
    {
        base.Awake();
        tickDirections = GetDirectionsFromCenter(ticks);
        currentSpread = openSpread;
    }

    protected override void Update()
    {
        base.Update();

        bool isFull = archetype.IsFullyCharged;
        Color color = ApplyFizzleTint(GetStateColor());

        chargeRing.fillAmount = archetype.ChargeProgress;
        chargeRing.color = color;

        float targetSpread = GetTargetSpread();
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, snapSpeed * Time.deltaTime);
        PlacePieces(ticks, tickDirections, currentSpread, color);

        centerDot.enabled = !isFull;
        centerDot.color = color;
    }

    private Color GetStateColor()
    {
        if (!archetype.IsCharging)
        {
            return idleColor;
        }
        if (archetype.IsFullyCharged)
        {
            return readyColor;
        }
        return chargingColor;
    }
    private float GetTargetSpread()
    {
        if (archetype.IsFullyCharged)
        {
            return closedSpread;
        }
        return Mathf.Lerp(openSpread, nearlyFullSpread, archetype.ChargeProgress);
    }
}
