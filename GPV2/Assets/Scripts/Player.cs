using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    public float moveSpeed = 5f;
    public float jumpForce = 15f;

    public GameObject attackHitbox;
    public float attackDelay = 0.2f;
    public float attackDuration = 0.4f;

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;

    private float hMove = 0f;
    private bool isGrounded = false;
    private bool isRight = true;

    public const int Max_Health = 100;
    public int health;
    public const int Max_Mana = 100;
    public int mana;
    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;

    public Transform staffSlot;            // ★ 지팡이가 붙을 위치
    private Weapon equippedWeapon = null;  // ★ 현재 장착된 무기

    // ī ,  ,  
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    // κ丮, ī UI
    public InventoryUI inventoryUIManager;
    public GameObject cardListWindow;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);

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
            anim.SetTrigger("doJump");
        }

        if (Input.GetButtonDown("Fire1"))
        {
            anim.SetTrigger("doAttack");
            Invoke("ActivateHitbox", attackDelay);

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
        if (other.tag == "Entrance" || other.tag == "Exit" || other.tag == "Chest")
        {
            isAtEvent = true;
        }

        if (other.CompareTag("Weapon"))
        {
            Weapon w = other.GetComponent<Weapon>();
            if (w != null)
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
                    Debug.Log("  Ʈ н: " + itemTag);
                }
            }
            AddItemToInventory(itemTag, 1);

            Destroy(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Entrance") || other.CompareTag("Exit"))
        {
            isAtEvent = false;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isAtEvent)
        {

            if (Interaction)
            {
                if (other.CompareTag("Entrance"))
                {
                    gameManager.NextStage();
                    Interaction = false;
                }
                else if (other.CompareTag("Exit"))
                {
                    gameManager.PreviousStage();
                    Interaction = false;
                }
            }
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

        w.transform.SetParent(staffSlot);
        w.transform.localPosition = Vector3.zero;

        // ★ 타입별 장착 각도
        if (w.weaponType == WeaponType.Ranged)
            w.transform.localRotation = Quaternion.identity;              // 원거리: 그대로
        else
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);   // 근거리/하이브리드: Z 180°

        Collider2D col = w.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb2 = w.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.simulated = false;
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
        // κ丮 ̹ ش  ִ Ȯ
        if (inventory.ContainsKey(itemName))
        {
            //   
            inventory[itemName] += amount;
        }
        else
        {
            //   ߰
            inventory.Add(itemName, amount);
        }
    }
    void UpdateGamePauseState()
    {
        // κ丮 â̳ ī  â *ϳ* ȰȭǾ ִٸ
        if (inventoryUIManager.gameObject.activeSelf || cardListWindow.activeSelf)
        {
            //  ð ϴ.
            Time.timeScale = 0f;
        }
        else
        {
            //  â  ִٸ  ð ٽ 1 մϴ.
            Time.timeScale = 1f;
        }
    }
    public void UseItem(string itemTag)
    {
        // 1. κ丮 ش  ִ Ȯ
        if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return;

        bool itemUsed = false; //  뿡 ߴ 

        // 2. ±(ڿ)   ȿ 
        switch (itemTag)
        {
            case "RedPotion":
                if (health < 100)
                {
                    health += 20; // ȹ  Ȥ ϴ 
                    if (health > 100) health = 100;
                    Debug.Log("HP  .  ü: " + health);
                    itemUsed = true;
                }
                break;
            case "BluePotion":
                if (mana < 100)
                {
                    mana += 20;
                    if (mana > 100) mana = 100;
                    Debug.Log("MP  .  : " + mana);
                    itemUsed = true;
                }
                break;
                // (߿ ٸ  ± ߰)
        }

        // 3.  뿡  쿡  
        if (itemUsed)
        {
            inventory[itemTag]--;
            //   0 Ǹ κ丮 
            if (inventory[itemTag] <= 0)
            {
                inventory.Remove(itemTag);
            }
        }
    }
}