using UnityEngine;
using System.Collections.Generic;

public class HandGrabDetector : MonoBehaviour
{
    private List<GrabPoint> overlappedPoints = new List<GrabPoint>();

    public GrabPoint currentPoint
    {
        get
        {
            if (overlappedPoints.Count == 0) return null;

            GrabPoint closestPoint = null;
            float closestDistance = Mathf.Infinity;

            for (int i = overlappedPoints.Count - 1; i >= 0; i--)
            {
                if (overlappedPoints[i] == null)
                {
                    overlappedPoints.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, overlappedPoints[i].transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = overlappedPoints[i];
                }
            }

            return closestPoint;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point == null) point = other.GetComponentInParent<GrabPoint>();

        Debug.LogWarning($"🟢 {gameObject.name} Trigger Enter: {other.gameObject.name}, GrabPoint: {point}");

        if (point != null && !overlappedPoints.Contains(point))
        {
            overlappedPoints.Add(point);
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point == null) point = other.GetComponentInParent<GrabPoint>();

        Debug.LogWarning($"🔴 {gameObject.name} Trigger Exit: {other.gameObject.name}, GrabPoint: {point}");

        if (point != null && overlappedPoints.Contains(point))
        {
            overlappedPoints.Remove(point);
        }
    }

    public void ClearCache()
    {
        overlappedPoints.Clear();
    }
}