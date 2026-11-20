using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Put on the Animator's GameObject (the character's root or a child). Hands the Animator's root motion to a
    // handler, such as the rigidbody motor, so it can move the character by it. Having OnAnimatorMove means the
    // Animator never applies root motion itself, so this does what it would: nothing while applyRootMotion is off,
    // and when no handler takes it, the built-in root motion. Characters without one keep the Animator's own.
    [RequireComponent(typeof(Animator))]
    public sealed class RootMotionRelayScript : MonoBehaviour
    {
        Animator _animator;

        public IRootMotionHandler Handler { get; set; }

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        void OnAnimatorMove()
        {
            // ApplyBuiltinRootMotion applies root motion even with applyRootMotion off. On a Rigidbody it would end
            // up in the body's velocity every frame, and the idle or walk clip's root motion would keep it moving.
            if (!_animator.applyRootMotion)
                return;

            if (Handler != null && Handler.TryApplyRootMotion(_animator.deltaPosition, _animator.deltaRotation))
                return;

            _animator.ApplyBuiltinRootMotion();
        }
    }
}
