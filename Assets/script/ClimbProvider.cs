using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;

public class ClimbProvider : MonoBehaviour
{
    [Header("손 설정")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;

    [Header("중력 및 바닥 체크 설정")]
    public bool useGravity = true;
    public float gravity = 9.81f;
    public float floorCheckDistance = 0.1f; // [보정] 이미 CharacterController가 있으므로 레이 길이를 줄입니다.
    public LayerMask floorLayer;

    [Header("리스폰 설정")]
    public float respawnDelay = 3.0f;

    [Header("클라이밍 배율")]
    public float climbMultiplier = 1.0f;

    private XROrigin xrOrigin;
    private CharacterController characterController; // [추가] 캐릭터 컨트롤러 변수
    private ClimbingHand activeHand;
    private Vector3 lastHandWorldPos;
    private Vector3 fallVelocity;
    private Vector3 lastCheckpointPos;
    private bool isRespawning = false;
    private bool isClimbing = false;
    private Coroutine respawnCoroutine;

    void Start()
    {
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
            xrOrigin = FindAnyObjectByType<XROrigin>();

        if (xrOrigin != null)
        {
            // XR Origin에 붙어있는 CharacterController를 가져옵니다.
            characterController = xrOrigin.GetComponent<CharacterController>();
        }

        if (xrOrigin == null || characterController == null)
        {
            Debug.LogError("[ClimbProvider] XROrigin 또는 CharacterController를 찾을 수 없습니다!");
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
                lastHandWorldPos = GetHandTrackedPosition(activeHand);
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

        if (xrOrigin.transform.position.y < -30f)
        {
            ResetToSafety();
        }
    }

    ClimbingHand GetCurrentHand()
    {
        // 현재 잡고 있는 손이 있다면 그 손을 우선 유지
        if (activeHand != null && activeHand.isGrabbing)
            return activeHand;

        // 새로 잡은 손이 있는지 체크
        if (rightHand != null && rightHand.isGrabbing)
            return rightHand;
        if (leftHand != null && leftHand.isGrabbing)
            return leftHand;

        return null;
    }

    Vector3 GetHandTrackedPosition(ClimbingHand hand)
    {
        if (xrOrigin != null)
        {
            return xrOrigin.transform.InverseTransformPoint(hand.transform.position);
        }
        return hand.transform.position;
    }

    void PerformClimb()
    {
        if (activeHand == null || characterController == null) return;

        Vector3 currentHandPos = GetHandTrackedPosition(activeHand);
        Vector3 delta = currentHandPos - lastHandWorldPos;

        if (delta.sqrMagnitude > 0.000001f)
        {
            Vector3 worldDelta = xrOrigin.transform.TransformDirection(delta);
            Vector3 move = -worldDelta * climbMultiplier;

            Debug.Log($"[ClimbProvider] 캐릭터 이동 시도! 이동 거리: {move}");
            characterController.Move(move);
        }

        lastHandWorldPos = currentHandPos;
    }

    void ApplyGravity()
    {
        if (characterController == null) return;

        // [수정] 내장 함수인 isGrounded를 활용하여 바닥 체크를 더 정확하게 바꿉니다.
        if (characterController.isGrounded)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
        }
        else
        {
            fallVelocity.y -= gravity * Time.deltaTime;

            // 중력 적용 시에도 CharacterController를 통해 이동합니다.
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

        if (!isClimbing && activeHand == null)
        {
            ResetToSafety();
        }

        isRespawning = false;
        respawnCoroutine = null;
    }

    void ResetToSafety()
    {
        fallVelocity = Vector3.zero;

        Vector3 cameraOffset = xrOrigin.Camera.transform.position - xrOrigin.transform.position;
        cameraOffset.y = 0;

        Vector3 targetSpawnPos = lastCheckpointPos - cameraOffset;
        targetSpawnPos.y += 0.3f;

        // 텔레포트 시에는 CharacterController를 잠시 꺼두어야 좌표가 제대로 씹히지 않고 이동합니다.
        characterController.enabled = false;
        xrOrigin.transform.position = targetSpawnPos;
        characterController.enabled = true;

        StopRespawn();
        Debug.Log($"[ClimbProvider] 리스폰 완료: {targetSpawnPos}");
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
            lastCheckpointPos = xrOrigin.transform.position;
            Debug.Log($"[ClimbProvider] 체크포인트 갱신: {lastCheckpointPos}");
        }
    }
}