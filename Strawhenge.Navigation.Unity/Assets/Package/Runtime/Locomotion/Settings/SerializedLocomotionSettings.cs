using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLocomotionSettings : ILocomotionSettings
    {
        [SerializeField, Header("Speed")] float _walkSpeed = DefaultLocomotionSettings.WalkSpeed;
        [SerializeField] float _runSpeed = DefaultLocomotionSettings.RunSpeed;
        [SerializeField] float _sprintSpeed = DefaultLocomotionSettings.SprintSpeed;

        [SerializeField, Header("Acceleration")]
        float _acceleration = DefaultLocomotionSettings.Acceleration;

        [SerializeField] float _deceleration = DefaultLocomotionSettings.Deceleration;

        [SerializeField, Header("Turning")] float _turnSpeed = DefaultLocomotionSettings.TurnSpeed;
        [SerializeField] SerializedPivotProfile[] _stationaryPivots;
        [SerializeField] SerializedPivotProfile[] _movingPivots;

        [SerializeField, Header("Jumping")] float _jumpHeight = DefaultLocomotionSettings.JumpHeight;
        [SerializeField] float _coyoteTime = DefaultLocomotionSettings.CoyoteTime;
        [SerializeField] bool _deferJumpTrigger;
        [SerializeField] SerializedLanding[] _jumpLandings;

        [SerializeField, Header("Gravity")] float _gravity = DefaultLocomotionSettings.Gravity;
        [SerializeField] float _groundedGravity = DefaultLocomotionSettings.GroundedGravity;

        [SerializeField, Header("Falling")] float _fallDistance = DefaultLocomotionSettings.FallDistance;
        [SerializeField] SerializedLanding[] _fallLandings;

        public float WalkSpeed => _walkSpeed;

        public float RunSpeed => _runSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float Acceleration => _acceleration;

        public float Deceleration => _deceleration;

        public float TurnSpeed => _turnSpeed;

        public SerializedPivotProfile[] StationaryPivots => _stationaryPivots;

        public SerializedPivotProfile[] MovingPivots => _movingPivots;

        public float JumpHeight => _jumpHeight;

        public float CoyoteTime => _coyoteTime;

        public bool DeferJumpTrigger => _deferJumpTrigger;
        
        public SerializedLanding[] JumpLandings => _jumpLandings;

        public float Gravity => _gravity;

        public float GroundedGravity => _groundedGravity;

        public float FallDistance => _fallDistance;
        
        public SerializedLanding[] FallLandings => _fallLandings;
    }
}