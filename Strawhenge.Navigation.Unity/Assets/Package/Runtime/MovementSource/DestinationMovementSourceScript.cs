using Strawhenge.Navigation.Unity.Destination;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class DestinationMovementSourceScript : MovementSourceScript
    {
        [SerializeField] DestinationScript _destination;

        DestinationMovementSource _movementSource;

        public override IMovementSource MovementSource =>
            _movementSource ??= new DestinationMovementSource(_destination.DestinationController);
    }
}