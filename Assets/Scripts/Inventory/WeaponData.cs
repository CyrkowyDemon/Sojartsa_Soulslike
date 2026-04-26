using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Sojartsa/Inventory/Weapon")]
public class WeaponData : ItemData
{
    [Header("Statystyki Broni")]
    public GameObject weaponPrefab;
    public int baseDamage = 25;
    public float attackSpeed = 1.0f;


    private void OnValidate()    {
        type = ItemType.Weapon;
    }
}
