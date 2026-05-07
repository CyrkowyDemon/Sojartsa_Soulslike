using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class WeaponEnchantVisuals : MonoBehaviour
{
    [Header("Referencje Miecza")]
    [SerializeField] private WeaponTrailMesh weaponTrail; 
    [SerializeField] private WeaponHitbox weaponHitbox;

    private List<GameObject> activeVFXList = new List<GameObject>();
    private GameObject currentActivePrefab;
    private FMOD.Studio.EventInstance _loopInstance;

    // Cache na oryginalne dane, żeby móc do nich wrócić po zgaśnięciu
    private Material originalTrailMaterial;

    private void Awake()
    {
        if (weaponTrail == null) weaponTrail = GetComponent<WeaponTrailMesh>();
        if (weaponHitbox == null) weaponHitbox = GetComponent<WeaponHitbox>();
        
        if (weaponTrail != null)
        {
            // Próbujemy zapamiętać materiał, który już tam jest
            originalTrailMaterial = weaponTrail.trailMaterial;
            
            // Jeśli pole w skrypcie jest puste, kradniemy go z rendera
            if (originalTrailMaterial == null)
            {
                MeshRenderer mr = weaponTrail.GetComponent<MeshRenderer>();
                if (mr != null) originalTrailMaterial = mr.sharedMaterial;
            }
        }
    }

    private void Start() => RegisterToPlayer();
    private void OnEnable() => RegisterToPlayer();

    private void RegisterToPlayer()
    {
        PlayerEnchantController controller = GetComponentInParent<PlayerEnchantController>();
        if (controller != null)
        {
            controller.SetActiveWeapon(this);
            Debug.Log($"<color=cyan>[VISUALS] Miecz {gameObject.name} zarejestrowany.</color>");
        }
    }

    public void ActivateVisuals(EnchantData data)
    {
        if (data == null) return;

        // 1. Zmiana wyglądu Traila
        if (weaponTrail != null)
        {
            Material baseMat = data.overrideTrailMaterial != null ? data.overrideTrailMaterial : originalTrailMaterial;

            if (baseMat != null)
            {
                Material tempMat = new Material(baseMat);
                string[] colorParams = { "_Color", "Color", "_BaseColor", "_TintColor", "_EmissionColor" };

                foreach (string param in colorParams)
                {
                    if (tempMat.HasProperty(param))
                    {
                        tempMat.SetColor(param, data.trailColor);
                        if (param == "_EmissionColor") tempMat.EnableKeyword("_EMISSION");
                    }
                }
                
                weaponTrail.SetMaterial(tempMat);
            }
        }

        // 2. Odpalenie Particle Systemu na wszystkich punktach hitboxa
        if (data.weaponVFXPrefab != null && weaponHitbox != null && weaponHitbox.hitPoints != null)
        {
            currentActivePrefab = data.weaponVFXPrefab;
            
            foreach (var point in weaponHitbox.hitPoints)
            {
                if (point == null) continue;

                GameObject vfx = SimplePool.Spawn(currentActivePrefab, point.position, point.rotation);
                vfx.transform.SetParent(point);
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = Quaternion.identity;
                
                activeVFXList.Add(vfx);
            }
        }

        // 3. Audio Loop (DODANO)
        if (!data.loopSound.IsNull)
        {
            _loopInstance = RuntimeManager.CreateInstance(data.loopSound);
            RuntimeManager.AttachInstanceToGameObject(_loopInstance, transform);
            _loopInstance.start();
        }
    }

    public void DeactivateVisuals()
    {
        // 1. Reset Traila
        if (weaponTrail != null && originalTrailMaterial != null)
        {
            weaponTrail.SetMaterial(originalTrailMaterial);
        }

        // 2. Usunięcie wszystkich VFX
        if (currentActivePrefab != null)
        {
            foreach (var vfx in activeVFXList)
            {
                if (vfx == null) continue;
                vfx.transform.SetParent(null); 
                SimplePool.Despawn(vfx, currentActivePrefab); 
            }
            activeVFXList.Clear();
            currentActivePrefab = null;
        }

        // 3. Stop Audio Loop (DODANO)
        if (_loopInstance.isValid())
        {
            _loopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _loopInstance.release();
        }
    }

    private void OnDestroy()
    {
        if (_loopInstance.isValid())
        {
            _loopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _loopInstance.release();
        }
    }
}
