using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // ★ Player2에서 가져옴 (AddCardToCollection 사용 위해)

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("이동 및 점프")]
    public float moveSpeed = 5f;
    public float jumpForce = 30f; // Player1의 높은 점프력 유지
    private float hMove = 0f;
    private bool isGrounded = false;
    public bool isRight = true;

    [Header("전투 기본")]
    public GameObject attackHitbox;
    public float attackDelay = 0.2f;
    public float attackDuration = 0.4f;

    [Header("원거리 공격")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;

    [Header("상태 및 스탯")]
    public const int Max_Health = 100;
    public int health;
    public const int Max_Mana = 100000; // Player1의 높은 마나통 유지 (테스트용 추정)
    public float mana;
    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;

    [Header("장비 및 오디오")]
    public Transform staffSlot;
    public Transform swordSlot;
    public Weapon equippedWeapon = null;
    public AudioSource DoorOpen;
    public AudioSource normalAttack;
    public Weapon EquippedWeapon { get { return equippedWeapon; } }

    [Header("전투 스킬 (Player1)")]
    public bool isManaGuardOn = false;
    private bool isSwinging = false;
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

    [Header("카드 및 인벤토리")]
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    [Header("UI")]
    public InventoryUI inventoryUIManager;
    public GameObject cardListWindow;
    private Animator cardListAnimator; // ★ Player2에서 가져옴

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) Debug.LogError("Player의 SpriteRenderer를 찾을 수 없습니다!");

        // ★ Player2의 UI 애니메이터 로직 통합
        if (cardListWindow != null)
        {
            cardListAnimator = cardListWindow.GetComponent<Animator>();
            cardListWindow.SetActive(false); // 시작 시 닫기
        }

        health = Max_Health;
        mana = Max_Mana;

        // ★ 카드는 일단 Player1의 수동 추가 방식을 유지하되, 필요하면 Player2의 루프 방식으로 교체 가능
        collectedCards.Add(new CardData(CardSuit.Spade, 1));
        collectedCards.Add(new CardData(CardSuit.Spade, 2));
        collectedCards.Add(new CardData(CardSuit.Spade, 3));
        collectedCards.Add(new CardData(CardSuit.Spade, 4));
        collectedCards.Add(new CardData(CardSuit.Spade, 5));
    }

    void Update()
    {
        hMove = Input.GetAxisRaw("Horizontal");

        // 점프
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            // anim.SetTrigger("doJump"); // Player1은 주석처리 되어있음
        }

        // ---------------- [공격 로직: Player1의 차지/스윙 시스템 채택] ----------------
        if (Input.GetButtonDown("Fire1"))
        {
            if (!isSwinging && equippedWeapon != null)
            {
                isCharging = true;
                fire1HoldTime = 0f;
            }
        }

        if (isCharging)
        {
            fire1HoldTime += Time.deltaTime;
            // 차지 이펙트 처리
            if (fire1HoldTime >= 0.5f && chargeEffectInstance == null)
            {
                if (chargeEffectPrefab != null && fire1HoldTime < 2f)
                {
                    chargeEffectInstance = Instantiate(chargeEffectPrefab, transform.position, Quaternion.identity, transform);
                    chargeEffectInstance.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                }
            }
            // 풀차지 이펙트 처리
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
            if (isCharging)
            {
                isCharging = false;
                // 이펙트 정리
                if (chargeEffectInstance != null) { Destroy(chargeEffectInstance); chargeEffectInstance = null; }
                if (chargedEffectInstance != null) { Destroy(chargedEffectInstance); chargedEffectInstance = null; }

                if (!isSwinging && equippedWeapon != null)
                {
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
                        currentAttackMultiplier = 1.0f;
                        StartCoroutine(SwingWeapon());
                    }
                }
            }
        }
        // -------------------------------------------------------------------------

        // 원거리 공격
        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime && HasRangedWeaponReady())
        {
            float rate = Mathf.Max(0.0001f, GetCurrentFireRate());
            nextFireTime = Time.time + (1f / rate);
            Shoot();
        }

        anim.SetBool("isMoving", hMove != 0);

        // 상호작용
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (isAtEvent) Interaction = true;
        }

        // 인벤토리 UI
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryUIManager.gameObject.SetActive(!inventoryUIManager.gameObject.activeSelf);
            UpdateGamePauseState();
        }

        // 카드 리스트 UI (★ Player2의 애니메이터 로직 적용)
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (cardListWindow.activeSelf)
            {
                // 열려있으면 닫기 애니메이션
                if (cardListAnimator != null) cardListAnimator.SetTrigger("doClose");
                else { cardListWindow.SetActive(false); UpdateGamePauseState(); }
            }
            else
            {
                // 닫혀있으면 열기 애니메이션
                cardListWindow.SetActive(true);
                if (cardListAnimator != null) cardListAnimator.SetTrigger("doOpen");
                UpdateGamePauseState();
            }
        }
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(hMove * moveSpeed, rb.velocity.y);
        Flip(hMove);
    }

    // ---------------- [Player1의 전투 메서드들] ----------------

    // [추가됨] 스킬 매니저에서 호출하는 공격 모션 함수
    public void PerformSkillSwing()
    {
        if (!isSwinging && equippedWeapon != null)
        {
            StartCoroutine(SwingWeapon());
        }
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
        if (proj != null) proj.Setup(shootDirection);

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

    // ---------------- [Player1의 복잡한 무기 장착 로직 유지] ----------------
    private void EquipWeapon(Weapon w)
    {
        if (equippedWeapon != null) Destroy(equippedWeapon.gameObject);

        equippedWeapon = w;
        if (w != null) w.SetOwner(this);

        // 지팡이/칼 슬롯 구분
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

    // ---------------- [Player1의 스윙 코루틴 유지] ----------------
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

    // Player1의 강공격 로직
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
        int finalDamage = Mathf.RoundToInt(equippedWeapon.damage * currentAttackMultiplier);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController_2D enemy = enemyCollider.GetComponent<EnemyController_2D>();
            if (enemy != null) enemy.TakeDamage(finalDamage);
        }
    }

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

    // ---------------- [충돌 처리] ----------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 태그가 Ground인 물체와 부딪혔을 때
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 부딪힌 지점의 법선 벡터(충돌 면이 바라보는 방향)를 가져옵니다.
            // (0, 1)이면 윗면(바닥), (1, 0)이면 옆면(벽), (0, -1)이면 아랫면(천장)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 법선 벡터의 Y값이 0.7 이상이면 '위쪽을 보고 있는 면' 즉, 바닥입니다.
                if (contact.normal.y > 0.7f)
                {
                    isGrounded = true;
                    // Debug.Log("진짜 바닥에 착지!");
                    return; // 하나라도 바닥이면 OK
                }
            }

            // 여기까지 왔다면 Ground 태그지만 바닥은 아님 (벽이나 천장)
            // Debug.Log("이건 벽이나 천장이야!");
        }
    }

    // 떨어질 때도 마찬가지로 체크 (선택 사항이지만 추천)
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Door" || other.tag == "Chest") isAtEvent = true;

        if (other.CompareTag("Weapon"))
        {
            Weapon w = other.GetComponent<Weapon>();
            if (w != null && w != equippedWeapon) EquipWeapon(w);
        }

        if (other.tag == "RedPotion" || other.tag == "BluePotion")
        {
            string itemTag = other.tag;
            if (itemTag == "RedPotion") TakeRedPotion();
            else if (itemTag == "BluePotion") TakeBluePotion();

            if (!knownItemSprites.ContainsKey(itemTag))
            {
                SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) knownItemSprites.Add(itemTag, sr.sprite);
            }
            AddItemToInventory(itemTag, 1);
            Destroy(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Door") isAtEvent = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Player1의 Door 컴포넌트 상호작용 방식 유지 (더 구체적이므로)
        Door doorComponent = other.GetComponent<Door>();

        if (isAtEvent && Interaction)
        {
            if (other.CompareTag("Door"))
            {
                Interaction = false;
                if (doorComponent != null) doorComponent.InitiateTransition();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        // Player1의 마나 가드 로직 유지
        if (isManaGuardOn && mana > 0)
        {
            if (mana >= damage)
            {
                mana -= damage;
                Debug.Log($"마나 가드 방어! (소모 마나: {damage})");
                return;
            }
            else
            {
                int remainingDmg = damage - (int)mana;
                mana = 0;
                isManaGuardOn = false;
                health -= remainingDmg;
                Debug.Log("마나 가드 파괴!");
            }
        }
        else
        {
            health -= damage;
        }

        if (health <= 0)
        {
            health = 0;
            Debug.Log("플레이어 사망");
        }
    }

    public void VelocityZero()
    {
        rb.velocity = Vector2.zero;
    }

    // ---------------- [유틸리티 및 아이템] ----------------
    private bool HasRangedWeaponReady()
    {
        if (equippedWeapon == null) return projectilePrefab != null;
        if (equippedWeapon.weaponType == WeaponType.Ranged || equippedWeapon.weaponType == WeaponType.Hybrid)
            return equippedWeapon.projectilePrefab != null;
        return false;
    }

    private float GetCurrentFireRate()
    {
        return (equippedWeapon != null) ? equippedWeapon.fireRate : fireRate;
    }

    private GameObject GetCurrentProjectilePrefab()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null)
            ? equippedWeapon.projectilePrefab : projectilePrefab;
    }

    private int GetCurrentProjectileManaCost()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.ManaCost : 10;
    }

    public void TakeRedPotion()
    {
        if (health < Max_Health)
        {
            health += Max_Health / 2;
            if (health > Max_Health) health = Max_Health;
        }
    }

    public void TakeBluePotion()
    {
        if (mana < Max_Mana)
        {
            mana += Max_Mana / 2;
            if (mana > Max_Mana) mana = Max_Mana;
        }
    }

    public void AddItemToInventory(string itemName, int amount)
    {
        if (inventory.ContainsKey(itemName)) inventory[itemName] += amount;
        else inventory.Add(itemName, amount);
    }

    // ★ Player2에서 public으로 변경된 부분 적용
    public void UpdateGamePauseState()
    {
        if (inventoryUIManager.gameObject.activeSelf || cardListWindow.activeSelf)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
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
            if (inventory[itemTag] <= 0) inventory.Remove(itemTag);
        }
    }

    // ★ Player2의 신규 메서드 추가 (카드 중복 방지)
    public void AddCardToCollection(CardData newCard)
    {
        bool alreadyExists = collectedCards.Any(card =>
            card.suit == newCard.suit &&
            card.number == newCard.number
        );

        if (!alreadyExists)
        {
            collectedCards.Add(newCard);
            Debug.Log(newCard.suit + " " + newCard.number + " 카드를 획득했습니다.");
        }
        else
        {
            Debug.Log(newCard.suit + " " + newCard.number + " 카드는 이미 보유 중이라 무시합니다.");
        }
    }
}