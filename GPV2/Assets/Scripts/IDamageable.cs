// IDamageable.cs
public interface IDamageable
{
    // 이 인터페이스를 상속받는 모든 클래스는 반드시 아래 함수를 구현해야 함을 강제합니다.
    void TakeDamage(int damageAmount);
    void Die();
    void TakePercentDamage(float percent);
}