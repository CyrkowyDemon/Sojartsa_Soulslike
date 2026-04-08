using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    [SerializeField] private Animator animator;
    private bool _isDead = false;
    public bool IsDead => _isDead;

    [SerializeField] private bool _isInvincible = false; // I-Frames z dasha

    [Header("UI")]
    [SerializeField] private PlayerHUD hud;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Debug.Log($"<color=green>[HEALTH] Skrypt PlayerHealth zainicjalizowany na {gameObject.name}. HP: {currentHealth}/{maxHealth}</color>");
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (hud != null) hud.UpdateHP(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage, bool isKnockback = false)
    {
        if (_isDead) return;

        if (_isInvincible)
        {
            Debug.Log("<color=cyan>[I-FRAME] Gracz przetanczyl przez atak!</color>");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"<color=orange>Gracz oberwal! Pozostale HP: {currentHealth}</color>");

        // ---- POPRAWKA: Resetuj rany tylko jesli wrog NIE jest w stanie Broken ----
        // Używamy nowoczesnego FindObjectsByType (zamiast przestarzałego OfType)
        EnemyHealth[] allEnemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (!enemy.IsBroken) 
            {
                enemy.ResetWounds();
            }
            else
            {
                Debug.Log("<color=cyan>[WOUNDS] Wróg jest w stanie Broken - Rany NIE zostają zresetowane!</color>");
            }
        }

        if (hud != null) hud.UpdateHP(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log($"<color=red>[HEALTH] KRYTYCZNE: Gracz otrzymuje śmiertelne obrażenia! Aktualne HP: {currentHealth}</color>");
            Die();
        }
        else
        {
            // Zamykamy miecz, jeśli oberwaliśmy w trakcie wymachu!
            SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
            
            if (animator != null) 
            {
                if (isKnockback) 
                {
                    animator.SetTrigger("Knockback");
                    Debug.Log("<color=red>[HEALTH] Otrzymano cios z odrzutem!</color>");
                }
                else 
                {
                    animator.SetTrigger("HitReaction");
                }
            }
        }
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        Debug.Log($"<color=red>[HEALTH] ZGON! Wywołano Die(). Scena: {SceneManager.GetActiveScene().name}</color>");
        
        // Zamykamy miecz przy zgonie
        SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
        
        if (animator != null) animator.SetTrigger("Die");
        Invoke("RestartScene", 3f);
    }
    
    private void RestartScene() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    // === ANIMATION EVENTS: Dodaj na animacji dashu! ===
    public void EnableIFrames() { _isInvincible = true; }
    public void DisableIFrames() { _isInvincible = false; }

    /// <summary>
    /// Ożywia gracza i odnawia mu zdrowie (Używane przy teleportacji)
    /// </summary>
    public void Revive(int healthPercent = 100)
    {
        _isDead = false;
        currentHealth = Mathf.CeilToInt(maxHealth * (healthPercent / 100f));
        
        if (animator != null) 
        {
            animator.SetTrigger("Recover"); 
            animator.ResetTrigger("Die");
        }

        if (hud != null) hud.UpdateHP(currentHealth, maxHealth);
        Debug.Log("<color=green>[HEALTH] Gracz został ożywiony i zrefreshowany!</color>");
    }
}
