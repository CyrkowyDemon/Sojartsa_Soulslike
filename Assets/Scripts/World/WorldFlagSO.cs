using UnityEngine;

/// <summary>
/// Profesjonalny system flag świata.
/// Tworzysz plik tego typu (np. "BossDefeated") i przeciągasz go do warunków NPC.
/// Zero literówek, 100% profesjonalizmu.
/// </summary>
[CreateAssetMenu(fileName = "NewWorldFlag", menuName = "Sojartsa/World/Flag")]
public class WorldFlagSO : ScriptableObject
{
    [TextArea(2, 5)]
    public string description; // Tylko dla Twojej wiadomości, co ta flaga robi.
}
