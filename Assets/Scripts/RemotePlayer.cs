using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public Vector3 targetPosition;
    public float smoothSpeed = 10f;

    void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
    }

    public void SetTargetPosition(Vector3 newPos)
    {
        targetPosition = newPos;
    }
}