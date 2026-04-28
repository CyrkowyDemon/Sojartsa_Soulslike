using UnityEngine;
using System.Collections.Generic;

namespace SojartsaAI.v3
{
    /// <summary>
    /// GLOBALNY REŻYSER WALKI (AAA Protocol).
    /// Zarządza Slotami Bojowymi (Combat Circles) i Tokenami Ataku.
    /// Sprawia, że wrogowie osaczają gracza, zamiast stać w miejscu.
    /// </summary>
    public class GlobalCombatDirector : MonoBehaviour
    {
        public static GlobalCombatDirector Instance { get; private set; }

        [Header("Zarządzanie Atakiem")]
        public int maxMeleeAttackTokens = 1;
        
        [Header("Zarządzanie Slotami (Osaczanie)")]
        public float circleRadius = 4f;
        public float anglePerSlot = 60f; // Ilu wrogów w pełnym kole (360/60 = 6)

        private List<AIBrain> _enemiesInCombat = new List<AIBrain>();
        private HashSet<AIBrain> _activeAttackers = new HashSet<AIBrain>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ================================================================
        // SYSTEM SLOTÓW (SURROUNDING)
        // ================================================================

        public void RegisterEnemy(AIBrain enemy)
        {
            if (!_enemiesInCombat.Contains(enemy))
            {
                _enemiesInCombat.Add(enemy);
                UpdateSlots();
            }
        }

        public void UnregisterEnemy(AIBrain enemy)
        {
            if (_enemiesInCombat.Remove(enemy))
            {
                _activeAttackers.Remove(enemy);
                UpdateSlots();
            }
        }

        private void UpdateSlots()
        {
            // Przypisujemy każdemu wrogowi jego "miejsce w kolejce" wokół gracza
            for (int i = 0; i < _enemiesInCombat.Count; i++)
            {
                _enemiesInCombat[i].CombatSlotIndex = i;
            }
        }

        /// <summary>
        /// Zwraca pozycję w świecie, w której wróg powinien stać (bazując na jego slocie).
        /// </summary>
        public Vector3 GetSlotPosition(AIBrain enemy, Transform player)
        {
            if (enemy.CombatSlotIndex == -1 || player == null) return enemy.transform.position;

            // Obliczamy kąt dla danego slotu (0, 60, -60, 120, -120...)
            float angle = (enemy.CombatSlotIndex % 2 == 0) 
                ? (enemy.CombatSlotIndex / 2) * anglePerSlot 
                : (enemy.CombatSlotIndex / 2 + 1) * -anglePerSlot;

            Vector3 offset = Quaternion.Euler(0, angle, 0) * player.forward * circleRadius;
            return player.position + offset;
        }

        // ================================================================
        // SYSTEM TOKENÓW (ATTACK PERMISSION)
        // ================================================================

        public bool RequestAttackToken(AIBrain enemy)
        {
            if (_activeAttackers.Count < maxMeleeAttackTokens)
            {
                _activeAttackers.Add(enemy);
                enemy.HasAttackToken = true;
                return true;
            }
            return false;
        }

        public void ReleaseAttackToken(AIBrain enemy)
        {
            _activeAttackers.Remove(enemy);
            enemy.HasAttackToken = false;
        }
    }
}
