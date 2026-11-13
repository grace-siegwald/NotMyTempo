using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair for the Prime Shot.
/// Unprimed: an open, flat X around a dot. Priming snaps the X shut over the dot, and a row of
/// pips counts down the beats left to launch. Launching (or losing the prime) springs the X open.
/// </summary>
public class PrimeCrosshair : ArchetypeCrosshair
{
    [Header("References")]
    [SerializeField] private PrimeShotArchetype archetype;
    [Tooltip("The four angled pieces that lock into an X. Their direction comes from where they sit in the editor")]
    [SerializeField] private Image[] xPieces;
    [Tooltip("Center dot. Hidden while primed, because the locked X covers it")]
    [SerializeField] private Image centerDot;
    [Tooltip("One pip per beat of prime lifetime, left to right. Hidden when unprimed")]
    [SerializeField] private Image[] lifetimePips;

    [Header("X Spread")]
    [Tooltip("Distance from center, in pixels, while unprimed")]
    [SerializeField] private float openSpread = 16f;
    [Tooltip("Distance from center, in pixels, while primed. Half a piece's length makes the pieces meet in a solid X")]
    [SerializeField] private float lockedSpread = 6f;
    [Tooltip("How fast the pieces snap between open and locked. Higher = snappier")]
    [SerializeField] private float snapSpeed = 30f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color primedColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField] private Color spentPipColor = new Color(1f, 1f, 1f, 0.15f);

    private Vector2[] pieceDirections;
    private float currentSpread;

    protected override FireArchetype Archetype { get { return archetype; } }

    protected override void Awake()
    {
        base.Awake();
        pieceDirections = GetDirectionsFromCenter(xPieces);
        currentSpread = openSpread;
    }

    protected override void Update()
    {
        base.Update();

        bool isPrimed = archetype.IsPrimed;
        Color color = ApplyFizzleTint(isPrimed ? primedColor : idleColor);

        float targetSpread = isPrimed ? lockedSpread : openSpread;
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, snapSpeed * Time.deltaTime);
        PlacePieces(xPieces, pieceDirections, currentSpread, color);

        centerDot.enabled = !isPrimed;
        centerDot.color = color;

        UpdateLifetimePips(isPrimed);
    }

    private void UpdateLifetimePips(bool isPrimed)
    {
        int beatsLeft = archetype.BeatsLeftToLaunch;
        int visiblePipCount = Mathf.Min(lifetimePips.Length, archetype.PrimeLifetimeBeats);

        for (int i = 0; i < lifetimePips.Length; i++)
        {
            bool isVisible = isPrimed && i < visiblePipCount;
            lifetimePips[i].enabled = isVisible;

            bool isBeatStillAvailable = i < beatsLeft;
            lifetimePips[i].color = isBeatStillAvailable ? primedColor : spentPipColor;
        }
    }
}
