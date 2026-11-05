using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    [Serializable]
    public class SerializedDestinationSettings : IDestinationSettings
    {
        [Header("Jumping")]
        [Tooltip("Horizontal speed while jumping across an off-mesh link.")]
        [SerializeField] float _jumpSpeed = DefaultDestinationSettings.JumpSpeed;

        [Header("Falling")]
        [Tooltip("Horizontal speed while dropping down an off-mesh link.")]
        [SerializeField] float _fallHorizontalSpeed = DefaultDestinationSettings.FallHorizontalSpeed;

        [Tooltip("Downward acceleration while dropping down an off-mesh link.")]
        [SerializeField] float _fallAcceleration = DefaultDestinationSettings.FallAcceleration;

        [Tooltip("Layers that count as ground when checking whether a drop has left its ledge. " +
                 "Leave out layers of the character's own colliders (hitboxes, ragdoll parts).")]
        [SerializeField] LayerMask _groundLayers = DefaultDestinationSettings.GroundLayers;

        public float JumpSpeed => _jumpSpeed;

        public float FallHorizontalSpeed => _fallHorizontalSpeed;

        public float FallAcceleration => _fallAcceleration;

        public LayerMask GroundLayers => _groundLayers;
    }
}
