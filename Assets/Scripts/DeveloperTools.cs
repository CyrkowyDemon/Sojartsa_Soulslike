using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class DeveloperTools : MonoBehaviour
{
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // R - Reset gry
        if (keyboard.rKey.wasPressedThisFrame)
        {
            Debug.Log("<color=yellow>[DEV] Resetowanie sceny...</color>");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // K - Kill all enemies
        if (keyboard.kKey.wasPressedThisFrame)
        {
            Debug.Log("<color=red>[DEV] KILL ALL ENEMIES!</color>");
            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.TakeDamage(9999);
            }
        }
    }
}
