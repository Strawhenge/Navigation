using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Navigation.Unity.Destination;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class LocomotionToggleScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] DestinationScript _destination;
        [SerializeField] BaseLocomotionToggleConditionsScript _conditions;

        void Awake()
        {
            ComponentRefHelper
                .EnsureHierarchyComponent(ref _locomotion, nameof(_locomotion), this);

            if (_destination != null)
            {
                _destination.DestinationController.Activated += OnStateChanged;
                _destination.DestinationController.Deactivated += OnStateChanged;
            }

            if (_conditions != null)
                _conditions.StateChanged += OnStateChanged;
        }

        void OnStateChanged()
        {
            _locomotion.enabled = LocomotionShouldBeActive();
        }

        bool LocomotionShouldBeActive() =>
            (_destination == null || !_destination.DestinationController.IsActive) &&
            (_conditions == null || _conditions.LocomotionShouldBeActive());
    }
}