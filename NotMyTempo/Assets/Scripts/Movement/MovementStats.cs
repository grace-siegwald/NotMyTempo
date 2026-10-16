using UnityEngine;

/// <summary>
/// Every movement tuning number in one asset. Edits made in Play mode are kept after you stop,
/// because this is an asset on disk, not a component in the scene.
/// Create one with Assets > Create > Not My Tempo > Movement Stats.
/// COOL COOL COOL
/// </summary>
[CreateAssetMenu(fileName = "MovementStats", menuName = "Scriptable Objects/MovementStats")]
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

    // Air Stuff
    [Header("Air")]
    [Tooltip("Downward acceleration, m/s^2")]
    [SerializeField] private float _gravity = 30f; // downward acceleration
    [Tooltip("Gravity multiplier while falling. Above 1 = snappy jumps and heavy landings.")]
    [SerializeField] private float _fallGravityMultiplier = 1.4f; // gravity gets "heavier" while falling, heavier landings
    [Tooltip("Cap for fall speed, m/s")]
    [SerializeField] private float _maxFallSpeed = 50f; // cap for fall speed
    [Tooltip("Top speed you can reach in the air through input, m/s. Higher than ground speed because FUN")]
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


    // Public Properties (read only, cannot be changed by other scripts)
    // -- Ground --
    public float GroundSpeed => _groundSpeed;
    public float GroundAcceleration => _groundAcceleration;
    public float GroundDeceleration => _groundDeceleration;
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
}
