using UnityEngine;

public enum WeaponType { Melee, Ranged, Hybrid }

public class Weapon : MonoBehaviour
{
    [Header("무기 기본 정보")]
    public string weaponName = "기본 지팡이";
    public int damage = 10;
    public int weaponLevel = 1;

    [Header("근접 스윙")]
    public float swingDuration = 0.25f;
    public float swingStartAngle = 45f;
    public float swingEndAngle = -45f;

    [Header("강한 베기")] 
    public float strongAttackMultiplier = 3f;

    [Header("발사체 설정")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float fireRate = 0.5f;
    public int ManaCost = 5;

    [Header("타입")]
    public WeaponType weaponType = WeaponType.Melee;   // ★ 추가

    [HideInInspector] public bool isSwinging = false;
    private Player ownerPlayer = null;

    public void SetOwner(Player player)
    {
        ownerPlayer = player;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (ownerPlayer == null) return;

            int finalDamage = Mathf.RoundToInt(damage * ownerPlayer.currentAttackMultiplier);

            Debug.Log(other.name + "에게 " + finalDamage + " 데미지!");

            EnemyController_2D enemy = other.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
            }
        }
    }
}