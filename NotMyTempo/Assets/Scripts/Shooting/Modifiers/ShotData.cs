using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Everything about one shot. The Gun builds it, its WeaponModifiers change it, then the Gun fires whatever it describes.
/// Not MonoBehavior because it never sits on a GameObject
/// </summary>
public class ShotData
{   
    public float Damage {  get; set; } // damage per "pellet" (after the archetype multiplier if we keeeeeep that)
    public float Range { get; set; } // range of the weapon, obviously
    public Vector3 Origin { get; set; } // where the ray starts (center of camera)
    public List<Vector3> Directions { get; } = new List<Vector3>(); // every "pellet" has a direction
    public int PierceCount { get; set; } // number of enemies a shot can pierce
    public float BeamDuration { get; set; } // how long a beam lingers visually
}
