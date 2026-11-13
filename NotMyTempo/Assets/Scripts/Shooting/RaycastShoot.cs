using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;

public class RaycastShoot : MonoBehaviour 
{
    [Tooltip("Damage multiplier for shots fired on the beat")]
    [SerializeField] private float onBeatDamageMultiplier = 2f;

    public float gunDamage = 1f; // Set the number of hitpoints that this gun will take away from shot objects with a health script
    public float fireRate = 0.25f; // Number in seconds which controls how often the player can fire
    public float weaponRange = 50f; // Distance in Unity units over which the player can fire
    public float hitForce = 100f; // Amount of force which will be added to objects with a rigidbody shot by the player
    public Transform gunEnd; // Holds a reference to the gun end object, marking the muzzle location of the gun
    [Tooltip("Layers the gun can hit. Untick Player so you can't shoot yourself.")]
    public LayerMask shootableLayers = ~0; // ~0 means "Everything"

    private Camera fpsCam; // Holds a reference to the first person camera
    private WaitForSeconds shotDuration = new WaitForSeconds(0.07f); // WaitForSeconds object used by our ShotEffect coroutine, determines time laser line will remain visible
    private AudioSource gunAudio; // Reference to the audio source which will play our shooting sound effect
    private LineRenderer laserLine; // Reference to the LineRenderer component which will display our laserline
    private float nextFire; // Float to store the time the player will be allowed to fire again, after firing

    private WeaponOverheats overheat;
    private Conductor conductor;

    [SerializeField] private InputActionReference fireAction; // Reference to the Input Action for firing
    [SerializeField] private InputActionReference fireActionSecondary; // Reference to the Input Action for firing non-raycast projectiles

    public float ProjectileForce = 1500f; // Force applied to the projectile when fired

    [SerializeField] private GameObject projectilePrefab; // Reference to the projectile prefab

    void Start () 
    {
        // Get and store a reference to our LineRenderer component
        laserLine = GetComponent<LineRenderer>();

        // Get and store a reference to our AudioSource component
        gunAudio = GetComponent<AudioSource>();

        // Get and store a reference to our Camera by searching this GameObject and its parents
        fpsCam = GetComponentInParent<Camera>();

        overheat = GetComponent<WeaponOverheats>();
        conductor = FindAnyObjectByType<Conductor>();
        // Never let the gun hit the player, whatever the Inspector says
        shootableLayers &= ~LayerMask.GetMask("Player");
    }


    void Update()
    {
        bool primary = fireAction?.action?.triggered == true;
        bool secondary = fireActionSecondary?.action?.triggered == true;

        if (primary == secondary || Time.time <= nextFire)
            return;

        if (overheat != null && !overheat.CanFire)
            return;

        bool onBeat = conductor != null && conductor.JudgeAction();

        if (overheat != null && !overheat.TryFire(onBeat))
            return;

        float damage = onBeat ? gunDamage * onBeatDamageMultiplier : gunDamage;

        if (primary)
        {
            nextFire = Time.time + fireRate;
            StartCoroutine(ShotEffect());

            Vector3 rayOrigin = fpsCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            laserLine.SetPosition(0, gunEnd.position);

            if (Physics.Raycast(rayOrigin, fpsCam.transform.forward, out RaycastHit hit,
                    weaponRange, shootableLayers, QueryTriggerInteraction.Ignore))
            {
                laserLine.SetPosition(1, hit.point);

                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    target.TakeDamage(damage);

                if (hit.rigidbody != null)
                    hit.rigidbody.AddForce(-hit.normal * hitForce);
            }
            else
            {
                laserLine.SetPosition(1, rayOrigin + fpsCam.transform.forward * weaponRange);
            }
        }
        else
        {
            ShootProjectile(damage);
        }
    }

    private void ShootProjectile(float damage)
    {
        nextFire = Time.time + fireRate;

        GameObject projectile = Instantiate(projectilePrefab, gunEnd.position, gunEnd.rotation);

        ProjectileShoot projectileShoot = projectile.GetComponent<ProjectileShoot>();
        projectileShoot.gunDamage = damage;
        projectileShoot.hitForce = hitForce;

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.AddForce(gunEnd.forward * ProjectileForce);
    }

    private IEnumerator ShotEffect()
    {
        // Play the shooting sound effect
        gunAudio.Play ();

        // Turn on our line renderer
        laserLine.enabled = true;

        //Wait for .07 seconds
        yield return shotDuration;

        // Deactivate our line renderer after waiting
        laserLine.enabled = false;
    }
}