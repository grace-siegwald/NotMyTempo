using UnityEngine;

public class ProjectileShoot : MonoBehaviour
{
    public int gunDamage; // Damage dealt by the projectile
    public float hitForce; // Force applied to the object hit by the projectile

    private void OnCollisionEnter(Collision collision)
    {
        // Don't react to the player who fired this
        if (collision.gameObject.CompareTag("Player"))
            return;

        // Can whatever we hit (or its parents) take damage?
        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(gunDamage);
            Debug.Log("Projectile dealt " + gunDamage + " damage to: " + collision.gameObject.name);
        }

        // Push anything with a Rigidbody away from the impact
        if (collision.rigidbody != null)
        {
            collision.rigidbody.AddForce(-collision.GetContact(0).normal * hitForce);
        }

        // The projectile is used up whatever it hit
        Destroy(gameObject);
    }
}
