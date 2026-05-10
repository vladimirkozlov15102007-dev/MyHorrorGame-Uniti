using System;
using UnityEngine;

namespace OldAmberFactory.Audio
{
    public enum NoiseSource
    {
        Footstep,
        Gunshot,
        ThrownObjectImpact,
        PropBreak,
        Voice,
        Generic
    }

    public struct NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public NoiseSource source;
        public GameObject emitter;
        public float timeStamp;
    }

    /// <summary>
    /// Global decoupled noise bus. Anything that can be heard by AI (player footsteps,
    /// weapons, thrown bottles, breaking props) publishes here. The AI perception
    /// subsystem subscribes once per enemy and evaluates events against hearing radius.
    /// </summary>
    public static class NoiseEventBus
    {
        public static event Action<NoiseEvent> OnNoise;

        public static void Broadcast(Vector3 position, float radius, NoiseSource source, GameObject emitter)
        {
            OnNoise?.Invoke(new NoiseEvent
            {
                position  = position,
                radius    = radius,
                source    = source,
                emitter   = emitter,
                timeStamp = Time.time
            });
        }
    }
}
