using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("필수 연결")]
    public ClimbingHand leftHand;
    public ClimbingHand rightHand;
    public StaminaVignette staminaVignette;

    [Header("스태미나 설정")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("초당 변화량")]
    public float consumeRate = 2.5f;
    public float recoverRate = 15f;

    [Header("Checkpoint Detection")]
    public float checkpointDetectionRadius = 0.75f;
    [Header("상태 확인")]
    public bool isInsideCheckpoint = false;

    private readonly Collider[] checkpointHits = new Collider[16];
    private CharacterController characterController;
    void Start()
    {
        characterController = GetComponentInParent<CharacterController>();
        if (characterController == null) characterController = FindObjectOfType<CharacterController>();

        currentStamina = maxStamina;

        if (staminaVignette != null)
        {
            staminaVignette.UpdateVignette(currentStamina);
        }
    }

    void Update()
    {
        isInsideCheckpoint = IsInsideCheckpoint();
        bool isGrabbingAny = false;

        if (leftHand != null && leftHand.isGrabbing) isGrabbingAny = true;
        if (rightHand != null && rightHand.isGrabbing) isGrabbingAny = true;

        if (isInsideCheckpoint)
        {
            currentStamina += recoverRate * Time.deltaTime;
        }
        else if (isGrabbingAny)
        {
            currentStamina -= consumeRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (staminaVignette != null)
        {
            staminaVignette.UpdateVignette(currentStamina);
        }

        if (currentStamina <= 0f && isGrabbingAny)
        {
            ForceReleaseAllHands();
        }
    }

    void ForceReleaseAllHands()
    {
        Debug.LogWarning("[스태미나 부족] 모든 손을 강제로 놓습니다!");

        if (leftHand != null && leftHand.isGrabbing) leftHand.Release();
        if (rightHand != null && rightHand.isGrabbing) rightHand.Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            isInsideCheckpoint = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            isInsideCheckpoint = false;
        }
    }

    private bool IsInsideCheckpoint()
    {
        Vector3 probePosition = transform.position;
        if (characterController != null)
        {
            Bounds bounds = characterController.bounds;
            probePosition = new Vector3(bounds.center.x, bounds.min.y + 0.2f, bounds.center.z);
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            probePosition,
            Mathf.Max(checkpointDetectionRadius, 0.1f),
            checkpointHits,
            ~0,
            QueryTriggerInteraction.Collide);

        bool foundCheckpoint = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = checkpointHits[i];
            checkpointHits[i] = null;

            if (hit != null && hit.CompareTag("Checkpoint"))
            {
                foundCheckpoint = true;
            }
        }

        return foundCheckpoint;
    }
}