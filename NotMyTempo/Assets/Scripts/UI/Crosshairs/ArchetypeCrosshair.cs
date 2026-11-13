using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared behaviour for the archetype crosshairs: shakes and flashes the fizzle color
/// when the player loses a shot. Subclasses handle what's unique to their archetype.
/// Put a CrosshairPulse on the same object for the on-beat pulse.
/// </summary>
[RequireComponent(typeof(RectTransform))] // to make sure were on a UI object
public abstract class ArchetypeCrosshair : MonoBehaviour // abstract because it's a base class for the different crosshair types
{
    // --- Inspector fields ---
    [Header("Fizzle Feedback")]
    [Tooltip("How long the shake and red flash last, in seconds")]
    [SerializeField] private float fizzleDuration = 0.35f;
    [Tooltip("How far the crosshair shakes, in pixels")]
    [SerializeField] private float fizzleShakeDistance = 6f;
    [SerializeField] private Color fizzleColor = new Color(1f, 0.2f, 0.25f);

    // --- Properties ---
    private RectTransform rectTransform;
    private Vector2 restingPosition;
    private float fizzleEndTime;
    
    protected abstract FireArchetype Archetype { get; } // Abstract so that the children MUST provide an archetype
    protected float FizzleAmount // stores when the efffect ends and works out how much is left (countdown from 0 to 1, hence the Mathf.Clamp01)
    {
        get
        {
            return Mathf.Clamp01((fizzleEndTime - Time.time)  / fizzleDuration);
        }
    }

    // --- Methods --- 
    protected virtual void Awake()
    {
        rectTransform = (RectTransform)transform; // casting because we gotta
        restingPosition = rectTransform.anchoredPosition; // middle, anchor position
    }
    protected virtual void OnEnable() // subscribe to fizzled action
    {
        Archetype.Fizzled += HandleFizzled;
    }
    protected virtual void OnDisable() // unsubscribes from fizzled action, cancels any shake, and snaps back to center
    {
        Archetype.Fizzled -= HandleFizzled; // unsubscribe
        fizzleEndTime = 0f; // cancels shake
        rectTransform.anchoredPosition = restingPosition; // snap to center
    }
    protected virtual void Update()
    {
        Vector2 shakeOffset = Random.insideUnitCircle * (fizzleShakeDistance * FizzleAmount); // random shake offset, zero when there's no FizzleAmount
        rectTransform.anchoredPosition = restingPosition + shakeOffset; // do the shakey shake
    }
    protected Color ApplyFizzleTint(Color stateColor) // helper for blending between normal and fizzle colors
    {
        return Color.Lerp(stateColor, fizzleColor, FizzleAmount);
    }

    private void HandleFizzled() // fires on a fizzle
    {
        fizzleEndTime = Time.time + fizzleDuration;
    }


    // -- helpers for placing pieces of crosshair around the center --
    protected static Vector2[] GetDirectionsFromCenter(Graphic[] pieces)
    {
        Vector2[] directions = new Vector2[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            directions[i] = pieces[i].rectTransform.anchoredPosition.normalized;
        }
        return directions;
    }
    protected static void PlacePieces(Graphic[] pieces, Vector2[] directions, float distanceFromCenter, Color color)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].rectTransform.anchoredPosition = directions[i] * distanceFromCenter;
            pieces[i].color = color;
        }
    }
    /// <summary>
    /// Remembers each piece's size from the editor, so lengths can be scaled from it later.
    /// </summary>
    protected static Vector2[] GetRestingSizes(Graphic[] pieces)
    {
        Vector2[] sizes = new Vector2[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            sizes[i] = pieces[i].rectTransform.sizeDelta;
        }
        return sizes;
    }

    /// <summary>
    /// Stretches or shrinks each piece along its long side only, so thin ticks stay thin.
    /// 1 = editor length, 0.5 = half as long.
    /// </summary>
    protected static void ScalePieceLengths(Graphic[] pieces, Vector2[] restingSizes, float lengthScale)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            Vector2 size = restingSizes[i];
            bool isTall = size.y >= size.x;
            if (isTall)
            {
                size.y *= lengthScale;
            }
            else
            {
                size.x *= lengthScale;
            }
            pieces[i].rectTransform.sizeDelta = size;
        }
    }
}
