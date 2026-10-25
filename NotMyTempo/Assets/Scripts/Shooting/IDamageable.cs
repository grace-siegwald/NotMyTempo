using UnityEngine;

/// <summary>
/// Anything the player can shoot: enemies, props, etc.
/// The gun and projectiles only look for this, so they don't need to know
/// what kind of thing they hit.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
