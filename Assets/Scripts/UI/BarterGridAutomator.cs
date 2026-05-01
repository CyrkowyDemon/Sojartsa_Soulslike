using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mordo, to jest Twój "Automatyzator". 
/// Wrzuć to na obiekt, który jest rodzicem Twoich 9 slotów na stole.
/// On sam je znajdzie i ponumeruje. Koniec z wpisywaniem ID ręcznie!
/// </summary>
public class BarterGridAutomator : MonoBehaviour
{
    public void SetupSlots()
    {
        // Szukamy wszystkich dzieci, które mają skrypt BarterGridSlotUI
        var slots = GetComponentsInChildren<BarterGridSlotUI>(true);
        
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotIndex = i; // Automatyczne przypisanie 0, 1, 2...
            Debug.Log($"[BARTER] Automatycznie ustawiono Slot {i} dla {slots[i].name}");
        }
    }

    // Wywołaj to raz w Inspektorze (przycisk) lub w Awake
    private void Awake()
    {
        SetupSlots();
    }
}
