using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WarpManager : MonoBehaviour
{
    private static WarpManager _instance;
    public static WarpManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<WarpManager>();
            }
            return _instance;
        }
    }

    [Header("Ustawienia")]
    public float fadeDuration = 1.0f;
    public float waitTimeInDark = 0.5f;

    private bool _isWarping = false;
    private string _targetSpawnPointID;

    // --- SOS: Powrót do Poprzedniej Lokalizacji ---
    private string _lastSceneName;
    private string _lastSpawnPointID;

    /// <summary>
    /// Ustawia punkt odrodzenia (np. przy ognisku).
    /// </summary>
    public void SetLastCheckpoint(string sceneName, string spawnPointID)
    {
        _lastSceneName = sceneName;
        _lastSpawnPointID = spawnPointID;
        Debug.Log($"<color=lime>[WARP] Nowy punkt odrodzenia: {sceneName} / {spawnPointID}</color>");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Teleportacja do konkretnej sceny i punktu.
    /// </summary>
    public void Warp(string sceneName, string spawnPointID, string returnID = "")
    {
        if (_isWarping) return;
        
        // Zapamiętujemy "skąd przyszliśmy" tylko jeśli to normalny Warp (nie powrotny)
        _lastSceneName = SceneManager.GetActiveScene().name;
        _lastSpawnPointID = returnID;

        StartCoroutine(WarpRoutine(sceneName, spawnPointID));
    }

    /// <summary>
    /// Powrót do ostatnio zapamiętanej lokacji (np. Wyjście z Karczmy).
    /// </summary>
    public void WarpBack()
    {
        if (_isWarping) return;

        if (string.IsNullOrEmpty(_lastSceneName))
        {
            Debug.LogWarning("[WARP] Nie mam zapisanej pamięci powrotu! Domyślny Pojezierza.");
            Warp("Pojezierza", "DEFAULT");
            return;
        }

        Debug.Log($"<color=cyan>[WARP] Wracam do: {_lastSceneName} (Punkt: {_lastSpawnPointID})</color>");
        StartCoroutine(WarpRoutine(_lastSceneName, _lastSpawnPointID));
    }

    private IEnumerator WarpRoutine(string sceneName, string spawnPointID)
    {
        _isWarping = true;
        _targetSpawnPointID = spawnPointID;

        // 1. Ekran ciemnieje
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOut(fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }

        // 2. Ładowanie sceny
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Po załadowaniu sceny szukamy punktu
        yield return StartCoroutine(CompleteWarp());
    }

    private IEnumerator CompleteWarp()
    {
        // Dajemy scenie jedną klatkę na oddech
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(waitTimeInDark);

        SpawnPoint[] points = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint targetPoint = null;

        foreach (var p in points)
        {
            if (p.identifier == _targetSpawnPointID)
            {
                targetPoint = p;
                break;
            }
        }

        // Jeśli nie znaleźliśmy punktu po ID, bierzemy pierwszy lepszy (zabezpieczenie)
        if (targetPoint == null && points.Length > 0)
        {
            targetPoint = points[0];
            Debug.LogWarning($"[WARP] Nie znalazłem '{_targetSpawnPointID}', używam domyślnego {targetPoint.identifier}");
        }

        if (targetPoint != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                PlayerHealth ph = player.GetComponent<PlayerHealth>();

                // --- FIX: Nowa Kolejność ---
                // 1. Ożywiamy gracza ZANIM go przeniesiemy, żeby był świeży
                if (ph != null) ph.Revive(); 

                // 2. Wyłączamy CC, żeby grawitacja nie porwała go w trakcie ustawiania Transform
                if (cc != null) cc.enabled = false;

                // 3. Teleportacja Transform
                player.transform.position = targetPoint.transform.position;
                player.transform.rotation = targetPoint.transform.rotation;

                Debug.Log($"<color=green>[WARP] Bingo! Gracz dowieziony do: {targetPoint.identifier}.</color>");
                
                // 4. Ruszamy fizykę z powrotem
                if (cc != null) cc.enabled = true;
            }
            else
            {
                Debug.LogError("<color=red>[WARP] Zgubiłem gracza! Ma tag 'Player'?</color>");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[WARP] Nie znalazłem żadnego SpawnPoint!</color>");
        }

        // 4. Ekran jaśnieje
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeIn(fadeDuration);
        }

        _isWarping = false;
    }
}
