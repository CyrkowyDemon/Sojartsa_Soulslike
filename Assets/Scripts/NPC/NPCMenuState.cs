using UnityEngine;

/// <summary>
/// Stan, w którym gracz otworzył "Tablicę Ogłoszeń" NPC (Porozmawiaj, Sklep, Bywaj).
/// Blokuje ruch gracza, odpala kursor i czeka na wybór z UI.
/// </summary>
public class NPCMenuState : INPCState
{
    public void EnterState(PeacefulNPC npc)
    {
        Debug.Log($"[NPC] {npc.name} otwiera Menu Główne (Hub Menu).");
        
        // Zmuszamy TargetHandler na graczu, by skupił się na NPC
        TargetHandler th = GameObject.FindAnyObjectByType<TargetHandler>(); 
        if(th != null) th.ForceLockOn(npc.lockOnTransform);

        // Zablokuj sterowanie graczem
        PlayerMovement pm = GameObject.FindAnyObjectByType<PlayerMovement>();
        if(pm != null)
        {
            if (pm.InputReader != null) pm.InputReader.SetDialogueState();
            pm.isMovementLocked = true;
        }

        // Uwolnij kursor myszy, żeby można było klikać w menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Zamroź kamerę
        Unity.Cinemachine.CinemachineInputAxisController ciac = GameObject.FindAnyObjectByType<Unity.Cinemachine.CinemachineInputAxisController>();
        if (ciac != null) ciac.enabled = false;

        // Odpalamy menedżer UI, który wyciągnie na ekran dynamiczne przyciski!
        if (NPCMenuUI.Instance != null)
        {
            NPCMenuUI.Instance.OpenMainMenu(npc);
        }
        else
        {
            Debug.LogError("[NPCMenuState] Brak NPCMenuUI na scenie!");
        }
    }

    public void UpdateState(PeacefulNPC npc)
    {
        // NPC może stać w miejscu i patrzeć na gracza
    }

    public void ExitState(PeacefulNPC npc)
    {
        Debug.Log($"[NPC] {npc.name} zamyka Menu Główne.");
        
        TargetHandler th = GameObject.FindAnyObjectByType<TargetHandler>();
        if(th != null) th.ClearForcedLockOn();
        
        // Odblokuj sterowanie graczem
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

        // Upewniamy się, że UI jest schowane
        if (NPCMenuUI.Instance != null)
        {
            // CloseMenu samo zmienia stan na Idle, ale tutaj jesteśmy w ExitState więc to już się dzieje
        }
    }
}
