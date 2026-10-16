using UnityEngine;

/// <summary>
/// Handles all the velocity stuff and moves the CharacterController (hence "motor"). Also manages running and gravity!
/// The Player's pivot MUST be at its feet or else things break
/// Long story short, it does a bunch of velocity shit for different player states 
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerInput))] // requires character controller and input reader
public class PlayerMotor : MonoBehaviour
{
    // Ground Stuff
    [Header("Ground")] // all speeds are in m/s, all accelerations are in m/s^2
    [Tooltip("Top speed ")]
    [SerializeField] private float _groundSpeed = 10f; // top speed
    [Tooltip("How quickly you get to top speed / Higher = snappier")]
    [SerializeField] private float _groundAcceleration = 100f; // how quickly you get to top speed / Higher = snappier
    [Tooltip("How quickly you stop")]
    [SerializeField] private float _groundDeceleration = 80f; // how quickly you stop / Higher = stop quicker
    [Tooltip("Small force on player to keep them pressed on ground")]
    [SerializeField] private float _groundStickyForce = 3f; // small force on player to keep them pressed on ground

    // Air Stuff
    [Header("Air")]
    [Tooltip("Downward acceleration")]
    [SerializeField] private float _gravity = 20f; // downward acceleration
    [Tooltip("Cap for fall speed")]
    [SerializeField] private float _maxFallSpeed = 50f; // cap for fall speed
    [Tooltip("Top speed you can reach in the air through input")]
    [SerializeField] private float _airSpeed = 12f; // higher than ground speed because FUN
    [Tooltip("The strength of your air steering. Higher = sharper turns in the air")]
    [SerializeField] private float _airAcceleration = 40f;

    // Ground *Detection* Stuff
    [Header("Ground Detection")]
    [Tooltip("Defines what the player can stand on")]
    [SerializeField] private LayerMask _groundMask = Physics.DefaultRaycastLayers; // defines what the player can stand on
    [Tooltip("How far below the feet we 'look' for the ground")]
    [SerializeField] private float _groundProbeDistance = 0.15f; // how far below the feet we "look" for the ground

    // Jump Stuff 
    [Header("Jump")]
    [Tooltip("The height of a jump, meters")]
    [SerializeField] private float _jumpHeight = 2.5f; // just the height of a jump dummy
    [Tooltip("Gravity multiplier while falling. Above 1 = snappy jumps and heavy landings.")]
    [SerializeField] private float _fallGravityMultiplier = 1.4f; // gravity gets "heavier" while falling, heavier landings

    // fields / components to get
    private CharacterController _controller;
    private PlayerInput _inputReader;
    private UnityEngine.Vector3 _velocity; // THE BIG HOMIE, THE MAN OF THE HOUR
    private const float RisingSpeedThreshold = 0.01f; // new stuff, not fully understood
    private const float GroundProbeRadiusScale = 0.9f; // new stuff, not fully understood
    private const float MinimumSlopeMoveSqrMagnitude = 0.0001f; // new stuff, not fully understood
    private const float MinimumInputSqrMagnitude = 0.01f;
    private float _lastGroundedTime = float.NegativeInfinity; 

    // Properties (using the get accessors to recalculate every time it's "read" in Update)
    public Vector3 HorizontalVelocity { get { return new Vector3(_velocity.x, 0f, _velocity.z); } }
    public float HorizontalSpeed { get { return HorizontalVelocity.magnitude; } }
    public bool IsGrounded { get; private set; } 
    public Vector3 GroundNormal { get; private set; } = Vector3.up; // the normal is the perpendicular vector pointing out from the plane
    public float TimeSinceGrounded { get { return Time.time - _lastGroundedTime; } } // new stuff, not fully understood

    public Vector3 WishDirection { get; private set; } // BASICALLY Where the player is "asking" or "wishes" to go this frame, from 1 to 0 :)
    public bool HasMoveInput { get { return WishDirection.sqrMagnitude > MinimumInputSqrMagnitude; } }

    private void Awake() // runs as soon as the objects exists in the scene
    {
        _controller = GetComponent<CharacterController>();
        _inputReader = GetComponent<PlayerInput>();
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateGrounded();
        WishDirection = CalculateWishDirection();

        if (IsGrounded)
        {
            AccelerateOnGround(deltaTime);
            StickToGround();
        }
        else
        {
            AccelerateInAir(deltaTime);
            ApplyGravity(deltaTime);
        }

        TryJump();

        MoveController(deltaTime); // FINALLY where the movement actually happens! 
    }

    private void TryJump()
    {
        bool canJump = _inputReader.HasBufferedJump && IsGrounded;
        if (!canJump) // if we can't jump, get outa here !
        {
            return;
        }
        _inputReader.ConsumeJump();

        IsGrounded = false;
        _velocity.y = Mathf.Sqrt(2f * _gravity * _jumpHeight); // I do NOT understand this maths bruv
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
        if (HasMoveInput) // if we have player input, accelerate
        {
            rate = _groundAcceleration;
        }
        else // decelerate 
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
        float gravity = _gravity;
        bool isFalling = _velocity.y < 0f; // decide if falling or not 

        if (isFalling)
        {
            gravity *= _fallGravityMultiplier; // multiply gravity by the gravity multiplier
        }

        _velocity.y = Mathf.Max(_velocity.y - _gravity * deltaTime, -_maxFallSpeed); // math.max to just give it a cap of the max fall speed
    }
    
    // for movement in the air, player isn't a dead fish y'know
    private void AccelerateInAir(float deltaTime)
    {
        if (!HasMoveInput) // if the player's not trying to do anything, get outa here
        {
            return; 
        }

        Vector3 horizontalVelocity = HorizontalVelocity;
        // speedCap takes whichever is bigger, your current speed (velocity magnitiude), or your current air speed
        // SO momentum you currently have is never taken away !
        float speedCap = Mathf.Max(horizontalVelocity.magnitude, _airSpeed); 
        Vector3 steeredVelocity = horizontalVelocity + WishDirection * (_airAcceleration * deltaTime);
        SetHorizontalVelocity(Vector3.ClampMagnitude(steeredVelocity, speedCap));
    }

    // utility for applying ground force to vertical velocity
    private void StickToGround()
    {
        _velocity.y = -_groundStickyForce;
    }
    
    // finds the ground with a sphere cast
    private void UpdateGrounded()
    {
        float probeRadius = _controller.radius * GroundProbeRadiusScale;
        Vector3 probeOrigin = transform.position + _controller.center;
        float probeDistance = _controller.height * 0.5f - probeRadius + _controller.skinWidth + _groundProbeDistance;

        // SphereCast drags an invisible ball along a line and reports the first thing it hits
        bool didHitGround = Physics.SphereCast(probeOrigin, probeRadius, Vector3.down, out RaycastHit groundHit, probeDistance, _groundMask, QueryTriggerInteraction.Ignore); // longest bool ever, not sure what it means yet
        bool isWalkable = didHitGround && Vector3.Angle(groundHit.normal, Vector3.up) <= _controller.slopeLimit; // new stuff, not fully understood
        bool isRising = _velocity.y > RisingSpeedThreshold; // needed because probe would read grounded *close* to the ground when it shouldn't

        IsGrounded = !isRising && (isWalkable || _controller.isGrounded);
        if (isWalkable)
        {
            GroundNormal = groundHit.normal;
        }
        else
        {
            GroundNormal = Vector3.up;
        }

        if (IsGrounded)
        {
            _lastGroundedTime = Time.time;
        }
    }

    private void MoveController(float deltaTime) // new addition, not fully understood yet
    {
        Vector3 horizontalVelocity = HorizontalVelocity;
        Vector3 displacement;

        bool isFollowingGround = IsGrounded && _velocity.y <= 0f;
        if (isFollowingGround)
        {
            Vector3 alongGround = Vector3.ProjectOnPlane(horizontalVelocity, GroundNormal); // just gets a vector that lies flat on the surface
            bool isMovingAlongGround = alongGround.sqrMagnitude > MinimumSlopeMoveSqrMagnitude;
            if (isMovingAlongGround)
            {
                alongGround = alongGround.normalized * horizontalVelocity.magnitude; // movement equation
            }
            displacement = (alongGround + Vector3.up * _velocity.y) * deltaTime; // movement equation
        }
        else
        {
            displacement = _velocity * deltaTime; // movement equation
        }

        CollisionFlags collisionFlags = _controller.Move(displacement);

        bool hitCeiling = (collisionFlags & CollisionFlags.Above) != 0;
        if (hitCeiling && _velocity.y > 0f)
        {
            _velocity.y = 0f;
        }
    }
}