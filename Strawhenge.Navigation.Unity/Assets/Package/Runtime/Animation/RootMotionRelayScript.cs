using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Put on the Animator's GameObject (the character's root or a child). Hands the Animator's root motion to the
    // character's motor when it's an IRootMotionHandler (the rigidbody motor is), so it can move the character by
    // it. Having OnAnimatorMove means the Animator never applies root motion itself, so this does what it would:
    // nothing while applyRootMotion is off, and the built-in root motion when the motor doesn't take it. Characters
    // without one keep the Animator's own. Motors know nothing about it.
    [RequireComponent(typeof(Animator))]
    public sealed class RootMotionRelayScript : MonoBehaviour
    {
        [Tooltip("Optional: found in the parents if not set.")]
        [SerializeField] CharacterMotorScript _motor;

        // For the editor, which sets it when it adds the relay.
        internal const string MotorFieldName = nameof(_motor);

        Animator _animator;
        IRootMotionHandler _handler;

        void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_motor == null)
                _motor = GetComponentInParent<CharacterMotorScript>();

            // Null for a motor that doesn't take root motion (e.g. the CharacterController motor).
            _handler = _motor != null ? _motor.Motor as IRootMotionHandler : null;
        }

        void OnAnimatorMove()
        {
            // ApplyBuiltinRootMotion applies root motion even with applyRootMotion off. On a Rigidbody it would end
            // up in the body's velocity every frame, and the idle or walk clip's root motion would keep it moving.
            if (!_animator.applyRootMotion)
                return;

            if (_handler != null && _handler.TryApplyRootMotion(_animator.deltaPosition, _animator.deltaRotation))
                return;

            _animator.ApplyBuiltinRootMotion();
        }
    }
}
