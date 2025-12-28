using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float MaxSpeed = 8.5f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravity_Scale = 10f;

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
            if (isGrounded)
            {
                if (direction == Vector2.left && rb.linearVelocityX > -MaxSpeed)
                {
                    if (rb.linearVelocityX > direction.x * (moveSpeed / 2))
                    {
                        rb.linearVelocityX = direction.x * (moveSpeed / 2);
                    }
                    rb.linearVelocityX = direction.x * (moveSpeed);
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed * 2f, -moveSpeed, moveSpeed));
                }
                if (direction == Vector2.right && rb.linearVelocityX < MaxSpeed)
                {

                    if (rb.linearVelocityX < direction.x * (moveSpeed / 2))
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
            if (rb.linearVelocityX != 0)
            {
                rb.AddForceX(-moveInput.x);
                if (moveInput == Vector2.left)
                {
                    if (rb.linearVelocityX > 0)
                    {
                        rb.linearVelocityX = 0;
                    }
                    if (rb.linearVelocityX < -(moveSpeed / 2) && isGrounded)
                    {
                        rb.linearVelocityX = -(moveSpeed / 2);
                    }
                }
                if (moveInput == Vector2.right)
                {
                    if (rb.linearVelocityX < 0)
                    {
                        rb.linearVelocityX = 0;
                    }
                    if (rb.linearVelocityX > (moveSpeed / 2) && isGrounded)
                    {
                        rb.linearVelocityX = (moveSpeed / 2);
                    }
                }
            }
        }

        //점프
        if (inputActions.Player.Jump.IsPressed() && isGrounded)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);
            isGrounded = false;
        }

        CheckGround();
    }
    /*


    private void Update()
    {
        //이동
        if (inputActions.Player.Move.IsPressed())
        {
            Vector2 direction = inputActions.Player.Move.ReadValue<Vector2>();
            moveInput = direction;
        }
        else
        {
            moveInput = Vector2.zero;
        }

        //점프
        if (inputActions.Player.Jump.IsPressed() && isGrounded)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);
            isGrounded = false;
        }

        CheckGround();
        //땅에 있지 않으면 중력 적용
        //rb.gravityScale = isGrounded ? 1f : gravity_Scale;

    }
    private void FixedUpdate()
    {
        float targetSpeed = moveInput.x * moveSpeed;

        float accelrate = Mathf.Abs(targetSpeed) > 0.01f ?  20f: 10f;

        float newx = Mathf.MoveTowards(rb.linearVelocityX, targetSpeed, accelrate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newx, rb.linearVelocityY);

        if (isGrounded)
        {
            if (moveInput == Vector2.left && rb.linearVelocityX > -MaxSpeed)
            {
                if (rb.linearVelocityX > moveInput.x * (moveSpeed / 2))
                {
                    rb.linearVelocityX = moveInput.x * (moveSpeed / 2);
                }
                rb.linearVelocityX = moveInput.x * (moveSpeed);
                rb.AddForceX(Mathf.Clamp(moveInput.x * moveSpeed * 2f * Time.deltaTime, -moveSpeed, moveSpeed));
            }
            if (moveInput == Vector2.right && rb.linearVelocityX < MaxSpeed)
            {

                if (rb.linearVelocityX < moveInput.x * (moveSpeed / 2))
                {
                    rb.linearVelocityX = moveInput.x * (moveSpeed / 2);
                }
                rb.linearVelocityX = moveInput.x * (moveSpeed);
                rb.AddForceX(Mathf.Clamp(moveInput.x * moveSpeed * 2f * Time.deltaTime, -moveSpeed, moveSpeed));
            }
        }
        else
        {
            if (moveInput == Vector2.left && rb.linearVelocityX > -MaxSpeed)
            {
                rb.AddForceX(Mathf.Clamp(moveInput.x * moveSpeed * Time.deltaTime, -moveSpeed, moveSpeed));
            }
            if (moveInput == Vector2.right && rb.linearVelocityX < MaxSpeed)
            {
                rb.AddForceX(Mathf.Clamp(moveInput.x * moveSpeed * Time.deltaTime, -moveSpeed, moveSpeed));
            }
        }
        if(moveInput == Vector2.zero)
        {
            if (rb.linearVelocityX != 0)
            {
                rb.AddForceX(-moveInput.x);
                if (moveInput == Vector2.left)
                {
                    if (rb.linearVelocityX > 0)
                    {
                        rb.linearVelocityX = 0;
                    }
                    if (rb.linearVelocityX < -(moveSpeed / 2) && isGrounded)
                    {
                        rb.linearVelocityX = -(moveSpeed / 2);
                    }
                }
                if (moveInput == Vector2.right)
                {
                    if (rb.linearVelocityX < 0)
                    {
                        rb.linearVelocityX = 0;
                    }
                    if (rb.linearVelocityX > (moveSpeed / 2) && isGrounded)
                    {
                        rb.linearVelocityX = (moveSpeed / 2);
                    }
                }
            }
        } 
    }
    */
    private void CheckGround()
    {
        //플레이어 아래로 레이캐스트 발사
        int ground_layer = LayerMask.GetMask("Ground");
        
        if(rb.linearVelocityY > 0)
        {
            return;
        }

        //바로 아래 발사
        RaycastHit2D hit    =   Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - transform.localScale.y/2), Vector2.down,             0.2f, ground_layer);
        //대각선왼쪽아래 발사
        RaycastHit2D hit_DL =   Physics2D.Raycast(new Vector2(transform.position.x - transform.localScale.x/2, transform.position.y - transform.localScale.y / 2), Vector2.down,   0.2f, ground_layer);
        //대각선 오른쪽아래 발사
        RaycastHit2D hit_DR =   Physics2D.Raycast(new Vector2(transform.position.x + transform.localScale.x / 2, transform.position.y - transform.localScale.y / 2), Vector2.down,    0.2f, ground_layer);

        Debug.DrawRay(new Vector2(transform.position.x, transform.position.y - transform.localScale.y / 2), Vector2.down * 0.2f);
        Debug.DrawRay(new Vector2(transform.position.x - transform.localScale.x / 2, transform.position.y - transform.localScale.y / 2), Vector2.down * 0.2f);
        Debug.DrawRay(new Vector2(transform.position.x + transform.localScale.x / 2, transform.position.y - transform.localScale.y / 2), Vector2.down * 0.2f);

        //레이캐스트가 아래 혹은 대각선 왼쪽아래 혹은 대각선오른쪽아래에 충돌 하고
        //캐릭터가 떨어지고 있고
        //캐릭터가 공중에 있을때
        if ((CheckRayCast(hit) || CheckRayCast(hit_DL) || CheckRayCast(hit_DR)) && rb.linearVelocityY <= 0 && !isGrounded)
        {
            Debug.Log("레이캐스트 충돌 확인");
            isGrounded = true;
        }
    }

    private bool CheckRayCast(RaycastHit2D raycast_hit)
    {
        if (raycast_hit.collider != null)
        {
            if (raycast_hit.transform.position.y + raycast_hit.transform.localScale.y/2 > transform.position.y - transform.localScale.y / 2)
            {
                return false;
            }
            if (raycast_hit.normal.y > 0.9f)
            {
                return true;
            }
        }
        return false;
    }

    //땅에서 떨어지면 공중판정
    private void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
