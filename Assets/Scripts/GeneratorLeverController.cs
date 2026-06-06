using UnityEngine;

public class GeneratorLeverController : MonoBehaviour
{
    [SerializeField]
    private Transform lever;

    [SerializeField]
    private float returnSpeed = 2f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isGrabbed = false;

    private void Start()
    {
        if (lever == null)
            lever = transform.Find("Lever_Pivot/Lever");

        if (lever != null)
        {
            initialPosition = lever.localPosition;
            initialRotation = lever.localRotation;
            Debug.Log("GeneratorLeverController initialized");
        }
        else
        {
            Debug.LogError("Lever not found!");
        }
    }

    private void Update()
    {
        if (lever == null) return;

        if (isGrabbed)
        {
            // Grab 중: 고정
            lever.localPosition = initialPosition;
            lever.localRotation = Quaternion.Euler(-60f, 0f, 0f);
        }
        else
        {
            // Grab 해제 후: 복구
            lever.localPosition = Vector3.Lerp(lever.localPosition, initialPosition, Time.deltaTime * returnSpeed);
            lever.localRotation = Quaternion.Lerp(lever.localRotation, initialRotation, Time.deltaTime * returnSpeed);
        }
    }

    public void OnGrab()
    {
        isGrabbed = true;
    }

    public void OnRelease()
    {
        isGrabbed = false;
    }
}
