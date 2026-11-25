using UnityEngine;

namespace Development
{
    public class TimeScaleScript : MonoBehaviour
    {
        [SerializeField, Min(0)] float _timeScale = 1f;

        void Update()
        {
            if (!Mathf.Approximately(Time.timeScale, _timeScale))
                Time.timeScale = _timeScale;
        }
    }
}