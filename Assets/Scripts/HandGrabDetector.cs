using UnityEngine;
using System.Collections.Generic;

public class HandGrabDetector : MonoBehaviour
{
    // 현재 내 손 주변에 닿아있는 모든 그랩 포인트들을 담아두는 리스트
    private List<GrabPoint> overlappedPoints = new List<GrabPoint>();

    // ClimbingHand가 최종적으로 "지금 잡을 타겟"으로 가져갈 프로퍼티
    public GrabPoint currentPoint
    {
        get
        {
            if (overlappedPoints.Count == 0) return null;

            // [핵심 로직] 리스트에 담긴 포인트 중 '내 손 중심점과 가장 가까운' 포인트를 찾아서 반환합니다.
            GrabPoint closestPoint = null;
            float closestDistance = Mathf.Infinity;

            for (int i = overlappedPoints.Count - 1; i >= 0; i--)
            {
                // 혹시 오브젝트가 파괴되었거나 null이면 리스트에서 제거
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
        if (point != null && !overlappedPoints.Contains(point))
        {
            overlappedPoints.Add(point);
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabPoint point = other.GetComponent<GrabPoint>();
        if (point != null && overlappedPoints.Contains(point))
        {
            overlappedPoints.Remove(point);
        }
    }

    // 손을 놓거나 그랩할 때 리스트를 깔끔하게 비워주는 초기화 함수
    public void ClearCache()
    {
        overlappedPoints.Clear();
    }
}