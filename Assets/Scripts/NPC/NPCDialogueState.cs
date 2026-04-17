using UnityEngine;

public class NPCDialogueState : INPCState
{
    public void EnterState(PeacefulNPC npc)
    {
        DialogueConversation conv = npc.GetCurrentConversation();
        if (conv == null) return;

        Debug.Log($"[NPC] {npc.name} wchodzi w stan Rozmowy. LocksPlayer: {conv.locksPlayer}");
        
        // Zastosuj blokady TYLKO jeśli rozmowa tego wymaga (locksPlayer = true)
        if (conv.locksPlayer)
        {
            // Zmuszamy TargetHandler na graczu, by skupił się na NPC
            TargetHandler th = GameObject.FindAnyObjectByType<TargetHandler>(); 
            if(th != null) th.ForceLockOn(npc.lockOnTransform);

            // Zablokuj sterowanie centralnie (Styl Settings/Pause)
            PlayerMovement pm = GameObject.FindAnyObjectByType<PlayerMovement>();
            if(pm != null)
            {
                if (pm.InputReader != null) pm.InputReader.SetDialogueState();
                pm.isMovementLocked = true; // Zostawiamy dla animacji
            }

            // Uwolnij kursor myszy!!
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Zamroź kamerę (wyłącz sterowanie osiami)
            Unity.Cinemachine.CinemachineInputAxisController ciac = GameObject.FindAnyObjectByType<Unity.Cinemachine.CinemachineInputAxisController>();
            if (ciac != null) ciac.enabled = false;
        }

        // Startujemy UI Dialogów
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation(conv, npc);
        }
    }

    public void UpdateState(PeacefulNPC npc)
    {
        // Jeśli chcemy, NPC może powoli odwracać się w stronę gracza podczas rozmowy
    }

    public void ExitState(PeacefulNPC npc)
    {
        Debug.Log($"[NPC] {npc.name} kończy rozmowę.");
        TargetHandler th = GameObject.FindAnyObjectByType<TargetHandler>();
        if(th != null) th.ClearForcedLockOn();
        
        // Odblokuj sterowanie centralnie
        PlayerMovement pm = GameObject.FindAnyObjectByType<PlayerMovement>();
        if(pm != null)
        {
            if (pm.InputReader != null) pm.InputReader.UnlockAllInput();
            pm.isMovementLocked = false;
        }

        // Przywróć kamerę i kursor
        Unity.Cinemachine.CinemachineInputAxisController ciac = GameObject.FindAnyObjectByType<Unity.Cinemachine.CinemachineInputAxisController>();
        if (ciac != null) ciac.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndConversation();
        }
    }
}
