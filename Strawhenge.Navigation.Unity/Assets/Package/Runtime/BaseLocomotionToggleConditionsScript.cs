using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public abstract class BaseLocomotionToggleConditionsScript : MonoBehaviour
    {
        internal event Action StateChanged;

        protected void OnStateChanged() => StateChanged?.Invoke();

        protected internal abstract bool LocomotionShouldBeActive();
    }
}