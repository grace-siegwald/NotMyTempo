using System.ComponentModel;
using UnityEngine;

/// <summary>
/// Prime Shot: tap + tap.
/// Alt-fire on a beat to prime (ready) the shot, then fire on a LATER beat to launch it.
/// The prime only lasts a few beats. Priming off the beat, launching off the beat,
/// firing without a prime, or letting the prime run out all fizzle.
/// </summary>
public class PrimeShotArchetype : FireArchetype
{
    // --- Inspector fields ---
    [Header("Prime Shot")]
    [Tooltip("How many beats after priming you can still launch. 4 = any of the next 4 beats")]
    [Min(1)]
    [SerializeField] private int primeLifetimeBeats = 4;
    [Tooltip("Damage multiplier for a successful launch")]
    [SerializeField] private float primedDamageMultiplier = 3f;

    // --- Properties ---
    private int primedBeat; // the beat number when the gun was primed.
    public bool IsPrimed { get; private set; }
    public int PrimeLifetimeBeats { get { return primeLifetimeBeats; } } // for the crosshair to read, readonly

    public int BeatsLeftToLaunch // the countdown the crosshair draws 
    {
        get
        {
            if (!IsPrimed) // if the gun isn't primed, return 0
                return 0;
            int lastLaunchBeat = primedBeat + primeLifetimeBeats; // the last beat the player can launch on before it fizzles
            return Mathf.Clamp(lastLaunchBeat - Conductor.BeatNumber, 0, primeLifetimeBeats);
        }
    }
    private bool HasPrimeExpired
    {
        get { return NearestBeat() > primedBeat + primeLifetimeBeats; }
    }

    // --- Methods ---
    protected override void Awake() // check for the alt fire input action, if it's not there, disable the component
    {
        base.Awake();
        if (AltFireInput == null)
        {
            Debug.LogError(name + ": Prime Shot needs an Alt Fire Action assigned.", this);
            enabled = false;
        }
    }
    protected override void CancelShot() // base class calls this OnDisable
    {
        IsPrimed = false; // stop charging
    }
    private void Update() // each frame, at most one of these three things happen :)
    {
        if (AltFireInput.WasPressedThisFrame())
            Prime();
        else if (FireInput.WasPressedThisFrame())
            Release();
        else if (IsPrimed && HasPrimeExpired)
            Fizzle();
    }
    private void Prime() // prime da shot
    {
        bool isOnBeat = Conductor.JudgeAction();
        if (!isOnBeat)
        {
            Fizzle();
            return;
        }

        IsPrimed = true;
        primedBeat = NearestBeat();
    }
    private void Release() // release da shot
    {
        if (!IsPrimed)
        {
            Fizzle();
            return;
        }

        bool isSameBeatAsPrime = NearestBeat() == primedBeat;
        if (isSameBeatAsPrime)
        {
            return;
        }

        bool isOnBeat = Conductor.JudgeAction();
        if (!isOnBeat)
        {
            Fizzle();
            return;
        }

        IsPrimed = false;
        Gun.Fire(primedDamageMultiplier);
        NotifyFired();
    }
    private void Fizzle() // fizzle da shot
    {
        IsPrimed = false;
        NotifyFizzled();
    }
}
