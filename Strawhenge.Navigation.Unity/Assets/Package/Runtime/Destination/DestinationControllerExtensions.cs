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
            bool leisurely = false,
            bool strafe = false) =>
            GoTo(controller, location, speed, mustBeExact: true, callback: _ => { }, leisurely, strafe);

        public static void GoToExactly(
            this DestinationController controller,
            Vector3 location,
            float speed,
            Action<DestinationResult> callback,
            bool leisurely = false,
            bool strafe = false) =>
            GoTo(controller, location, speed, mustBeExact: true, callback, leisurely, strafe);

        public static void GoToApproximately(
            this DestinationController controller,
            Vector3 location,
            float speed,
            bool leisurely = false,
            bool strafe = false) =>
            GoTo(controller, location, speed, mustBeExact: false, callback: _ => { }, leisurely, strafe);

        public static void GoToApproximately(this DestinationController controller, Vector3 location, float speed,
            Action<DestinationResult> callback,
            bool leisurely = false,
            bool strafe = false) =>
            GoTo(controller, location, speed, mustBeExact: false, callback, leisurely, strafe);

        static void GoTo(
            this DestinationController controller,
            Vector3 location,
            float speed,
            bool mustBeExact,
            Action<DestinationResult> callback,
            bool leisurely,
            bool strafe) =>
            controller.GoTo(new DestinationArgs(location, speed, mustBeExact, leisurely, strafe, callback));
    }
}