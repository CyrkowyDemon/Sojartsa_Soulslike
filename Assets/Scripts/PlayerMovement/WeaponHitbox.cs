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

    private void Start()
    {
        _ownerCombat = transform.root.GetComponentInChildren<PlayerCombat>();
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
                trailMesh.basePoint = hitPoints[0];
                trailMesh.tipPoint = hitPoints[hitPoints.Count - 1];
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

        GameObject target = enemyHP != null ? enemyHP.gameObject : (playerHP != null ? playerHP.gameObject : null);

        if (target != null && !_hitObjectsThisSwing.Contains(target))
        {
            _hitObjectsThisSwing.Add(target);
            if (enemyHP != null)
            {
                int finalDamage = damageAmount;
                if (transform.root.CompareTag("Player") && EquipmentManager.Instance != null)
                    finalDamage = EquipmentManager.Instance.GetCurrentAttackDamage();
                enemyHP.TakeDamage(finalDamage, isKnockbackAttack, poiseDamage);
                isEnemy = true;
            }
            else if (playerHP != null)
            {
                playerHP.TakeDamage(damageAmount, isKnockbackAttack);
                isPlayer = true;
            }

            SpawnHitVFX(hit);
            PlayHitSound(hit, target);
            return true;
        }
        return false;
    }

    private void TriggerHitEffects(bool hitEnemy, bool hitPlayer)
    {
        float duration = hitPlayer ? hitStopOnPlayerHit : hitStopDuration;
        if (HitStop.Instance != null) HitStop.Instance.Freeze(duration);
        var source = GetComponent<CinemachineImpulseSource>() ?? GetComponentInParent<CinemachineImpulseSource>();
        if (source != null) source.GenerateImpulse();
    }

    private void SpawnHitVFX(RaycastHit hit)
    {
        Vector3 pos = hit.point == Vector3.zero ? hit.collider.bounds.center : hit.point;
        if (hitVFXPrefab != null)
        {
            GameObject vfx = SimplePool.Spawn(hitVFXPrefab, pos, Quaternion.LookRotation(hit.normal == Vector3.zero ? Vector3.up : hit.normal));
            SimplePool.Despawn(vfx, hitVFXPrefab, 2.0f);
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
        string material = target.GetComponentInChildren<SurfaceAudio>()?.surfaceLabel ?? defaultMaterialLabel;
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
