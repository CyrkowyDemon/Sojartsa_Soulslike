using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private Animator _animator;
    private bool _isDead = false;
    public bool IsDead => _isDead;

    private bool _isInvincible = false; // I-Frames

    [Header("UI")]
    [SerializeField] private PlayerHUD hud;

    void Start()
    {
        currentHealth = maxHealth;
        _animator = GetComponent<Animator>();
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
            Die();
        }
        else
        {
            // Zamykamy miecz, jeśli oberwaliśmy w trakcie wymachu!
            SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
            
            if (_animator != null) 
            {
                if (isKnockback) 
                {
                    _animator.SetTrigger("Knockback");
                    Debug.Log("<color=red>[HEALTH] Otrzymano cios z odrzutem!</color>");
                }
                else 
                {
                    _animator.SetTrigger("HitReaction");
                }
            }
        }
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        // Zamykamy miecz przy zgonie
        SendMessage("CloseDamage", SendMessageOptions.DontRequireReceiver);
        
        if (_animator != null) _animator.SetTrigger("Die");
        Invoke("RestartScene", 3f);
    }
    
    private void RestartScene() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    // === ANIMATION EVENTS: Dodaj na animacji dashu! ===
    public void EnableIFrames() { _isInvincible = true; }
    public void DisableIFrames() { _isInvincible = false; }
}
