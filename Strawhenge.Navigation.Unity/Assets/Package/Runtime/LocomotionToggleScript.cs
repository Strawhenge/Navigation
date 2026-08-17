using Strawhenge.Navigation.Unity.Destination;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionToggleScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] DestinationScript _destination;

        void Awake()
        {
            if (_destination != null)
            {
                _destination.DestinationController.Activated += OnStateChanged;
                _destination.DestinationController.Deactivated += OnStateChanged;
            }
        }

        void OnStateChanged()
        {
            _locomotion.enabled = LocomotionShouldBeActive();
        }

        bool LocomotionShouldBeActive() =>
            _destination == null || !_destination.DestinationController.IsActive;
    }
}