using System.Collections.Generic;
using OldAmberFactory.AI;
using UnityEngine;
using UnityEngine.AI;

namespace OldAmberFactory.World
{
    /// <summary>
    /// Used by the finale: once the truck engine starts, spawns the last wave
    /// of skeletons around the truck. Also used for initial placement of the 10 archers.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class Wave
        {
            public SkeletonArcher prefab;
            public int count = 3;
            public Transform[] spawnPoints;
        }

        [SerializeField] private Wave[] _waves;
        [SerializeField] private float _spawnInterval = 1.5f;

        private readonly List<SkeletonArcher> _spawned = new();

        public int SpawnedCount => _spawned.Count;

        public void SpawnWave(int index)
        {
            if (index < 0 || index >= _waves.Length) return;
            StartCoroutine(SpawnRoutine(_waves[index]));
        }

        private System.Collections.IEnumerator SpawnRoutine(Wave w)
        {
            for (int i = 0; i < w.count; i++)
            {
                var point = w.spawnPoints[i % w.spawnPoints.Length];
                if (NavMesh.SamplePosition(point.position, out var nh, 3f, NavMesh.AllAreas))
                {
                    var instance = Instantiate(w.prefab, nh.position, point.rotation);
                    _spawned.Add(instance);
                }
                yield return new WaitForSeconds(_spawnInterval);
            }
        }
    }
}
