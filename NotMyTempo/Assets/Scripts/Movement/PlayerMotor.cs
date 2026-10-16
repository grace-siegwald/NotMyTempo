using UnityEngine;

/// <summary>
/// Handles all the velocity stuff and moves the CharacterController (hence "motor"). Also manages running and gravity!
/// The Player's pivot MUST be at its feet or else things break
/// Long story short, it does a bunch of velocity shit for different player states 
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerInput))] // requires character controller and input reader
public class PlayerMotor : MonoBehaviour
{
    [Header("Ground")] // all speeds are in m/s, all accelerations are in m/s^2
    [Tooltip("Top speed ")]
    [SerializeField] private float _groundSpeed = 10f; // top speed
    [Tooltip("How quickly you get to top speed / Higher = snappier")]
    [SerializeField] private float _groundAcceleration = 100f; // how quickly you get to top speed / Higher = snappier
    [Tooltip("How quickly you stop")]
    [SerializeField] private float _groundDeceleration = 80f; // how quickly you stop
    [Tooltip("Small force on player to keep them pressed on ground")]
    [SerializeField] private float _groundStickyForce = 3f; // small force on player to keep them pressed on ground

    [Header("Air")]
    [Tooltip("Downward acceleration")]
    [SerializeField] private float _gravity = 20f; // downward acceleration
    [Tooltip("Cap for fall speed")]
    [SerializeField] private float _maxFallSpeed = 50f; // cap for fall speed

    private const float MinimumInputSqrMagnitude = 0.01f;

    // Object references
    private CharacterController _controller;
    private PlayerInput _inputReader;
    private Vector3 _velocity; // THE BIG HOMIE, THE MAN OF THE HOUR

    // Properties (using the get accessors to recalculate every time it's "read", like in Update !!)
    public Vector3 HorizontalVelocity { get { return new Vector3(_velocity.x, 0f, _velocity.z); } }
    public float HorizontalSpeed { get { return HorizontalVelocity.magnitude; } }
    public bool IsGrounded { get; private set; }

    // movement input as a direction along the ground, from 0 to 1 (because of length and such)
    public Vector3 WishDirection { get; private set; } // BASICALLY Where the player is "asking" or "wishes" to go this frame :)
    public bool HasMoveInput { get { return WishDirection.sqrMagnitude > MinimumInputSqrMagnitude; } }

    private void Awake() // runs as soon as the objects exists in the scene
    {
        _controller = GetComponent<CharacterController>();
        _inputReader = GetComponent<PlayerInput>();
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        IsGrounded = _controller.isGrounded; // doing here instead of with get accesor because awake needs to have the componenet instance first
        WishDirection = CalculateWishDirection();

        if (IsGrounded)
        {
            AccelerateOnGround(deltaTime);
            StickToGround();
        }
        else
        {
            ApplyGravity(deltaTime);
        }

        _controller.Move(_velocity * deltaTime); // FINALLY where the movement actually happens 
    }

    // Gets the direction the player wants to go, converts it to World Direction (input is local, need to convert it)
    private Vector3 CalculateWishDirection()
    {
        Vector2 input = _inputReader.MoveInput;
        return transform.forward * input.y + transform.right * input.x; // converting to world direction
    }
    
    // calculates the accleration of the player while on the ground
    private void AccelerateOnGround(float deltaTime) // passing in same time we use in Update
    {
        Vector3 targetVelocity = WishDirection * _groundSpeed; // velocity is almost always direction * speed, vector maths baby
        float rate;
        if (HasMoveInput)
        {
            rate = _groundAcceleration;
        }
        else
        {
            rate = _groundDeceleration;
        }
        // "Move Towards" brings the hori velocity to the target velocity at a constant pace
        Vector3 newVelocity = Vector3.MoveTowards(HorizontalVelocity, targetVelocity, rate * deltaTime);
        SetHorizontalVelocity(newVelocity);
    }
    
    // utility for setting hori velocity
    private void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        _velocity.x = horizontalVelocity.x;
        _velocity.z = horizontalVelocity.z;
    }
    
    // exactly as it sounds, utility for applying gravity to the player (with a max)
    private void ApplyGravity (float deltaTime)
    {
        _velocity.y = Mathf.Max(_velocity.y - _gravity * deltaTime, -_maxFallSpeed); // math.max to just give it a cap of the max fall speed
    }

    // utility for applying ground force to vertical velocity
    private void StickToGround()
    {
        _velocity.y = -_groundStickyForce;
    }
}