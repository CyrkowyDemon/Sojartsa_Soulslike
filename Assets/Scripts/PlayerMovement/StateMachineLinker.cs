using UnityEngine;

public class StateMachineLinker : StateMachineBehaviour
{
    // Ten skrypt zostanie u¿yty przez animacje do odblokowania akcji
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Resetujemy flagi przy wejœciu w nowy stan (np. Idle)
        if (stateInfo.IsTag("Idle"))
        {
            animator.SetBool("CanCancel", true);
            animator.GetComponent<PlayerCombat>().ResetCombatFlags();
        }
    }
}
