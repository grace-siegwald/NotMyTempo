using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public int gunDamage; // Damage dealt by the projectile
    public float hitForce; // Force applied to the object hit by the projectile

    [SerializeField] private Collider collider; // Reference to the collider of the projectile

    void Awake()
    {
        // Get the collider component attached to the projectile
        collider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Projectile collided with: " + collision.gameObject.name  + " and got destroyed.");

            // Check if the projectile collided with an object that has a ShootableBox component
            ShootableBox health = collision.collider.GetComponent<ShootableBox>();

            // If there was a health script attached
            if (health != null)
            {
                // Call the damage function of that script, passing in our gunDamage variable
                health.Damage(gunDamage);
                Debug.Log("Projectile dealt " + gunDamage + " damage to: " + collision.gameObject.name);
            }

            // Check if the object we hit has a rigidbody attached
            if (collision.rigidbody != null)
            {
            // Add force to the rigidbody we hit, in the direction from which it was hit
            collision.rigidbody.AddForce(-collision.contacts[0].normal * hitForce);
            }

            // Destroy the projectile after it hits something
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Projectile collided with Player and did not get destroyed");
        }
        else
        {
            Debug.Log("Projectile collided with: " + collision.gameObject.name + " and got destroyed.");
            // Destroy the projectile after it hits something
            Destroy(gameObject);
        }
    }
}
