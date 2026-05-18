using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// PROFESJONALNY MENEDŻER ODRODZEŃ (Respawn Manager)
/// Odpowiada za: Zasady śmierci, Kary, Punkty Odrodzenia i koordynację z WarpManagerem.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    private static RespawnManager _instance;
    public static RespawnManager Instance => _instance;

    [Header("Ustawienia Śmierci")]
    [SerializeField] private float deathContemplationTime = 3.0f;
    [SerializeField] private float deathScreenFadeTime = 2.0f;

    [Header("Ostatni Checkpoint")]
    private string _checkpointScene;
    private string _checkpointID;
    private Vector3 _lastDeathPosition;

    [Header("Death Drop (Bloodstain)")]
    [SerializeField] private GameObject deathDropPrefab;
    private int _currentDropAmount = 0;
    private Vector3 _currentDropPos;
    private string _currentDropScene;
    private GameObject _activeDropInstance; 

    public Vector3 LastDeathPosition => _lastDeathPosition;
    public string CheckpointScene => _checkpointScene;
    public string CheckpointID => _checkpointID;
    public int CurrentDropAmount => _currentDropAmount;
    public Vector3 CurrentDropPos => _currentDropPos;
    public string CurrentDropScene => _currentDropScene;

    /// <summary>
    /// Wczytuje dane o dropie z zapisu.
    /// </summary>
    public void LoadDeathDropData(int amount, Vector3 pos, string sceneName)
    {
        _currentDropAmount = amount;
        _currentDropPos = pos;
        _currentDropScene = sceneName;
        
        if (_currentDropAmount > 0 && SceneManager.GetActiveScene().name == _currentDropScene)
        {
            SpawnDeathDropInScene();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

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
        // Kiedy wchodzimy do sceny, sprawdzamy czy czeka tam na nas nasz "drop"
        if (_currentDropAmount > 0 && scene.name == _currentDropScene)
        {
            SpawnDeathDropInScene();
        }
    }

    /// <summary>
    /// Zapisuje punkt odrodzenia (np. przy dotknięciu ogniska).
    /// </summary>
    public void SetCheckpoint(string sceneName, string spawnPointID)
    {
        _checkpointScene = sceneName;
        _checkpointID = spawnPointID;
        Debug.Log($"<color=lime>[RESPAWN] Punkt odrodzenia zapisany: {sceneName} / {spawnPointID}</color>");
    }

    /// <summary>
    /// Główna sekwencja śmierci wywoływana z PlayerHealth.
    /// </summary>
    public void StartDeathSequence()
    {
        // Zapamiętujemy gdzie gracz padł, zanim go przeniesiemy
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _lastDeathPosition = player.transform.position;

        StartCoroutine(DeathRoutine());
    }
    
    private IEnumerator DeathRoutine()
    {
        // 1. Dajemy czas na animację upadku
        yield return new WaitForSeconds(1.0f);

        // 2. Pokazujemy napis "YOU DIED"
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.FadeDeathScreen(true, deathScreenFadeTime);
        }

        // 3. Czas na smutek i kontemplację
        yield return new WaitForSeconds(deathContemplationTime);

        // 4. KARA: Zabieramy monety
        ApplyDeathPenalty();

        // 5. TRANSPORT: Prosimy WarpManager o przewiezienie nas do checkpointu
        ExecuteRespawnWarp();
    }

    private void ApplyDeathPenalty()
    {
        if (CurrencyManager.Instance != null && InventoryController.Instance != null)
        {
            string id = CurrencyManager.Instance.CurrencyItemID;
            int total = CurrencyManager.Instance.CurrentCurrency;
            int toLose = total / 2; 

            // KLASYKA: Jeśli już był jakiś drop, to on przepada!
            if (_currentDropAmount > 0)
            {
                Debug.Log("<color=orange>[RESPAWN] Stary drop przepadł bezpowrotnie...</color>");
                if (_activeDropInstance != null) Destroy(_activeDropInstance);
            }

            if (toLose > 0)
            {
                InventoryController.Instance.RemoveItem(id, toLose);
                
                // Zapisujemy dane o nowym dropie
                _currentDropAmount = toLose;
                _currentDropPos = _lastDeathPosition;
                _currentDropScene = SceneManager.GetActiveScene().name;

                Debug.Log($"<color=red>[RESPAWN] Kara: -{toLose} monet. Drop czeka w {_currentDropScene}.</color>");
            }
            else
            {
                _currentDropAmount = 0;
                Debug.Log($"<color=yellow>[RESPAWN] Brak monet do zabrania.</color>");
            }
        }
    }

    private void SpawnDeathDropInScene()
    {
        // Jeśli już jest na scenie, to nie duplikujemy
        if (_activeDropInstance != null) return;
        if (deathDropPrefab == null)
        {
            Debug.LogWarning("[RESPAWN] Brak prefabu DeathDrop w RespawnManager!");
            return;
        }

        _activeDropInstance = Instantiate(deathDropPrefab, _currentDropPos, Quaternion.identity);
        
        DeathDrop dd = _activeDropInstance.GetComponent<DeathDrop>();
        if (dd != null) dd.Setup(_currentDropAmount);
    }

    public void ClearActiveDrop()
    {
        _currentDropAmount = 0;
        _currentDropScene = "";
        _activeDropInstance = null;
        Debug.Log("<color=lime>[RESPAWN] Drop podniesiony - czyszczę pamięć.</color>");
    }

    private void ExecuteRespawnWarp()
    {
        if (WarpManager.Instance != null)
        {
            string targetScene = string.IsNullOrEmpty(_checkpointScene) ? "Pojezierza" : _checkpointScene;
            string targetID = string.IsNullOrEmpty(_checkpointID) ? "DEFAULT" : _checkpointID;

            Debug.Log($"<color=orange>[RESPAWN] Wołam WarpManager do: {targetScene}</color>");
            WarpManager.Instance.WarpToCheckpoint(targetScene, targetID);
        }
    }
}
