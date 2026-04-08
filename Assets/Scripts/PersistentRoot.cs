using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Mordo, to jest nasz "Bramkarz". Jak przechodzisz między scenami, ten skrypt pilnuje,
/// żeby Twoja "Walizka" (Gracz, Kamera, HUD) przetrwała podróż, 
/// ale też żeby nie narobiło się klonów, jeśli w Hubie już ktoś taki stał.
/// </summary>
public class PersistentRoot : MonoBehaviour
{
    public static PersistentRoot Instance { get; private set; }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jeśli trafiliśmy do menu (Build Index 0), niszczymy cały majdan, 
        // żeby nie śmiecił w menu i żeby nowa gra zaczęła się od czystej karty.
        if (scene.buildIndex == 0)
        {
            Debug.Log("<color=yellow>[PERSISTENCE] Powrót do menu. Czyścimy świat.</color>");
            Destroy(gameObject);
            return;
        }

        // --- FIX: Czyszczenie obcych EventSystemów, Kamer i Listenerów ---
        // Skoro mamy własne w walizce, usuwamy nadmiarowe ze sceny.
        EventSystem[] esScripts = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (var es in esScripts)
        {
            if (!es.transform.IsChildOf(this.transform)) Destroy(es.gameObject);
        }

        AudioListener[] alScripts = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var al in alScripts)
        {
            if (!al.transform.IsChildOf(this.transform)) Destroy(al.gameObject);
        }

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (!cam.transform.IsChildOf(this.transform)) Destroy(cam.gameObject);
        }
    }

    private void Awake()
    {
        // 1. Sprawdzamy, czy "Stary" Bramkarz już tu jest
        if (Instance != null && Instance != this)
        {
            Debug.Log($"<color=yellow>[PERSISTENCE] Klon wykryty! Niszczę nadmiarowy {gameObject.name} w tej scenie.</color>");
            Destroy(gameObject); // Nowy (duplikat) popełnia samobójstwo
            return;
        }

        // 2. Jeśli jesteśmy pierwszy, to ogłaszamy się szefem
        Instance = this;
        
        // 3. Mówimy Unity: "Tego obiektu nie wyrzucaj do śmieci przy zmianie sceny!"
        DontDestroyOnLoad(gameObject);
        
        Debug.Log($"<color=green>[PERSISTENCE] Persistent_Root ubezpieczony. Podróżujemy!</color>");
    }
}
