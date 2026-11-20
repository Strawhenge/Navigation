using Strawhenge.Common.Unity.Helpers;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed partial class CharacterControllerMotorScript : CharacterMotorScript, ICharacterControllerMotorSettings
    {
        [SerializeField] CharacterController _characterController;

        [Tooltip("Vertical speed (negative) applied while grounded, so the CharacterController stays grounded.")]
        [SerializeField] float _groundedPush = -2f;

        ICharacterMotor _motor;

        // Created on first use, as LocomotionScript asks for it in its Awake.
        public override ICharacterMotor Motor => _motor ??= Create();

        float ICharacterControllerMotorSettings.GroundedPush => _groundedPush;

        ICharacterMotor Create()
        {
            ComponentRefHelper
                .EnsureHierarchyComponent(ref _characterController, nameof(_characterController), this);

            return _characterController != null
                ? new CharacterControllerMotor(_characterController, this)
                : NullCharacterMotor.Instance;
        }
    }
}
