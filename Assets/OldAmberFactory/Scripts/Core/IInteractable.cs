using UnityEngine;

namespace OldAmberFactory.Core
{
    /// <summary>
    /// Anything the player can focus and activate via the interact ray (E key).
    /// Implementations: doors, pickups, the truck, the security room, key items, power switches.
    /// </summary>
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject user);
        void Interact(GameObject user);
    }
}
