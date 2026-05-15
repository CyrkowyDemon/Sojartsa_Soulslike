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

    // --- SOS: Powrót do Poprzedniej Lokalizacji (Drzwi/Warp) ---
    private string _lastSceneName;
    private string _lastSpawnPointID;

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
        
        // Zapamiętujemy "skąd przyszliśmy" tylko dla celów powrotu przez WarpBack
        _lastSceneName = SceneManager.GetActiveScene().name;
        _lastSpawnPointID = returnID;

        StartCoroutine(WarpRoutine(sceneName, spawnPointID));
    }

    /// <summary>
    /// Specjalny warp dla odrodzenia (nie nadpisuje pamięci powrotu).
    /// </summary>
    public void WarpToCheckpoint(string sceneName, string spawnPointID)
    {
        if (_isWarping) return;
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

    private Vector3 _warpOldPos;

    private IEnumerator WarpRoutine(string sceneName, string spawnPointID)
    {
        _isWarping = true;
        _targetSpawnPointID = spawnPointID;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _warpOldPos = player.transform.position;

        // 1. Ekran ciemnieje
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeOut(fadeDuration);
            // Czekamy aż wyciemni się do końca
            yield return new WaitForSeconds(fadeDuration);
            // DODATKOWY FAIL-SAFE: Upewniamy się, że jest absolutnie czarno
            FadeManager.Instance.SetAlpha(1f);
        }

        // 2. Ładowanie sceny (Prawdziwy RELOAD)
        Debug.Log($"<color=white>[WARP] Ładuję scenę: {sceneName}...</color>");
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

        if (targetPoint == null && points.Length > 0) targetPoint = points[0];

        if (targetPoint != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                PlayerHealth ph = player.GetComponent<PlayerHealth>();

                // --- KLUCZOWY FIX: Najpierw ożywiamy i CHOWAMY, potem przesuwamy ---
                if (ph != null) 
                {
                    ph.Revive();
                }

                if (cc != null) cc.enabled = false;

                // CHOWAMY MODEL (na wszelki wypadek, żeby nie było widać trupa ani przeskoku)
                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers) r.enabled = false;

                player.transform.position = targetPoint.transform.position;
                player.transform.rotation = targetPoint.transform.rotation;

                Physics.SyncTransforms(); 

                var brain = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineBrain>();
                if (brain != null)
                {
                    Unity.Cinemachine.CinemachineCore.OnTargetObjectWarped(player.transform, player.transform.position - _warpOldPos);
                }

                if (cc != null) cc.enabled = true;

                // Wymuszamy na animatorze natychmiastowe przeliczenie stanów
                // ROBIMY TO TERAZ, gdy CC jest już aktywny!
                Animator anim = player.GetComponentInChildren<Animator>();
                if (anim != null) anim.Update(0);

                // Przywracamy model DOPIERO po wszystkim
                foreach (var r in renderers) r.enabled = true;
                
                Debug.Log($"<color=green>[WARP] Gracz odrodzony w punkcie: {targetPoint.identifier}</color>");
            }
        }

        // 4. Ekran jaśnieje
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeIn(fadeDuration);
        }

        _isWarping = false;
    }
}
