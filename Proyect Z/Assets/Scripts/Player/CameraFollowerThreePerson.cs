using UnityEngine;

public class CameraFollowerThreePerson : MonoBehaviour
{
    public Transform targetA;   // Primer objeto
    //public Transform targetB;   // Segundo objeto
    public Vector3 offset = new Vector3(0f, 10f, -10f); // Ajusta este valor para posicionar la cámara correctamente

    private Transform currentTarget; // El target actual que la cámara seguirá

    void Update()
    {
        // Detectar cuál está activo
        if (targetA != null && targetA.gameObject.activeSelf)
            currentTarget = targetA;
        /*else if (targetB != null && targetB.gameObject.activeSelf)
            currentTarget = targetB;*/

        // Seguir al target actual
        if (currentTarget != null)
            transform.position = currentTarget.position + offset; // Ajusta la posición de la cámara con el offset
    }
}
