using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Sojartsa/Inventory/Weapon")]
public class WeaponData : ItemData
{
    [Header("Statystyki Broni")]
    public GameObject weaponPrefab;
    public int baseDamage = 25;
    public float attackSpeed = 1.0f;
    public int poiseDamage = 10;

    [Header("Audio (FMOD)")]
    public EventReference swingSound;
    public EventReference hitSound;

    private void OnValidate()
    {
        type = ItemType.Weapon;
    }
}
