using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public abstract class MovementSourceScript : MonoBehaviour
    {
        public abstract IMovementSource MovementSource { get; }
    }
}