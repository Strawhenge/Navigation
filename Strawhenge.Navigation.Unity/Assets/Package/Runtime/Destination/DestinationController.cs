using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public class DestinationController
    {
        readonly Agent _agent;
        State _state;

        internal DestinationController(IDestinationContext context, Agent agent)
        {
            _agent = agent;

            var statesContainer = new StatesContainer(
                context,
                agent,
                OnChangeState,
                OnJumpBegan,
                OnJumpEnded,
                OnFallBegan,
                OnFallEnded);
            _state = statesContainer.Idle;
            _state.IsCurrent = true;
        }

        public event Action Activated;
        public event Action Deactivated;
        public event Action JumpBegan;
        public event Action JumpEnded;
        public event Action FallBegan;
        public event Action FallEnded;

        public bool IsActive => _state.IsActive;

        public Vector3 CurrentVelocity => _state.Velocity;

        public bool IsStrafing => _state.IsActive && !_agent.NavMeshAgent.updateRotation;

        // True if there is a complete path from the agent's current position, using the area mask of
        // the given mode. Has no side effects on the agent.
        public bool IsLocationAccessible(Vector3 location, bool leisurely = false) =>
            _agent.IsLocationAccessible(location, leisurely);

        public void GoTo(DestinationArgs args) => _state.GoTo(args);

        public void Cancel() => _state.Cancel();

        internal void Update(float deltaTime) => _state.Update(deltaTime);

        // While the GameObject is inactive: frees the avoidance priority slot, keeping the trip.
        internal void Suspend() => _agent.Suspend();

        internal void Resume() => _agent.Resume();

        void OnJumpBegan() => JumpBegan?.Invoke();

        void OnJumpEnded() => JumpEnded?.Invoke();

        void OnFallBegan() => FallBegan?.Invoke();

        void OnFallEnded() => FallEnded?.Invoke();

        void OnChangeState(State newState)
        {
            var oldState = _state;
            _state = newState;

            oldState.IsCurrent = false;
            newState.IsCurrent = true;

            if (!oldState.IsActive && newState.IsActive)
                Activated?.Invoke();

            if (oldState.IsActive && !newState.IsActive)
                Deactivated?.Invoke();
        }
    }
}