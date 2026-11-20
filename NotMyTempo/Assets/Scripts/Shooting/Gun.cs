using System.Collections;
using UnityEditor.EditorTools;
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
    [Tooltip("Spawned once per bullet to draw the shot")]
    [SerializeField] private BeamTracer beamPrefab;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileForce = 1500f;

    private Camera _camera;
    private AudioSource _audio;
    private WeaponModifier[] _modifiers; // array of modifiers the gun can have
    private float nextFireTime;

    public enum DeliveryMethod { Hitscan, Projectile }

    public bool IsReady { get { return Time.time >= nextFireTime; } }

    // gets all the coponents we need references to, and removes the Player layer from the shootable layers so you can't shoot yourself
    private void Awake()
    {
        _camera = GetComponentInParent<Camera>();
        _audio = GetComponent<AudioSource>();
        _modifiers = GetComponents<WeaponModifier>(); // plural, gets all of them modifiers
        shootableLayers &= ~LayerMask.GetMask("Player");
    }

    // Fires one shot right now with the given damage multiplier (from the archetype). The archetype decides when to call this.
    public void Fire(float damageMultiplier)
    {
        // sets the next time the gun can fire
        nextFireTime = Time.time + fireCooldown;
        
        ShotData shot = BuildBaseShot(damageMultiplier); // build the shot
        ApplyModifiers(shot);

        // fires the shot based on delivery method
        if (deliveryMethod == DeliveryMethod.Projectile)
        {
            FireProjectile(shot.Damage);
        }
        else
        {
            FireHitscan(shot);
        }
    }

    private void FireHitscan(ShotData shot)
    {
        _audio.Play();
        foreach (Vector3 direction in shot.Directions)
        {
            TraceShot(shot, direction);
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
    
    // Building shot data
    private ShotData BuildBaseShot(float damageMultiplier)
    {
        return new ShotData()
        {
            Damage = damage * damageMultiplier,
            Range = range,
            Origin = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f)), // the center of the camera's viewport
            Directions = {_camera.transform.forward},
            PierceCount = 0,
            BeamDuration = laserDuration
        };
    }
    // Applying modifiers
    private void ApplyModifiers(ShotData shot)
    {
        foreach (var modifier in _modifiers)
        {
            if (!modifier.isActiveAndEnabled)
                continue; 
            modifier.ModifyShot(shot);
        }
    }
    // Render the shot in game, calculate its interactions
    private void TraceShot(ShotData shot, Vector3 direction)
    {
        Vector3 endPoint = shot.Origin + direction * shot.Range; // where the beam ends if it hits nothing
        bool didHit = Physics.Raycast(shot.Origin, direction, out RaycastHit hit, range, shootableLayers, QueryTriggerInteraction.Ignore);
        if (didHit)
        {
            endPoint = hit.point;

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(shot.Damage);
            }

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * hitForce);
            }

            NotifyImpact(shot, hit.point);
        }

        BeamTracer beam = Instantiate(beamPrefab);
        beam.Show(muzzle.position, endPoint, shot.BeamDuration);
    }
    private void NotifyImpact(ShotData shot, Vector3 point)
    {
        foreach (var modifier in _modifiers)
        {
            if (!modifier.isActiveAndEnabled)
                continue; 
            modifier.OnImpact(shot, point);
        }
    }
}

