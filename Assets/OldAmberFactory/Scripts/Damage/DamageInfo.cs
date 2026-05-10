using UnityEngine;

namespace OldAmberFactory.Damage
{
    public enum BodyPart { Head, Torso, Legs, Arms, Generic }

    public struct DamageInfo
    {
        public float baseDamage;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public Vector3 hitForce;
        public BodyPart bodyPart;
        public GameObject source;
        public bool isProjectile;
        public float armorPenetration; // 0..1

        public float FinalDamage()
        {
            return bodyPart switch
            {
                BodyPart.Head   => baseDamage * 3f,   // 10 base -> 30
                BodyPart.Torso  => baseDamage * 2f,   // 10 base -> 20
                BodyPart.Legs   => baseDamage * 1f,   // 10 base -> 10
                BodyPart.Arms   => baseDamage * 1f,
                _               => baseDamage,
            };
        }
    }
}
