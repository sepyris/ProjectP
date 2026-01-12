using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Threading;
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float MaxSpeed = 8.5f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravity_Scale = 10f;
    [SerializeField] int damage = 10;


    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.right;

    private GameObject currentPlatform = null;

    private bool isGrounded = true;
    private bool canDoubleJump = true;
    private bool isRushing = false;
    private bool isAttack = false;
    private bool isDownAttack = false;
    private bool isSkillAttack = false;
    private bool isCanUseBuff = true;
    private bool isBuff = false;

    private float buffCooldown = 120f;//버프 쿨타임은 2분으로 설정
    private float buffDuration = 30f;//버프 지속은 30초로 설정

    private float lastJumpTime; // 마지막 점프 시간 저장
    public float jumpCooldown = 0.05f; // 점프 간 최소 간격

    private HashSet<MonsterController> hitMonstersThisAttack = new HashSet<MonsterController>();

    [SerializeField] private GameObject skillEffect;



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
        Vector2 direction = inputActions.Player.Move.ReadValue<Vector2>();
        direction.x = direction.x > 0 ? 1 : (direction.x < 0 ? -1 : 0);
        moveInput = direction;
        if (!isRushing && !isDownAttack && !isAttack && !isSkillAttack)
        {
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
        }
        

        //점프키를 누르고 있을때
        if (inputActions.Player.Jump.IsPressed())
        {
            if (isGrounded && !isRushing && !isDownAttack && Time.time - lastJumpTime >= jumpCooldown && moveInput.y >= 0)
            {
                PerformJump();
            }
        }

        // 이단 점프 및 특수 점프 체크 (새로 눌렀을 때만)
        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            // 공중에서만 이단 점프 실행
            if (!isGrounded && !isRushing && !isDownAttack && canDoubleJump && Time.time - lastJumpTime >= jumpCooldown)
            {
                PerformDoubleJump();
            }

            // 아래 키 + 점프 (플랫폼 통과)
            if (!isRushing && !isDownAttack && moveInput.y < 0 && isGrounded && rb.linearVelocityY == 0)
            {
                StartCoroutine(falldown());
            }
        }

        if(inputActions.Player.Rush.IsPressed())
        {
            if (isGrounded && !isRushing && !isDownAttack && !isAttack && !isSkillAttack)
            {
                StartCoroutine(rush());
            }
        }

        if (inputActions.Player.Attack.IsPressed())
        {
            //돌진중이 아닐때 아래로찍기 공격 아닐때
            if(!isRushing && !isDownAttack && !isSkillAttack && !isAttack)
            {
                hitMonstersThisAttack.Clear();
                //기본 공격
                StartCoroutine(Attack());

            }
            //공중에 있고 스킬공격을 하지 않은 상태에서 아래키를 누른 상태에서
            if(!isGrounded && !isSkillAttack && moveInput.y < 0)
            {
                //아래로 찍기 공격
                StartCoroutine(PerformDownAttack());
            }
        }
        //키를 눌러 아래로 공격
        if(inputActions.Player.DownAttack.WasPressedThisFrame())
        {
            //공중에 있을때
            if (!isGrounded && !isSkillAttack)
            {
                //아래로 찍기 공격
                StartCoroutine(PerformDownAttack());
            }
        }
        //스킬 공격
        if (inputActions.Player.SkillAttack.IsPressed())
        {
            //돌진중이 아닐때,기본공격중이 아닐때,아래로찍기 공격 아닐때
            if (!isRushing && !isAttack && !isDownAttack && !isSkillAttack)
            {
                //기본 공격
                hitMonstersThisAttack.Clear();
                StartCoroutine(SkillAttack());

            }
        }
        if (inputActions.Player.Buff.WasPressedThisFrame())
        {

        }

        CheckGround();
    }
    private IEnumerator PerformDownAttack()
    {
        //아래로 찍기 공격
        //여기선 플래그만 설정 실제 공격은 땅 체크 할때 공격
        isDownAttack = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -70f);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator Attack()
    {
        //공격 시작
        isAttack = true;
        LayerMask enemy_layer = LayerMask.GetMask("Enemy");
        int x_Position = 4;
        int y_Position = 3;
        int attack_enemy_Count = 1;
        Vector2 center = (Vector2)transform.position + new Vector2((x_Position / 2) * lastDirection.x , y_Position/2);
        Collider2D[] enemy = Physics2D.OverlapBoxAll(center, new Vector2(x_Position, y_Position), 0f,enemy_layer);
        foreach(var e in enemy)
        {
            if(e!= null)
            {
                MonsterController monster = e.GetComponent<MonsterController>();
                if (monster != null)
                {
                    if (!hitMonstersThisAttack.Contains(monster) && hitMonstersThisAttack.Count <= attack_enemy_Count -1)
                    {
                        int actual_Damage = damage;
                        monster.TakeDamage(actual_Damage);
                    }
                    hitMonstersThisAttack.Add(monster);
                }
            }
        }
        //딜레이를 넣어서 공격 상태유지
        yield return  new WaitForSeconds(1f);
        //공격종료
        isAttack = false;
    }
    private IEnumerator SkillAttack()
    {
        //공격 시작
        isSkillAttack = true;
        float attack_delay = 0.5f;
        int attack_enemy_Count = 15;
        float attackDamageRate = 200f;
        int attackCount = 10;
        int x_Position = 10;
        int y_Position = 6;
        float xRate = 1f;
        float yRate = 1f;
        //총 데미지는 2000%
        if (isBuff)
        {
            attack_delay = 0.2f;
            attack_enemy_Count = 20;
            attackDamageRate = 500f;
            attackCount = 20;
            xRate = 1.5f;
            yRate = 1.5f;
            //총데미지는 10000%
        }

        LayerMask enemy_layer = LayerMask.GetMask("Enemy");
        Vector2 center = transform.position;
        Collider2D[] enemy = Physics2D.OverlapBoxAll(center, new Vector2(x_Position * xRate, y_Position * yRate), 0f, enemy_layer);
        SpawnEffect(transform.position);
        
        foreach (var e in enemy)
        {
            //공격 횟수시행
            for(int j = 0;j<attack_enemy_Count;j++)
            {
                MonsterController monster = e.GetComponent<MonsterController>();
                if(monster != null)
                {
                    for (int i = 0; i < attackCount; i++)
                    {
                        if (!hitMonstersThisAttack.Contains(monster) && hitMonstersThisAttack.Count <= attack_enemy_Count - 1)
                        {
                            int actual_Damage = Mathf.FloorToInt(damage * (attackDamageRate / 100f));
                            monster.TakeDamage(actual_Damage);
                            
                        }
                    }
                    hitMonstersThisAttack.Add(monster);
                }
            }
        }
        //딜레이를 넣어서 공격 상태유지
        yield return new WaitForSeconds(attack_delay);
        //공격종료
        isSkillAttack = false;
    }

    private void SpawnEffect(Vector2 spawn_position)
    {
        float effectAngle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
        bool isFacingLeft = lastDirection.x < 0;
        GameObject effect = Instantiate(skillEffect,spawn_position, Quaternion.Euler(0, 0, effectAngle));
        if(isFacingLeft)
        {
            Vector3 scale = effect.transform.localScale;
            scale.y *= -1;
            effect.transform.localScale = scale;
        }
        effect.transform.parent = gameObject.transform;
        Animator animator = effect.GetComponent<Animator>();
        if(animator != null)
        {
            float delete_time = GetAnimationLength(animator);
            Destroy(effect, delete_time);
        }
        else
        {
            Destroy(animator, 0.5f);
        }
    }
    private float GetAnimationLength(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller.animationClips.Length > 0)
        {
            return controller.animationClips[0].length;
        }

        return 0.5f;
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

    private IEnumerator rush()
    {
        //이동거리
        float move_Distance = 10f;
        if (currentPlatform != null)
        {
            isRushing = true;
            //목적지 = 현위치 에서 10정도 거리 정도
            float destinationx = transform.position.x + move_Distance * lastDirection.x;
            if (lastDirection == Vector2.left)
            {
                //10정도거리보다 현재 플랫폼길이가 짧으면
                if(currentPlatform.transform.position.x - (currentPlatform.transform.localScale.x/2) + (transform.localScale.x/2) > transform.position.x- move_Distance)
                {
                    //거리를 플랫폼길이로 설정
                    destinationx = currentPlatform.transform.position.x - (currentPlatform.transform.localScale.x / 2) + (transform.localScale.x / 2);
                }
            }
            if(lastDirection == Vector2.right)
            {
                if (currentPlatform.transform.position.x + (currentPlatform.transform.localScale.x / 2) - (transform.localScale.x / 2) < transform.position.x + move_Distance)
                {
                    destinationx = currentPlatform.transform.position.x + (currentPlatform.transform.localScale.x / 2) - (transform.localScale.x / 2);
                }
            }
            while (Vector2.Distance(transform.position,new Vector2(destinationx,transform.position.y)) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(destinationx, transform.position.y), 30f*Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
            isRushing = false;
        }
        else
        {
            yield return null;
        }
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
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 12);

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
            //만약 아래로 찍기 공격일시
            DownAttack();
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
                DownAttack();
            }
        }
    }

    private void DownAttack()
    {
        //만약 아래로 찍기 공격일시
        if (isDownAttack)
        {
            Collider[] enemy = new Collider[3];
            LayerMask enemy_layer = LayerMask.GetMask("Enemy");
            Physics.OverlapBoxNonAlloc(transform.forward, new Vector3(5, 3, transform.position.z), enemy, transform.rotation, enemy_layer);
            foreach (var e in enemy)
            {
                //TODO:실제 몬스터 공격
            }
            isDownAttack = false;
        }
    }
}
