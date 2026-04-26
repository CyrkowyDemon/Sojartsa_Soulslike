using UnityEngine;

public class RootMotionProxy : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Szukamy komponentów na rodzicu (nasze logiczne "naczynie")
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerCombat = GetComponentInParent<PlayerCombat>();
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnAnimatorMove()
    {
        if (playerMovement != null && animator != null)
        {
            // Przekazujemy "paczkę" z ruchem do szefa
            // Korzystamy z deltaPosition i deltaRotation z Animatora
            playerMovement.ApplyBuiltInRootMotion(animator.deltaPosition, animator.deltaRotation);
        }
    }

    // --- PRZEKIEROWANIE ANIMATION EVENTS ---

    public void EnableIFrames()
    {
        if (playerHealth != null) playerHealth.EnableIFrames();
    }

    public void DisableIFrames()
    {
        if (playerHealth != null) playerHealth.DisableIFrames();
    }

    public void OpenDamage()
    {
        if (playerCombat != null) playerCombat.OpenDamage();
    }

    public void CloseDamage()
    {
        if (playerCombat != null) playerCombat.CloseDamage();
    }

    public void OpenTrail()
    {
        if (playerCombat != null) playerCombat.OpenTrail();
    }

    public void CloseTrail()
    {
        if (playerCombat != null) playerCombat.CloseTrail();
    }

    public void EnableCancel()
    {
        if (playerCombat != null) playerCombat.EnableCancel();
    }

    public void ResetCombatFlags()
    {
        if (playerCombat != null) playerCombat.ResetCombatFlags();
    }
}
