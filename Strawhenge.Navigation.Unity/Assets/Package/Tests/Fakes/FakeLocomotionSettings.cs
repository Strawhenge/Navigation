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
        public float SharpTurnAngle { get; set; } = DefaultLocomotionSettings.SharpTurnAngle;
        public float ReversalAngle { get; set; } = DefaultLocomotionSettings.ReversalAngle;
        public float StationaryPivotMaxSpeed { get; set; } = DefaultLocomotionSettings.StationaryPivotMaxSpeed;
        public IPivotProfile[] StationaryPivots { get; set; } = Array.Empty<IPivotProfile>();
        public IPivotProfile[] MovingPivots { get; set; } = Array.Empty<IPivotProfile>();
        public float JumpHeight { get; set; } = DefaultLocomotionSettings.JumpHeight;
        public float CoyoteTime { get; set; } = DefaultLocomotionSettings.CoyoteTime;
        public IJumpProfile[] Jumps { get; set; } = Array.Empty<IJumpProfile>();
        public IStrafeJumpProfile[] StrafeJumps { get; set; } = Array.Empty<IStrafeJumpProfile>();
        public ILandingProfile[] JumpLandings { get; set; } = Array.Empty<ILandingProfile>();
        public float Gravity { get; set; } = DefaultLocomotionSettings.Gravity;
        public float FallStartSpeed { get; set; } = DefaultLocomotionSettings.FallStartSpeed;
        public float FallDistance { get; set; } = DefaultLocomotionSettings.FallDistance;
        public ILandingProfile[] FallLandings { get; set; } = Array.Empty<ILandingProfile>();
        public bool AllowAirborneRotation { get; set; } = DefaultLocomotionSettings.AllowAirborneRotation;
        public AirborneMovement AirborneMovement { get; set; } = DefaultLocomotionSettings.AirborneMovement;
    }
}
