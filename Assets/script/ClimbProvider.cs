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
    public float floorCheckDistance = 0.5f;
    public LayerMask floorLayer;

    [Header("리스폰 설정")]
    public float respawnDelay = 3.0f;

    [Header("클라이밍 배율")]
    public float climbMultiplier = 1.0f;

    private XROrigin xrOrigin;
    private ClimbingHand activeHand;
    private Vector3 lastHandWorldPos;
    private Vector3 fallVelocity;
    private Vector3 lastCheckpointPos;
    private bool isRespawning = false;
    private bool isClimbing = false;

    void Start()
    {
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
            xrOrigin = FindAnyObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("[ClimbProvider] 씬에서 XROrigin을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        // 게임 시작 시 처음 위치를 안전한 첫 스폰 지점으로 기억
        lastCheckpointPos = xrOrigin.transform.position;
    }

    void Update()
    {
        ClimbingHand newHand = GetCurrentHand();

        if (newHand != null)
        {
            if (activeHand != newHand)
            {
                activeHand = newHand;
                lastHandWorldPos = GetHandTrackedPosition(activeHand);
                isClimbing = true;
            }

            PerformClimb();
            fallVelocity = Vector3.zero;
            StopRespawn();
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

        // [안전장치] 무한 추락 시 리스폰 강제 호출
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

    Vector3 GetHandTrackedPosition(ClimbingHand hand)
    {
        return hand.transform.position;
    }

    void PerformClimb()
    {
        if (activeHand == null) return;

        Vector3 currentHandPos = GetHandTrackedPosition(activeHand);
        Vector3 delta = currentHandPos - lastHandWorldPos;

        if (delta.sqrMagnitude > 0.000001f)
        {
            Vector3 move = -delta * climbMultiplier;
            xrOrigin.transform.position += move;
        }

        lastHandWorldPos = currentHandPos;
    }

    void ApplyGravity()
    {
        if (xrOrigin == null || xrOrigin.Camera == null) return;

        Vector3 rayStart = xrOrigin.Camera.transform.position;
        float totalRayLength = xrOrigin.CameraInOriginSpaceHeight + floorCheckDistance;

        bool isGrounded = Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            totalRayLength,
            floorLayer
        );

        Debug.DrawRay(rayStart, Vector3.down * totalRayLength, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
        }
        else
        {
            fallVelocity.y -= gravity * Time.deltaTime;
            xrOrigin.transform.position += fallVelocity * Time.deltaTime;

            if (!isRespawning)
                StartCoroutine(RespawnAfterDelay());
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        if (activeHand == null)
        {
            ResetToSafety();
        }

        isRespawning = false;
    }

    void ResetToSafety()
    {
        // 떨어지던 물리 속도 완벽 제거
        fallVelocity = Vector3.zero;

        // [트래킹 먹통 방지 핵심 코드]
        // 단순히 .position을 주면 카메라 트래킹(Tracked Pose Driver)이 풀려 시야가 고정됩니다.
        // 카메라의 현재 수평 오차(HMD 오프셋)를 유지하면서 VR Rig 전체를 텔레포트시키는 공식을 사용합니다.
        Vector3 cameraOffset = xrOrigin.Camera.transform.position - xrOrigin.transform.position;
        cameraOffset.y = 0; // 수평 위치 오차만 계산

        // 안전한 리스폰 좌표 계산 (바닥 파묻힘을 방지하기 위해 0.3m 여유 높이 부여)
        Vector3 targetSpawnPos = lastCheckpointPos - cameraOffset;
        targetSpawnPos.y += 0.3f;

        // 이동 적용
        xrOrigin.transform.position = targetSpawnPos;

        Debug.Log($"[ClimbProvider] 카메라 트래킹을 유지하며 안전 리스폰 완료: {targetSpawnPos}");
    }

    void StopRespawn()
    {
        if (isRespawning)
        {
            StopAllCoroutines();
            isRespawning = false;
        }
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