using UnityEngine;

// Definicja warunków ataku
public enum AttackCondition { Normal, PunishHeal, Counter }

[CreateAssetMenu(fileName = "NewAttack", menuName = "Sojartsa/Enemy Attack")]
public class EnemyAttackData : ScriptableObject
{
    [Header("Animacja")]
    public string animationTrigger = "Attack";
    
    [Header("Zasięg")]
    public float minDistance = 0f;
    public float maxDistance = 3f;

    [Header("Pula Losowania")]
    [Tooltip("Im wyższa waga, tym częściej AI wybierze ten atak spośród pasujących.")]
    public float weight = 1.0f;
    
    [Header("Parametry")]
    public float attackCooldown = 1.5f;
    public AttackCondition condition = AttackCondition.Normal;

    [Header("Specjalne")]
    public bool isJumpAttack = false; // Jeśli true, AI traktuje to jako doskok
}
