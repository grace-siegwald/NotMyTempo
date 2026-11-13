using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Decides when a shot happens :)
/// Base class for every firing archetype (Single Shot, Charge Shot, Prime Shot).
/// Reads the fire inputs, checks them against the beat, and tells the Gun when to fire.
/// Put any number of archetypes on a Gun. Only the ENABLED one does anything,
/// and it turns its own crosshair on and off with it.
/// </summary>

[RequireComponent(typeof(Gun))]
public abstract class FireArchetype : MonoBehaviour
{
    // --- Inspector fields ---
    [Header("Input")]
    [Tooltip("\"Player/Attack\" from the InputSystem_Actions asset")]
    [SerializeField] private InputActionReference fireAction;
    [Tooltip("\"Player/AltAttack\". Only archetypes with a second input (Prime Shot) use it")]
    [SerializeField] private InputActionReference altFireAction;

    [Header("Crosshair")]
    [Tooltip("The crosshair object for this archetype. Shown only while this archetype is enabled")]
    [SerializeField] private GameObject crosshair;

    // --- Events --- 
    public event Action Fired;    // a shot actually went off
    public event Action Fizzled;  // the player tried, but the timing was wrong and the shot was failed

    // --- Properties ---
    // protected so that only derived classes can access them
    protected Gun Gun { get; private set; }
    protected Conductor Conductor { get; private set; }
    protected InputAction FireInput { get { return fireAction.action; } }
    protected InputAction AltFireInput { get { return altFireAction != null ? altFireAction.action : null; } } // returns null if the archetype doesn't have a second input / one-line if-else statement


    // --- Unity methods ---
    protected virtual void Awake() // gets the Gun and Conductor references, checks for a Conductor, and shows the crosshair if it exists
    {
        Gun = GetComponent<Gun>(); 
        Conductor = FindAnyObjectByType<Conductor>(); 

        if (Conductor == null) // conductor check
        {
            Debug.LogError(name + ": no Conductor in the scene, so " + GetType().Name + " can't judge the beat.", this);
            enabled = false;
        }

        if (crosshair != null) // Only shows the crosshair if the archetype is enabled (and if it exists)
        {
            crosshair.SetActive(enabled);
        }
    }

    protected virtual void OnEnable() // turns on the input actions and crosshair when the archetype is enabled
    {
        FireInput.Enable(); // turn on the input actions
        if (AltFireInput != null) 
        {
            AltFireInput.Enable();
        }

        if (crosshair != null) // crosshair check
        {
            crosshair.SetActive(true);
        }
    }
    protected virtual void OnDisable() // turns off the input actions and crosshair when the archetype is disabled
    {
        CancelShot();

        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }
    }
    protected virtual void CancelShot() // empty base. Just Charge shot and Prime shot need this
    {
    }

    // --- Beat Helpers ---
    // Needed to correctly judge what beat an action happened
    protected int NearestBeat() // Rounds the current song time to the nearest beat and returns that beat number
    {
        return Mathf.RoundToInt(Conductor.SongTime / Conductor.SecondsPerBeat);
    }

    protected float BeatsSince(int beat) // returns same thing as above, but 
    {
        return Conductor.SongTime / Conductor.SecondsPerBeat - beat;
    }

    // --- Firing helpers ---
    protected void NotifyFired()
    {
        Fired?.Invoke();
    }
    protected void NotifyFizzled()
    {
        Fizzled?.Invoke();
    }
}
