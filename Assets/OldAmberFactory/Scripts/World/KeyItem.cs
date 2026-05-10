using OldAmberFactory.Core;
using UnityEngine;

namespace OldAmberFactory.World
{
    /// <summary>The ignition key pickup. Connects to YellowTruck when interacted with.</summary>
    public class KeyItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private YellowTruck _truck;
        public string Prompt => "Pick up ignition key";
        public bool CanInteract(GameObject user) => _truck != null && !_truck.HasKey;
        public void Interact(GameObject user)
        {
            _truck.InsertKey();
            Destroy(gameObject);
        }
    }

    /// <summary>Power switch inside the admin block.</summary>
    public class PowerSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private YellowTruck _truck;
        public string Prompt => "Flip power switch";
        public bool CanInteract(GameObject user) => _truck != null && !_truck.IsPowerOn;
        public void Interact(GameObject user) => _truck.SetPower(true);
    }
}
