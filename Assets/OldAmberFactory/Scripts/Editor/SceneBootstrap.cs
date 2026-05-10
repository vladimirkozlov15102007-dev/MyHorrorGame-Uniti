#if UNITY_EDITOR
using OldAmberFactory.AI;
using OldAmberFactory.Audio;
using OldAmberFactory.Damage;
using OldAmberFactory.Player;
using OldAmberFactory.Rendering;
using OldAmberFactory.UI;
using OldAmberFactory.Weapons;
using OldAmberFactory.World;
using UnityEditor;
using UnityEngine;

namespace OldAmberFactory.EditorTools
{
    /// <summary>
    /// One-click bootstrapper that assembles a playable skeleton of the game in the active scene:
    ///   - Player rig (CharacterController + camera + input router + stamina + health)
    ///   - Pistol child
    ///   - Global HDRP volume placeholder marker
    ///   - Game manager + group coordinator + behavior analyzer
    ///   - HUD canvas
    /// Artist must still place geometry, lighting, NavMesh, cover points, skeleton prefabs.
    /// </summary>
    public static class SceneBootstrap
    {
        [MenuItem("Old Amber Factory/Bootstrap Empty Scene")]
        public static void Bootstrap()
        {
            var root = new GameObject("OldAmberFactory_Root");

            // Player
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.layer = 6;
            player.tag   = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.32f;

            var input    = player.AddComponent<InputRouter>();
            var stam     = player.AddComponent<PlayerStamina>();
            var hp       = player.AddComponent<HealthSystem>();
            var fps      = player.AddComponent<FPSController>();
            player.AddComponent<PlayerInteractor>();

            // Camera pivot
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("MainCamera");
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShaker>();

            // Managers
            var managers = new GameObject("_Managers");
            managers.transform.SetParent(root.transform, false);
            managers.AddComponent<GameManager>();
            managers.AddComponent<PlayerBehaviorAnalyzer>();
            managers.AddComponent<GroupAICoordinator>();
            managers.AddComponent<HDRPBootstrap>();

            Debug.Log("Old Amber Factory scene bootstrapped. Now: add NavMesh, cover points, lighting, prefabs.");
        }
    }
}
#endif
