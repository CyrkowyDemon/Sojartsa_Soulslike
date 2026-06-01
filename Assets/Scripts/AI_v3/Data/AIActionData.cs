using UnityEngine;

namespace SojartsaAI.v3
{
    public enum AIActionType { Attack, Dodge, Special, Buff }

    [CreateAssetMenu(fileName = "New AI Action", menuName = "AAA SOJARTSA AI/Action")]
    public class AIActionData : ScriptableObject
    {
        public string actionName;
        public AIActionType type;
        public string animationTrigger;

        [Header("Audio (The Tell)")]
        public FMODUnity.EventReference actionTellSound;

        [Header("Zasięg")]
        public float minDistance = 0f;
        public float maxDistance = 5f;

        [Header("Balans")]
        public float weight = 1.0f;
        public float cooldown = 2.0f;
        public int damageAmount = 10;
        public float poiseDamage = 10f;
        
        [Header("AAA - Dynamic Tracking (The Hunting)")]
        [Tooltip("Jak silnie AI ma się obracać w stronę gracza w trakcie ataku (0-1)")]
        [Range(0, 1)] public float trackingIntensity = 0.5f;
        [Tooltip("Do którego momentu animacji AI ma śledzić gracza (np. 0.8 = do 80%)")]
        [Range(0, 1)] public float trackingCutoff = 0.5f;

        [Header("Kierunkowość")]
        public bool isBehindOnly = false;
        
        [Header("AAA - Environmental Awareness")]
        public bool checkEnvironment = false;
        public Vector3 environmentCheckDir = Vector3.back;
        public float checkDistance = 2f;
        
        [Header("Combo (AAA Extension)")]
        public AIActionData followUpAction;
        
        [Header("Branching Combo (AAA Extension)")]
        [Tooltip("Lista losowych ciosów kontynuujących (combo) z ich szansami procentowymi.")]
        public System.Collections.Generic.List<AIRandomFollowUp> randomFollowUps;

        [Header("System Cancelowania (Cancel Window)")]
        [Tooltip("Akcja do której można cancelować ten ruch (np. Dash). Puste = brak cancela.")]
        public AIActionData cancelIntoAction;
        [Tooltip("Odznaczone: cancel odpala się gdy animator wyśle sygnał 'CanCancel'.\nZaznaczone: system losuje moment cancela z podanego zakresu.")]
        public bool useRandomCancelWindow = false;
        [Tooltip("Minimalny procent animacji od którego może nastąpić cancel (0.0 - 1.0)")]
        [Range(0, 1)] public float cancelWindowMin = 0.3f;
        [Tooltip("Maksymalny procent animacji do którego może nastąpić cancel (0.0 - 1.0)")]
        [Range(0, 1)] public float cancelWindowMax = 0.7f;
    }

    [System.Serializable]
    public struct AIRandomFollowUp
    {
        [Tooltip("Akcja do wykonania w ramach combo.")]
        public AIActionData followUpAction;
        [Tooltip("Szansa procentowa na wykonanie tej akcji (0-100).")]
        [Range(0, 100)] public int chance;
    }
}
