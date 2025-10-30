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

    //Enemy ÆÄÆ®
    public int health = 100;
    public int mana = 100;
    public GameManager gameManager;
    private bool isAtEvent = false;
    public bool Interaction = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackHitbox.SetActive(false);
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
    }
    void FixedUpdate()
    {
        rb.velocity = new Vector2(hMove * moveSpeed, rb.velocity.y);
        Flip(hMove);
    }


    void Shoot()
    {
        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 shootDirection = isRight ? Vector2.right : Vector2.left;
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
        if (other.tag == "Entrance" || other.tag == "Exit" || other.tag == "Chest")
        {
            isAtEvent = true;
        }


        if (other.tag == "RedPotion")
        {
            if (health < 100)
            {
                health += 20;
                if (health > 100)
                {
                    health = 100;
                }
            }
            Destroy(other.gameObject);
        }

        if (other.tag == "BluePotion")
        {
            if (mana < 100)
            {
                mana += 20;
                if (mana > 100)
                {
                    mana = 100;
                }
            }
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
}