using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems; // ★ UI 클릭 감지를 위해 필수

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;

    [Header("Jump System (Synergy)")]
    public int maxJumpCount = 1;      // 기본 1, 페어/트리플 시 2
    public int currentJumpCount = 0;
    public float jumpMultiplier = 1.0f; // 트리플 시 1.3배 점프

    [Header("Melee Combat")]
    public GameObject attackHitbox;
    public float baseAttackDelay = 0.2f; // 기본 공격 딜레이
    public float attackDuration = 0.4f;

    // 콤보 공격 관련 변수
    private int comboStep = 0;
    private float lastAttackTime = 0f;
    public float comboResetTime = 1.0f;

    [Header("Ranged Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float baseFireRate = 0.1f; // 기본 발사 속도
    private float nextFireTime = 0f;

    // 상태 변수
    private float hMove = 0f;
    private bool isGrounded = false;
    private bool isRight = true;
    private bool isDead = false; // 사망 상태 체크

    [Header("Status")]
    public int baseMaxHealth = 100;
    public int maxHealth; // 실제 최대 체력 (기본 + 카드 + 포카드 버프)
    public int health;

    public int baseMaxMana = 100;
    public int maxMana;   // 실제 최대 마나 (기본 + 카드 + 포카드 버프)
    public int mana;

    public float baseAttackDamage = 10f;
    public float currentAttackDamage; // 실제 공격력

    public float baseDefense = 0f;
    public float currentDefense;      // 실제 방어력

    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;

    public Transform staffSlot;
    private Weapon equippedWeapon = null;

    // 덱 시너지 플래그 (족보 효과)
    public bool hasResurrection = false; // 스트레이트: 부활
    public bool isResurrectionUsed = false; // 부활 사용 여부
    public float cooldownReduction = 0f; // 플러시: 쿨타임 감소 (0.2 = 20%)
    public bool hasDoubleStrike = false; // 풀하우스: 추가 타격(데미지 2배)
    public bool hasStatBuff = false;     // 포카드: 올스탯 10%
    public bool hasUltimate = false;     // 스트레이트 플러시: 궁극기(Z키)

    // 카드, 인벤토리 데이터
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    // UI 관련
    public InventoryUI inventoryUIManager;
    private Animator inventoryUIAnimator;
    public GameObject cardListWindow;
    private Animator cardListAnimator;
    public GameObject optionWindow;
    private Animator optionUIAnimator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);

        // UI 컴포넌트 초기화
        if (cardListWindow != null) cardListAnimator = cardListWindow.GetComponent<Animator>();
        if (inventoryUIManager != null) inventoryUIAnimator = inventoryUIManager.gameObject.GetComponent<Animator>();
        if (optionWindow != null)
        {
            optionUIAnimator = optionWindow.GetComponent<Animator>();
            optionWindow.SetActive(false);
        }

        // 초기 스탯 계산 (기본값 적용)
        RecalculateStats();
        health = maxHealth;
        mana = maxMana;

        // 테스트용 카드 지급
        for (int i = 1; i <= 13; ++i) AddCardToCollection(new CardData(CardSuit.Spade, i));
        for (int i = 1; i <= 12; ++i) AddCardToCollection(new CardData(CardSuit.Clover, i));
        for (int i = 1; i <= 11; ++i) AddCardToCollection(new CardData(CardSuit.Heart, i));
        for (int i = 1; i <= 10; ++i) AddCardToCollection(new CardData(CardSuit.Diamond, i));

        if (cardListWindow != null) cardListWindow.SetActive(false);
    }

    void Update()
    {
        // 사망 시 조작 불가
        if (isDead) return;

        // UI 키 입력 (I, S, ESC)은 언제나 처리해야 함
        HandleUIInput();

        // ★ [핵심 수정] UI가 열려있거나(IsUIOpen), 마우스가 UI 위에 있다면(IsPointerOverUI)
        // 캐릭터 조작(이동, 점프, 공격)을 실행하지 않음.
        if (IsUIOpen() || IsPointerOverUI())
        {
            // UI 상태에서도 애니메이터 파라미터가 꼬이지 않도록 기본적인 값은 넣어줌 (이동 정지 등)
            anim.SetBool("isMoving", false);
            return;
        }

        hMove = Input.GetAxisRaw("Horizontal");

        // 매 프레임 덱 시너지 상태 확인
        CheckDeckSynergy();

        // 점프 구현 (시너지 적용: 페어/트리플)
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || currentJumpCount < maxJumpCount)
            {
                DoJump();
            }
        }

        // 근접 공격 (시너지 적용: 플러시 쿨타임 감소)
        if (Input.GetButtonDown("Fire1"))
        {
            float currentComboResetTime = comboResetTime * (1f - cooldownReduction);

            if (Time.time - lastAttackTime > currentComboResetTime)
            {
                comboStep = 0;
            }

            comboStep++;
            lastAttackTime = Time.time;

            if (comboStep == 1) anim.SetTrigger("doAttack1");
            else { anim.SetTrigger("doAttack2"); comboStep = 0; }

            float currentAttackDelay = baseAttackDelay * (1f - cooldownReduction);
            Invoke("ActivateHitbox", currentAttackDelay);
        }

        // 원거리 공격 (시너지 적용: 플러시 발사 속도 증가)
        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime && HasRangedWeaponReady())
        {
            float rate = Mathf.Max(0.0001f, GetCurrentFireRate());
            rate = rate / (1f - cooldownReduction);

            nextFireTime = Time.time + (1f / rate);
            TryShootAnimation();
        }

        // 궁극기 사용 (시너지 적용: 스트레이트 플러시, Z키)
        if (Input.GetKeyDown(KeyCode.Z) && hasUltimate)
        {
            CastUltimate();
        }

        // 애니메이터 파라미터 업데이트
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isMoving", hMove != 0);

        // 상호작용 키
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (isAtEvent) Interaction = true;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // UI가 열려있으면 물리 이동도 정지 (선택 사항이지만 안전함)
        if (IsUIOpen())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = new Vector2(hMove * moveSpeed, rb.velocity.y);
        Flip(hMove);
    }

    // ★ [추가됨] UI가 하나라도 열려있는지 확인하는 함수
    bool IsUIOpen()
    {
        return (inventoryUIManager != null && inventoryUIManager.gameObject.activeSelf) ||
               (cardListWindow != null && cardListWindow.activeSelf) ||
               (optionWindow != null && optionWindow.activeSelf);
    }

    // ★ [추가됨] 마우스 포인터가 UI 요소(버튼, 패널 등) 위에 있는지 확인하는 함수
    // EventSystem이 없으면 false 반환
    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ================================================================
    // 덱 시너지(족보) 체크 및 적용 로직
    // ================================================================
    public void CheckDeckSynergy()
    {
        maxJumpCount = 1;
        jumpMultiplier = 1.0f;
        hasResurrection = false;
        cooldownReduction = 0f;
        hasDoubleStrike = false;
        hasStatBuff = false;
        hasUltimate = false;

        if (activeDeck.Count < 5)
        {
            CheckPartialSynergy();
            RecalculateStats();
            return;
        }

        var sortedDeck = activeDeck.OrderBy(c => c.number).ToList();
        var numberGroups = activeDeck.GroupBy(c => c.number).ToList();
        var suitGroups = activeDeck.GroupBy(c => c.suit).ToList();

        bool isStraight = true;
        for (int i = 0; i < 4; i++)
        {
            if (sortedDeck[i + 1].number - sortedDeck[i].number != 1)
            {
                isStraight = false;
                break;
            }
        }

        bool isFlush = suitGroups.Any(g => g.Count() == 5);
        bool isFourCard = numberGroups.Any(g => g.Count() == 4);
        bool isFullHouse = numberGroups.Any(g => g.Count() == 3) && numberGroups.Any(g => g.Count() == 2);

        if (isStraight && isFlush) hasUltimate = true;
        else if (isFourCard) hasStatBuff = true;
        else if (isFullHouse) hasDoubleStrike = true;
        else if (isFlush) cooldownReduction = 0.2f;
        else if (isStraight) hasResurrection = true;
        else CheckPartialSynergy();

        RecalculateStats();
    }

    void CheckPartialSynergy()
    {
        var numberGroups = activeDeck.GroupBy(c => c.number).ToList();
        bool isTriple = numberGroups.Any(g => g.Count() >= 3);
        bool isPair = numberGroups.Any(g => g.Count() == 2);

        if (isTriple)
        {
            maxJumpCount = 2;
            jumpMultiplier = 1.3f;
        }
        else if (isPair)
        {
            maxJumpCount = 2;
            jumpMultiplier = 1.0f;
        }
    }

    void DoJump()
    {
        float finalJumpForce = jumpForce;
        if (!isGrounded) finalJumpForce *= jumpMultiplier;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, finalJumpForce), ForceMode2D.Impulse);

        anim.ResetTrigger("doLand");
        anim.SetTrigger("doJump");

        isGrounded = false;
        currentJumpCount++;
    }

    void CastUltimate()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Queen"))
            {
                enemy.SendMessage("TakeDamage", 1000, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Destroy(enemy);
            }
        }
        Debug.Log("Ultimate Activated: Royal Straight Flush!");
    }

    // ================================================================
    // 능력치 계산 및 전투 로직
    // ================================================================

    public void RecalculateStats()
    {
        float addedAttack = 0;
        int addedHealth = 0;
        float addedDefense = 0;
        int addedMana = 0;

        foreach (CardData card in collectedCards)
        {
            switch (card.suit)
            {
                case CardSuit.Spade: addedAttack += card.number; break;
                case CardSuit.Heart: addedHealth += card.number; break;
                case CardSuit.Diamond: addedDefense += card.number; break;
                case CardSuit.Clover: addedMana += card.number; break;
            }
        }

        float tempAttack = baseAttackDamage + addedAttack;
        float tempHealth = baseMaxHealth + addedHealth;
        float tempDefense = baseDefense + addedDefense;
        float tempMana = baseMaxMana + addedMana;

        if (hasStatBuff)
        {
            tempAttack *= 1.1f;
            tempHealth *= 1.1f;
            tempDefense *= 1.1f;
            tempMana *= 1.1f;
        }

        currentAttackDamage = tempAttack;
        maxHealth = (int)tempHealth;
        currentDefense = tempDefense;
        maxMana = (int)tempMana;

        if (health > maxHealth) health = maxHealth;
        if (mana > maxMana) mana = maxMana;
    }

    public void AddCardToCollection(CardData newCard)
    {
        collectedCards.Add(newCard);
        RecalculateStats();
    }

    public float GetTotalAttackDamage()
    {
        float weaponDamage = (equippedWeapon != null) ? equippedWeapon.damage : 0;
        float total = currentAttackDamage + weaponDamage;

        if (hasDoubleStrike)
        {
            total *= 2.0f;
        }

        return total;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.Max(1, damage - (int)currentDefense);
        health -= finalDamage;

        if (health <= 0)
        {
            if (hasResurrection && !isResurrectionUsed)
            {
                health = maxHealth / 2;
                isResurrectionUsed = true;
                Debug.Log("Resurrected by Straight Synergy!");
            }
            else
            {
                health = 0;
                Die();
            }
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("doDie");
        if (gameManager != null)
        {
            // gameManager.GameOver();
        }
    }

    // ================================================================
    // 기존 기능 (애니메이션 이벤트, 물리, UI, 아이템 등)
    // ================================================================

    void TryShootAnimation()
    {
        int manaCost = GetCurrentProjectileManaCost();
        if (mana <= manaCost) return;
        anim.SetTrigger("doShoot");
    }

    public void ExecuteShoot()
    {
        int manaCost = GetCurrentProjectileManaCost();
        if (mana <= manaCost) return;

        GameObject prefab = GetCurrentProjectilePrefab();
        if (prefab == null) return;

        GameObject projectileObject = Instantiate(prefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = isRight ? Vector2.left : Vector2.right;

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

    private void Flip(float h)
    {
        if ((h < 0 && !isRight) || (h > 0 && isRight))
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
            if (!isGrounded)
            {
                anim.SetTrigger("doLand");
            }

            anim.ResetTrigger("doJump");
            isGrounded = true;
            currentJumpCount = 0;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void HandleUIInput()
    {
        if (Input.GetKeyDown(KeyCode.I)) ToggleUI(inventoryUIManager.gameObject, inventoryUIAnimator, cardListWindow, optionWindow);
        if (Input.GetKeyDown(KeyCode.S)) ToggleUI(cardListWindow, cardListAnimator, inventoryUIManager.gameObject, optionWindow);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionWindow.activeSelf)
            {
                OptionPage optionScript = optionWindow.GetComponent<OptionPage>();
                if (optionScript != null) optionScript.ClickConfirm();

                if (optionUIAnimator != null) optionUIAnimator.SetTrigger("doClose");
                else optionWindow.SetActive(false);
            }
            else
            {
                if (inventoryUIManager.gameObject.activeSelf) inventoryUIManager.gameObject.SetActive(false);
                if (cardListWindow.activeSelf) cardListWindow.SetActive(false);

                optionWindow.SetActive(true);
                if (optionUIAnimator != null) optionUIAnimator.SetTrigger("doOpen");
                UpdateGamePauseState();
            }
        }
    }

    void ToggleUI(GameObject targetUI, Animator targetAnim, GameObject otherUI1, GameObject otherUI2)
    {
        if (targetUI.activeSelf)
        {
            // UI를 닫을 때, 혹시라도 켜져있을 수 있는 공격 트리거를 초기화하여
            // 창을 닫자마자 공격하는 현상 방지
            ResetCombatTriggers();

            if (targetAnim != null) targetAnim.SetTrigger("doClose");
        }
        else
        {
            targetUI.SetActive(true);
            if (targetAnim != null) targetAnim.SetTrigger("doOpen");
            UpdateGamePauseState();
            if (otherUI1.activeSelf) otherUI1.SetActive(false);
            if (otherUI2.activeSelf) otherUI2.SetActive(false);
        }
    }

    // ★ [추가됨] 전투 관련 트리거 초기화 함수 (UI 닫을 때 사용)
    void ResetCombatTriggers()
    {
        if (anim != null)
        {
            anim.ResetTrigger("doAttack1");
            anim.ResetTrigger("doAttack2");
            anim.ResetTrigger("doShoot");
            anim.ResetTrigger("doJump");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Entrance" || other.tag == "Exit" || other.tag == "Chest") isAtEvent = true;

        if (other.CompareTag("Weapon"))
        {
            Weapon w = other.GetComponent<Weapon>();
            if (w != null) EquipWeapon(w);
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
        if (other.CompareTag("Entrance") || other.CompareTag("Exit")) isAtEvent = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isAtEvent && Interaction)
        {
            if (other.CompareTag("Entrance")) { gameManager.NextStage(); Interaction = false; }
            else if (other.CompareTag("Exit")) { gameManager.PreviousStage(); Interaction = false; }
        }
    }

    public void VelocityZero() { rb.velocity = Vector2.zero; }

    private void EquipWeapon(Weapon w)
    {
        if (equippedWeapon != null) Destroy(equippedWeapon.gameObject);
        equippedWeapon = w;
        w.transform.SetParent(staffSlot);
        w.transform.localPosition = Vector3.zero;
        if (w.weaponType == WeaponType.Ranged) w.transform.localRotation = Quaternion.identity;
        else w.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        Collider2D col = w.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.simulated = false;
    }

    private bool HasRangedWeaponReady()
    {
        if (equippedWeapon == null) return projectilePrefab != null;
        if (equippedWeapon.weaponType == WeaponType.Ranged || equippedWeapon.weaponType == WeaponType.Hybrid)
            return equippedWeapon.projectilePrefab != null;
        return false;
    }

    private float GetCurrentFireRate()
    {
        return (equippedWeapon != null) ? equippedWeapon.fireRate : baseFireRate;
    }

    private GameObject GetCurrentProjectilePrefab()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.projectilePrefab : projectilePrefab;
    }

    private int GetCurrentProjectileManaCost()
    {
        return (equippedWeapon != null && equippedWeapon.projectilePrefab != null) ? equippedWeapon.ManaCost : 10;
    }

    public void TakeRedPotion()
    {
        if (health < maxHealth) { health += maxHealth / 2; if (health > maxHealth) health = maxHealth; }
    }

    public void TakeBluePotion()
    {
        if (mana < maxMana) { mana += maxMana / 2; if (mana > maxMana) mana = maxMana; }
    }

    public void AddItemToInventory(string itemName, int amount)
    {
        if (inventory.ContainsKey(itemName)) inventory[itemName] += amount;
        else inventory.Add(itemName, amount);
    }

    public void UpdateGamePauseState()
    {
        if (inventoryUIManager.gameObject.activeSelf || cardListWindow.activeSelf || optionWindow.activeSelf) Time.timeScale = 0f;
        else Time.timeScale = 1f;
    }

    public void UseItem(string itemTag)
    {
        if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return;
        bool itemUsed = false;
        switch (itemTag)
        {
            case "RedPotion":
                if (health < maxHealth) { health += 20; if (health > maxHealth) health = maxHealth; itemUsed = true; }
                break;
            case "BluePotion":
                if (mana < maxMana) { mana += 20; if (mana > maxMana) mana = maxMana; itemUsed = true; }
                break;
        }
        if (itemUsed) { inventory[itemTag]--; if (inventory[itemTag] <= 0) inventory.Remove(itemTag); }
    }
}