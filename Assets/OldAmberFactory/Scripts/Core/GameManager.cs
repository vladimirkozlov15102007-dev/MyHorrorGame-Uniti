using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.Core
{
    /// <summary>
    /// Central game singleton. Tracks global game state (alive skeletons, objective progress, game over / victory).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { Intro, Exploring, InCombat, EscapeSequence, Victory, GameOver }

        [Header("Objective")]
        [SerializeField] private int totalSkeletons = 10;
        [SerializeField] private bool truckPowerActivated;
        [SerializeField] private bool truckKeyFound;
        [SerializeField] private bool truckStarted;

        public GameState State { get; private set; } = GameState.Intro;
        public int SkeletonsAlive { get; private set; }
        public bool TruckPowerActivated => truckPowerActivated;
        public bool TruckKeyFound => truckKeyFound;
        public bool TruckStarted => truckStarted;

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<int> OnSkeletonCountChanged;
        public event System.Action OnVictory;
        public event System.Action OnGameOver;

        private readonly List<GameObject> _registeredSkeletons = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SkeletonsAlive = totalSkeletons;
        }

        public void RegisterSkeleton(GameObject skeleton)
        {
            if (!_registeredSkeletons.Contains(skeleton))
            {
                _registeredSkeletons.Add(skeleton);
                SkeletonsAlive = _registeredSkeletons.Count;
                OnSkeletonCountChanged?.Invoke(SkeletonsAlive);
            }
        }

        public void ReportSkeletonDeath(GameObject skeleton)
        {
            if (_registeredSkeletons.Remove(skeleton))
            {
                SkeletonsAlive = _registeredSkeletons.Count;
                OnSkeletonCountChanged?.Invoke(SkeletonsAlive);
                if (SkeletonsAlive <= 0) SetState(GameState.EscapeSequence);
            }
        }

        public void ActivateTruckPower() { truckPowerActivated = true; }
        public void CollectTruckKey() { truckKeyFound = true; }
        public void StartTruck() { truckStarted = true; TriggerVictory(); }

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            OnStateChanged?.Invoke(next);
        }

        public void TriggerVictory()
        {
            SetState(GameState.Victory);
            OnVictory?.Invoke();
        }

        public void TriggerGameOver()
        {
            SetState(GameState.GameOver);
            OnGameOver?.Invoke();
        }
    }
}
