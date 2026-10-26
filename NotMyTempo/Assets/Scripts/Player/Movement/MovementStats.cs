using UnityEngine;

/// <summary>
/// Every movement tuning number in one asset. Edits made in Play mode are kept after you stop,
/// because this is an asset on disk, not a component in the scene.
/// Create one with Assets > Create > Not My Tempo > Movement Stats.
/// COOL COOL COOL
/// </summary>
[CreateAssetMenu(fileName = "MovementStats", menuName = "Not My Tempo/MovementStats")]
public class MovementStats : ScriptableObject
{
    // Ground Stuff
    [Header("Ground")] // all speeds are in m/s, all accelerations are in m/s^2
    [Tooltip("Top speed, m/s")]
    [SerializeField] private float _groundSpeed = 10f; // top speed
    [Tooltip("How quickly you get to top speed, m/s^2.  Higher = snappier")]
    [SerializeField] private float _groundAcceleration = 100f; // how quickly you get to top speed / Higher = snappier
    [Tooltip("How quickly you stop, m/s^2")]
    [SerializeField] private float _groundDeceleration = 80f; // how quickly you stop / Higher = stop quicker
    [Tooltip("How quickly speed above your run speed bleeds off on the ground, m/s². Lower = momentum lasts longer")]
    [SerializeField] private float _overspeedDeceleration = 20f;
    [Tooltip("Seconds after landing before extra speed starts bleeding off. Jump inside this window to keep all of it")]
    [SerializeField] private float _landingGraceTime = 0.12f;

    // Air Stuff
    [Header("Air")]
    [Tooltip("Downward acceleration, m/s^2")]
    [SerializeField] private float _gravity = 30f; // downward acceleration
    [Tooltip("Gravity multiplier while falling. Above 1 = snappy jumps and heavy landings")]
    [SerializeField] private float _fallGravityMultiplier = 1.4f; // gravity gets "heavier" while falling, heavier landings
    [Tooltip("Cap for fall speed, m/s")]
    [SerializeField] private float _maxFallSpeed = 50f; // cap for fall speed
    [Tooltip("Top speed you can reach in the air through input, m/s")]
    [SerializeField] private float _airSpeed = 8f; 
    [Tooltip("The strength of your air steering. Higher = sharper turns in the air, m/s^2")]
    [SerializeField] private float _airAcceleration = 40f;

    // Jump Stuff 
    [Header("Jump")]
    [Tooltip("The height of a jump, meters")]
    [SerializeField] private float _jumpHeight = 2.5f; // just the height of a jump 
    [Tooltip("Number of extra jumps allowed in air")]
    [SerializeField] private int _airJumps = 1; // just one feels fine to me lol
    [Tooltip("Time after leaving a ledge you can still jump, as if grounded / Higher = more forgiving")]
    [SerializeField] private float _coyoteTime = 0.12f; // the higher the more forgiving 

    // Dash Stuff
    [Header("Dash")]
    [Tooltip("Speed during a dash, m/s")]
    [SerializeField] private float _dashSpeed = 30f; 
    [Tooltip("How long a dash lasts, seconds. Distance = speed x duration")]
    [SerializeField] private float _dashDuration = 0.15f;
    [Tooltip("Dashes you can store")]
    [SerializeField] private int _dashCharges = 3;
    [Tooltip("Seconds to recharge one dash")]
    [SerializeField] private float _dashRechargeTime = 1f;
    [Tooltip("Lowest speed you leave a dash with, m/s. You never leave slower than you entered")]
    [SerializeField] private float _dashExitSpeed = 14f;


    // Public Properties (read only, cannot be changed by other scripts)
    // -- Ground --
    public float GroundSpeed => _groundSpeed;
    public float GroundAcceleration => _groundAcceleration;
    public float GroundDeceleration => _groundDeceleration;
    public float OverspeedDeceleration => _overspeedDeceleration;
    public float LandingGraceTime => _landingGraceTime;
    // -- Air --
    public float Gravity => _gravity;
    public float FallGravityMultiplier => _fallGravityMultiplier;
    public float MaxFallSpeed => _maxFallSpeed;
    public float AirSpeed => _airSpeed;
    public float AirAcceleration => _airAcceleration;
    // -- Jump --
    public float JumpHeight => _jumpHeight;
    public int AirJumps => _airJumps;
    public float CoyoteTime => _coyoteTime;
    // -- Dash --
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public int DashCharges => _dashCharges;
    public float DashRechargeTime => _dashRechargeTime;
    public float DashExitSpeed => _dashExitSpeed;
}
