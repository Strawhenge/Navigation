using System;

namespace Strawhenge.Navigation.Unity.Destination
{
    public interface IDestinationArgsBuilder
    {
        DestinationArgs Build();

        IDestinationArgsBuilder WithSpeed(float speed);

        IDestinationArgsBuilder WithinDistance(float distance);

        IDestinationArgsBuilder Leisurely();
        
        IDestinationArgsBuilder Strafe();

        IDestinationArgsBuilder Callback(Action callback);

        IDestinationArgsBuilder Callback(Action<DestinationResult> callback);
    }
}