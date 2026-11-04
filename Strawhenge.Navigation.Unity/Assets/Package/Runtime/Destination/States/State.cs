using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    abstract class State
    {
        internal event Action<State> ChangeStateRequested;

        protected StatesContainer States { get; private set; }

        protected internal virtual bool IsActive => true;

        // Whether this is the controller's current state. Maintained by DestinationController.
        internal bool IsCurrent { get; set; }

        protected internal abstract Vector3 Velocity { get; }

        protected internal abstract void GoTo(DestinationArgs args);

        protected internal abstract void Update(float deltaTime);

        protected internal abstract void Cancel();

        // The character's GameObject was reactivated. Unity resets a NavMeshAgent when its GameObject
        // is deactivated, so its path is gone: states that rely on it must set it up again.
        protected internal virtual void OnAgentReactivated()
        {
        }

        internal void SetStatesContainer(StatesContainer states) => States = states;

        protected void ChangeState(State state) => ChangeStateRequested?.Invoke(state);
    }
}