using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Navigation.Unity.Destination;
using UnityEngine;

namespace Development
{
    // The development character's context: it can't navigate while it's ragdolled, or while 'Cannot Navigate'
    // is ticked in the inspector (which can be changed in play mode).
    public class DevelopmentDestinationContextScript : DestinationContextScript
    {
        [SerializeField] RagdollScript _ragdoll;

        [Tooltip("Stops the character navigating, as if something in the game prevented it.")]
        [SerializeField] bool _cannotNavigate;

        public override bool CanNavigate => !_cannotNavigate && !IsRagdolled;

        bool IsRagdolled => _ragdoll != null && _ragdoll.IsRagdolled;

        void Awake()
        {
            ComponentRefHelper.EnsureRootHierarchyComponent(ref _ragdoll, nameof(_ragdoll), this);
        }
    }
}
