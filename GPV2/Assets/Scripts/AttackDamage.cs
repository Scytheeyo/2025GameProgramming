using UnityEngine;

public class AttackDamage : MonoBehaviour
{
    public int damage = 10;

    // AttackDamage.cs
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy")|| other.CompareTag("Boss"))
        {
            IDamageable enemy = other.GetComponent<IDamageable>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}