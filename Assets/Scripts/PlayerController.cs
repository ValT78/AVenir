using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallJumpForce = 10f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private Sprite glitchedSprite;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite spriteWithGun;
    private SpriteRenderer spriteRenderer;
    private Animator _animator;
    private int _movingBoolHash;

    [SerializeField]
    private float wallJumpDuration = 0.2f; // Durée pendant laquelle le mouvement horizontal est désactivé
    [SerializeField] private GameObject platform; //plateforme qui bouche le trou au début du jeu

    private Rigidbody2D rb;
    public float moveInput;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    public bool isFacingRight = true;
    public bool canMove = true;
    private int wallDirection;
    private bool isPlayingFootsteps;

    public GameObject ballPrefab; // Référence au Prefab de la balle


    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _movingBoolHash = Animator.StringToHash("IsMoving");
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!GameManager.Instance.hasPressedStart)
        {
            spriteRenderer.sprite = normalSprite;
        }

        else if(GameManager.Instance.hasGun)
        {
            spriteRenderer.sprite = spriteWithGun;
        }

        else if (GameManager.Instance.unlockedJump)
        {
            spriteRenderer.sprite = normalSprite;
        }
        else
        {
               spriteRenderer.sprite = glitchedSprite;
            _animator.enabled = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
            moveInput = context.ReadValue<float>();

        
        
    }
    private IEnumerator PlayFootsteps()
    {
        isPlayingFootsteps = true;
        while (moveInput != 0 && isGrounded)
        {
            int i = Random.Range(1, 4);
            string footstep1 = "footstep" + i;
            AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.footstep1);
            yield return new WaitForSeconds(0.7f); // Ajustez l'intervalle selon vos besoins
        }
        isPlayingFootsteps = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {

        if (GameManager.Instance.unlockedJump || !GameManager.Instance.hasPressedStart)
        {
            if (isGrounded)
            {
                AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.jumpSound);
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }

            if (GameManager.Instance.unlockedWallJump)
            {
                if (isTouchingWall && !isGrounded)
                {
                    WallJump();
                }
            }
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //Debug.Log("OnClick");
            CollectiblePlaform.MouseClicked();
            PlatPlaceholder.MouseClicked();
        }
    }

    public void ReturnMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            print("swapping");
            SceneManager.LoadScene("Baptiste");
            GameManager.Instance.returnedOnce = true;
            GameManager.Instance.ResetGame();
        }
    }

    public void Shoot(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.hasGun && context.performed)
        {
            Instantiate(ballPrefab, transform.position, transform.rotation).GetComponent<Bullet>().SetDirection(isFacingRight ? new Vector2(1, 0) : new Vector2(-1, 0));
        }
    }


    public void ActivateEyes()
    {
        StartCoroutine(GameManager.Instance.GlitchAnimation(eyesRenderer, null, eyesRenderer.sprite, 1.5f, 0.2f));
    }

    public void DesactivateEyes()
    {
        StartCoroutine(GameManager.Instance.GlitchAnimation(eyesRenderer, eyesRenderer.sprite, null, 1.5f, 0.2f));
    }

    public void GetGlitchedSprite()
    {
        StartCoroutine(GameManager.Instance.GlitchAnimation(spriteRenderer, normalSprite, glitchedSprite, 1.5f, 0.2f));
    }

    public void GetNormalSprite()
    {
        StartCoroutine(GameManager.Instance.GlitchAnimation(spriteRenderer, glitchedSprite, normalSprite, 1.5f, 0.2f));
    }

    void Update()
    {

        if (moveInput != 0)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
            if (moveInput > 0)
            {
                Flip(true);
            }
            else
            {
                Flip(false);
            }
            if (isGrounded && !isPlayingFootsteps)
            {
                StartCoroutine(PlayFootsteps());
            }
        }

        if (platform != null)
        {
            if (!GameManager.Instance.unlockedPlatform)
            {
                platform.SetActive(false);
            }
            else
            {
                platform.SetActive(true);
            }
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        isTouchingWall = Physics2D.OverlapBox(wallCheck.position, new Vector2(0.1f, 1f), 0f, groundLayer);
        
        if (isTouchingWall && !isGrounded)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            wallDirection = isFacingRight ? 1 : -1;
        }
        else
        {
            isWallSliding = false;
        }
        
        if(rb.velocity.x > 0.5 || rb.velocity.x < -0.5) _animator.SetBool(_movingBoolHash, true);
        else _animator.SetBool(_movingBoolHash, false);

        
    }

    void WallJump()
    {
        canMove = false;
        rb.velocity = new Vector2(-wallDirection * wallJumpForce, jumpForce);
        
        Flip(!isFacingRight);

        AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.wallJumpSound);
        StartCoroutine(EnableMovementAfterDelay());
    }

    IEnumerator EnableMovementAfterDelay()
    {
        yield return new WaitForSeconds(wallJumpDuration);
        canMove = true;
    }

    public void Flip(bool direction)
    {
        isFacingRight = direction;
        if(isFacingRight)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    
}