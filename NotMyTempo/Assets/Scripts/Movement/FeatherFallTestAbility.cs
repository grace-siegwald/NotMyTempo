using UnityEngine;

namespace NotMyTempo.Movement
{
    /// <summary>
    /// TEMPORARY test for the ability framework. Delete before committing checkpoint 6.
    /// While falling, caps your fall speed so you drift down slowly.
    /// </summary>
    public class FeatherFallTestAbility : MovementAbility
    {
        [Tooltip("Fastest you can fall while this is active, m/s.")]
        [SerializeField] private float _maxFallSpeed = 2f;

        public override bool WantsToActivate()
        {
            bool isFalling = !Motor.IsGrounded && Motor.Velocity.y < 0f;
            return isFalling;
        }

        public override void UpdateActive(float deltaTime)
        {
            if (Motor.IsGrounded)
            {
                Stop();
                return;
            }

            Motor.ApplyGravity(deltaTime);
            Vector3 velocity = Motor.Velocity;
            velocity.y = Mathf.Max(velocity.y, -_maxFallSpeed);
            Motor.Velocity = velocity;
        }
    }
}