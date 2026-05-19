using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그를 확인하거나 StaminaManager 컴포넌트가 있는지 체크
        StaminaManager stamina = other.GetComponent<StaminaManager>();
        if (stamina != null)
        {
            stamina.isInCheckpoint = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StaminaManager stamina = other.GetComponent<StaminaManager>();
        if (stamina != null)
        {
            stamina.isInCheckpoint = false;
        }
    }
}