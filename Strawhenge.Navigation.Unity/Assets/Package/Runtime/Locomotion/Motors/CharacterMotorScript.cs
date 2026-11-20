using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public abstract class CharacterMotorScript : MonoBehaviour
    {
        public abstract ICharacterMotor Motor { get; }
    }
}
