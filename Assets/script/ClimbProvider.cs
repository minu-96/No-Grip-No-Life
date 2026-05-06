using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ClimbProvider : MonoBehaviour
{
    [Header("인터랙션 설정")]
    public GameObject leftHandObject;
    public GameObject rightHandObject;

    [Header("낙하 & 체크포인트 설정")]
    public float gravity = 9.81f;
    public float floorCheckDistance = 0.2f;
    public LayerMask floorLayer;
    public float respawnDelay = 3.0f;

    private XROrigin xrOrigin;
    private Vector3 lastHandPosition;
    private IXRSelectInteractor activeInteractor;
    private bool isClimbing = false;
    private Vector3 fallVelocity;

    private Vector3 lastCheckpointPos;
    private bool isRespawning = false;

    void Start()
    {
        xrOrigin = GetComponentInParent<XROrigin>();
        lastCheckpointPos = xrOrigin.transform.position;
    }

    void LateUpdate()
    {
        CheckHand(leftHandObject);
        CheckHand(rightHandObject);

        if (isClimbing && activeInteractor != null)
        {
            PerformClimb(); 
            fallVelocity = Vector3.zero;

            if (isRespawning)
            {
                StopAllCoroutines();
                isRespawning = false;
            }
        }
        else
        {
            ApplyGravity();
        }
    }

    void CheckHand(GameObject handObject)
    {
        if (handObject == null) return;

        var interactor = handObject.GetComponentInChildren<IXRSelectInteractor>();

        if (interactor != null && interactor.hasSelection)
        {
            if (!isClimbing)
            {
                isClimbing = true;
                activeInteractor = interactor;
                lastHandPosition = ((MonoBehaviour)activeInteractor).transform.position;
                Debug.Log("클라이밍 시작: " + handObject.name);
            }
        }
        else if (isClimbing && activeInteractor == interactor)
        {
            isClimbing = false;
            activeInteractor = null;
            Debug.Log("클라이밍 종료");
        }
    }

    void PerformClimb()
    {
        Vector3 currentHandPosition = ((MonoBehaviour)activeInteractor).transform.position;
        Vector3 delta = currentHandPosition - lastHandPosition;

        if (delta != Vector3.zero)
        {
            xrOrigin.transform.position -= delta;
        }

        lastHandPosition = ((MonoBehaviour)activeInteractor).transform.position;
    }

    void ApplyGravity()
    {
        
        Camera mainCam = xrOrigin.GetComponentInChildren<Camera>();
        if (mainCam == null) return;

        Vector3 rayStart = new Vector3(mainCam.transform.position.x, xrOrigin.transform.position.y + 0.1f, mainCam.transform.position.z);

        bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, floorCheckDistance + 0.2f, floorLayer);

        Debug.DrawRay(rayStart, Vector3.down * (floorCheckDistance + 0.2f), isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            fallVelocity = Vector3.zero;
            if (isRespawning)
            {
                StopAllCoroutines();
                isRespawning = false;
            }
        }
        else if (!isClimbing)
        {
            fallVelocity.y -= gravity * Time.deltaTime;
            xrOrigin.transform.position += fallVelocity * Time.deltaTime;

            if (!isRespawning)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        if (!isClimbing)
        {
            xrOrigin.transform.position = lastCheckpointPos;
            fallVelocity = Vector3.zero;
        }
        isRespawning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            lastCheckpointPos = other.transform.position + new Vector3(0, 0.5f, 0);
            Debug.Log("새로운 거점 도달: " + other.gameObject.name);
        }
    }
}