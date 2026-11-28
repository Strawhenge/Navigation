using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Put on a state that goes back to the Locomotion state, such as a moving pivot, to say where in the walk and
    // run cycle Locomotion picks up, so the legs carry on from where this state's clip leaves them. It sets
    // 'Locomotion Cycle Offset', which the controller uses as the Locomotion state's cycle offset. Locomotion starts
    // from the beginning of its cycle each time it's entered, plus that offset.
    //
    // It only sets the offset once the transition into this state has finished, and stops when the transition
    // out of it starts: changing a state's cycle offset while that state is playing makes it jump.
    public partial class LocomotionCycleOffsetStateMachineBehaviour : StateMachineBehaviour
    {
        [Tooltip("Where Locomotion's cycle picks up after this state (0 to 1): the point in the walk and run cycle " +
                 "where the legs are as this state's clip leaves them. For a mirrored copy of a state, about 0.5 " +
                 "further on.")]
        [SerializeField, Range(0f, 1f)] float _cycleOffset;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.IsInTransition(layerIndex))
                return;

            animator.SetFloat(AnimatorParameters.LocomotionCycleOffset.Id, _cycleOffset);
        }
    }
}
