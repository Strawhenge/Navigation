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
        internal const float SharpTurnAngle = 40;
        internal const float ReversalAngle = 120;
        internal const float StationaryPivotMaxSpeed = 1;
        internal const float JumpHeight = 1;
        internal const float CoyoteTime = 0.2f;
        internal const float Gravity = -9.81f;
        internal const float FallStartSpeed = 2;
        internal const float FallDistance = 1;
        internal const bool AllowAirborneRotation = true;
        internal const AirborneMovement AirborneMovement = Unity.AirborneMovement.Controlled;

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

        float ILocomotionSettings.SharpTurnAngle => SharpTurnAngle;

        float ILocomotionSettings.ReversalAngle => ReversalAngle;

        float ILocomotionSettings.StationaryPivotMaxSpeed => StationaryPivotMaxSpeed;

        IPivotProfile[] ILocomotionSettings.StationaryPivots => Array.Empty<IPivotProfile>();

        IPivotProfile[] ILocomotionSettings.MovingPivots => Array.Empty<IPivotProfile>();

        float ILocomotionSettings.JumpHeight => JumpHeight;

        float ILocomotionSettings.CoyoteTime => CoyoteTime;

        IJumpProfile[] ILocomotionSettings.Jumps => Array.Empty<IJumpProfile>();

        IStrafeJumpProfile[] ILocomotionSettings.StrafeJumps => Array.Empty<IStrafeJumpProfile>();

        ILandingProfile[] ILocomotionSettings.JumpLandings => Array.Empty<ILandingProfile>();

        float ILocomotionSettings.Gravity => Gravity;

        float ILocomotionSettings.FallStartSpeed => FallStartSpeed;

        float ILocomotionSettings.FallDistance => FallDistance;

        ILandingProfile[] ILocomotionSettings.FallLandings => Array.Empty<ILandingProfile>();

        bool ILocomotionSettings.AllowAirborneRotation => AllowAirborneRotation;

        AirborneMovement ILocomotionSettings.AirborneMovement => AirborneMovement;
    }
}