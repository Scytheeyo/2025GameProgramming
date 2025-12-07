using UnityEngine;

public class AttackDamage : MonoBehaviour
{
    // 외부(Player)에서 주입받을 데미지 변수
    private int currentDamage = 10;

    // 데미지 수치를 갱신하는 메서드
    public void UpdateDamage(int amount)
    {
        currentDamage = amount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            IDamageable enemy = other.GetComponent<IDamageable>();

            if (enemy != null)
            {
                enemy.TakeDamage(currentDamage); // 갱신된 데미지 적용
            }
            else
            {
                // 인터페이스 없는 적을 위해 SendMessage 처리 추가
                other.SendMessage("TakeDamage", currentDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}