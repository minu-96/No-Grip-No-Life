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

    [Header("클라이밍 배율")]
    public float climbMultiplier = 1.0f;

    private XROrigin xrOrigin;
    private CharacterController characterController;
    private LocomotionMediator locomotionMediator;

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

                // [핵심] 그랩을 잡은 순간 실제 컨트롤러가 있던 월드 위치를 기록합니다.
                lastHandWorldPos = activeHand.transform.position;
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
        if (activeHand != null && activeHand.isGrabbing)
            return activeHand;

        if (rightHand != null && rightHand.isGrabbing)
            return rightHand;
        if (leftHand != null && leftHand.isGrabbing)
            return leftHand;

        return null;
    }

    void PerformClimb()
    {
        
    }

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
            characterController.Move(fallVelocity * Time.deltaTime);

            if (!isRespawning && respawnCoroutine == null)
            {
                respawnCoroutine = StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    // ... 리스폰 및 체크포인트 로직은 기존과 동일 ...
    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);
        if (!isClimbing && activeHand == null) ResetToSafety();
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

        if (characterController != null) characterController.enabled = false;
        xrOrigin.transform.position = targetSpawnPos;
        if (characterController != null) characterController.enabled = true;
        StopRespawn();
    }

    void StopRespawn()
    {
        if (respawnCoroutine != null) { StopCoroutine(respawnCoroutine); respawnCoroutine = null; }
        isRespawning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint")) lastCheckpointPos = xrOrigin.transform.position;
    }
}