using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionMovementSourceScript : MovementSourceScript
    {
        [SerializeField] LocomotionScript _locomotion;

        LocomotionMovementSource _movementSource;

        public override IMovementSource MovementSource =>
            _movementSource ??= new LocomotionMovementSource(_locomotion);
    }
}