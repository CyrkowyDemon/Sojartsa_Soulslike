namespace SojartsaAI
{
    /// <summary>
    /// Interfejs dla wszystkiego co można zranić (Gracz, AI, Beczki).
    /// Pozwala na komunikację bez sztywnego dziedziczenia.
    /// </summary>
    public interface IDamageable
    {
        void OnDamagedByPlayer(float healthDamage, float poiseDamage, UnityEngine.Vector3 hitSource);
    }
}
