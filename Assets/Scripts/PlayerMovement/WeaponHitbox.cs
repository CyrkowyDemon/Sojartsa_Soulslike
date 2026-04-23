using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; // W nowych wersjach (3.0+) dodali "Unity." na początku
using FMODUnity;

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
    [SerializeField] private float poiseDamage = 35f;

    [Header("VFX")]
    [SerializeField] private TrailRenderer swordTrail;
    [SerializeField] private GameObject hitVFXPrefab; // NOWE: Prefab efektu trafienia (np. Impact Flash / Krew)
    [SerializeField] private GameObject hitLightPrefab; // NOWE: Prefab światła trafienia (Hit Light - FromSoftware style)

    [Header("Audio (FMOD)")]
    [SerializeField] private EventReference swingSound; // Dźwięk zamachu (windup/swing)
    [SerializeField] private EventReference hitSound;   // Dźwięk trafienia (impact)

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.04f; // Szybsze, bardziej "ostre"
    [SerializeField] private float hitStopOnPlayerHit = 0.04f;

    private bool _isAttacking = false;

    // Zeby pamietac punkty z poprzedniej klatki
    private Vector3 _previousBasePosition;
    private Vector3 _previousTipPosition;

    // Lista pamietajaca obiekty juz trafione W TYM JEDNYM uderzeniu
    private HashSet<GameObject> _hitObjectsThisSwing = new HashSet<GameObject>();

    // OPTYMALIZACJA: Przechowujemy hity w cache'u (0 GC w klatce)
    private RaycastHit[] _hitCache = new RaycastHit[20];

    private void Awake()
    {
        if (swordTrail != null) swordTrail.emitting = false; // Na starcie wylaczamy slad!
    }

    public void OpenDamageWindow()
    {
        // FAIL-SAFE: Jeśli bron (lub jej punkty) ulegla zniszczeniu, ignorujemy próbę ataku.
        // Zapobiega to błędom MissingReferenceException, gdy np. wróg umiera, ale stara animacja wysle jeszcze event.
        if (basePoint == null || tipPoint == null) return;

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

    /// <summary>
    /// Wywoływane przez Animation Event w dowolnym momencie wymachu.
    /// </summary>
    public void PlaySwingSound()
    {
        if (!swingSound.IsNull)
        {
            RuntimeManager.PlayOneShot(swingSound, transform.position);
        }
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

        // OPTYMALIZACJA: CapsuleCastNonAlloc
        int hitCount = Physics.CapsuleCastNonAlloc(
            _previousBasePosition, 
            _previousTipPosition, 
            weaponThickness, 
            movementDirection.normalized, 
            _hitCache,
            movementDistance, 
            enemyLayer);

        // --- NOWA LOGIKA FLAG ---
        bool hitEnemy = false;
        bool hitPlayer = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitCache[i];
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
                        enemyHP.TakeDamage((int)damageAmount, isKnockbackAttack, poiseDamage);
                        hitEnemy = true;
                    }
                    else if (playerHP != null)
                    {
                        playerHP.TakeDamage(damageAmount, isKnockbackAttack);
                        hitPlayer = true;
                    }

                    // --- NOWE: Spawnowanie VFX w punkcie trafienia ---
                    if (hitVFXPrefab != null)
                    {
                        // FIX: Jeśli Unity zwraca punkt (0,0,0) (częste przy CapsuleCast), używamy środka ostrza
                        Vector3 vfxPos = hit.point;
                        Vector3 vfxNormal = hit.normal;

                        if (vfxPos == Vector3.zero)
                        {
                            vfxPos = Vector3.Lerp(currentBase, currentTip, 0.5f);
                            vfxNormal = Vector3.up; // Fallback na górę
                        }

                        // Debug: Fioletowa linia w oknie Scene pokaże nam dokładnie punkt trafienia
                        Debug.DrawRay(vfxPos, vfxNormal * 1.0f, Color.magenta, 2.0f);

                        // Tworzymy efekt w punkcie styku (Styl FromSoftware - pooling!)
                        GameObject vfx = SimplePool.Spawn(hitVFXPrefab, vfxPos, Quaternion.LookRotation(vfxNormal));
                        
                        // Zabezpieczenie: Jeśli "Play on Awake" jest wyłączone, zmuszamy cząsteczki do startu
                        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
                        if (ps != null) ps.Play();

                        // NOWE: Automatycznie zwracamy do puli po 2 sekundach
                        SimplePool.Despawn(vfx, hitVFXPrefab, 2.0f);
                    }
                    else
                    {
                        // Ostrzeżenie, jeśli zapomniałeś przypisać prefab w inspektorze
                        Debug.LogWarning($"<color=yellow>[HIT-VFX] Mordo, zapomniałeś przypisać 'hitVFXPrefab' w inspektorze na {gameObject.name}!</color>");
                    }

                    // --- NOWE: Spawnowanie "Hit Light" (FromSoftware style) ---
                    if (hitLightPrefab != null)
                    {
                        Vector3 lightPos = hit.point;
                        if (lightPos == Vector3.zero) lightPos = Vector3.Lerp(currentBase, currentTip, 0.5f);

                        // Spawny punktowe światło dokładnie w miejscu trafienia (Pooling!)
                        GameObject lightObj = SimplePool.Spawn(hitLightPrefab, lightPos, Quaternion.identity);
                        
                        // Hit Light jest krótki, zwracamy po 0.5s
                        SimplePool.Despawn(lightObj, hitLightPrefab, 0.5f);
                    }

                    // --- NOWE: Dźwięk trafienia (FMOD) ---
                    if (!hitSound.IsNull)
                    {
                        RuntimeManager.PlayOneShot(hitSound, hit.point != Vector3.zero ? hit.point : transform.position);
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
