using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public abstract class DestinationContextScript : MonoBehaviour, IDestinationContext
    {
        public abstract bool CanNavigate { get; }
    }
}