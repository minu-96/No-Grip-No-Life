using UnityEngine;
using System.Collections.Generic;

public class HandGrabDetector : MonoBehaviour
{
    private readonly List<GrabPoint> overlappedPoints = new List<GrabPoint>();

    public GrabPoint currentPoint
    {
        get
        {
            if (overlappedPoints.Count == 0) return null;

            GrabPoint closestPoint = null;
            float closestDistance = Mathf.Infinity;

            for (int i = overlappedPoints.Count - 1; i >= 0; i--)
            {
                GrabPoint point = overlappedPoints[i];
                if (point == null)
                {
                    overlappedPoints.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, point.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }

            return closestPoint;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point == null) point = other.GetComponentInParent<GrabPoint>();
        if (point == null) return;

        Debug.LogWarning($"[HandGrabDetector] {gameObject.name} entered {other.gameObject.name}, grabPoint={point.gameObject.name}");

        if (!overlappedPoints.Contains(point))
        {
            overlappedPoints.Add(point);
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point == null) point = other.GetComponentInParent<GrabPoint>();
        if (point == null) return;

        Debug.LogWarning($"[HandGrabDetector] {gameObject.name} exited {other.gameObject.name}, grabPoint={point.gameObject.name}");

        if (overlappedPoints.Contains(point))
        {
            overlappedPoints.Remove(point);
        }
    }

    public void ClearCache()
    {
        overlappedPoints.Clear();
    }
}
