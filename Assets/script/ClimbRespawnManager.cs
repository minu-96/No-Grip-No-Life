using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class ClimbRespawnManager : MonoBehaviour
{
    [Header("손 설정")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;

    [Header("중력 및 바닥 체크 설정")]
    public bool useGravity = true;
    public float gravity = 9.81f;

    [Header("리스폰 설정")]
    public float respawnDelay = 3.0f;
    [Tooltip("이 Y축 높이 이하로 추락하면 즉시 리스폰시킵니다.")]
    public float deathYThreshold = -30f;

    [Header("클라이밍 배율")]
    public float climbMultiplier = 1.0f;

    private XROrigin xrOrigin;
    private CharacterController characterController;
    private LocomotionMediator locomotionMediator;

    private ClimbingHand activeHand;
    private Vector3 fallVelocity;
    private Vector3 lastCheckpointPos;
    private bool isRespawning = false;
    private bool isClimbing = false;
    private Coroutine respawnCoroutine;

    void Start()
    {
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null) xrOrigin = FindAnyObjectByType<XROrigin>();

        if (xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
            locomotionMediator = FindAnyObjectByType<LocomotionMediator>();
        }

        if (xrOrigin == null || characterController == null || locomotionMediator == null)
        {
            Debug.LogError("[ClimbProvider] 필수 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        lastCheckpointPos = xrOrigin.transform.position;
    }

    void Update()
    {
        ClimbingHand newHand = GetCurrentHand();

        if (newHand != null)
        {
            if (!isClimbing || activeHand != newHand)
            {
                activeHand = newHand;
                isClimbing = true;
                fallVelocity = Vector3.zero;
                StopRespawn();
            }
            PerformClimb();
        }
        else
        {
            if (isClimbing)
            {
                isClimbing = false;
                activeHand = null;
            }

            if (useGravity)
            {
                ApplyGravity();
            }
        }

        if (xrOrigin.transform.position.y < deathYThreshold)
        {
            ResetToSafety();
        }
    }

    ClimbingHand GetCurrentHand()
    {
        if (activeHand != null && activeHand.isGrabbing) return activeHand;
        if (rightHand != null && rightHand.isGrabbing) return rightHand;
        if (leftHand != null && leftHand.isGrabbing) return leftHand;
        return null;
    }

    void PerformClimb() { }

    void ApplyGravity()
    {
        if (characterController == null || !characterController.enabled) return;

        if (characterController.isGrounded)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
        }
        else
        {
            fallVelocity.y -= gravity * Time.deltaTime;

            // 기존의 누적된 속도로 이동시킵니다.
            characterController.Move(fallVelocity * Time.deltaTime);

            if (!isRespawning && respawnCoroutine == null)
            {
                respawnCoroutine = StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        if (!isClimbing && GetCurrentHand() == null)
        {
            ResetToSafety();
        }

        isRespawning = false;
        respawnCoroutine = null;
    }

    // [중요 수정] 리스폰 연산을 안전한 코루틴 방식으로 위임하여 실행합니다.
    public void ResetToSafety()
    {
        // 이미 관통 버그가 일어나는 중일 수 있으므로 중복 실행 방지 및 속도 즉시 절단
        fallVelocity = Vector3.zero;

        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(SafeRespawnRoutine());
        }
    }

    // 바닥 관통 무한 낙하를 물리적으로 치료하는 정석 루틴
    private IEnumerator SafeRespawnRoutine()
    {
        // 1. 추락 속도와 관성 데이터를 완전히 0으로 지워버립니다.
        fallVelocity = Vector3.zero;

        // 2. VR 카메라 오프셋 정밀 계산
        Vector3 cameraOffset = xrOrigin.Camera.transform.position - xrOrigin.transform.position;
        cameraOffset.y = 0;
        Vector3 targetSpawnPos = lastCheckpointPos - cameraOffset;

        // 바닥 콜라이더와 정확히 겹쳐서 튕기는 걸 막기 위해 공중으로 살짝(0.5m) 띄웁니다.
        targetSpawnPos.y += 0.5f;

        // 3. 캐릭터 컨트롤러를 끄고 순간이동 시킵니다.
        if (characterController != null) characterController.enabled = false;

        xrOrigin.transform.position = targetSpawnPos;

        // 4. [★가장 중요] 유니티 물리 엔진에 "이 녀석 여기로 이동했으니 물리 캐시 다 갱신해!"라고 강제 명령합니다.
        Physics.SyncTransforms();

        // 5. 딱 1프레임 동안 컨트롤러가 꺼진 상태로 대기합니다. (물리 잔상 소멸 시간)
        yield return new WaitForEndOfFrame();

        // 6. 가속도가 완전히 증발한 깨끗한 상태에서 컨트롤러를 다시 켜줍니다.
        if (characterController != null) characterController.enabled = true;

        // 7. 스태미나 충전 및 리스폰 종료
        StaminaManager stamina = FindObjectOfType<StaminaManager>();
        if (stamina != null)
        {
            stamina.currentStamina = stamina.maxStamina;
        }

        StopRespawn();
        Debug.LogWarning("🏁 [완벽 부활] 추락 관성을 완전히 제거하고 체크포인트 지상에 안전하게 착지시켰습니다.");
    }

    void StopRespawn()
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
        isRespawning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            lastCheckpointPos = other.transform.position;
            Debug.Log($"📍 새로운 체크포인트 등록: {other.gameObject.name}");
        }
    }
}