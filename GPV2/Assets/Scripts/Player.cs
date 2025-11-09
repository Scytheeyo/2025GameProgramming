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
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;

    private float hMove = 0f;
    private bool isGrounded = false;
    private bool isRight = true;

    //Enemy 파트
    public int health = 100;
    public int mana = 100;
    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;

    // 카드 목록, 현재 덱, 아이템 목록
    public List<CardData> collectedCards = new List<CardData>();
    public List<CardData> activeDeck = new List<CardData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public Dictionary<string, Sprite> knownItemSprites = new Dictionary<string, Sprite>();

    // 인벤토리, 카드 UI
    public InventoryUI inventoryUIManager;
    public GameObject cardListWindow;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);
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

        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
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
        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = isRight ? Vector2.left : Vector2.right;
        projectileObject.GetComponent<Projectile>().Setup(shootDirection);
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


        if (other.tag == "RedPotion" || other.tag == "BluePotion")
        {
            string itemTag = other.tag;
            if (!knownItemSprites.ContainsKey(itemTag))
            {
                SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    knownItemSprites.Add(itemTag, sr.sprite);
                    Debug.Log("새 아이템 스프라이트 학습: " + itemTag);
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
    public void AddItemToInventory(string itemName, int amount)
    {
        // 인벤토리에 이미 해당 아이템이 있는지 확인
        if (inventory.ContainsKey(itemName))
        {
            // 있으면 개수 증가
            inventory[itemName] += amount;
        }
        else
        {
            // 없으면 새로 추가
            inventory.Add(itemName, amount);
        }
    }
    void UpdateGamePauseState()
    {
        // 인벤토리 창이나 카드 덱 창이 *하나라도* 활성화되어 있다면
        if (inventoryUIManager.gameObject.activeSelf || cardListWindow.activeSelf)
        {
            // 게임 시간을 멈춥니다.
            Time.timeScale = 0f;
        }
        else
        {
            // 두 창이 모두 닫혀있다면 게임 시간을 다시 1배속으로 설정합니다.
            Time.timeScale = 1f;
        }
    }
    public void UseItem(string itemTag)
    {
        // 1. 인벤토리에 해당 아이템이 있는지 확인
        if (!inventory.ContainsKey(itemTag) || inventory[itemTag] <= 0) return;

        bool itemUsed = false; // 아이템 사용에 성공했는지 여부

        // 2. 태그(문자열)에 따라 아이템 효과 적용
        switch (itemTag)
        {
            case "RedPotion":
                if (health < 100)
                {
                    health += 20; // 기획서의 값 혹은 원하는 값
                    if (health > 100) health = 100;
                    Debug.Log("HP 물약 사용. 현재 체력: " + health);
                    itemUsed = true;
                }
                break;
            case "BluePotion":
                if (mana < 100)
                {
                    mana += 20;
                    if (mana > 100) mana = 100;
                    Debug.Log("MP 물약 사용. 현재 마나: " + mana);
                    itemUsed = true;
                }
                break;
                // (나중에 다른 아이템 태그 추가)
        }

        // 3. 아이템 사용에 성공한 경우에만 개수 차감
        if (itemUsed)
        {
            inventory[itemTag]--;
            // 만약 아이템이 0개가 되면 인벤토리에서 제거
            if (inventory[itemTag] <= 0)
            {
                inventory.Remove(itemTag);
            }
        }
    }
}