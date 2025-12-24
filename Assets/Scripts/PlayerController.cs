using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float MaxSpeed = 8.5f;
    [SerializeField] float jumpForce = 10f;

    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    private bool isGrounded = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        inputActions?.Enable();
    }

    private void Update()
    {
        if (inputActions.Player.Move.IsPressed())
        {
            Vector2 direction = inputActions.Player.Move.ReadValue<Vector2>();
            moveInput = direction;
            if(isGrounded)
            {
                if (direction == Vector2.left && rb.linearVelocityX > -MaxSpeed)
                {
                    if(rb.linearVelocityX > direction.x * (moveSpeed/2))
                    {
                        rb.linearVelocityX = direction.x * (moveSpeed / 2);
                    }
                    rb.linearVelocityX = direction.x * (moveSpeed);
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed * 2f, -moveSpeed, moveSpeed));
                }
                if (direction == Vector2.right && rb.linearVelocityX < MaxSpeed)
                {

                    if(rb.linearVelocityX < direction.x * (moveSpeed/2))
                    {
                        rb.linearVelocityX = direction.x * (moveSpeed / 2);
                    }
                    rb.linearVelocityX = direction.x * (moveSpeed);
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed * 2f, -moveSpeed, moveSpeed));
                }
            }
            else
            {
                if (direction == Vector2.left && rb.linearVelocityX > -MaxSpeed)
                {
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed, -moveSpeed, moveSpeed));
                }
                if (direction == Vector2.right && rb.linearVelocityX < MaxSpeed)
                {
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed, -moveSpeed, moveSpeed));
                }
            }
            
        }
        else
        {
            if(rb.linearVelocityX != 0)
            {
                rb.AddForceX(-moveInput.x);
                if(moveInput == Vector2.left)
                {
                    if(rb.linearVelocityX > 0) 
                    {
                        rb.linearVelocityX = 0;
                    }
                    if(rb.linearVelocityX < -(moveSpeed/2) && isGrounded)
                    {
                        rb.linearVelocityX = -(moveSpeed/2);
                    }
                }
                if(moveInput == Vector2.right)
                {
                    if(rb.linearVelocityX < 0) 
                    {
                        rb.linearVelocityX = 0;
                    }
                    if(rb.linearVelocityX > (moveSpeed/2) && isGrounded)
                    {
                        rb.linearVelocityX = (moveSpeed/2);
                    }
                }
            }
        }
        if (inputActions.Player.Jump.IsPressed() && isGrounded)
        {
            Vector2 currentVelocity = rb.linearVelocity; // 최신 스타일 명칭 (또는 velocity)
            rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);
            isGrounded = false;
        }

        //플레이어 아래로 레이캐스트 발사
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f);
        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            //바닥에 닿았는지 확인
            if (hit.collider.transform.up.y > 0)
                isGrounded = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
