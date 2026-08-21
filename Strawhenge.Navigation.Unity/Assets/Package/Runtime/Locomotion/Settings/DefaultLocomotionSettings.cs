using System;

namespace Strawhenge.Navigation.Unity
{
    sealed class DefaultLocomotionSettings : ILocomotionSettings
    {
        internal const float WalkSpeed = 1.5f;
        internal const float RunSpeed = 5;
        internal const float SprintSpeed = 8;
        internal const float Acceleration = 10;
        internal const float Deceleration = 30;
        internal const float TurnSpeed = 360;
        internal const float JumpHeight = 1;
        internal const float CoyoteTime = 0.2f;
        internal const float Gravity = -9.81f;
        internal const float GroundedGravity = -2;
        internal const float FallDistance = 1;

        public static ILocomotionSettings Instance { get; } = new DefaultLocomotionSettings();

        DefaultLocomotionSettings()
        {
        }

        float ILocomotionSettings.WalkSpeed => WalkSpeed;

        float ILocomotionSettings.RunSpeed => RunSpeed;

        float ILocomotionSettings.SprintSpeed => SprintSpeed;

        float ILocomotionSettings.Acceleration => Acceleration;

        float ILocomotionSettings.Deceleration => Deceleration;

        float ILocomotionSettings.TurnSpeed => TurnSpeed;

        IPivotProfile[] ILocomotionSettings.StationaryPivots => Array.Empty<IPivotProfile>();

        IPivotProfile[] ILocomotionSettings.MovingPivots => Array.Empty<IPivotProfile>();

        float ILocomotionSettings.JumpHeight => JumpHeight;

        float ILocomotionSettings.CoyoteTime => CoyoteTime;

        bool ILocomotionSettings.DeferJumpTrigger => false;

        SerializedLandingProfile[] ILocomotionSettings.JumpLandings => Array.Empty<SerializedLandingProfile>();

        float ILocomotionSettings.Gravity => Gravity;

        float ILocomotionSettings.GroundedGravity => GroundedGravity;

        float ILocomotionSettings.FallDistance => FallDistance;

        SerializedLandingProfile[] ILocomotionSettings.FallLandings => Array.Empty<SerializedLandingProfile>();
    }
}