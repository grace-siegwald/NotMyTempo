using UnityEngine;

/// <summary>
/// Single Shot: tap to fire.
/// Shots on the beat get bonus damage. Off-beat shots still fire, at base damage.
/// </summary>
public class SingleShotArchetype : FireArchetype
{
    [Header("Single Shot")]
    [Tooltip("Damage multiplier for shots fired on the beat")]
    [SerializeField] private float onBeatDamageMultiplier = 2f;

    // the base class already has the object references and input actions, so we don't need to get them again here

    // --- Unity methods ---
    private void Update()
    {
        bool wantsToFire = FireInput.WasPressedThisFrame() && Gun.IsReady; // true if the player pressed the fire button this frame AND gun is ready to fire
        if (!wantsToFire) // if the player didn't press anything, or the gun is on cooldown, get outa here !
            return;

        bool isOnBeat = Conductor.JudgeAction(); // true if the player pressed on the beat, false if off-beat
        float damageMultiplier = isOnBeat ? onBeatDamageMultiplier : 1f; // if on-beat, use the bonus multiplier, otherwise just use 1x

        Gun.Fire(damageMultiplier); // tell the gun to fire with the appropriate damage multiplier
        NotifyFired(); // notify any listeners that a shot was fired
    }
}
