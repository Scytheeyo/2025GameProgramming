using UnityEngine;

public class AttackDamage : MonoBehaviour
{
    public int damage = 10;

    // AttackDamage.cs
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyController_2D enemy = other.GetComponent<EnemyController_2D>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}