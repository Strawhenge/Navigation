using System;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeLocomotionSettings : ILocomotionSettings
    {
        public float WalkSpeed { get; set; } = DefaultLocomotionSettings.WalkSpeed;
        public float RunSpeed { get; set; } = DefaultLocomotionSettings.RunSpeed;
        public float SprintSpeed { get; set; } = DefaultLocomotionSettings.SprintSpeed;
        public float Acceleration { get; set; } = DefaultLocomotionSettings.Acceleration;
        public float Deceleration { get; set; } = DefaultLocomotionSettings.Deceleration;
        public float TurnSpeed { get; set; } = DefaultLocomotionSettings.TurnSpeed;
        public IPivotProfile[] StationaryPivots { get; set; } = Array.Empty<IPivotProfile>();
        public IPivotProfile[] MovingPivots { get; set; } = Array.Empty<IPivotProfile>();
        public float JumpHeight { get; set; } = DefaultLocomotionSettings.JumpHeight;
        public float CoyoteTime { get; set; } = DefaultLocomotionSettings.CoyoteTime;
        public bool DeferJumpTrigger { get; set; }
        public ILandingProfile[] JumpLandings { get; set; } = Array.Empty<ILandingProfile>();
        public float Gravity { get; set; } = DefaultLocomotionSettings.Gravity;
        public float GroundedGravity { get; set; } = DefaultLocomotionSettings.GroundedGravity;
        public float FallDistance { get; set; } = DefaultLocomotionSettings.FallDistance;
        public ILandingProfile[] FallLandings { get; set; } = Array.Empty<ILandingProfile>();
    }
}
