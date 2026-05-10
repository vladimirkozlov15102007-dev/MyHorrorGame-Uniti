namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Implemented by any root that can receive damage (player, skeleton, destructible prop).
    /// </summary>
    public interface IDamageReceiver
    {
        bool IsDead { get; }
        void ReceiveDamage(DamageInfo info);
    }
}
