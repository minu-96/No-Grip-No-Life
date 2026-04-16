using System.Collections.Generic;
using UnityEngine;

public class HandGrabDetector : MonoBehaviour
{
    public GrabPoint currentPoint;

    private List<GrabPoint> points = new List<GrabPoint>();

    private void OnTriggerEnter(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point != null && !points.Contains(point))
        {
            points.Add(point);
            UpdateNearestPoint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point != null && points.Contains(point))
        {
            points.Remove(point);
            UpdateNearestPoint();
        }
    }

    private void UpdateNearestPoint()
    {
        currentPoint = null;
        float minDistance = Mathf.Infinity;

        foreach (GrabPoint point in points)
        {
            if (point == null || point.occupied) continue;

            float dist = Vector3.Distance(transform.position, point.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                currentPoint = point;
            }
        }
    }
}