using UnityEngine;
public class HideWeapon : MonoBehaviour
{
    [SerializeField] private GameObject Weapon;
    public void Hide() => Weapon.SetActive(false);
    public void Unhide() => Weapon.SetActive(true);
}
