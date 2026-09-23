using UnityEngine;
using UnityEngine.InputSystem;

namespace BeaverCreek
{
    public sealed class CreekCinematic : MonoBehaviour
    {
        [System.Serializable] public struct Shot
        {
            public string name;
            public Vector3 start, end, lookStart, lookEnd;
            public float duration, fieldOfView;
        }
        public Camera view;
        public Shot[] shots;
        public bool loop = true;
        public int ShotIndex { get; private set; }
        float elapsed;
        bool paused;
        public bool InputEnabled { get; set; } = true;
        public bool IsPaused => paused;
        public string CurrentShotName => shots != null && shots.Length > 0 ? shots[ShotIndex].name : "Beaver Creek";
        public void TogglePause() { paused = !paused; }
        public void Restart() { ShotIndex = 0; elapsed = 0; paused = false; Evaluate(0, 0); }
        public void Evaluate(int index, float progress)
        {
            if (shots == null || shots.Length == 0 || !view) return;
            var shot = shots[Mathf.Clamp(index,0,shots.Length-1)];
            float t = Mathf.SmoothStep(0,1,progress);
            view.transform.position = Vector3.Lerp(shot.start,shot.end,t);
            view.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(shot.lookStart,shot.lookEnd,t) - view.transform.position);
            view.fieldOfView = shot.fieldOfView;
        }
        void Start() { Evaluate(0,0); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void Update()
        {
            if (!InputEnabled || shots == null || shots.Length == 0) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame) TogglePause();
                if (Keyboard.current.rKey.wasPressedThisFrame) Restart();
            }
            if (!paused) elapsed += Time.deltaTime;
            float duration = Mathf.Max(1, shots[ShotIndex].duration);
            if (elapsed >= duration)
            {
                elapsed=0;
                if (ShotIndex < shots.Length-1) ShotIndex++; else if(loop) ShotIndex=0; else { paused=true; elapsed=duration; }
            }
            Evaluate(ShotIndex,elapsed / Mathf.Max(1,shots[ShotIndex].duration));
        }
    }
}
