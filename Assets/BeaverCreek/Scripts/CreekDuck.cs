using UnityEngine;

namespace BeaverCreek
{
    [DisallowMultipleComponent]
    public sealed class CreekDuck : MonoBehaviour
    {
        public Animator animator;
        public int seed = 1;
        public bool swimming;
        public float roamRadius = 1.2f;
        public float actionDuration = 3.8f;
        public string CurrentBehavior { get; private set; }
        public int BehaviorChanges { get; private set; }
        System.Random random;
        Vector3 origin;
        float nextAction, actionEnd, phase;
        int action;
        void Start()
        {
            random = new System.Random(seed);
            origin = transform.position;
            phase = (float)random.NextDouble() * 6.28f;
            nextAction = Time.time + 2 + (float)random.NextDouble() * 7;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Play(swimming ? "Swim" : "Idle");
            animator.Update(0);
        }
        void Play(string state)
        {
            CurrentBehavior = state; BehaviorChanges++;
            animator.CrossFadeInFixedTime(state, .25f, 0, 0);
        }
        void Update()
        {
            if (!animator) return;
            if (actionEnd > 0 && Time.time >= actionEnd)
            {
                Play(swimming ? "Swim" : "Idle"); actionEnd = 0;
                nextAction = Time.time + 6 + (float)random.NextDouble() * 12;
            }
            if (actionEnd == 0 && Time.time >= nextAction)
            {
                // Alternate flapping and two supplied preening clips; independent timing per bird.
                string state = action % 3 == 0 ? "Flap" : action % 3 == 1 ? "PreenFront" : "PreenBack";
                action++; Play(state);
                actionEnd = Time.time + (state == "Flap" ? 2.46f : actionDuration);
            }
            if (!swimming) return;
            float t = Time.time * .045f + phase;
            transform.position = origin + new Vector3(Mathf.Sin(t) * roamRadius, Mathf.Sin(Time.time * 1.6f + phase) * .018f, Mathf.Cos(t) * roamRadius * .5f);
            transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(t),0,-Mathf.Sin(t)*.5f));
        }
    }
}
