using UnityEngine;

namespace GOAP.Actions
{
    public sealed class GoapTimer
    {
        private float start;
        private float duration;
        private bool started;

        public void Start(float time)
        {
            start = Time.time;
            duration = time;
            started = true;
        }

        public bool Done => started && (Time.time - start) >= duration;

        public void Reset()
        {
            started = false;
            start = 0f;
            duration = 0f;
        }
    }
}
