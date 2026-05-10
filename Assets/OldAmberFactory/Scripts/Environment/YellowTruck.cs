using System.Collections;
using UnityEngine;
using OldAmberFactory.Player;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// The Yellow Truck / escape vehicle. Requires:
    ///   1. Power activated at a switch somewhere in the factory
    ///   2. Key collected
    ///   3. Engine started (hold E for X seconds, then final wave spawns)
    ///   4. Drive away -> victory
    /// </summary>
    public class YellowTruck : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform exitWaypoint;
        [SerializeField] private float startupHold = 3f;
        [SerializeField] private float driveSpeed = 7f;
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioClip engineCrankClip;
        [SerializeField] private AudioClip engineRunClip;
        [SerializeField] private Light[] headlights;
        [SerializeField] private ParticleSystem exhaust;
        [SerializeField] private Rigidbody body;

        public string Prompt
        {
            get
            {
                var gm = Core.GameManager.Instance;
                if (gm == null) return "";
                if (!gm.TruckPowerActivated) return "Truck has no power";
                if (!gm.TruckKeyFound) return "You need the key";
                if (!gm.TruckStarted) return "Hold E to start the engine";
                return "Engine running";
            }
        }

        bool _starting;
        float _startTimer;

        public bool CanInteract(GameObject interactor)
        {
            var gm = Core.GameManager.Instance;
            return gm != null && gm.TruckPowerActivated && gm.TruckKeyFound && !gm.TruckStarted;
        }

        public void Interact(GameObject interactor)
        {
            if (_starting) return;
            StartCoroutine(StartEngine(interactor));
        }

        IEnumerator StartEngine(GameObject interactor)
        {
            _starting = true;
            if (engineCrankClip && engineSource) engineSource.PlayOneShot(engineCrankClip);
            _startTimer = 0f;
            while (_startTimer < startupHold)
            {
                if (!Input.GetKey(KeyCode.E)) { _starting = false; yield break; }
                _startTimer += Time.deltaTime;
                yield return null;
            }
            Core.GameManager.Instance.StartTruck();
            if (engineSource && engineRunClip)
            {
                engineSource.clip = engineRunClip;
                engineSource.loop = true;
                engineSource.Play();
            }
            foreach (var l in headlights) if (l) l.enabled = true;
            if (exhaust) exhaust.Play();
            yield return DriveAway(interactor);
        }

        IEnumerator DriveAway(GameObject passenger)
        {
            if (passenger) passenger.transform.SetParent(transform, worldPositionStays: false);
            if (body) body.isKinematic = true;
            while (exitWaypoint && Vector3.Distance(transform.position, exitWaypoint.position) > 1.5f)
            {
                Vector3 dir = (exitWaypoint.position - transform.position).normalized;
                transform.position += dir * driveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), Time.deltaTime * 2f);
                yield return null;
            }
            Core.GameManager.Instance?.TriggerVictory();
        }
    }

    /// <summary>
    /// Pickup for the truck key. Sets the flag on GameManager.
    /// </summary>
    public class TruckKey : MonoBehaviour, IInteractable
    {
        public string Prompt => "Pick up key (E)";
        public bool CanInteract(GameObject interactor) => true;
        public void Interact(GameObject interactor)
        {
            Core.GameManager.Instance?.CollectTruckKey();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Interactable switch that activates the truck power system.
    /// </summary>
    public class PowerSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private Light[] roomLights;
        public string Prompt => "Activate power (E)";
        public bool CanInteract(GameObject interactor) => Core.GameManager.Instance != null && !Core.GameManager.Instance.TruckPowerActivated;
        public void Interact(GameObject interactor)
        {
            Core.GameManager.Instance.ActivateTruckPower();
            if (clickClip) Audio.AudioManager.Instance?.PlayOneShot3D(clickClip, transform.position, 0.8f);
            foreach (var l in roomLights) if (l) l.enabled = true;
        }
    }
}
