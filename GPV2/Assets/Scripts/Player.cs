using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    public float moveSpeed = 5f;
    public float jumpForce = 30f;
    private bool isChangingStage = false;
    public GameObject attackHitbox;
    public float attackDelay = 0.2f;
    public float attackDuration = 0.4f;

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;

    private float hMove = 0f;
    private bool isGrounded = false;
    public bool isRight = true;

    public const int Max_Health = 100;
    public int health;
    public const int Max_Mana = 100000;
    public float mana;
    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;
    public AudioSource DoorOpen;

    public Transform staffSlot;            // ★ 지팡이가 붙을 위치
    public Transform swordSlot;
    public Weapon equippedWeapon = null;  // ★ 현재 장착된 무기
    public AudioSource normalAttack;
    public Weapon EquippedWeapon
    {
        get { return equippedWeapon; }
    }

    public bool isManaGuardOn = false; // 마나 가드 상태
    private bool isSwinging = false;

    [Header("강한 베기")]
    private bool isCharging = false;      
    private float fire1HoldTime = 0f;    
    public float chargehold = 2f;  
    public float currentAttackMultiplier = 1.0f;
    public GameObject strongAttackEffectPrefab;
    public float strongAttackRadius = 2.0f;
    public LayerMask enemyLayer;
    public GameObject chargeEffectPrefab;
    public GameObject chargedEffectPrefab;
    public AudioSource StrongAttack;
    private GameObject chargeEffectInstance = null;
    private GameObject chargedEffectInstance = null;
    private SpriteRenderer sr;
    


    [Header("카드")]
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    public InventoryUI inventoryUIManager;
    public GameObject cardListWindow;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("Player의 SpriteRenderer를 찾을 수 없습니다!");
        }

        health = Max_Health;
        mana = Max_Mana;
        collectedCards.Add(new CardData(CardSuit.Spade, 1));
        collectedCards.Add(new CardData(CardSuit.Spade, 2));
        collectedCards.Add(new CardData(CardSuit.Spade, 3));
        collectedCards.Add(new CardData(CardSuit.Spade, 4));
        collectedCards.Add(new CardData(CardSuit.Spade, 5));
    }

    void Update()
    {
        hMove = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            //anim.SetTrigger("doJump");
        }

        if (Input.GetButtonDown("Fire1"))
        {

            if (!isSwinging && equippedWeapon != null )
            {
                isCharging = true;
                fire1HoldTime = 0f;
            }
        }

        if (isCharging)
        {
            fire1HoldTime += Time.deltaTime;
            if (fire1HoldTime >= 0.5f && chargeEffectInstance == null)
            {
                if (chargeEffectPrefab != null && fire1HoldTime < 2f)
                {
                    chargeEffectInstance = Instantiate(chargeEffectPrefab, transform.position, Quaternion.identity, transform);
                    chargeEffectInstance.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                } 
            }
            if (chargeEffectInstance != null && fire1HoldTime >= 2f)
            {
                Destroy(chargeEffectInstance);
                chargeEffectInstance = null;
                chargedEffectInstance = Instantiate(chargedEffectPrefab, transform.position, Quaternion.identity, transform);
                chargedEffectInstance.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            }
        }

        if (Input.GetButtonUp("Fire1"))
        {
            if (!isCharging) return; 

            isCharging = false;
            if (chargeEffectInstance != null)
            {
                Destroy(chargeEffectInstance);
                chargeEffectInstance = null;
            }
            if (chargedEffectInstance != null)
            {
                Destroy(chargedEffectInstance);
                chargedEffectInstance = null;
            }
            if (isSwinging) return; // 이미 스윙 중이면 무시
            if (equippedWeapon == null) return;

            if (equippedWeapon.weaponType == WeaponType.Melee && equippedWeapon.weaponLevel >= 2 && fire1HoldTime >= chargehold)
            {
                Debug.Log("강한 베기 발동!");
                currentAttackMultiplier = equippedWeapon.strongAttackMultiplier;
                CastStrongAttack();
                currentAttackMultiplier = 1.0f;
            }
            else 
            {
                Debug.Log("일반 베기");
                normalAttack.Play();
                currentAttackMultiplier = 1.0f; // 배율 1배
                StartCoroutine(SwingWeapon());
            }
        }

        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime && HasRangedWeaponReady())
        {
            float rate = Mathf.Max(0.0001f, GetCurrentFireRate()); // 초당 발사 수
            nextFireTime = Time.time + (1f / rate);                // ← 0.1f/fireRate 대신 '초당 n발' 표준식
            Shoot();
            // anim.SetTrigger("doShoot");
        }

        anim.SetBool("isMoving", hMove != 0);

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (isAtEvent)
                Interaction = true;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryUIManager.gameObject.SetActive(!inventoryUIManager.gameObject.activeSelf);
            UpdateGamePauseState();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            cardListWindow.SetActive(!cardListWindow.activeSelf);
            UpdateGamePauseState();
        }
    }
    void FixedUpdate()
    {
        rb.velocity = new Vector2(hMove * moveSpeed, rb.velocity.y);
        Flip(hMove);
    }


    void Shoot()
    {
        int manaCost = GetCurrentProjectileManaCost();
        if (mana <= manaCost) return;

        GameObject prefab = GetCurrentProjectilePrefab();
        if (prefab == null) return;

        GameObject projectileObject = Instantiate(prefab, firePoint.position, Quaternion.identity);

        Vector2 shootDirection = isRight ? Vector2.right : Vector2.left;

        var proj = projectileObject.GetComponent<Projectile>();
        if (proj != null)
            proj.Setup(shootDirection);

        mana -= manaCost;
    }

    private void ActivateHitbox()
    {
        attackHitbox.SetActive(true);
        Invoke("DisableHitbox", attackDuration);
    }

    private void DisableHitbox()
    {
        attackHitbox.SetActive(false);
    }

<<<<<<< Updated upstream
=======
    private void EquipWeapon(Weapon w)
    {
        if (equippedWeapon != null) Destroy(equippedWeapon.gameObject);

        equippedWeapon = w;
        if (w != null) w.SetOwner(this);

        w.transform.SetParent(swordSlot);

        if (w.weaponType == WeaponType.Ranged)
            w.transform.localPosition = new Vector3(0.35f, 0.1f, 0f);
        else
            w.transform.localPosition = new Vector3(0.35f, 0f, 0f);

        if (!isRight)
            w.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        else
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        Collider2D col = w.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.simulated = false;
    }

    private IEnumerator SwingWeapon()
    {
        isSwinging = true;
        Collider2D weaponCollider = equippedWeapon.GetComponent<Collider2D>();
        Rigidbody2D weaponRb = equippedWeapon.GetComponent<Rigidbody2D>();
        if (weaponCollider != null) weaponCollider.enabled = true;
        if (weaponRb != null) weaponRb.simulated = true;

        float duration = equippedWeapon.swingDuration;
        float startAngle = equippedWeapon.swingStartAngle;
        float endAngle = equippedWeapon.swingEndAngle;
        float timer = 0f;
        Quaternion baseRotation = swordSlot.localRotation;
        Quaternion startRot = baseRotation * Quaternion.Euler(0, 0, startAngle);
        Quaternion endRot = baseRotation * Quaternion.Euler(0, 0, endAngle);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            swordSlot.localRotation = Quaternion.Lerp(startRot, endRot, progress);
            yield return null;
        }

        swordSlot.localRotation = baseRotation;
        if (weaponCollider != null) weaponCollider.enabled = false;
        if (weaponRb != null) weaponRb.simulated = false;
        isSwinging = false;
        currentAttackMultiplier = 1.0f;
    }

    void CastStrongAttack()
    {
        StrongAttack.Play();
        if (strongAttackEffectPrefab == null) return;

        Vector3 spawnPosition = firePoint.position;
        Vector3 effectScale = Vector3.one;
        if (!isRight) effectScale.x = -1;

        GameObject effectInstance = Instantiate(strongAttackEffectPrefab, spawnPosition, Quaternion.identity);
        effectInstance.transform.localScale = effectScale;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPosition, strongAttackRadius, enemyLayer);

        float baseDmg = equippedWeapon.damage + currentAttackDamage;
        if (hasDoubleStrike) baseDmg *= 2.0f;

        int finalDamage = Mathf.RoundToInt(baseDmg * currentAttackMultiplier);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            IDamageable target = enemyCollider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(finalDamage);
            }
        }
    }

>>>>>>> Stashed changes
    private void Flip(float h)
    {
        if ((h < 0 && isRight) || (h > 0 && !isRight))
        {
            isRight = !isRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Door" || other.tag == "Chest")
        {
            isAtEvent = true;
        }

        if (other.CompareTag("Weapon"))
        {
            Weapon w = other.GetComponent<Weapon>();
            if (w != null && w != equippedWeapon)
            {
                EquipWeapon(w);
            }
        }

        if (other.tag == "RedPotion" || other.tag == "BluePotion")
        {
            string itemTag = other.tag;

            if (itemTag == "RedPotion")
            {
                TakeRedPotion();
            }
            else if (itemTag == "BluePotion")
            {
                TakeBluePotion();
            }

            if (!knownItemSprites.ContainsKey(itemTag))
            {
                SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    knownItemSprites.Add(itemTag, sr.sprite);
                }
            }
            AddItemToInventory(itemTag, 1);

            Destroy(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Door")
        {
            isAtEvent = false;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Door doorComponent = other.GetComponent<Door>();

        if (isAtEvent)
        {

            if (Interaction)
            {
                if (other.CompareTag("Door"))
                {
                    Interaction = false;
                    doorComponent.InitiateTransition();
                    
                }

            }
        }
    }

    public void TakeDamage(int damage)
    {
        // 레벨 3 스킬: 마나 가드 활성화 중이고 마나가 있을 때
        if (isManaGuardOn && mana > 0)
        {
            // 마나로 데미지를 대신 받음 (비율 1:1 예시)
            if (mana >= damage)
            {
                mana -= damage;
                Debug.Log($"마나 가드 방어! (소모 마나: {damage})");
                return; // 체력 감소 없음
            }
            else
            {
                // 마나가 부족하면 남은 데미지는 체력으로
                int remainingDmg = damage - (int)mana;
                mana = 0;
                isManaGuardOn = false; // 마나 고갈로 가드 해제
                health -= remainingDmg;
                Debug.Log("마나 가드 파괴!");
            }
        }
        else
        {
            // 일반 피격
            health -= damage;
        }

        if (health <= 0)
        {
            health = 0;
            Debug.Log("플레이어 사망");
            // Die() 함수 호출 등
        }
    }

    public void VelocityZero()
    {
        rb.velocity = Vector2.zero;
    }

    private void EquipWeapon(Weapon w)
    {
        if (equippedWeapon != null)
            Destroy(equippedWeapon.gameObject);

        equippedWeapon = w;
        if (w != null)
        {
            w.SetOwner(this);
        }
        w.transform.SetParent(swordSlot);
        if (w.weaponType == WeaponType.Ranged)
            w.transform.localPosition = new Vector3(0.35f, 0.1f, 0f);
        else
            w.transform.localPosition = new Vector3(0.35f, 0f, 0f);

        if(!isRight)
           w.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        else
           w.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        
           

        Collider2D col = w.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.simulated = false;
    }

    private IEnumerator SwingWeapon()
    {
    isSwinging = true;
    Collider2D weaponCollider = equippedWeapon.GetComponent<Collider2D>();
    Rigidbody2D weaponRb = equippedWeapon.GetComponent<Rigidbody2D>();
    if (weaponCollider != null) weaponCollider.enabled = true;
    if (weaponRb != null) weaponRb.simulated = true;

    float duration = equippedWeapon.swingDuration;
    float startAngle = equippedWeapon.swingStartAngle;
    float endAngle = equippedWeapon.swingEndAngle;
    float timer = 0f;
    Quaternion baseRotation = swordSlot.localRotation;
    Quaternion startRot = baseRotation * Quaternion.Euler(0, 0, startAngle);
    Quaternion endRot = baseRotation * Quaternion.Euler(0, 0, endAngle);

    while (timer < duration)
    {
        timer += Time.deltaTime;
        float progress = timer / duration;
        swordSlot.localRotation = Quaternion.Lerp(startRot, endRot, progress);

        yield return null;
    }

    swordSlot.localRotation = baseRotation; 
    if (weaponCollider != null) weaponCollider.enabled = false; 
    if (weaponRb != null) weaponRb.simulated = false;
    isSwinging = false;
    currentAttackMultiplier = 1.0f;
    }

    public void PerformSkillSwing()
    {
        if (isSwinging) return;
        currentAttackMultiplier = 1.0f;
        StartCoroutine(SwingWeapon());
    }

    void CastStrongAttack()
    {
        StrongAttack.Play();
        if (strongAttackEffectPrefab == null) return;

        Vector3 spawnPosition = firePoint.position;
        Vector3 effectScale = Vector3.one;
        if (!isRight)
        {
            effectScale.x = -1; // 이펙트의 x 스케일을 -1로 만들어서 뒤집음
        }
        GameObject effectInstance = Instantiate(strongAttackEffectPrefab, spawnPosition, Quaternion.identity);
        effectInstance.transform.localScale = effectScale;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPosition, strongAttackRadius, enemyLayer);
        int finalDamage = Mathf.RoundToInt(equippedWeapon.damage * currentAttackMultiplier);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController_2D enemy = enemyCollider.GetComponent<EnemyController_2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
            }
        }
    }

    private bool HasRangedWeaponReady()
    {
        // 장착 무기가 있고, 원거리 또는 하이브리드이며, 프리팹이 존재
        if (equippedWeapon == null) return projectilePrefab != null; // 무기 없으면 Player 기본값 사용
        if (equippedWeapon.weaponType == WeaponType.Ranged || equippedWeapon.weaponType == WeaponType.Hybrid)
            return equippedWeapon.projectilePrefab != null;
        return false;
    }

    private float GetCurrentFireRate()
    {
        // 무기 있으면 무기 연사속도, 없으면 Player 기본값
        return (equippedWeapon != null) ? equippedWeapon.fireRate : fireRate;
    }

    private GameObject GetCurrentProjectilePrefab()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null)
            ? equippedWeapon.projectilePrefab
            : projectilePrefab;
    }

    private int GetCurrentProjectileManaCost()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.ManaCost : 10; // 기본 소모마나(필요시 Player 필드로 승격)
    }

    public void TakeRedPotion()
    {
        if (health < Max_Health)
        {
            health += Max_Health / 2;
            if (health > Max_Health)
            {
                health = Max_Health;
            }
        }
    }

    public void TakeBluePotion()
    {
        if (mana < Max_Mana)
        {
            mana += Max_Mana / 2;
            if (mana > Max_Mana)
            {
                mana = Max_Mana;
            }
        }
    }

    public void AddItemToInventory(string itemName, int amount)
    {
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] += amount;
        }
        else
        {
            inventory.Add(itemName, amount);
        }
    }
    void UpdateGamePauseState()
    {
        if (inventoryUIManager.gameObject.activeSelf || cardListWindow.activeSelf)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    public void UseItem(string itemTag)
    {
       
        if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return;

        bool itemUsed = false; 
        switch (itemTag)
        {
            case "RedPotion":
                if (health < 100)
                {
                    health += 20; 
                    if (health > 100) health = 100;
                    itemUsed = true;
                }
                break;
            case "BluePotion":
                if (mana < 100)
                {
                    mana += 20;
                    if (mana > 100) mana = 100;
                    itemUsed = true;
                }
                break;
        }

        if (itemUsed)
        {
            inventory[itemTag]--;
            if (inventory[itemTag] <= 0)
            {
                inventory.Remove(itemTag);
            }
        }
    }
}