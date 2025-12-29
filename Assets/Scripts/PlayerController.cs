using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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
    private Vector2 lastDirection = Vector2.right;

    private GameObject currentPlatform = null;

    private bool isGrounded = true;

    private float lastJumpTime; // 마지막 점프 시간 저장
    public float jumpCooldown = 0.05f; // 점프 간 최소 간격
    private bool canDoubleJump = true;

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

        //inputActions.Player.Jump.performed += ctx => OnJump();
    }


    private void Update()
    {
        Vector2 direction = inputActions.Player.Move.ReadValue<Vector2>();
        direction.x = direction.x > 0 ? 1 : (direction.x < 0 ? -1 : 0);
        moveInput = direction;
        if (direction.x != 0) // x축 입력이 있다면 (좌 혹은 우)
        {
            // 마지막 방향 기록 (x값 기준)
            if (direction.x < 0) lastDirection = Vector2.left;
            else if (direction.x > 0) lastDirection = Vector2.right;
            
            if (isGrounded)
            {
                // 왼쪽 입력 중이고 최대 속도보다 느릴 때
                if (direction.x < 0 && rb.linearVelocityX > -MaxSpeed)
                {
                    // 급격한 방향 전환 시 속도 보정
                    if (rb.linearVelocityX > direction.x * (moveSpeed / 2))
                    {
                        rb.linearVelocityX = direction.x * (moveSpeed / 2);
                    }
                    rb.linearVelocityX = direction.x * moveSpeed;
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed * 2f, -moveSpeed, moveSpeed));
                }
                // 오른쪽 입력 중이고 최대 속도보다 느릴 때
                else if (direction.x > 0 && rb.linearVelocityX < MaxSpeed)
                {
                    if (rb.linearVelocityX < direction.x * (moveSpeed / 2))
                    {
                        rb.linearVelocityX = direction.x * (moveSpeed / 2);
                    }
                    rb.linearVelocityX = direction.x * moveSpeed;
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed * 2f, -moveSpeed, moveSpeed));
                }
            }
            else // 공중 상태
            {
                if ((direction.x < 0 && rb.linearVelocityX > -MaxSpeed) ||
                    (direction.x > 0 && rb.linearVelocityX < MaxSpeed))
                {
                    rb.AddForceX(Mathf.Clamp(direction.x * moveSpeed, -moveSpeed, moveSpeed));
                }
            }
        }
        else // 방향키를 뗐을 때 (direction.x == 0)
        {
            if (inputActions.Player.Jump.IsPressed())
            {
                if (rb.linearVelocityX != 0)
                {
                    rb.AddForceX(-(rb.linearVelocityX / 2));

                    // 이전에 누르던 moveInput의 x 성분으로 판단
                    if (moveInput.x < 0) // 왼쪽이었을 때
                    {
                        if (rb.linearVelocityX > 0) rb.linearVelocityX = 0;
                        if (rb.linearVelocityX < -(moveSpeed / 2) && isGrounded)
                        {
                            rb.linearVelocityX = -(moveSpeed / 2);
                        }
                    }
                    else if (moveInput.x > 0) // 오른쪽이었을 때
                    {
                        if (rb.linearVelocityX < 0) rb.linearVelocityX = 0;
                        if (rb.linearVelocityX > (moveSpeed / 2) && isGrounded)
                        {
                            rb.linearVelocityX = (moveSpeed / 2);
                        }
                    }
                }
            }
        }

        //점프키를 누르고 있을때
        if (inputActions.Player.Jump.IsPressed())
        {
            if (isGrounded && Time.time - lastJumpTime >= jumpCooldown && moveInput.y >= 0)
            {
                PerformJump();
            }
        }

        // 이단 점프 및 특수 점프 체크 (새로 눌렀을 때만)
        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            // 공중에서만 이단 점프 실행
            if (!isGrounded && canDoubleJump && Time.time - lastJumpTime >= jumpCooldown)
            {
                PerformDoubleJump();
            }

            // 아래 키 + 점프 (플랫폼 통과)
            if (moveInput.y < 0 && isGrounded && rb.linearVelocityY == 0)
            {
                StartCoroutine(falldown());
            }
        }

        CheckGround();


    }
    // 일반 점프
    private void PerformJump()
    {
        rb.linearVelocityY = jumpForce;
        isGrounded = false;
        lastJumpTime = Time.time;
    }
    //특수 점프
    private void PerformDoubleJump()
    {
        // 위 방향키 + 점프 (높은 이단 점프)
        if (moveInput.y >= 0.5f)
        {
            rb.linearVelocityY = jumpForce * 1.5f;
        }
        //점프후에 한번더 점프시
        else
        {
            if (lastDirection == Vector2.left) rb.linearVelocityX = -moveSpeed * 3;
            if (lastDirection == Vector2.right) rb.linearVelocityX = moveSpeed * 3;
            rb.linearVelocityY = jumpForce / 2;
        }
        canDoubleJump = false;
        lastJumpTime = Time.time;
    }

    private IEnumerator falldown()
    {
        //현재 발판이 있는지 확인
        if(currentPlatform != null)
        {
            //플랫폼 이펙터2D가 있는지 확인
            if (currentPlatform.GetComponent<PlatformEffector2D>() != null)
            {
                //플레이어를 살짝 위로 점프

                Vector2 currentVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector2(currentVelocity.x, 12);

                //플레이어와 발판의 충돌 무시

                gameObject.GetComponent<FrictionJoint2D>().enableCollision = false;
                yield return new WaitForSeconds(0.4f);
                //플레이어 충돌 복원
                gameObject.GetComponent<FrictionJoint2D>().enableCollision = true ;
            }
        }
        else
        {
            yield return null;
        }
    }
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
            canDoubleJump = true;
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
                currentPlatform = raycast_hit.collider.gameObject;
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
            //땅에서 떨어지면 현재 발판 초기화
            if(currentPlatform != null)
            {
                currentPlatform = null;
            }
            isGrounded = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //플랫폼 이펙터2D가 아닌 오브젝트와 충돌했을때
        if (collision.gameObject.GetComponent<PlatformEffector2D>() == null)
        {
            //땅과 충돌했을때
            if (collision.gameObject.CompareTag("Ground"))
            {
                currentPlatform = collision.collider.gameObject;
                isGrounded = true;
                canDoubleJump = true;
            }
        }
    }
}
