using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    abstract class State
    {
        internal event Action<State> ChangeStateRequested;

        protected StatesContainer States { get; private set; }

        protected internal virtual bool IsActive => true;

        protected internal abstract Vector3 Velocity { get; }

        protected internal abstract void GoTo(DestinationArgs args);

        protected internal abstract void Update(float deltaTime);

        protected internal abstract void Cancel();

        internal void SetStatesContainer(StatesContainer states) => States = states;

        protected void ChangeState(State state) => ChangeStateRequested?.Invoke(state);
    }
}