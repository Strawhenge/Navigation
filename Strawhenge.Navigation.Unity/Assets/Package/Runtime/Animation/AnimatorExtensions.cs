using Strawhenge.Common.Unity;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public static class AnimatorExtensions
    {
        // TODO: Move to Common
        public static void SetFloat(
            this Animator animator,
            AnimatorParameter parameter,
            float value,
            float dampTime,
            float deltaTime) =>
            animator.SetFloat(parameter.Id, value, dampTime, deltaTime);
    }
}