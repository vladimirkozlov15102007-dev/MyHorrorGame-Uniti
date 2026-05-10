using OldAmberFactory.Player;
using UnityEngine;

namespace OldAmberFactory.AI
{
    public enum PlayerTactic
    {
        Neutral,
        CoverCamper,       // hides behind cover a lot => enemies flank
        Runner,            // constantly sprinting   => enemies lead shots / predict
        PositionLocked,    // stays in same area     => enemies set ambushes
        Aggressive,        // closes the distance    => enemies keep range
        Fixed_Shooter      // shoots from one spot   => enemies suppress
    }

    /// <summary>
    /// Samples the player every tick and classifies their behavioural style.
    /// The classification is fed to the GroupAICoordinator which assigns matching roles.
    /// Implementation is heuristic / rolling-window rather than ML to keep it deterministic.
    /// </summary>
    public class PlayerBehaviorAnalyzer : MonoBehaviour
    {
        [SerializeField] private FPSController _player;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private float _sampleInterval = 0.4f;
        [SerializeField] private float _windowSeconds = 10f;

        private float _nextSampleAt;

        // rolling aggregates
        private float _sprintTime;
        private float _crouchTime;
        private float _shootTime;
        private float _totalTime;

        private Vector3 _startPos;
        private float _travelledDistance;
        private Vector3 _lastPos;

        private int _shotsInWindow;
        private Vector3 _shotCenter;
        private float _shotSpread;

        public PlayerTactic CurrentTactic { get; private set; } = PlayerTactic.Neutral;

        private void Start()
        {
            if (_playerTransform != null) _lastPos = _startPos = _playerTransform.position;
        }

        public void RegisterPlayerShot(Vector3 pos)
        {
            _shotsInWindow++;
            // running mean / spread
            Vector3 prev = _shotCenter;
            _shotCenter = Vector3.Lerp(_shotCenter, pos, 0.4f);
            _shotSpread += Vector3.Distance(prev, pos);
        }

        private void Update()
        {
            if (_player == null || _playerTransform == null) return;
            if (Time.time < _nextSampleAt) return;
            _nextSampleAt = Time.time + _sampleInterval;

            float dt = _sampleInterval;
            _totalTime += dt;
            if (_player.IsSprinting)  _sprintTime  += dt;
            if (_player.IsCrouched)   _crouchTime  += dt;
            // shoot frequency tracked via RegisterPlayerShot

            _travelledDistance += Vector3.Distance(_playerTransform.position, _lastPos);
            _lastPos = _playerTransform.position;

            if (_totalTime >= _windowSeconds)
            {
                CurrentTactic = Classify();
                ResetWindow();
            }
        }

        private PlayerTactic Classify()
        {
            float sprintRatio = _sprintTime / _totalTime;
            float crouchRatio = _crouchTime / _totalTime;
            float travel      = _travelledDistance;
            float shotRate    = _shotsInWindow / _totalTime;

            // Runner: constantly sprinting and covering ground.
            if (sprintRatio > 0.5f && travel > 40f)                return PlayerTactic.Runner;
            // Aggressive: rushes, high shot rate, moves a lot.
            if (shotRate > 1.1f && travel > 25f)                   return PlayerTactic.Aggressive;
            // PositionLocked / camper: crouches, barely moves.
            if (crouchRatio > 0.5f && travel < 8f)                 return PlayerTactic.PositionLocked;
            // CoverCamper: crouches+moves between covers (moderate travel + high crouch).
            if (crouchRatio > 0.35f && travel > 8f && travel < 25f) return PlayerTactic.CoverCamper;
            // Fixed shooter: many shots, low travel, tight grouping.
            if (_shotsInWindow >= 6 && travel < 10f && _shotSpread < 4f) return PlayerTactic.Fixed_Shooter;

            return PlayerTactic.Neutral;
        }

        private void ResetWindow()
        {
            _sprintTime = _crouchTime = _shootTime = _totalTime = 0f;
            _travelledDistance = 0f;
            _shotsInWindow = 0;
            _shotSpread = 0f;
            _shotCenter = _playerTransform != null ? _playerTransform.position : Vector3.zero;
        }
    }
}
