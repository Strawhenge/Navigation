using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // TODO: Move to Common.
    // For a StateMachineBehaviour to find a component on its Animator's GameObject, through the Animator each
    // callback is given. Unity creates new behaviour instances whenever it rebuilds the Animator (turned off and
    // on, GameObject deactivated, controller swapped, rebound), so nothing outside may hold on to them. Each
    // instance belongs to one Animator, so it can cache what it finds; new instances start empty and look again.
    sealed class AnimatorComponentCache<T> where T : Component
    {
        T _component;

        // Looks again while nothing is cached, or the cached component was destroyed.
        public bool TryGet(Animator animator, out T component)
        {
            if (_component == null)
                animator.TryGetComponent(out _component);

            component = _component;
            return _component != null;
        }
    }
}
