using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLocomotionSettings
    {
        [SerializeField, Header("Speed")] float _walkSpeed = 1f;
        [SerializeField] float _runSpeed = 5f;
        [SerializeField] float _sprintSpeed = 8f;

        [SerializeField, Header("Acceleration")]
        float _acceleration = 10f;

        [SerializeField] float _deceleration = 30f;

        [SerializeField, Header("Turning")] float _turnSpeed = 360f;
        [SerializeField] SerializedPivot[] _pivots;

        [SerializeField, Header("Jumping")] float _jumpHeight = 1.5f;
        [SerializeField] float _coyoteTime = 0.2f;
        [SerializeField] bool _deferJumpTrigger;

        [SerializeField, Header("Gravity")] float _gravity = -9.81f;
        [SerializeField] float _groundedGravity = -2f;

        [SerializeField, Header("Falling")] float _fallDistance = 1f;

        public float WalkSpeed => _walkSpeed;

        public float RunSpeed => _runSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float Acceleration => _acceleration;

        public float Deceleration => _deceleration;

        public float TurnSpeed => _turnSpeed;

        public SerializedPivot[] Pivots => _pivots;

        public float JumpHeight => _jumpHeight;

        public float CoyoteTime => _coyoteTime;

        public bool DeferJumpTrigger => _deferJumpTrigger;

        public float Gravity => _gravity;

        public float GroundedGravity => _groundedGravity;

        public float FallDistance => _fallDistance;
    }
}