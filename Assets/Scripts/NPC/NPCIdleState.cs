using UnityEngine;

public class NPCIdleState : INPCState
{
    public void EnterState(PeacefulNPC npc)
    {
        // Tutaj w przyszłości można odpalić animację stania lub palenia fajki itp.
        Debug.Log($"[NPC] {npc.name} wchodzi w stan Idle. Czeka na zaczepkę.");
    }

    public void UpdateState(PeacefulNPC npc)
    {
        // Kiedy gracz jest obok, NPC może delikatnie kręcić głową w jego stronę (opcjonalne)
    }

    public void ExitState(PeacefulNPC npc)
    {
        // Wychodzimy z Idle, żeby zająć się czymś innym (np. zacząć gadać)
    }
}
