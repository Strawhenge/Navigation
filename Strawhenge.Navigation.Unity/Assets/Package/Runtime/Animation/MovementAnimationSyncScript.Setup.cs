using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive); Awake keeps what was set.
    public partial class MovementAnimationSyncScript
    {
        internal void SetAnimator(Animator animator) => _animator = animator;

        internal void SetMovementSourceManager(MovementSourceManagerScript movementSourceManager) =>
            _movementSourceManager = movementSourceManager;

        // In place of the Animator controller's own behaviours: tests can't build a controller, so they call
        // the behaviours' OnStateEnter / OnStateMachineExit themselves.
        internal void SetBehaviours(PivotStateMachineBehaviour pivot, LandingStateMachineBehaviour landing)
        {
            _pivotBehaviour = pivot;
            _landingBehaviour = landing;
        }
    }
}
