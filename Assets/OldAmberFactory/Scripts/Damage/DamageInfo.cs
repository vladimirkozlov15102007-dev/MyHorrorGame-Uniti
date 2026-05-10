using UnityEngine;

namespace OldAmberFactory.Damage
{
    public enum BodyPartType
    {
        Head   = 0,
        Torso  = 1,
        Legs   = 2,
        Arms   = 3,
        Other  = 4
    }

    /// <summary>
    /// A single damage event, passed by value between weapons, projectiles and receivers.
    /// Contains everything required for hit reactions, blood decals and directional damage UI.
    /// </summary>
    public struct DamageInfo
    {
        public float amount;
        public BodyPartType bodyPart;
        public Vector3 hitPoint;
        public Vector3 hitDirection;   // world-space direction of the incoming shot/strike
        public Vector3 hitNormal;
        public float impulse;          // physical force applied to ragdolls / rigidbodies
        public GameObject instigator;  // who caused the damage (player, skeleton, projectile owner)
        public bool canPenetrateArmor;

        public static DamageInfo FromHit(float amount, BodyPartType part, RaycastHit hit,
            Vector3 direction, float impulse, GameObject instigator, bool pierce = false)
        {
            return new DamageInfo
            {
                amount             = amount,
                bodyPart           = part,
                hitPoint           = hit.point,
                hitDirection       = direction.normalized,
                hitNormal          = hit.normal,
                impulse            = impulse,
                instigator         = instigator,
                canPenetrateArmor  = pierce
            };
        }
    }
}
