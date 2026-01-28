using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Transform targetA;   // Primer objeto
    public Transform targetB;   // Segundo objeto
    public Vector3 offset = new Vector3(0f, 10f, 0f);

    private Transform currentTarget;

    void Update()
    {
        // Detectar cuál está activo
        if (targetA != null && targetA.gameObject.activeSelf)
            currentTarget = targetA;
        else if (targetB != null && targetB.gameObject.activeSelf)
            currentTarget = targetB;

        // Seguir al target actual
        if (currentTarget != null)
            transform.position = currentTarget.position + offset;
    }
}
