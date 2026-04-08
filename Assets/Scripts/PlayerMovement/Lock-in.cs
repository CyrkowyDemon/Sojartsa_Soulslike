using UnityEngine;
using Unity.Cinemachine;

public class TargetHandler : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [Header("Ustawienia Lock-on")]
    [SerializeField] private float lockOnRange = 15f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer; // NOWE: Żeby nie lockować przez ściany!

    [Header("Kamera Lock-on")]
    [SerializeField] private CinemachineCamera lockOnCamera; 
    [SerializeField] private CinemachineTargetGroup targetGroup; 

    private Transform _currentTarget;
    private Transform _lockOnPoint; 
    private bool _isLockedOnInternal = false;

    // Optymalizacja: Pamięć zarezerwowana z góry, brak alokacji śmieci (GC) przy skanowaniu
    private readonly Collider[] _colliders = new Collider[32]; 
    private Camera _mainCamera;

    public Transform CurrentTarget => _currentTarget;
    public bool IsLockedOn => _isLockedOnInternal;

    private void Awake()
    {
        _mainCamera = Camera.main;

        // --- POPRAWKA: Kamera LockOn musi zawsze kogoś "pilnować", żeby nie było przeskoku ---
        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = transform; // Na starcie patrz na gracza
        }
    }

private void OnEnable()
    {
        if (inputReader == null) Debug.LogError("TargetHandler: Brak InputReadera!");
        inputReader.TargetEvent += ToggleLockOn;
        inputReader.SwitchTargetEvent += SwitchTarget; // <--- ODKOMENTUJ TO
    }

    private void OnDisable()
    {
        inputReader.TargetEvent -= ToggleLockOn;
        inputReader.SwitchTargetEvent -= SwitchTarget; // <--- I TO
    }
    private void ToggleLockOn()
    {
        if (IsLockedOn)
        {
            ClearTarget(); 
            return;
        }

        FindAndSetBestTarget();
    }

    private void FindAndSetBestTarget()
    {
        if (_mainCamera == null) _mainCamera = Camera.main; // AUTO-REFRESH KAMERY PO WARPIE

        // Używamy NonAlloc dla maksymalnej wydajności
        int count = Physics.OverlapSphereNonAlloc(transform.position, lockOnRange, _colliders, enemyLayer);
        
        Transform bestTarget = null;
        float closestToCenter = Mathf.Infinity;

        for (int i = 0; i < count; i++)
        {
            Transform enemy = _colliders[i].transform;
            if (enemy == transform) continue;

            // 1. Sprawdzamy czy nie ma ściany między nami a wrogiem
            Vector3 directionToEnemy = enemy.position - _mainCamera.transform.position;
            if (Physics.Raycast(_mainCamera.transform.position, directionToEnemy.normalized, out RaycastHit hit, lockOnRange * 1.5f, obstacleLayer))
            {
                // Jeśli trafiliśmy w coś, co nie jest tym wrogiem (np. ścianę), ignorujemy go
                if (hit.transform != enemy && !hit.transform.IsChildOf(enemy)) continue;
            }

            // 2. Przeliczamy pozycję wroga na ekran gracza (Viewport)
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(enemy.position);

            // Sprawdzamy, czy wróg jest w ogóle przed kamerą (z > 0) i czy jest na ekranie (x i y między 0 a 1)
            if (viewportPos.z > 0 && viewportPos.x is > 0 and < 1 && viewportPos.y is > 0 and < 1)
            {
                // Liczymy odległość wroga od idealnego ŚRODKA EKRANU (0.5, 0.5)
                Vector2 screenCenter = new Vector2(0.5f, 0.5f);
                float distanceFromCenter = Vector2.Distance(screenCenter, new Vector2(viewportPos.x, viewportPos.y));

                if (distanceFromCenter < closestToCenter)
                {
                    closestToCenter = distanceFromCenter;
                    bestTarget = enemy;
                }
            }
        }

        if (bestTarget != null)
        {
            SetTarget(bestTarget);
        }
    }

    // NOWA FUNKCJA DO ZMIANY TARGETU (Mysz/Gałka)
    public void SwitchTarget(Vector2 inputDirection)
    {
        if (!IsLockedOn || inputDirection.sqrMagnitude < 0.1f) return;
        if (_mainCamera == null) _mainCamera = Camera.main; // AUTO-REFRESH KAMERY PO WARPIE

        int count = Physics.OverlapSphereNonAlloc(transform.position, lockOnRange, _colliders, enemyLayer);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        Vector3 currentViewportPos = _mainCamera.WorldToViewportPoint(_currentTarget.position);
        bool lookingRight = inputDirection.x > 0; // Czy gracz wychylił gałkę w prawo?

        for (int i = 0; i < count; i++)
        {
            Transform enemy = _colliders[i].transform;
            if (enemy == _currentTarget || enemy == transform) continue;

            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(enemy.position);
            
            // 1. Odrzucamy wrogów poza ekranem lub za plecami
            if (viewportPos.z < 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1) continue; 

            // 2. Szukamy wrogów po właściwej stronie (prawo lub lewo od obecnego celu na ekranie)
            bool isToTheRight = viewportPos.x > currentViewportPos.x;
            if (lookingRight == isToTheRight) 
            {
                // Obliczamy odległość NA EKRANIE od obecnego celu
                float distance = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(currentViewportPos.x, currentViewportPos.y));
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = enemy;
                }
            }
        }

        if (bestTarget != null)
        {
            Debug.Log($"[TARGET] Przełączam na: {bestTarget.name}");
            ClearTarget(); // Zdejmujemy stary cel
            SetTarget(bestTarget); // Nakładamy nowy
        }
        else
        {
            Debug.Log("[TARGET] Nie znaleziono lepszego celu w tym kierunku.");
        }
    }

    private void SetTarget(Transform target)
    {
        _currentTarget = target;
        _isLockedOnInternal = true;
        
        Transform focalPoint = _currentTarget.Find("LockOnPoint");
        _lockOnPoint = focalPoint != null ? focalPoint : _currentTarget;

        if (lockOnCamera != null && targetGroup != null)
        {
            targetGroup.AddMember(_lockOnPoint, 1f, 1.5f); // Ustaw promień i wagę pod swoje potrzeby
            lockOnCamera.LookAt = targetGroup.transform; 
            lockOnCamera.Priority = 20; 
        }
    }

    private void Update()
    {
        if (!_isLockedOnInternal) return;

        if (_currentTarget == null || !IsTargetAliveAndValid())
        {
            ClearTarget();

            // --- AUTO-LOCK: Jeśli opcja włączona, szukamy kolejnego wroga ---
            bool autoLock = SettingsManager.Instance != null && SettingsManager.Instance.autoLock;
            if (autoLock)
            {
                FindAndSetBestTarget();
            }
            return; 
        }

        // Zrywamy lock-on, jeśli cel ucieknie za daleko
        if (Vector3.Distance(transform.position, _currentTarget.position) > lockOnRange)
        {
            ClearTarget();
        }
    }

    // Pomocnicza metoda sprawdzająca, czy cel nie schował się na stałe za ścianą
    private bool IsTargetAliveAndValid()
    {
        if (!_currentTarget.gameObject.activeInHierarchy) return false;

        // --- NOWE: Sprawdzamy czy wróg nie jest martwy (np. leży jako zwłoki przed Destroy) ---
        EnemyHealth hp = _currentTarget.GetComponentInParent<EnemyHealth>();
        if (hp != null && hp.IsDead) return false;

        return true;
    }

    public void ClearTarget()
    {
        if (_lockOnPoint != null && targetGroup != null)
        {
            targetGroup.RemoveMember(_lockOnPoint);
        }

        if (targetGroup != null)
        {
            for (int i = targetGroup.Targets.Count - 1; i >= 0; i--)
            {
                if (targetGroup.Targets[i].Object == null)
                {
                    targetGroup.Targets.RemoveAt(i);
                }
            }
        }

        _currentTarget = null;
        _lockOnPoint = null;
        _isLockedOnInternal = false;

        if (lockOnCamera != null)
        {
            lockOnCamera.Priority = 0;
            // --- POPRAWKA: Nie ustawiamy null! Kamera wraca do patrzenia na gracza ---
            lockOnCamera.LookAt = transform; 
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
    }
}