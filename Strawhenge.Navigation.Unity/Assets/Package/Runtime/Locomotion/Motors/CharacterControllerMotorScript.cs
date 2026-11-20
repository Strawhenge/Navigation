using Strawhenge.Common.Unity.Helpers;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed partial class CharacterControllerMotorScript : CharacterMotorScript
    {
        [SerializeField] CharacterController _characterController;

        ICharacterMotor _motor;

        // Created on first use, as LocomotionScript asks for it in its Awake.
        public override ICharacterMotor Motor => _motor ??= Create();

        ICharacterMotor Create()
        {
            ComponentRefHelper
                .EnsureHierarchyComponent(ref _characterController, nameof(_characterController), this);

            return _characterController != null
                ? new CharacterControllerMotor(_characterController)
                : NullCharacterMotor.Instance;
        }
    }
}
