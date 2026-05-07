using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using FMODUnity;
using Unity.Cinemachine;

public class WeaponHitbox : MonoBehaviour
{
    [Header("Ustawienia Hitboxa")]
    public int hitboxID;
    public bool isKnockbackAttack;
    public List<Transform> hitPoints;
    public float weaponThickness = 0.1f;
    public LayerMask enemyLayer;

    [Header("Obraenia")]
    public int damageAmount = 10;
    public int poiseDamage = 10;

    [Header("Trail & VFX")]
    [SerializeField] private WeaponTrailMesh trailMesh;
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private GameObject hitLightPrefab;

    [Header("Audio (FMOD)")]
    public EventReference swingSound;
    public EventReference hitSound;
    public string materialParameterName = "Surface";
    public string defaultMaterialLabel = "Flesh";

    [Header("Hit Stop")]
    public float hitStopDuration = 0.05f;
    public float hitStopOnPlayerHit = 0.04f;

    private bool _isAttacking;
    private readonly HashSet<GameObject> _hitObjectsThisSwing = new HashSet<GameObject>();
    private readonly Dictionary<Transform, Vector3> _previousPositions = new Dictionary<Transform, Vector3>();
    private RaycastHit[] _hitCache = new RaycastHit[10];
    private PlayerCombat _ownerCombat;
    private SojartsaAI.v3.AIBrain _ownerBrain;
    private WeaponData _weaponData; // NOWOŚĆ: Przechowuje dane z Inventory

    private void Awake()
    {
        _ownerBrain = transform.root.GetComponentInChildren<SojartsaAI.v3.AIBrain>();
        _ownerCombat = transform.root.GetComponentInChildren<PlayerCombat>();
    }

    /// <summary>
    /// Pobiera dane z systemu Inventory. Wywoływane przez EquipmentManager przy spawnowaniu broni.
    /// </summary>
    public void Initialize(WeaponData data)
    {
        _weaponData = data;
        if (data != null)
        {
            damageAmount = data.baseDamage;
            poiseDamage = data.poiseDamage;
            
            // Jeśli WeaponData ma przypisane dźwięki, nadpisujemy te lokalne
            if (!data.swingSound.IsNull) swingSound = data.swingSound;
            if (!data.hitSound.IsNull) hitSound = data.hitSound;
        }
    }

    private void Start()
    {
        if (_ownerCombat != null) _ownerCombat.SetActiveWeapon(this);
    }

    private void OnEnable()
    {
        if (_ownerCombat == null) _ownerCombat = transform.root.GetComponentInChildren<PlayerCombat>();
        if (_ownerCombat != null) _ownerCombat.SetActiveWeapon(this);
    }

    public void OpenDamageWindow()
    {
        if (hitPoints == null || hitPoints.Count == 0)
        {
            Debug.LogError($"<color=red>[MIECZ] Brak punktw hitPoints na {gameObject.name}!</color>");
            return;
        }

        _isAttacking = true;
        _hitObjectsThisSwing.Clear();

        foreach (var point in hitPoints)
        {
            if (point != null) _previousPositions[point] = point.position;
        }
    }

    public void CloseDamageWindow()
    {
        _isAttacking = false;
    }

    public void StartWeaponTrail()
    {
        if (trailMesh != null)
        {
            if (hitPoints != null && hitPoints.Count >= 2)
            {
                trailMesh.sourcePoints = hitPoints;
            }
            trailMesh.StartTrail();
        }
    }

    public void StopWeaponTrail()
    {
        if (trailMesh != null) trailMesh.StopTrail();
    }

    public void PlaySwingSound()
    {
        if (!swingSound.IsNull) RuntimeManager.PlayOneShot(swingSound, transform.position);
    }

    private void LateUpdate()
    {
        if (!_isAttacking) return;



        bool hasHitAnything = false;
        bool hitEnemy = false;
        bool hitPlayer = false;

        foreach (var point in hitPoints)
        {
            if (point == null) continue;

            Vector3 currentPos = point.position;
            Vector3 prevPos = _previousPositions.ContainsKey(point) ? _previousPositions[point] : currentPos;
            Vector3 direction = currentPos - prevPos;
            float distance = direction.magnitude;

            if (distance > 0.0001f)
            {
                int count = Physics.SphereCastNonAlloc(prevPos, weaponThickness, direction.normalized, _hitCache, distance, enemyLayer);
                for (int i = 0; i < count; i++)
                {
                    if (ProcessHit(_hitCache[i], out bool isE, out bool isP))
                    {
                        hasHitAnything = true;
                        if (isE) hitEnemy = true;
                        if (isP) hitPlayer = true;
                    }
                }
            }
            _previousPositions[point] = currentPos;
        }

        if (hasHitAnything) TriggerHitEffects(hitEnemy, hitPlayer);
    }



    private bool ProcessHit(RaycastHit hit, out bool isEnemy, out bool isPlayer)
    {
        isEnemy = false;
        isPlayer = false;
        Collider col = hit.collider;
        
        EnemyHealth enemyHP = col.GetComponentInParent<EnemyHealth>();
        PlayerHealth playerHP = col.GetComponentInParent<PlayerHealth>();

        bool isEnvironment = (enemyHP == null && playerHP == null);
        GameObject target = enemyHP != null ? enemyHP.gameObject : (playerHP != null ? playerHP.gameObject : col.gameObject);

        if (target != null)
        {
            // Elden Ring Fix: Jeśli to podłoga/ściana, sypiemy cząsteczkami ZAWSZE,
            // dopóki miecz sunie po ziemi.
            if (isEnvironment)
            {
                SpawnHitVFX(hit);
            }

            // Ale rejestrujemy główne trafienie (dźwięki, obrażenia, wibracje kamery) TYLKO RAZ
            if (!_hitObjectsThisSwing.Contains(target))
            {
                _hitObjectsThisSwing.Add(target);

                int finalDamage = damageAmount;
                float finalPoiseDamage = poiseDamage;

                if (_ownerBrain != null && _ownerBrain.ActiveAction != null)
                {
                    finalDamage = _ownerBrain.ActiveAction.damageAmount;
                    finalPoiseDamage = _ownerBrain.ActiveAction.poiseDamage;
                }

                if (enemyHP != null)
                {
                    if (transform.root.CompareTag("Player") && EquipmentManager.Instance != null)
                        finalDamage = EquipmentManager.Instance.GetCurrentAttackDamage();
                    enemyHP.TakeDamage(finalDamage, isKnockbackAttack, (int)finalPoiseDamage);
                    isEnemy = true;
                }
                else if (playerHP != null)
                {
                    playerHP.TakeDamage(finalDamage, isKnockbackAttack);
                    isPlayer = true;
                }

                // Dla żywych przeciwników sypiemy krwią tylko raz
                if (!isEnvironment) SpawnHitVFX(hit);

                PlayHitSound(hit, target); // Dźwięk zderzenia też tylko raz (inaczej rozwali bębenki)
                return true; // Odsyłamy true (wibracja kamery) tylko przy uderzeniu początkowym
            }
        }
        return false;
    }

    private void TriggerHitEffects(bool hitEnemy, bool hitPlayer)
    {
        // Pętla zatrzymuje czas tylko jeśli trafiono w istotę żywą, nie w ścianę
        if (hitEnemy || hitPlayer)
        {
            float duration = hitPlayer ? hitStopOnPlayerHit : hitStopDuration;
            if (HitStop.Instance != null) HitStop.Instance.Freeze(duration);
        }
        
        var source = GetComponent<CinemachineImpulseSource>() ?? GetComponentInParent<CinemachineImpulseSource>();
        if (source != null) source.GenerateImpulse();
    }

    private void SpawnHitVFX(RaycastHit hit)
    {
        Vector3 pos = hit.point == Vector3.zero ? hit.collider.bounds.center : hit.point;
        
        // Nowość: Pobieramy specyficzny efekt cząsteczkowy dla danego materiału (np. krew dla Flesh, drzazgi dla Wood)
        GameObject dynamicVFX = hitVFXPrefab; 
        if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null)
        {
            var surfaceType = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetSurface(hit);
            var customVFX = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetHitVFX(surfaceType);
            if (customVFX != null) dynamicVFX = customVFX;
        }

        if (dynamicVFX != null)
        {
            GameObject vfx = SimplePool.Spawn(dynamicVFX, pos, Quaternion.LookRotation(hit.normal == Vector3.zero ? Vector3.up : hit.normal));
            SimplePool.Despawn(vfx, dynamicVFX, 2.0f);
        }
        if (hitLightPrefab != null)
        {
            GameObject light = SimplePool.Spawn(hitLightPrefab, pos, Quaternion.identity);
            SimplePool.Despawn(light, hitLightPrefab, 0.5f);
        }
    }

    private void PlayHitSound(RaycastHit hit, GameObject target)
    {
        if (hitSound.IsNull) return;
        
        // Zamiast powolnego GetComponent na SurfaceAudio, używamy centralnego Managera i PhysicsMaterials!
        string material = defaultMaterialLabel;
        if (Sojartsa.Systems.Surface.SurfaceManager.Instance != null)
        {
            var surfaceType = Sojartsa.Systems.Surface.SurfaceManager.Instance.GetSurface(hit);
            if (surfaceType != Sojartsa.Systems.Surface.SurfaceType.Default)
            {
                material = surfaceType.ToString(); // Pobiera etykietę (np. "Metal", "Flesh") z Fizyki!
            }
        }

        var instance = RuntimeManager.CreateInstance(hitSound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(hit.point != Vector3.zero ? hit.point : transform.position));
        instance.setParameterByNameWithLabel(materialParameterName, material);
        instance.start();
        instance.release();
    }

    private void OnDrawGizmos()
    {
        if (hitPoints == null || hitPoints.Count == 0) return;
        Gizmos.color = _isAttacking ? Color.red : Color.yellow;
        for (int i = 0; i < hitPoints.Count; i++)
        {
            if (hitPoints[i] == null) continue;
            Gizmos.DrawWireSphere(hitPoints[i].position, weaponThickness);
            if (i > 0 && hitPoints[i - 1] != null) Gizmos.DrawLine(hitPoints[i - 1].position, hitPoints[i].position);
        }
    }
}
