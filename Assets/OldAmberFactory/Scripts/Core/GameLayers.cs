using UnityEngine;

namespace OldAmberFactory.Core
{
    /// <summary>
    /// Centralised layer and tag identifiers.
    /// Expects the following layers to be configured in Unity's Tag &amp; Layer settings:
    ///   6  Player
    ///   7  Enemy
    ///   8  Projectile
    ///   9  Cover
    ///  10  Interactable
    ///  11  Throwable
    ///  12  EnemyRagdoll
    /// </summary>
    public static class GameLayers
    {
        public const int Player        = 6;
        public const int Enemy         = 7;
        public const int Projectile    = 8;
        public const int Cover         = 9;
        public const int Interactable  = 10;
        public const int Throwable     = 11;
        public const int EnemyRagdoll  = 12;

        public static int Mask(params int[] layers)
        {
            int m = 0;
            for (int i = 0; i < layers.Length; i++) m |= 1 << layers[i];
            return m;
        }

        public static readonly int HitscanMask =
            ~(1 << 2)                // ignore IgnoreRaycast
            & ~(1 << Projectile)
            & ~(1 << Player);
    }

    public static class GameTags
    {
        public const string Player    = "Player";
        public const string Enemy     = "Enemy";
        public const string Head      = "BodyPart_Head";
        public const string Torso     = "BodyPart_Torso";
        public const string Legs      = "BodyPart_Legs";
        public const string Truck     = "YellowTruck";
        public const string KeyItem   = "KeyItem";
    }
}
