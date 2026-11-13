using UnityEngine;

/// <summary>
/// Charge Shot: hold + release.
/// Press on a beat to start charging. The charge is full after a set number of beats.
/// Release on a beat once it's full to fire a powerful shot.
/// Starting off the beat, releasing before it's full, releasing off the beat,
/// or holding too long all fizzle the shot.
/// </summary>
public class ChargeShotArchetype : FireArchetype
{
    // --- Inspector fields ---
    [Header("Charge Shot")]
    [Tooltip("Beats it takes to fully charge.")]
    [Min(1)] // sets minimum value
    [SerializeField] private int beatsToFullCharge = 4;
    [Tooltip("Extra beats a full charge can be held before it fizzles out.")]
    [Min(0)] // sets minimum value
    [SerializeField] private int beatsBeforeFizzle = 2;
    [Tooltip("Damage multiplier for a sucessful charge shot.")]
    [SerializeField] private float chargedDamageMultiplier = 4f;

    // --- Properties ---
    private int chargeStartBeat; // the beat the player started charging on
    public bool IsCharging { get; private set; } // true if the player is currently charging a shot
    public float ChargeProgress // 0 = not charged, 1 = fully charged, >1 = overcharged
    {
        get
        {
            if (!IsCharging) // if the player isn't charging, return 0
                return 0f;
            // returns a value between 0 and 1, based on how many beats have passed since the player started charging
            return Mathf.Clamp01(BeatsSince(chargeStartBeat) / beatsToFullCharge); // dividing by beatsToFullCharge gives a value between 0 and 1, and Clamp01 makes sure it doesn't go below 0 or above 1
        }
    }
    public bool IsFullyCharged { get { return ChargeProgress >= 1f; } } // true if the shot is fully charged
    private bool HasOvercharged // returns true if the player is holding the charge past the last beat they can release on before it fizzles
    {
        get
        {
            int lastReleaseBeat = chargeStartBeat + beatsToFullCharge + beatsBeforeFizzle; // the last beat the player can release before it fizzles
            return NearestBeat() > lastReleaseBeat; // true if the player is holding the charge past the last release beat
        }
    }

    // --- Unity methods ---
    protected override void CancelShot() // base class calls this OnDisable
    {
        IsCharging = false; // stop charging
    }
    private void Update() // each frame, at most one of these three things happen :)
    {
        if (FireInput.WasPressedThisFrame()) // if the player pressed the fire button this frame
            StartCharging();
        else if (IsCharging && FireInput.WasReleasedThisFrame()) // if the player released the fire button this frame while charging
            ReleaseCharge();
        else if (IsCharging && HasOvercharged) // if the player is holding the charge past the last beat they can release on before it fizzles
            Fizzle();
    }
    private void StartCharging() // called when the player presses the fire button
    {
        bool isOnBeat = Conductor.JudgeAction(); // true if the player pressed on the beat, false if off-beat
        if (!isOnBeat) // if the player pressed off-beat, fizzle the shot , get outa here 
        {
            Fizzle();
            return;
        }

        IsCharging = true; // start charging
        chargeStartBeat = NearestBeat(); // record the beat the player started charging on
    }   
    private void ReleaseCharge() // called when the player releases the fire button
    {
        int beatsHeld = NearestBeat() - chargeStartBeat; // how many beats the player held the charge for
        bool isChargeComplete = beatsHeld >= beatsToFullCharge; // true if the player held the charge for at least the required number of beats
        if (!isChargeComplete) // if the player released before the charge was complete, fizzle the shot
        {
            Fizzle();
            return;
        }
        bool isOnBeat = Conductor.JudgeAction(); // true if the player released on the beat, false if off-beat
        if (!isOnBeat) // if the player released off-beat, fizzle the shot
        {
            Fizzle();
            return;
        }
        
        IsCharging = false; // stop charging
        Gun.Fire(chargedDamageMultiplier); // tell the gun to fire with the appropriate damage multiplier
        NotifyFired(); // notify any listeners that a shot was fired
    }
    private void Fizzle() // called when the player fizzes out the shot
    {
        IsCharging = false; // stop charging
        NotifyFizzled(); // notify any listeners that the shot fizzled
    }
}
