using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public static class DestinationControllerExtensions
    {
        public static void GoToExactly(
            this DestinationController controller,
            Vector3 location,
            float speed,
            bool leisurely = false) =>
            GoTo(controller, location, speed, mustBeExact: true, callback: _ => { }, leisurely);

        public static void GoToExactly(this DestinationController controller, Vector3 location, float speed,
            Action<DestinationResult> callback,
            bool leisurely = false) =>
            GoTo(controller, location, speed, mustBeExact: true, callback, leisurely);

        public static void GoToApproximately(this DestinationController controller, Vector3 location, float speed,
            bool leisurely = false) =>
            GoTo(controller, location, speed, mustBeExact: false, callback: _ => { }, leisurely);

        public static void GoToApproximately(this DestinationController controller, Vector3 location, float speed,
            Action<DestinationResult> callback,
            bool leisurely = false) =>
            GoTo(controller, location, speed, mustBeExact: false, callback, leisurely);

        static void GoTo(this DestinationController controller, Vector3 location, float speed, bool mustBeExact,
            Action<DestinationResult> callback,
            bool leisurely) =>
            controller.GoTo(new DestinationArgs(location, speed, mustBeExact, leisurely, callback));
    }
}