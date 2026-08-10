using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public class DestinationController
    {
        readonly NavMeshAgent _agent;
        State _state;

        internal DestinationController(IDestinationContext context, Agent agent)
        {
            _agent = agent.NavMeshAgent;

            var statesContainer = new StatesContainer(context, agent, OnChangeState);
            _state = statesContainer.Idle;
        }

        public event Action Activated;
        public event Action Deactivated;

        public bool IsActive => _state.IsActive;

        public Vector3 CurrentVelocity => _state.Velocity;

        public bool IsLocationAccessible(Vector3 location) => _agent.IsLocationAccessible(location);

        public void GoTo(DestinationArgs args) => _state.GoTo(args);

        public void Cancel() => _state.Cancel();

        public void Update() => _state.Update();

        void OnChangeState(State newState)
        {
            var oldState = _state;
            _state = newState;

            if (!oldState.IsActive && newState.IsActive)
                Activated?.Invoke();

            if (oldState.IsActive && !newState.IsActive)
                Deactivated?.Invoke();
        }
    }
}