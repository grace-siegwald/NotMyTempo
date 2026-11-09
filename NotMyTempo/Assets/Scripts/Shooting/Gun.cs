using System.Collections;
using UnityEngine;

/// <summary>
/// Knows what a shot does :)
/// The weapon "type": what a shot does once it's fired (damage, range, hitscan or projectile, effects).
/// It never reads input and never checks the beat. A FireArchetype on the same object decides
/// WHEN to call Fire(), so any gun type can use any archetype.
/// </summary>
public class Gun : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Damage of one shot before the archetype's multiplier is applied")]
    [SerializeField] private float damage = 1f;
    [Tooltip("Minimum seconds between shots")]
    [SerializeField] private float fireCooldown = 0.25f;
    [Tooltip("Force added to rigidbodies that get hit")]
    [SerializeField] private float hitForce = 100f;

    [Header("Delivery")]
    [Tooltip("Hitscan = instant raycast with a laser line. Projectile = spawns a physical bullet")]
    [SerializeField] private DeliveryMethod deliveryMethod = DeliveryMethod.Hitscan;
    [Tooltip("The muzzle. Laser lines and projectiles start here")]
    [SerializeField] private Transform muzzle;

    [Header("Hitscan")]
    [Tooltip("How far a hitscan shot reaches, in Unity units")]
    [SerializeField] private float range = 50f;
    [Tooltip("Layers the gun can hit. The Player layer is always removed so you can't shoot yourself")]
    [SerializeField] private LayerMask shootableLayers;
    [Tooltip("How long the laser line stays visible, in seconds")]
    [SerializeField] private float laserDuration = 0.07f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileForce = 1500f;

    private Camera _camera;
    private AudioSource _audio;
    private LineRenderer laserLine;
    private WaitForSeconds laserWait;
    private float nextFireTime;

    public enum DeliveryMethod { Hitscan, Projectile }

    public bool IsReady { get { return Time.time >= nextFireTime; } }

    // Fires one shot right now with the given damage multiplier (from the archetype). The archetype decides when to call this.
    public void Fire(float damageMultiplier)
    {
        // sets the next time the gun can fire
        nextFireTime = Time.time + fireCooldown;
        // calculates the damage of this shot, factoring in the archetype's multiplier
        float d = damage * damageMultiplier;

        // fires the shot based on delivery method
        if (deliveryMethod == DeliveryMethod.Projectile)
        {
            FireProjectile(d);
        }
        else
        {
            FireHitscan(d);
        }
    }

    // gets all the coponents we need references to, and removes the Player layer from the shootable layers so you can't shoot yourself
    private void Awake()
    {
        _camera = GetComponentInParent<Camera>();
        _audio = GetComponent<AudioSource>();
        laserLine = GetComponent<LineRenderer>();
        // creates a WaitForSeconds object for the laser duration so we don't have to keep creating new ones every time we shoot
        laserWait = new WaitForSeconds(laserDuration);
        shootableLayers &= ~LayerMask.GetMask("Player");
    }

    private void FireHitscan(float damage)
    {
        StartCoroutine(PlayShotEffect());

        Vector3 rayOrigin = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f)); // the center of the camera's viewport
        Vector3 rayDirection = _camera.transform.forward; // the direction the camera is facing
        laserLine.SetPosition(0, muzzle.position); // the bulllet starts at the muzzle

        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range, shootableLayers, QueryTriggerInteraction.Ignore);
        if (!didHit) // if the raycast didn't hit anything, we just draw the laser line to the max range and return
        {
            laserLine.SetPosition(1, rayOrigin + rayDirection * range);
            return;
        }

        laserLine.SetPosition(1, hit.point); // if we hit something, we draw the laser line to the hit point

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>(); // get the IDamageable component on the hit object
        if (target != null)
        {
            target.TakeDamage(damage); // deals damage to the target if it has an IDamageable component
        }

        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForce(-hit.normal * hitForce); // adds force to the hit rigidbody (if it has one)
        }
    }
    private void FireProjectile(float damage)
    {
        GameObject projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation); // spawn a projectile at the muzzle position

        ProjectileShoot projectileShoot = projectile.GetComponent<ProjectileShoot>(); // get the ProjectileShoot component on the projectile
        projectileShoot.gunDamage = damage; // assign the damage to the projectile
        projectileShoot.hitForce = hitForce; // assign the hit force to the projectile

        projectile.GetComponent<Rigidbody>().AddForce(muzzle.forward * projectileForce); // add force to the projectile's rigidbody to shoot it forward
    }

    // plays the shot effect (sound and laser line) for a short duration
    private IEnumerator PlayShotEffect()
    {
        _audio.Play();
        laserLine.enabled = true;
        yield return laserWait;
        laserLine.enabled = false;
    }

}

