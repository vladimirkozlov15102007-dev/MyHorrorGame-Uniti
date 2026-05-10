using System;
using OldAmberFactory.AI;
using OldAmberFactory.Core;
using OldAmberFactory.Damage;
using UnityEngine;
using UnityEngine.Events;

namespace OldAmberFactory.World
{
    /// <summary>
    /// Top-level game state and mission progression.
    /// Tracks skeleton deaths, truck objectives, and triggers the cinematic ending.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private HealthSystem _playerHealth;
        [SerializeField] private YellowTruck _truck;
        [SerializeField] private WaveSpawner _finalWaveSpawner;
        [SerializeField] private int _initialSkeletonCount = 10;

        [SerializeField] private UnityEvent _onAllEnemiesDead;
        [SerializeField] private UnityEvent _onGameOver;
        [SerializeField] private UnityEvent _onVictory;

        public event Action<int> OnSkeletonsRemainingChanged;

        private int _skeletonsRemaining;
        public int SkeletonsRemaining => _skeletonsRemaining;
        public bool AllDead => _skeletonsRemaining <= 0;

        private void Awake()
        {
            ServiceLocator.Register(this);
            _skeletonsRemaining = _initialSkeletonCount;
        }

        private void OnDestroy() => ServiceLocator.Unregister<GameManager>();

        private void Start()
        {
            // Discover all skeletons in scene and subscribe to death.
            var all = FindObjectsByType<SkeletonArcher>(FindObjectsSortMode.None);
            _skeletonsRemaining = all.Length;
            foreach (var s in all)
                s.Health.OnDied += _ => NotifySkeletonDied();

            if (_playerHealth != null)
                _playerHealth.OnDied += _ => _onGameOver?.Invoke();
        }

        public void NotifySkeletonDied()
        {
            _skeletonsRemaining = Mathf.Max(0, _skeletonsRemaining - 1);
            OnSkeletonsRemainingChanged?.Invoke(_skeletonsRemaining);
            if (_skeletonsRemaining == 0) _onAllEnemiesDead?.Invoke();
        }

        public void OnTruckEngineStarted()
        {
            // Final wave: survive long enough to actually escape.
            _finalWaveSpawner?.SpawnWave(0);
        }

        public void OnTruckDriveAway(Vector3 target, float driveTime)
        {
            StartCoroutine(DriveAwayRoutine(target, driveTime));
        }

        private System.Collections.IEnumerator DriveAwayRoutine(Vector3 target, float driveTime)
        {
            if (_truck == null) yield break;
            Vector3 start = _truck.transform.position;
            float t = 0f;
            while (t < driveTime)
            {
                t += Time.deltaTime;
                _truck.transform.position = Vector3.Lerp(start, target, t / driveTime);
                yield return null;
            }
            _onVictory?.Invoke();
        }
    }
}
