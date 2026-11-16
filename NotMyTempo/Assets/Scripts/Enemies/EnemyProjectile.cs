using UnityEngine;

/// <summary>
/// A projectile fired by an enemy. Flies in a straight line and damages the player on hit.
/// Uses a sphere cast each frame instead of a Rigidbody, so fast shots can't pass
/// through thin things and the prefab needs no physics setup.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Seconds before the projectile removes itself if it hasn't hit anything")]
    [SerializeField] private float _lifetime = 5f;

    [Tooltip("How fat the projectile is for hit detection, in meters")]
    [SerializeField] private float _radius = 0.15f;

    [Tooltip("What stops the projectile: the player's layer plus walls. Leave enemy layers out so they don't block each other's shots")]
    [SerializeField] private LayerMask _hitLayers = ~0;

    private Vector3 _velocity;
    private float _damage;

    /// <summary>Sends the projectile on its way. Call this right after Instantiate.</summary>
    public void Launch(Vector3 velocity, float damage)
    {
        _velocity = velocity;
        _damage = damage;
        Destroy(gameObject, _lifetime);
    }

    private void Update()
    {
        Vector3 step = _velocity * Time.deltaTime;
        float stepDistance = step.magnitude;
        if (stepDistance < 0.0001f)
            return;

        bool hitSomething = Physics.SphereCast(transform.position, _radius, step / stepDistance,
            out RaycastHit hit, stepDistance, _hitLayers, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(_damage);
            }

            Destroy(gameObject);
            return;
        }

        transform.position += step;
    }
}
