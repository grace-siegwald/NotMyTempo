using UnityEngine;

/// <summary>
/// Changes what a shot does (spread, pierce, aoe, etc), as opposed to an archetype, which changes how a shot is fired.
/// Can put multiple modifiers on one gun at the same time
/// Modifiers should always multiply or add to ShotData values, never overwrite them (so that multiple modifiers can work)
/// 
///     Three steps to a weapon modifier. Gun.Fire() runs in three seperate stages:
///         1. We build a ShotData object that holds the damage, range, a list of directions, how many enemies it can pierce, and how long the beam will stay
///         2. We check how many modifiers are on the gun, and each modifier gets to change that ShotData
///         3. We actually run the shot: trace each direction, apply the correct damange, and report each impact point back
/// </summary>

[RequireComponent(typeof(Gun))]
public abstract class WeaponModifier : MonoBehaviour // abstract so no one can just add one of these as a component (no no no!)
{
    public virtual void ModifyShot(ShotData shot) { } // Called once per shot before it fires. This is where the shot "changes"
    public virtual void OnImpact(ShotData shot, Vector3 point) { } // Called once per "pellet" at the point where that pellet impacted
    protected virtual void OnDisable() { } // Just here to make Unity show the enable checkbox in the Inspector (also might need to overwrite in subclasses)
}
