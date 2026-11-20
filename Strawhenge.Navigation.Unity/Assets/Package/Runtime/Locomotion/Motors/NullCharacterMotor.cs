using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Used when no motor is found: the character stays where it is. Never grounded, so it can't jump.
    sealed class NullCharacterMotor : ICharacterMotor
    {
        internal static NullCharacterMotor Instance { get; } = new();

        NullCharacterMotor()
        {
        }

        public Vector3 Position => Vector3.zero;

        public Quaternion Rotation => Quaternion.identity;

        public Vector3 Velocity => Vector3.zero;

        public bool IsGrounded => false;

        public MotorUpdateMode UpdateMode => MotorUpdateMode.Update;

        public void SetActive(bool active)
        {
        }

        public void SetRotation(Quaternion rotation)
        {
        }

        public MotorMoveResult Move(LocomotionStep step, float deltaTime) => default;
    }
}
