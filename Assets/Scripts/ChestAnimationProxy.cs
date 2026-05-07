using UnityEngine;

/// <summary>
/// MOSTEK (PROXY) dla skrzyni.
/// Wrzuć ten skrypt na obiekt, który ma komponent ANIMATOR.
/// Przekazuje sygnały z Animation Events do głównego skryptu ChestController.
/// </summary>
public class ChestAnimationProxy : MonoBehaviour
{
    private ChestController controller;

    private void Awake()
    {
        // Szukamy kontrolera na tym samym obiekcie lub u rodziców
        controller = GetComponentInParent<ChestController>();
        
        if (controller == null)
        {
            Debug.LogError($"[ChestAnimationProxy] Nie znaleziono ChestController na {gameObject.name} ani u rodziców!");
        }
    }

    // Tę funkcję wywołaj w Animation Event na końcu animacji 'isopening' i 'isclosing'
    public void UnlockInteraction()
    {
        if (controller != null)
        {
            controller.UnlockInteraction();
        }
    }
}
