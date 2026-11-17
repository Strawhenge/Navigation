using Strawhenge.Common.Unity.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Development
{
    // Put on the character's root. Switches the character between animated and ragdoll. The ragdoll is every
    // Rigidbody below this object (as the Ragdoll Wizard sets it up), with the colliders on the same
    // GameObjects. While animated, the bodies are kinematic and their colliders are off, so they don't block
    // the CharacterController or get hit by the destination's ledge check.
    public class RagdollScript : MonoBehaviour
    {
        const float GroundProbeHeight = 1f;
        const float GroundProbeDistance = 3f;

        [SerializeField] Animator _animator;

        // The pelvis body. Without one, the Animator's hips bone is used (humanoid rigs only).
        [SerializeField] Rigidbody _hips;

        // Searched below the hips when getting up. Leave out the character's own layers.
        [SerializeField] LayerMask _groundLayers = ~0;

        // Given to every body as the ragdoll starts, relative to the character's facing: (0, 2, -4) knocks it
        // back. Zero just drops it.
        [SerializeField] Vector3 _launchVelocity;

        readonly List<Rigidbody> _bodies = new();
        readonly List<Collider> _colliders = new();

        public event Action RagdollChanged;

        public bool IsRagdolled { get; private set; }

        void Awake()
        {
            ComponentRefHelper.EnsureHierarchyComponent(ref _animator, nameof(_animator), this);

            GetComponentsInChildren(includeInactive: true, _bodies);

            // Only the bones: a body on the root itself isn't part of the ragdoll.
            var rootBody = GetComponent<Rigidbody>();
            if (rootBody != null)
                _bodies.Remove(rootBody);

            foreach (var body in _bodies)
                _colliders.AddRange(body.GetComponents<Collider>());

            if (_bodies.Count == 0)
                Debug.LogWarning("No Rigidbodies found below this object - set up the ragdoll first.", this);

            if (_hips == null)
                ResolveHips();

            SetBodiesActive(false);
        }

        public void RagdollOn()
        {
            if (IsRagdolled)
                return;

            if (_animator != null)
                _animator.enabled = false;

            SetBodiesActive(true);

            var launchVelocity = transform.TransformDirection(_launchVelocity);
            foreach (var body in _bodies)
                body.velocity = launchVelocity;

            IsRagdolled = true;
            RagdollChanged?.Invoke();
        }

        // The bones are children of the root and snap back to it when the Animator starts again, so the root
        // is moved first to the ground below where the hips ended up. Its rotation is kept.
        public void RagdollOff()
        {
            if (!IsRagdolled)
                return;

            var hipsPosition = _hips != null ? _hips.position : transform.position;

            SetBodiesActive(false);

            transform.position = FindGround(hipsPosition);

            if (_animator != null)
                _animator.enabled = true;

            IsRagdolled = false;
            RagdollChanged?.Invoke();
        }

        void SetBodiesActive(bool active)
        {
            foreach (var body in _bodies)
                body.isKinematic = !active;

            foreach (var collider in _colliders)
                collider.enabled = active;
        }

        // The ragdoll's colliders are already off, so the probe can't hit the character.
        Vector3 FindGround(Vector3 position)
        {
            var origin = position + (Vector3.up * GroundProbeHeight);

            return Physics.Raycast(
                origin,
                Vector3.down,
                out var hit,
                GroundProbeHeight + GroundProbeDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
        }

        void ResolveHips()
        {
            Debug.LogWarning($"'{nameof(_hips)}' not set - using the Animator's hips bone.", this);

            var hipsBone = _animator != null && _animator.isHuman
                ? _animator.GetBoneTransform(HumanBodyBones.Hips)
                : null;

            if (hipsBone != null)
                _hips = hipsBone.GetComponent<Rigidbody>();

            if (_hips == null)
                Debug.LogWarning("No hips Rigidbody found - getting up leaves the character where it started.", this);
        }
    }
}
