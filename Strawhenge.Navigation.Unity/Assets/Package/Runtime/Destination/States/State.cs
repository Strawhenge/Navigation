using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    abstract class State
    {
        internal event Action<State> ChangeStateRequested;

        protected internal virtual bool IsActive => true;

        protected internal abstract Vector3 Velocity { get; }

        protected internal abstract void GoTo(DestinationArgs args);

        protected internal abstract void Update();

        protected internal abstract void Cancel();

        protected void ChangeState(State state) => ChangeStateRequested?.Invoke(state);
    }
}