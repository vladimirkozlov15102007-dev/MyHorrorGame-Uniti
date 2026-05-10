using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Central damage multipliers specified by design.
    /// Head = 30, Torso = 20, Legs = 10 on a 1.0-scale weapon.
    /// Weapons pass the raw base damage, and HitBoxes apply BodyPart multipliers on top.
    /// Using this table ensures a single source of truth for tuning.
    /// </summary>
    public static class DamageTable
    {
        public const float HeadDamage  = 30f;
        public const float TorsoDamage = 20f;
        public const float LegsDamage  = 10f;

        public static float ForBodyPart(BodyPartType part)
        {
            switch (part)
            {
                case BodyPartType.Head:  return HeadDamage;
                case BodyPartType.Torso: return TorsoDamage;
                case BodyPartType.Legs:  return LegsDamage;
                case BodyPartType.Arms:  return LegsDamage;
                default:                 return TorsoDamage;
            }
        }

        public static float Multiplier(BodyPartType part)
        {
            switch (part)
            {
                case BodyPartType.Head:  return 1.5f;
                case BodyPartType.Torso: return 1.0f;
                case BodyPartType.Legs:  return 0.5f;
                default:                 return 0.75f;
            }
        }
    }
}
