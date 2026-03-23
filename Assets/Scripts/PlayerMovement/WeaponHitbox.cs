using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; // W nowych wersjach (3.0+) dodali "Unity." na początku


public class WeaponHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public int hitboxID = 0; // NOWE: ID hitboxu (np. 1 = Miecz, 2 = Stopa)
    public bool isKnockbackAttack = false; // NOWE: Czy ten atak odpycha (Root Motion)?
    [SerializeField] private Transform basePoint; // Dol ostrza
    [SerializeField] private Transform tipPoint;  // Gora ostrza
    [SerializeField] private float weaponThickness = 0.2f; // Grubosc "kielbaski"
    [SerializeField] private LayerMask enemyLayer; // Kogo bijemy
    [SerializeField] private int damageAmount = 30;

    [Header("VFX")]
    [SerializeField] private TrailRenderer swordTrail;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.04f; // Szybsze, bardziej "ostre"
    [SerializeField] private float hitStopOnPlayerHit = 0.04f;

    private bool _isAttacking = false;

    // Zeby pamietac punkty z poprzedniej klatki
    private Vector3 _previousBasePosition;
    private Vector3 _previousTipPosition;

    // Lista pamietajaca obiekty juz trafione W TYM JEDNYM uderzeniu
    private HashSet<GameObject> _hitObjectsThisSwing = new HashSet<GameObject>();

    private void Awake()
    {
        if (swordTrail != null) swordTrail.emitting = false; // Na starcie wylaczamy slad!
    }

    public void OpenDamageWindow()
    {
        _isAttacking = true;
        _hitObjectsThisSwing.Clear(); // Czyscimy pamiec trafionych obiektow

        // Zapisujemy pozycje startowe wejscia w cios
        _previousBasePosition = basePoint.position;
        _previousTipPosition = tipPoint.position;
        
        if (swordTrail != null) 
        {
            swordTrail.Clear(); // Czyscimy stare smieci, zeby slad nie "ciagnal sie" z poprzedniego miejsca
            swordTrail.emitting = true;
        }
        Debug.Log("<color=green>[MIEKZ] Otwieram okno ataku! Uwazaj!</color>");
    }

    public void CloseDamageWindow()
    {
        _isAttacking = false;
        if (swordTrail != null) swordTrail.emitting = false;
        Debug.Log("<color=red>[MIECZ] Koniec uderzenia. Miecz jest tepy.</color>");
    }

    private void OnDisable()
    {
        // FAIL-SAFE: Jeśli skrypt zostanie wyłączony w trakcie ataku (np. postać zginie),
        // musimy wymusić zamknięcie okna obrażeń, żeby nie zostało "na zawsze".
        if (_isAttacking)
        {
            CloseDamageWindow();
        }
    }

    private void Update()
    {
        if (!_isAttacking) return;

        Vector3 currentBase = basePoint.position;
        Vector3 currentTip = tipPoint.position;
        
        Vector3 previousMid = Vector3.Lerp(_previousBasePosition, _previousTipPosition, 0.5f);
        Vector3 currentMid = Vector3.Lerp(currentBase, currentTip, 0.5f);
        
        Vector3 movementDirection = (currentMid - previousMid);
        float movementDistance = movementDirection.magnitude;
        
        // --- ZABEZPIECZENIE PRZED WIDMOWYMI HITAMI ---
        // Jeśli miecz prawie się nie ruszył (np. stoimy w miejscu), nie robimy casta.
        // To zapobiega "przypadkowym" trafieniom, gdy hitbox jest włączony, ale nie ma ruchu.
        if (movementDistance < 0.001f) 
        {
            _previousBasePosition = currentBase;
            _previousTipPosition = currentTip;
            return;
        }

        RaycastHit[] hits = Physics.CapsuleCastAll(
            _previousBasePosition, 
            _previousTipPosition, 
            weaponThickness, 
            movementDirection.normalized, 
            movementDistance, 
            enemyLayer);

        // --- NOWA LOGIKA FLAG ---
        bool hitEnemy = false;
        bool hitPlayer = false;

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            
            EnemyHealth enemyHP = hitCollider.GetComponentInParent<EnemyHealth>();
            PlayerHealth playerHP = hitCollider.GetComponentInParent<PlayerHealth>();

            GameObject targetObj = null;
            if (enemyHP != null) targetObj = enemyHP.gameObject;
            else if (playerHP != null) targetObj = playerHP.gameObject;

            if (targetObj != null)
            {
                if (!_hitObjectsThisSwing.Contains(targetObj))
                {
                    _hitObjectsThisSwing.Add(targetObj);
                    Debug.Log($"<color=orange>[HIT] Trafiono: {targetObj.name} (Obrażenia zadane!)</color>");

                    if (enemyHP != null)
                    {
                        enemyHP.TakeDamage(damageAmount, isKnockbackAttack);
                        hitEnemy = true;
                    }
                    else if (playerHP != null)
                    {
                        playerHP.TakeDamage(damageAmount, isKnockbackAttack);
                        hitPlayer = true;
                    }
                }
                else
                {
                    // Ten log pokaże nam, czy raycast wykrywa kogoś drugi raz w tym samym zamachu
                    Debug.Log($"<color=white>[HIT-SKIP] Ponowne wykrycie: {targetObj.name} (Zignorowano - już trafiony w tym zamachu)</color>");
                }
            }
        }

        // --- WYWOŁANIE EFEKTÓW RAZ NA KLATKĘ ---
       // --- WYWOŁANIE EFEKTÓW (Zoptymalizowane) ---
        // --- WYWOŁANIE EFEKTÓW (Zoptymalizowane pod Cinemachine 3.0) ---
        // --- WYWOŁANIE EFEKTÓW (Finalna poprawka pod CM 3.0) ---
        if (hitPlayer || hitEnemy)
        {
            float duration = hitPlayer ? hitStopOnPlayerHit : hitStopDuration;

            // 1. HitStop
            if (HitStop.Instance != null) 
            {
                HitStop.Instance.Freeze(duration);
            }

            // 2. Camera Shake (Impulse) 
            // W CM 3.0 klasa to nadal CinemachineImpulseSource
            var source = GetComponent<CinemachineImpulseSource>();
            if (source != null)
            {
                source.GenerateImpulse(); 
            }
            else 
            {
                // Szukamy u rodzica, jeśli skrypt jest na mieczu, a źródło na graczu
                GetComponentInParent<CinemachineImpulseSource>()?.GenerateImpulse();
            }
        }



        _previousBasePosition = currentBase;
        _previousTipPosition = currentTip;
    }

    // Dodatek, by widziec "kielbaske" w Unity Editor
    private void OnDrawGizmos()
    {
        if (basePoint != null && tipPoint != null)
        {
            Gizmos.color = _isAttacking ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(basePoint.position, weaponThickness);
            Gizmos.DrawWireSphere(tipPoint.position, weaponThickness);
            Gizmos.DrawLine(basePoint.position, tipPoint.position);
        }
    }
}
