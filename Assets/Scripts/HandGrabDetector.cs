using UnityEngine;

public class HandGrabDetector : MonoBehaviour
{
    public GrabPoint currentPoint;

    private void OnTriggerEnter(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();

        if (point != null && !point.occupied)
        {
            currentPoint = point;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();

        if (point != null && currentPoint == point)
        {
            currentPoint = null;
        }
    }
}