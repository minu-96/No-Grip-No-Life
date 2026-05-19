using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;

public class ClimbProvider : MonoBehaviour
{
    [Header("손 설정")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;

    [Header("중력 및 바닥 체크 설정")]
    public float gravity = 9.81f;
    public float floorCheckDistance = 0.2f; // 발바닥 기준이므로 거리를 조금 줄입니다.
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
        // 부모 또는 씬에서 XROrigin 찾아오기
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
            xrOrigin = FindAnyObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("[ClimbProvider] 씬에서 XROrigin을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (leftHand == null)
            Debug.LogError("[ClimbProvider] Left Hand 컴포넌트가 연결되지 않았습니다!");
        if (rightHand == null)
            Debug.LogError("[ClimbProvider] Right Hand 컴포넌트가 연결되지 않았습니다!");

        // 시작 위치를 첫 번째 체크포인트로 지정
        lastCheckpointPos = xrOrigin.transform.position;
        Debug.Log("[ClimbProvider] 초기화 완료");
    }

    void Update()
    {
        ClimbingHand newHand = GetCurrentHand();

        if (newHand != null)
        {
            // 손이 바뀌었거나 새로 잡은 경우 위치 갱신
            if (activeHand != newHand)
            {
                activeHand = newHand;
                lastHandWorldPos = GetHandTrackedPosition(activeHand);
                isClimbing = true;
                Debug.Log("[ClimbProvider] 활성화된 손 변경: " + activeHand.name);
            }

            PerformClimb();
            fallVelocity = Vector3.zero; // 클라이밍 중에는 중력 가속도 초기화
            StopRespawn();
        }
        else
        {
            if (isClimbing)
            {
                isClimbing = false;
                activeHand = null;
            }
            ApplyGravity();
        }
    }

    ClimbingHand GetCurrentHand()
    {
        // 잡고 있는 손 판정 (현재 활성화된 손 우선 처리)
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
            // 플레이어는 손이 움직인 반대 방향으로 이동해야 끌어당기는 느낌이 남
            Vector3 move = -delta * climbMultiplier;

            // XROrigin 자체를 이동
            xrOrigin.transform.position += move;
            Debug.Log($"[ClimbProvider] 이동: {move.y:F4} (손 변위 delta: {delta.y:F4})");
        }

        lastHandWorldPos = currentHandPos;
    }

    void ApplyGravity()
    {
        if (xrOrigin == null) return;

        // 기준점을 카메라가 아닌 플레이어의 발바닥(XROrigin 위치)으로 변경하여 VR 높낮이 대응
        Vector3 rayStart = xrOrigin.transform.position + Vector3.up * 0.1f;

        bool isGrounded = Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            floorCheckDistance,
            floorLayer
        );

        // 디버그 레이 시각화
        Debug.DrawRay(rayStart, Vector3.down * floorCheckDistance, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            fallVelocity = Vector3.zero;
            StopRespawn();
        }
        else
        {
            // 중력 적용 및 낙하
            fallVelocity.y -= gravity * Time.deltaTime;
            xrOrigin.transform.position += fallVelocity * Time.deltaTime;

            // 허공에 떠 있고 리스폰 루틴이 안 돌고 있다면 리스폰 타이머 시작
            if (!isRespawning)
                StartCoroutine(RespawnAfterDelay());
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        // 여전히 아무것도 안 잡고 있다면 마지막 체크포인트로 리스폰
        if (activeHand == null)
        {
            xrOrigin.transform.position = lastCheckpointPos;
            fallVelocity = Vector3.zero;
            Debug.Log("[ClimbProvider] 낙하로 인한 체크포인트 리스폰");
        }

        isRespawning = false;
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
        // 체크포인트 태그를 가진 트리거에 부딪히면 리스폰 위치 갱신
        if (other.CompareTag("Checkpoint"))
        {
            lastCheckpointPos = xrOrigin.transform.position;
            Debug.Log("[ClimbProvider] 체크포인트 저장 완료");
        }
    }
}