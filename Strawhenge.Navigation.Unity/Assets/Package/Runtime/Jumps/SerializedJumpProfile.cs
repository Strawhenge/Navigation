using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedJumpProfile : IJumpProfile
    {
        [SerializeField] JumpScriptableObject _jump;
        [SerializeField] ValueRange _speedRange;

        // Not _jump?.Id: '?.' skips Unity's null check, so a missing asset could count as assigned.
        public int? Id => _jump != null ? _jump.Id : null;

        public ValueRange SpeedRange => _speedRange;
    }
}
