using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    interface IState
    {
        bool IsActive { get; }

        Vector3 Velocity { get; }

        void GoTo(DestinationArgs args, Action<IState> changeState);

        void Update(Action<IState> changeState);

        void Cancel(Action<IState> changeState);
    }
}
