using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public interface IDestinationArgsBuilder
    {
        DestinationArgs Build();

        IDestinationArgsBuilder WithSpeed(float speed);

        IDestinationArgsBuilder WithinDistance(float distance);

        IDestinationArgsBuilder Leisurely();
        
        IDestinationArgsBuilder Strafe();

        IDestinationArgsBuilder FromStartingPoint(Vector3 point);

        IDestinationArgsBuilder Callback(Action callback);

        IDestinationArgsBuilder Callback(Action<DestinationResult> callback);
    }
}