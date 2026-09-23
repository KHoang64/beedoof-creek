using UnityEngine;

namespace BeaverCreek
{
    /// <summary>Visual-only, bounded rainfall; no per-drop objects or physics.</summary>
    public sealed class CreekWeather : MonoBehaviour
    {
        public Camera view;
        public ParticleSystem drizzle;
        public ParticleSystem pondRipples;
        public Vector3 emitterOffset = new Vector3(0,10,0);
        public int LiveParticles => (drizzle ? drizzle.particleCount : 0) + (pondRipples ? pondRipples.particleCount : 0);
        void Start()
        {
            if(!view) view=Camera.main;
            PositionEmitter();
            if(drizzle) {drizzle.Simulate(1.2f,true,true);drizzle.Play();}
            if(pondRipples) {pondRipples.Simulate(1.2f,true,true);pondRipples.Play();}
        }
        void LateUpdate() { PositionEmitter(); }
        void PositionEmitter() { if(view&&drizzle) drizzle.transform.position=view.transform.position+emitterOffset; }
        void OnDisable()
        {
            if(drizzle) drizzle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(pondRipples) pondRipples.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
