using UnityEngine;

public enum WeaponType { Melee, Ranged, Hybrid }

public class Weapon : MonoBehaviour
{
    [Header("무기 기본 정보")]
    public string weaponName = "기본 지팡이";
    public int damage = 10;

    [Header("근접 스윙")]
    public float swingDuration = 0.25f;
    public float swingStartAngle = 45f;
    public float swingEndAngle = -45f;

    [Header("발사체 설정")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float fireRate = 0.5f;
    public int ManaCost = 5;

    [Header("타입")]
    public WeaponType weaponType = WeaponType.Melee;   // ★ 추가

    [HideInInspector] public bool isSwinging = false;
}