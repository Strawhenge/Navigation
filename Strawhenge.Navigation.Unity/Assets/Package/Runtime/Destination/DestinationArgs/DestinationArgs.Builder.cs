using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public partial class DestinationArgs
    {
        class ArgsBuilder : IDestinationArgsBuilder
        {
            readonly Vector3 _location;
            readonly bool _exact;

            float? _distance;
            bool _leisurely;
            bool _strafe;
            float _speed = 1;
            Vector3? _startingPoint;
            Action<DestinationResult> _callback = _ => { };

            public ArgsBuilder(Vector3 location, bool exact)
            {
                _location = location;
                _exact = exact;
            }

            public IDestinationArgsBuilder WithSpeed(float speed)
            {
                _speed = speed;
                return this;
            }

            public IDestinationArgsBuilder WithinDistance(float distance)
            {
                _distance = distance;
                return this;
            }

            public IDestinationArgsBuilder Leisurely()
            {
                _leisurely = true;
                return this;
            }

            public IDestinationArgsBuilder Strafe()
            {
                _strafe = true;
                return this;
            }

            public IDestinationArgsBuilder FromStartingPoint(Vector3 point)
            {
                _startingPoint = point;
                return this;
            }

            public IDestinationArgsBuilder Callback(Action<DestinationResult> callback)
            {
                _callback = callback;
                return this;
            }

            public IDestinationArgsBuilder Callback(Action callback)
            {
                _callback = _ => callback();
                return this;
            }

            public DestinationArgs Build()
            {
                return new DestinationArgs
                {
                    Location = _location,
                    StartingLocation = _startingPoint,
                    Speed = _speed,
                    LocationMustBeExact = _exact,
                    Leisurely = _leisurely,
                    Strafe = _strafe,
                    TargetDistance = _distance,
                    Callback = _callback
                };
            }
        }
    }
}