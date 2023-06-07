using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    // --- COMPONENTS ---
    public Rigidbody2D rb;
    public Animator animator;
    public Text coinCountText;

    // --- PLAYER CONTROLS ---
    private PlayerControls controls;
    [SyncVar] private float direction = 0.0f;
    private bool jumpPressed;
    private bool firePressed;

    // --- MOVEMENT ---
    [SerializeField] private float speed = 25f;
    [SerializeField] private float airSpeed = 10f;

    // Ground detection variables
    public LayerMask groundLayer;
    public Transform feetPos;
    [SerializeField] private float checkRadius;
    public bool isAlive = true;

    // Direction the player is facing
    [SyncVar] private bool isFacingRight = true;

    // Player state flags
    private bool isJumping = false;
    [SyncVar] private bool isGrounded = false;

    // --- JUMPING ---
    // Jumping parameters
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferLength = 0.1f;

    // Coyote time and jump buffer timers
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // --- SHOOTING ---
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 20f;

    // --- COINS ---
    // Coin count
    [SyncVar(hook = nameof(OnCoinCountChanged))] [SerializeField] private int coins = 0;

    private void Awake()
    {
        InitializeControls();
    }

    // Called when a network client is active and initialized
    public override void OnStartLocalPlayer()
    {
        coinCountText = GameObject.Find("coinCount").GetComponent<Text>();
        controls.Enable();
    }

    // Called when the behavior becomes disabled
    void OnDisable()
    {
        controls.Disable();
    }

    [Client]
    void Update()
    {
        if (!isLocalPlayer) return;

        ManageJumpBufferAndCoyoteTime();

        // Fire projectile if fire button is pressed
        FireProjectileIfTriggered();

        //// Maintain UI orientation
        //MaintainUIOrientation();
    }


    [Client]
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        ManageCharacterMovement();

        ManageCharacterJump();

        DecreaseCoyoteAndBufferTimers();

        IncreaseFallingSpeedIfFalling();
    }

    // Called from client to server to increase coin count
    [Command]
    public void CmdCollectCoin()
    {
        coins++;
        RpcUpdateCoinCount(coins);
    }

    [ClientRpc]
    public void RpcUpdateCoinCount(int newCoins)
    {
        coins = newCoins;
        if (isLocalPlayer)
        {
            coinCountText.text = "x " + newCoins.ToString();
        }
    }

    // Initializes player controls
    private void InitializeControls()
    {
        controls = new PlayerControls();

        // Assign methods to input actions
        controls.Land.Move.performed += ctx => { direction = ctx.ReadValue<float>(); };
        controls.Land.Move.canceled += ctx => { direction = 0; };
        controls.Land.Jump.performed += ctx => { jumpPressed = true; };
        controls.Land.Jump.canceled += ctx => { jumpPressed = false; };
        controls.Land.Shoot.performed += ctx => { firePressed = true; };
        controls.Land.Shoot.canceled += ctx => { firePressed = false; };
    }

    // Manages coyote time and jump buffer for more responsive controls
    private void ManageJumpBufferAndCoyoteTime()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);

        if (wasGrounded && !isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }

        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferLength;
        }

        if ((isGrounded || coyoteTimeCounter > 0) && jumpBufferCounter > 0)
        {
            isJumping = true;
            jumpBufferCounter = 0;
        }
    }

    // Fires a projectile if fire button is pressed
    private void FireProjectileIfTriggered()
    {
        if (firePressed)
        {
            CmdFire(isFacingRight);
            firePressed = false;
        }
    }

    // Maintains the UI orientation
    //private void MaintainUIOrientation()
    //{
    //    if (coinCountText)
    //    {
    //        coinCountText.transform.rotation = Quaternion.identity;
    //    }
    //}

    // Manages character movement
    private void ManageCharacterMovement()
    {
        float currentSpeed = isGrounded ? speed : airSpeed;
        rb.velocity = new Vector2(direction * currentSpeed, rb.velocity.y);
        animator.SetFloat("speed", Mathf.Abs(direction));

        // Flips character if direction changes
        if ((isFacingRight && direction < 0) || (!isFacingRight && direction > 0))
        {
            Flip();
        }
    }

    // Manages character jump
    private void ManageCharacterJump()
    {
        if (isJumping)
        {
            Jump();
        }
    }

    // Decreases coyote time and jump buffer timers each frame
    private void DecreaseCoyoteAndBufferTimers()
    {
        coyoteTimeCounter -= Time.fixedDeltaTime;
        jumpBufferCounter -= Time.fixedDeltaTime;
    }

    // Increases the falling speed if the character is falling
    private void IncreaseFallingSpeedIfFalling()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    //Makes the Player jump
    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = false;
    }

    // Flips the player
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    // Called from client to server to fire a projectile
    [Command]
    void CmdFire(bool isFacingRight)
    {
        var projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.shooter = netIdentity;
        projectileComponent.Launch((isFacingRight ? Vector2.right : Vector2.left), isFacingRight);
        NetworkServer.Spawn(projectile);
    }

    [Client]
    public void CollectCoin()
    {
        if (isLocalPlayer)
        {
            CmdCollectCoin();
        }
    }

    // Updates coin count text when coin count changes
    void OnCoinCountChanged(int oldCoins, int newCoins)
    {
        if (isLocalPlayer)
        {
            coinCountText.text = "x " + newCoins.ToString();
        }
    }
    //Getter method for returning coins
    public int GetCoins()
    {
        return coins;
    }
    public void Die()
    {
        isAlive = false;
        this.enabled = false;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        if (isServer)
        {
            GameManager.instance.PlayerDied();
            RpcDie();
        }
    }

    [ClientRpc]
    public void RpcDie()
    {
        isAlive = false;
        this.enabled = false;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}