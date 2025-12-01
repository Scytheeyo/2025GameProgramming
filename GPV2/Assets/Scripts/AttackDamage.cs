using UnityEngine;

public class AttackDamage : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 일반 몬스터 피격
        if (other.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = other.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        // 2. [추가됨] 보스 피격 (태그가 Boss 이거나 스크립트가 있는 경우)
        else if (other.CompareTag("Boss"))
        {
            Boss_CardCaptain boss = other.GetComponent<Boss_CardCaptain>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }
        }
    }
}