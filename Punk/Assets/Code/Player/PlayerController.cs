using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Punk
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController instance;

        // Outlets
        Rigidbody2D _rigidbody2D;
        SpriteRenderer sprite;
        Animator animator;
        public Image healthBar;
        public Sprite[] healthStates;
        public GameObject musicNotePrefab;
        public Transform aimPivot;
        public TMP_Text ammoText;

        // State Tracking
        public int jumpsLeft;
        public int ammoLeft;
        public bool canDash;
        public float dashTimer;
        public bool isDashing;
        public bool isInvincible;
        private float invincibleTimer;
        public int health;
        private Vector2 savedVelocity; //for dashing
        private bool facingRight;
        public bool sfxPlaying = false;

        //Upgradables
        public float projectileDistanceTimer;
        public float damageMultiplier;
        public float speedMultiplier;
        public float dashMultiplier;
        public float jumpMultiplier;
        public int critOn;
        public int moshScore;
        public int theoryScore;
        public int presenceScore;

        // Methods (Start is called before the first frame update)
        void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            health = 3;
            canDash = true;
            facingRight = true;
        }
        void Awake()
        {
            instance = this;
            loadData();
        }

        void FixedUpdate()
        {
            // This Update Event is sync'd with the Physics Engine
            animator.SetFloat("Speed", _rigidbody2D.velocity.magnitude);
            if (_rigidbody2D.velocity.magnitude > 0)
            {
                animator.speed = _rigidbody2D.velocity.magnitude / 3f;
            }
            else
            {
                animator.speed = 1f;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (MenuController.instance.isPaused) return;

            float defaultSpeed = 18f;
            //Only lessen timers if they are positive to avoid underflow
            dashTimer -= (dashTimer > 0f) ? Time.deltaTime : 0f;
            invincibleTimer -= (invincibleTimer > 0f) ? Time.deltaTime : 0f;
            if (dashTimer <= 0) canDash = true;
            if (invincibleTimer > 0f) isInvincible = true;
            else isInvincible = false;

            float currentSpeed = defaultSpeed;

            //turn off dash on update
            if (isDashing && dashTimer <= 1.4f)
            {
                _rigidbody2D.velocity = savedVelocity;
                isDashing = false;
                animator.SetBool("Is Dashing", false);
            }

            //a lil extra time to move thru stuff
            if (dashTimer <= 1.3f)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Projectile"), false);
            }

            //Open Menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                MenuController.instance.Show();
            }

            // Move Player Left
            if (Input.GetKey(KeyCode.A))
            {
                _rigidbody2D.AddForce(Vector2.left * currentSpeed * speedMultiplier * Time.deltaTime, ForceMode2D.Impulse);
                Flip(false);

            }

            // Move Player Right
            if (Input.GetKey(KeyCode.D))
            {
                _rigidbody2D.AddForce(Vector2.right * currentSpeed * speedMultiplier * Time.deltaTime, ForceMode2D.Impulse);
                Flip(true);
            }

            // Dash
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                if (canDash)
                {
                    // SoundManager.instance.PlaySoundWhoosh();
                    savedVelocity = _rigidbody2D.velocity;
                    float dashForce = facingRight ? 50f : -50f;
                    _rigidbody2D.velocity = new Vector2(dashForce*dashMultiplier, _rigidbody2D.velocity.y);
                    canDash = false;
                    isDashing = true;
                    dashTimer = 1.5f;
                    animator.SetBool("Is Dashing", true);
                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Projectile"), true);
                }
            }

            // Jump
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (jumpsLeft > 0)
                {
                    jumpsLeft--;
                    SoundManager.instance.PlaySoundJump();
                    _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x*jumpMultiplier, 0);
                    _rigidbody2D.AddForce(Vector2.up * 16f, ForceMode2D.Impulse);
                }
            }
            animator.SetInteger("JumpsLeft", jumpsLeft);

            // Attack
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                animator.SetTrigger("Attack");
            }

            // Aim Toward Mouse
            Vector3 mousePosition = Input.mousePosition;
            Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector3 directionFromPlayerToMouse = mousePositionInWorld - transform.position;

            float radiansToMouse = Mathf.Atan2(directionFromPlayerToMouse.y, directionFromPlayerToMouse.x);
            float angleToMouse = radiansToMouse * Mathf.Rad2Deg;

            aimPivot.rotation = Quaternion.Euler(0, 0, angleToMouse);

            // Shoot
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (ammoLeft > 0)
                {
                    GameObject newProjectile = Instantiate(musicNotePrefab);
                    newProjectile.transform.position = transform.position;
                    newProjectile.transform.rotation = aimPivot.rotation;
                    animator.SetTrigger("Shoot");
                    ammoLeft -= 1;
                    updateDisplay();
                }
            }
        }

        void Flip(bool right)
        {
            facingRight = right ? true : false;
            // Multiply the player's x local scale by -1
            Vector3 theScale = transform.localScale;
            theScale.x = facingRight ? Mathf.Abs(theScale.x) : Mathf.Abs(theScale.x) * -1;
            transform.localScale = theScale;
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            // Check that we collided with Ground
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Platform"))
            {
                // Check what is directly below our character's feet
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 0.85f);
                // Debug.DrawRay(transform.position, Vector2.down * 0.85f); // Visualize Raycast

                //We might have multiple things below our character's feet
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit2D hit = hits[i];

                    // Check that we collided with ground below our feet
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Platform"))
                    {
                        // Reset jump count
                        jumpsLeft = 1;
                    }
                }
            }

            // Colliding with enemy
            if ((other.gameObject.GetComponent<EnemyController>() || other.gameObject.GetComponent<MeleeEnemyController>() 
                || other.gameObject.GetComponent<IdleEnemyController>() || other.gameObject.GetComponent<LaserController>()) && !isDashing && !isInvincible)
            {
                if (other.gameObject.GetComponent<LaserController>()) Destroy(other.gameObject);
                TakeDamage(1);
                animator.SetTrigger("Hurt");
                SoundManager.instance.PlaySoundHurt();
            }

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("DeathBox") && !isDashing && !isInvincible)
            {
                animator.SetTrigger("Hurt");
                sfxPlaying = true;
                SoundManager.instance.PlaySoundGameOver();
                Invoke("Die", 1);
            }
            if (other.gameObject.CompareTag("BossBullet") && !isDashing && !isInvincible)
            {
                TakeDamage(1);
                animator.SetTrigger("Hurt");
                SoundManager.instance.PlaySoundHurt();
            }
        }

        //Take Damage and set image
        void TakeDamage(int dmg)
        {
            health -= dmg;

            if (health <= 0)
            {
                healthBar.sprite = healthStates[0];
                sfxPlaying = true;
                SoundManager.instance.PlaySoundGameOver();
                Invoke("Die", 1);
                // Die();
            }
            else
            {
                healthBar.sprite = healthStates[health];
            }

            invincibleTimer = 1f;
        }

        // TODO: Change what Die() does, right now it just resets scene
        void Die()
        {
            sfxPlaying = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void saveData()
        {
            PlayerPrefs.SetInt("health", health);
            PlayerPrefs.SetFloat("dmgMult", damageMultiplier);
            PlayerPrefs.SetFloat("dashMult", dashMultiplier);
            PlayerPrefs.SetFloat("spMult", speedMultiplier);
            PlayerPrefs.SetFloat("jMult", jumpMultiplier);
            PlayerPrefs.SetInt("mosh", moshScore);
            PlayerPrefs.SetInt("theory", theoryScore);
            PlayerPrefs.SetInt("presence",  presenceScore);
        }

        public void getAmmo(int ammoAmt)
        {
            ammoLeft += ammoAmt;
            updateDisplay();
        }

        //Load all fields from
        public void loadData()
        {
            if (PlayerPrefs.HasKey("health")) health = PlayerPrefs.GetInt("health");
            else health = 3;
            if (PlayerPrefs.HasKey("dmgMult")) damageMultiplier = PlayerPrefs.GetFloat("dmgMult");
            else damageMultiplier = 1;
            if (PlayerPrefs.HasKey("dashMult")) dashMultiplier = PlayerPrefs.GetFloat("dmgMult");
            else dashMultiplier = 1;
            if (PlayerPrefs.HasKey("spMult")) speedMultiplier = PlayerPrefs.GetFloat("dmgMult");
            else speedMultiplier = 1;
            if (PlayerPrefs.HasKey("jMult")) jumpMultiplier = PlayerPrefs.GetFloat("dmgMult");
            else jumpMultiplier = 1;
            if (PlayerPrefs.HasKey("critOn")) critOn = PlayerPrefs.GetInt("critOn");
            else critOn = 20;
            if (PlayerPrefs.HasKey("mosh")) moshScore = PlayerPrefs.GetInt("mosh");
            else moshScore = 0;
            if (PlayerPrefs.HasKey("theory")) theoryScore = PlayerPrefs.GetInt("theory");
            else theoryScore = 0;
            if (PlayerPrefs.HasKey("presence")) presenceScore = PlayerPrefs.GetInt("presence");
            else presenceScore = 0;
        }

        void updateDisplay()
        {
            ammoText.text = "Left: " + ammoLeft.ToString();
        }
    }
}