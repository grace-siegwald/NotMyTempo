using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

/// <summary>
/// Handles all the velocity stuff and moves the CharacterController (hence "motor"). Also manages running and gravity!
/// The Player's pivot MUST be at its feet or else things break
/// 
/// Long story short, it does a bunch of velocity shit for different player states 
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerInput))] // requires character controller and input reader
public class PlayerMotor : MonoBehaviour
{
    
    [Tooltip("All movement tuning. Create one with Assets > Create > Not My Tempo > Movement Stats.")]
    [SerializeField] private MovementStats _stats; // getting stat assest

    
    // Ground Detection Stuff
    [Header("Ground Detection")]
    [Tooltip("Defines what the player can stand on")]
    [SerializeField] private LayerMask _groundMask = Physics.DefaultRaycastLayers; // defines what the player can stand on
    [Tooltip("How far below the feet we 'look' for the ground")]
    [SerializeField] private float _groundProbeDistance = 0.15f; // how far below the feet we "look" for the ground
    [Tooltip("Small force on player to keep them pressed on ground")]
    [SerializeField] private float _groundStickyForce = 3f; // small force on player to keep them pressed on ground


    // fields / getting components
    private CharacterController _controller;
    private PlayerInput _inputReader;
    private UnityEngine.Vector3 _velocity; // THE BIG HOMIE, THE MAN OF THE HOUR
    private const float RisingSpeedThreshold = 0.01f; // new stuff, not fully understood
    private const float GroundProbeRadiusScale = 0.9f; // new stuff, not fully understood
    private const float MinimumSlopeMoveSqrMagnitude = 0.0001f; // new stuff, not fully understood
    private const float MinimumInputSqrMagnitude = 0.01f;
    private float _lastGroundedTime = float.NegativeInfinity;
    private int _airJumpsUsed; // number of air jumps that have been used by the player
    private bool _hasJumpedSinceGrounded; // has the player jumped since they've been on the ground? (needed bc coyoted time)

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

        if (_stats == null)
        {
            Debug.Log("PlayerMotor script needs a MovementStats asset to be assigned", this);
            enabled = false;
        }
    }
    private void Update() // BIG TIME MOVEMENT: check the world, change the velocity, and move!
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
        if (!_inputReader.HasBufferedJump || !CanJump(out bool isGroundJump)) // if we can't jump, get outa here!
        {
            return;
        }
        _inputReader.ConsumeJump();

        if (!isGroundJump)
        {
            _airJumpsUsed++;
            RedirectMomentumTowardInput();
        }

        _hasJumpedSinceGrounded = true; // need this set for coyote time
        IsGrounded = false;
        _velocity.y = Mathf.Sqrt(2f * _stats.Gravity * _stats.JumpHeight); // I do NOT understand this maths bruv
    }
    
    // utility method for deciding if we can jump or not
    private bool CanJump(out bool isGroundJump) 
    {
        bool isInCoyoteTime = TimeSinceGrounded <= _stats.CoyoteTime && !_hasJumpedSinceGrounded;
        isGroundJump = IsGrounded || isInCoyoteTime;
        bool hasAirJumpLeft = _airJumpsUsed < _stats.AirJumps;

        return isGroundJump || hasAirJumpLeft; // return true if we're on ground or we have an air jump still
    }

    // kinda like a destiny style double jump: horizontal momentum swings toward your input and keeps its speed.
    private void RedirectMomentumTowardInput() // This is what lets a double jump turn a corner without losing speed FUN FUN FUN
    {
        if (!HasMoveInput) // if we don't have move input, get outa here !
        {
            return;
        }
        float speed = Mathf.Max(HorizontalSpeed, _stats.AirSpeed * WishDirection.magnitude); // speed takes whichever value is bigge
        SetHorizontalVelocity(WishDirection.normalized * speed);
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
        Vector3 targetVelocity = WishDirection * _stats.GroundSpeed; // velocity is almost always direction * speed, vector maths baby
        float rate;
        if (HasMoveInput) // if we have player input, accelerate
        {
            rate = _stats.GroundAcceleration;
        }
        else // decelerate 
        {
            rate = _stats.GroundDeceleration;
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
        float gravity = _stats.Gravity;
        bool isFalling = _velocity.y < 0f; // decide if falling or not 

        if (isFalling)
        {
            gravity *= _stats.FallGravityMultiplier; // multiply gravity by the gravity multiplier
        }

        _velocity.y = Mathf.Max(_velocity.y - _stats.Gravity * deltaTime, -_stats.MaxFallSpeed); // math.max to just give it a cap of the max fall speed
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
        float speedCap = Mathf.Max(horizontalVelocity.magnitude, _stats.AirSpeed); 
        Vector3 steeredVelocity = horizontalVelocity + WishDirection * (_stats.AirAcceleration * deltaTime);
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
        bool wasGrounded = IsGrounded;
        
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

        bool hasJustLanded = IsGrounded && !wasGrounded;
        if (hasJustLanded) // reset air jumps 
        {
            _airJumpsUsed = 0;
            _hasJumpedSinceGrounded = false;
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