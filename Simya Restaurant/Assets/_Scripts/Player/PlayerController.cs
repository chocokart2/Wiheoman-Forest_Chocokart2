using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어의 현재 상태를 저장하는 열거자입니다.
public enum EPlayerStatus
{
    none,
    idle,
    dead,
    moving,
    interaction,
}

public enum EPlayerAction
{
    empty,
    lightAttack,
    heavyAttack,
}

/// <summary>
///     플레이어 공격 액션(강공격/약공격/ 화살공격)을 실행할때 필요한 매개변수 목록입니다.
/// </summary>
[System.Serializable]
public struct AttackExecutionTupule
{
    public GameObject hitBox;
    public Vector3 attackPos;
    public float actionTime;
    string animationName;
    float attackRangeSummonTime;
}

public class PlayerController : MonoBehaviour
{
    static public PlayerController instance;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float acceleration = 2f;   // 가속도 계수 (감속 구현하려고 만든 변수) / 값이 클수록 빠르게 변함
    [SerializeField] private float jumpForce = 10f;
    private float playerInput;
    private Rigidbody playerRb;

    // 지면 체크 및 경사면 변수
    [System.Serializable]
    public class SlopeVariable
    {
        public bool isGround = true;
        public float slopeAngle;
        public float slopeAngleLimit;
        public float gravity;
        public float verticalVelocity;
        public float groundCastRadius;
        public float groundCheckDistance;
        public float groundCheckThreshold;
        public LayerMask groundLayer;
        public Vector3 groundCross;

        [HideInInspector]public float capsuleRadiusDiff;
    }
    [Header("Slope")]
    [SerializeField] private SlopeVariable _slopeVariable = new SlopeVariable();
    private SlopeVariable SV => _slopeVariable;

    // 플레이어 상태 변수
    private EPlayerStatus currentPlayerStatus;
#warning isAttacking 변수 삭제할 것
    private bool isAttacking = false;

    // 플레이어 방향
    private int playerLookingDirection = 1; // 안타깝게도 playerDirection만으로 플레이어가 현재 바라보는 방향을 저장할 수 없습니다. 해봤어요.
    private const int LOOK_RIGHT = 1;
    private const int LOOK_LEFT = -1;

    [Space (10f)]
    [Header("Light Attack")]
    // 플레이어 약공격 파트
    [SerializeField] private List<AttackExecutionTupule> HitboxAttackLight;
    [SerializeField] private float comboAttackResetTime;
    [SerializeField] private float comboAttackFinalDelay;
    private Coroutine lightAttackResetCoroutine;
    private int lightAttackCombo = 0;

    // 플레이어 강공격 파트
    [Space (5f)]
    [Header("Heavy Attack")]
    [SerializeField] private AttackExecutionTupule HitboxAttackHeavy;
    private Coroutine heavyAttackCoroutine;
    [SerializeField] private float heavyAttackChargeTime;
    private bool isHeavyAttackReady = false;

    // 플레이어 애니메이션
    private Animator animatorController;

    // 플레이어 체력 정보
    [SerializeField] public float health;
    public bool IsDead { get; private set; }

    // 임시 변수
    private string BaseLayer = "Base Layer.";

    // 시간 담당 변수
    bool isInputAllowed = true;
    bool isPlayerDoing = false;
    System.Action nextAct = null;
    [SerializeField] private float prePressAllowTime = 0.5f;
    Coroutine inputBlockingCoroutine;

    // 상호 작용 변수
    List<InteractiveObjectBase> approchableInteractives;
    int index = 0;

    /// <summary>
    ///     플레이어의 공격받은 것을 구현하는 함수입니다.
    /// </summary>
    /// <param name="damage"> 데미지의 양 </param>
    /// <param name="knockBackDirection"></param>
    /// <param name="force"></param>
    public void BeAttacked(float damage, Vector3 knockBackDirection, float force)
    {
        health -= damage;

        if (health <= 0.0f && (IsDead == false))
        {
            DoDeathHandle();
        }

        playerRb.AddForce(knockBackDirection.normalized * force, ForceMode.Impulse);
    }
    
    public void AddInteractive(InteractiveObjectBase target)
    {
        if (m_FindInteractive(target) != -1)
        {
            return;
        }
        approchableInteractives.Add(target);
    }

    public void RemoveInteractive(InteractiveObjectBase target)
    {
        int index = m_FindInteractive(target);
        if (index == -1)
        {
            return;
        }
        approchableInteractives.RemoveAt(index);
    }

    private void DoDeathHandle()
    {
        // 나중에 여기 구현하세요.
        IsDead = true;
        currentPlayerStatus = EPlayerStatus.dead;
        animatorController.CrossFade($"Dead", 0.15f);
    }

    private void Awake()
    {
        approchableInteractives = new List<InteractiveObjectBase>(32);

    }

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (playerRb == null)
        {
            playerRb = GetComponent<Rigidbody>();
        }

        EnemyBase.SetPlayer(gameObject);
        //animatorController = transform.Find("Armature").GetComponent<Animator>();
        
        // 함수 정상 진행 조건
        Debug.Assert(playerRb != null);
        //Debug.Assert(animatorController != null);

        SV.capsuleRadiusDiff = GetComponent<CapsuleCollider>().radius - SV.groundCastRadius + 0.05f;
    }

    void Update()
    {
        Move();
        Jump();

        //DoAttackLight();
        //DoAttackHeavy(); //여기 바로 윗줄 포함 주석해제
        //DoInteractive();
        //
        //TEMP_PlayAnimate();
    }

    private void FixedUpdate()
    {
        GroundCheck();
        Gravity();
    }

    void Move()
    {
        if (IsDead)
        {
            return;
        }

        playerInput = Input.GetAxis("Horizontal");
        Vector3 currentVelocity = playerRb.velocity;
        Vector3 targetVelocity = Vector3.zero;
        targetVelocity.x = playerInput * moveSpeed;

        Vector3 horizontalVelocity = Vector3.zero;

        //Vector3 horizontalVelocity = Quaternion.AngleAxis(-SV.slopeAngle, SV.groundCross)    //경사면 움직임 코드
        //                                * Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (SV.slopeAngle < SV.slopeAngleLimit)
        {
            horizontalVelocity = Quaternion.AngleAxis(-SV.slopeAngle, SV.groundCross) * targetVelocity;
        }
        playerRb.velocity = horizontalVelocity + Vector3.up * SV.verticalVelocity;
    }

    void GroundCheck()
    {
        float groundDistance = float.MaxValue;
        Vector3 groundNormal = Vector3.up;
        SV.slopeAngle = 0f;
        SV.isGround = false;

        Vector3 groundCastPos = transform.position + new Vector3(0f, SV.groundCastRadius + 0.05f, 0f);

        bool cast = Physics.SphereCast(groundCastPos, SV.groundCastRadius, Vector3.down, out var hit, SV.groundCheckDistance, SV.groundLayer);
        if (cast)
        {
            groundNormal = hit.normal;
            groundDistance = Mathf.Max(hit.distance - SV.capsuleRadiusDiff - SV.groundCheckThreshold, -10f);
            Debug.Log(hit.distance + " / " + groundDistance);
            SV.isGround = groundDistance <= 0.0001f;

            if (SV.isGround)
            {
                SV.slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            }
        }

        SV.groundCross = Vector3.Cross(groundNormal, Vector3.up);
    }

    private void Gravity()
    {
        if (SV.isGround)
        {
            SV.verticalVelocity = 0f;
        }
        else
        {
            SV.verticalVelocity += Time.fixedDeltaTime * SV.gravity;
        }
    }

    void Jump()
    {
        if (IsDead)
        {
            return;
        }
        if(Input.GetKeyDown(KeyCode.Space) && SV.isGround) 
        {
            playerRb.velocity = Vector3.zero;
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            SV.isGround = false;
        }
    }

    /// <summary>
    ///     약공격 실행 함수
    /// </summary>
    /// <remarks>
    ///     SRP 원칙을 지켜주세요 : 이 함수는 너무 무책임해서 약공격 실행만 책임집니다.
    /// </remarks>
    void DoAttackLight()
    {
        ReadyAttackLight();

        // 빈 현재 행동에 nextAct를 집어넣음
        if (nextAct != null && (isPlayerDoing == false)) // 이때 현재 행동중이 아니여야 함
        {
            nextAct();
            nextAct = null;
        }
    }

    /// <summary>
    ///     조건에 맞는 상태인 경우, 약공격 액션을 준비합니다.
    /// </summary>
    void ReadyAttackLight()
    {
        // 얼리 리턴 패턴을 사용하기 위해 함수 블록을 따로 빼두었습니다.
        if (currentPlayerStatus == EPlayerStatus.interaction ||
            currentPlayerStatus == EPlayerStatus.dead
            // 그 외 각종 상태는 허용
            )
        {
            return;
        }
        if (isInputAllowed == false)
        {
            //Debug.Log("공격 입력을 받을 수 없음");
            return;
        }
        if (Input.GetButtonDown("AttackLight") == false)
        {
            //Debug.Log("공격 입력 안함");
            return;
        }
        if (nextAct != null)
        {
            //Debug.Log("현재 행동 중 + 이미 예약이 가득 참");
            return;
        }
        // nextAct에 값 쑤셔넣기
        nextAct = () =>
        {
            if (lightAttackResetCoroutine != null)
            {
                StopCoroutine(lightAttackResetCoroutine);
            }
            lightAttackCombo++;

            Debug.Assert(
                lightAttackCombo >= 1 && lightAttackCombo <= 3,
                "PlayerController.DoAttackLight : 유효하지 않은 lightAttackCombo 값.");
            animatorController.CrossFade($"LightAttackCombo{lightAttackCombo}", 0.15f);

            if (lightAttackCombo < 3)
            {
                lightAttackResetCoroutine = StartCoroutine(
                    RunAfterDelay(
                        HitboxAttackLight[lightAttackCombo - 1].actionTime + comboAttackResetTime,
                        () => { lightAttackCombo = 0; }
                        )
                    );

                MakeHitbox(HitboxAttackLight[lightAttackCombo - 1]);
                inputBlockingCoroutine = BlockInputForSeconds(HitboxAttackLight[lightAttackCombo - 1].actionTime);
            }
            else
            {
                lightAttackCombo = 0;
                lightAttackResetCoroutine = StartCoroutine(
                    RunAfterDelay(
                        comboAttackFinalDelay,
                        () =>
                        {
                            MakeHitbox(HitboxAttackLight[2]);
                            inputBlockingCoroutine = BlockInputForSeconds(HitboxAttackLight[2].actionTime);
                        }
                        )
                    );
            }
            nextAct = null;
        };
    }

    /// <summary>
    ///     일정시간동안 입력을 받지 못하게 합니다.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    Coroutine BlockInputForSeconds(float time)
    {
        isInputAllowed = false;
        isPlayerDoing = true;
        
        return StartCoroutine(
            RunAfterDelay(
                (time - prePressAllowTime, () => { isInputAllowed = true; }),
                (prePressAllowTime, () => { isPlayerDoing = false; })
                )
            );
    }

    void DoAttackHeavy()
    {
        // 민혁씨 인용) 상호작용 시에는 강공격 차징 풀리게 할 것 같네욘
        if (currentPlayerStatus == EPlayerStatus.interaction ||
            currentPlayerStatus == EPlayerStatus.dead)                      // 그 외 각종 상태는 허용
        {
            return;
        }
        if (isInputAllowed == false)
        {
            return;
        }

        if (Input.GetButtonDown("AttackHeavy"))
        {
            heavyAttackCoroutine
                = StartCoroutine(
                    RunAfterDelay(
                        heavyAttackChargeTime,
                        () => {
                            isHeavyAttackReady = true;
                        }
                        )
                    );
        }
        if (Input.GetButtonUp("AttackHeavy") == false)
        {
            return;
        }

        if (isHeavyAttackReady)
        {
            animatorController.CrossFade("HeavyAttack", 0.15f);
            MakeHitbox(HitboxAttackHeavy);
            inputBlockingCoroutine = BlockInputForSeconds(HitboxAttackHeavy.actionTime);
        }
        else
        {
            if (heavyAttackCoroutine != null)
            {
                StopCoroutine(heavyAttackCoroutine);
            }
        }
    }

    void DoInteractive()
    {
        if (Input.GetButtonDown("Interactive") == false)
        {
            return;
        }
        if (approchableInteractives.Count == 0)
        {
            Debug.Log("인터랙티브 할 수 있는 대상이 없음");
            return;
        }
        Debug.Log("인터랙티브 진행");

        InteractiveObjectBase[] sortedArray = approchableInteractives.ToArray();
        UtilityFunctions.Sort(
            ref sortedArray,
            (InteractiveObjectBase left, InteractiveObjectBase right) =>
            {
                return (left.transform.position - transform.position).sqrMagnitude >
                    (right.transform.position - transform.position).sqrMagnitude;
            }
            );

        sortedArray[0].DoInteractiveWithThis();
    }

    /// <summary>
    ///     대기 시간 후 호출할 함수의 일련을 준비합니다.
    /// </summary>
    /// <param name="actionSequences">
    ///     대기 시간과 대기시간 후 실행할 무명 메서드의 튜플입니다.
    /// </param>
    /// <returns>
    ///     코루틴을 돌려줍니다.
    /// </returns>
    IEnumerator RunAfterDelay(params (float waitingTime, System.Action nextAction)[] actionSequences)
    {
        // for문을 따로 떼어둔 것은, 호출 시점에서 오류를 미리 잡아두는 것이 가장 좋기 때문입니다.
        for (int index = 0; index < actionSequences.Length; ++index)
        {
            Debug.Assert(
                actionSequences[index].waitingTime >= 0.0f,
                $"오류_PlayerController.RunAfterDelay : {index + 1}번째 시퀀스의 waitingTime이 {actionSequences[index].waitingTime}이며 0보다 작습니다."
                );
            Debug.Assert(
                actionSequences[index].nextAction != null, 
                $"오류_PlayerController.RunAfterDelay : {index + 1}번째 시퀀스의 nextAction이 널 값입니다.");
        }

        for (int index = 0; index < actionSequences.Length; ++index)
        {
            yield return new WaitForSeconds(actionSequences[index].waitingTime);
            actionSequences[index].nextAction();
        }
    }

    /// <summary>
    ///     대기 시간 후 호출할 함수를 준비합니다.
    /// </summary>
    /// <param name="waitingTime">
    ///     대기할 시간입니다.
    /// </param>
    /// <param name="nextAction">
    ///     waitingTime 뒤에 실행할 코드입니다. 무명 메서드를 넣을 수 있고, 혹은 입력과 출력이 void인 함수명을 넣을 수 있습니다.
    /// </param>
    /// <returns>
    ///     코루틴을 돌려줍니다.
    /// </returns>
    IEnumerator RunAfterDelay(float waitingTime, System.Action nextAction)
    {
        return RunAfterDelay((waitingTime, nextAction));
    }

    /// <summary>
    ///     공격 내용을 담은 튜플을 이용해 히트박스 객체를 생성합니다. 1초당 20회 이상 호출되는것을 권장하지 않습니다.
    /// </summary>
    /// <param name="attack"></param>
    void MakeHitbox(AttackExecutionTupule attack)
    {
        Quaternion rotation = attack.hitBox.transform.rotation;
        if (playerLookingDirection == LOOK_RIGHT)
        {
            rotation.eulerAngles += new Vector3(0, 180, 0);
        }
        Instantiate(
            attack.hitBox,
            new Vector3(
                transform.position.x + attack.attackPos.x * playerLookingDirection,
                transform.position.y + attack.attackPos.y,
                transform.position.z + attack.attackPos.z),
            rotation);
    }

    void TEMP_PlayAnimate()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            animatorController.Play($"{BaseLayer}LightAttackCombo1");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            animatorController.Play($"{BaseLayer}LightAttackCombo2");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            animatorController.Play($"{BaseLayer}LightAttackCombo3");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            animatorController.Play($"{BaseLayer}HeavyAttack");
        }
    }

    private int m_FindInteractive(InteractiveObjectBase target)
    {
        for (int index = 0; index < approchableInteractives.Count; ++index)
        {
            if (ReferenceEquals(approchableInteractives[index], target))
            {
                return index;
            }
        }
        return -1;
    }
}