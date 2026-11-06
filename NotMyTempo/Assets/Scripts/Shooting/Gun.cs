using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
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

    private Camera camera;
    private AudioSource audio;
    private LineRenderer laserLine;
    private WaitForSeconds laserWait;
    private float nextFireTime;

    public enum DeliveryMethod { Hitscan, Projectile }

    public bool IsReady { get { return Time.time >= nextFireTime; } }

    // Fires one shot right now with the given damage multiplier (from the archetype). The archetype decides when to call this.
    public void Fire(float damageMultiplier)
    {
        nextFireTime = Time.time + fireCooldown;
        float d = damage * damageMultiplier;

        if (deliveryMethod == DeliveryMethod.Projectile)
        {
            FireProjectile(d);
        }
        else
        {
            FireHitscan(d);
        }
    }

    private void Awake()
    {
        camera = GetComponentInParent<Camera>();
        audio = GetComponent<AudioSource>();
        laserLine = GetComponent<LineRenderer>();
        laserWait = new WaitForSeconds(laserDuration);
        shootableLayers &= ~LayerMask.GetMask("Player");
    }

    private void FireHitscan(float damage)
    {
        StartCoroutine(PlayShotEffect());

        Vector3 rayOrigin = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayDirection = camera.transform.forward;
        laserLine.SetPosition(0, muzzle.position);

        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range,
            shootableLayers, QueryTriggerInteraction.Ignore);
        if (!didHit)
        {
            laserLine.SetPosition(1, rayOrigin + rayDirection * range);
            return;
        }

        laserLine.SetPosition(1, hit.point);

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForce(-hit.normal * hitForce);
        }
    }

    private void FireProjectile(float damage)
    {
        GameObject projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

        ProjectileShoot projectileShoot = projectile.GetComponent<ProjectileShoot>();
        projectileShoot.gunDamage = damage;
        projectileShoot.hitForce = hitForce;

        projectile.GetComponent<Rigidbody>().AddForce(muzzle.forward * projectileForce);
    }

    private IEnumerator PlayShotEffect()
    {
        audio.Play();
        laserLine.enabled = true;
        yield return laserWait;
        laserLine.enabled = false;
    }
}

