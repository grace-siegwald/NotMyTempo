using UnityEngine;

/// <summary>
/// Titanfall / Ultrakill slide. Hold slide while moving fast on the ground, or land while holding it.
/// Starts with a speed boost, slows gently on flat ground, speeds up down slopes, and steers a little.
/// Jump out of it to slide-hop. Holding slide while slow just crouches.
/// </summary>
public class SlideAbility : MovementAbility
{
    [Tooltip("Speed needed above Slide Minimum Speed to start a slide, m/s. Stops the slide flickering on and off.")]
    [SerializeField] private float _startSpeedMargin = 2f;
    [Tooltip("Seconds you can be airborne (bumps, small drops) before the slide ends.")]
    [SerializeField] private float _airborneGraceTime = 0.15f;

    private const int SlidePriority = 10;
    private const float MinimumVelocitySqrMagnitude = 0.0001f;
    private float _airborneTime;
    
    public override int Priority { get { return SlidePriority; } }
    public override bool WantsToActivate()
    {
        bool isMMovingFastEnough = Motor.HorizontalSpeed >= Stats.SlideMinimumSpeed + _startSpeedMargin; // add the margin for consistency
        return InputReader.IsSlideHeld && Motor.IsGrounded && isMMovingFastEnough;
    }
    public override void UpdatePassive(float deltaTime)
    {
        if (!IsActive)
        {
            Motor.WantsToCrouch = InputReader.IsSlideHeld && Motor.IsGrounded;
        }
    }
    public override void UpdateActive(float deltaTime)
    {
        if (!InputReader.IsSlideHeld)
        {
            Stop();
            return;
        }

        Vector3 slideVelocity = Motor.HorizontalVelocity;

        if (Motor.IsGrounded)
        {
            _airborneTime = 0f;
            slideVelocity += CalculateSlopePush(deltaTime);
            Motor.StickToGround();
        }
        else
        {
            _airborneTime += deltaTime;
            bool hasLeftGround = _airborneTime > _airborneGraceTime;
            if (hasLeftGround)
            {
                Stop();
                return;
            }
            Motor.ApplyGravity(deltaTime);
        }

        float speed = Mathf.MoveTowards(slideVelocity.magnitude, 0f, Stats.SlideFriction * deltaTime);
        Vector3 direction = SteerTowardInput(slideVelocity, deltaTime);
        Motor.SetHorizontalVelocity(direction * speed);

        bool isTooSlow = speed < Stats.SlideMinimumSpeed;
        if (isTooSlow)
        {
            Stop();
        }
    }
    protected override void OnActivated()
    {
        Motor.WantsToCrouch = true;
        _airborneTime = 0f;

        Vector3 horizontalVelocity = Motor.HorizontalVelocity;
        float startSpeed = Mathf.Max(horizontalVelocity.magnitude, Stats.GroundSpeed + Stats.SlideBoost);
        Motor.SetHorizontalVelocity(horizontalVelocity.normalized * startSpeed);
    }
    protected override void OnDeactivated()
    {
        Motor.WantsToCrouch = false;
    }
    /// <summary>
    /// Gravity's pull along the ground: speeds you up going downhill, slows you going uphill, nothing on flat ground.
    /// </summary>
    private Vector3 CalculateSlopePush(float deltaTime)
    {
        Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, Motor.GroundNormal);
        downhill.y = 0f;
        return downhill * (Stats.Gravity * deltaTime);
    }
    private Vector3 SteerTowardInput(Vector3 slideVelocity, float deltaTime)
    {
        bool isMoving = slideVelocity.sqrMagnitude > MinimumVelocitySqrMagnitude;
        if (!isMoving)
        {
            return Motor.transform.forward;
        }

        Vector3 direction = slideVelocity.normalized;
        if (Motor.HasMoveInput)
        {
            float maxTurnRadians = Stats.SlideSteering * Mathf.Deg2Rad * deltaTime;
            direction = Vector3.RotateTowards(direction, Motor.WishDirection.normalized, maxTurnRadians, 0f);
        }
        return direction;
    }
}
