using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Navigation.Unity;
using UnityEngine;

namespace Development
{
    // Keeps LocomotionScript off while the character is ragdolled. LocomotionScript turns its motor (here the
    // CharacterController) on and off, so otherwise it would keep the motor on and keep moving the root while
    // the ragdoll is down. Assign it to the LocomotionToggleScript's Conditions.
    public class RagdollLocomotionConditionsScript : BaseLocomotionToggleConditionsScript
    {
        [SerializeField] RagdollScript _ragdoll;

        void Awake()
        {
            ComponentRefHelper.EnsureRootHierarchyComponent(ref _ragdoll, nameof(_ragdoll), this);

            if (_ragdoll != null)
                _ragdoll.RagdollChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (_ragdoll != null)
                _ragdoll.RagdollChanged -= OnStateChanged;
        }

        protected override bool LocomotionShouldBeActive() =>
            _ragdoll == null || !_ragdoll.IsRagdolled;
    }
}
