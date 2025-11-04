using UnityEngine;
using UnityEngine.UI;

public class ParallaxScript : MonoBehaviour
{
    Vector2 startPos;

    [SerializeField] int moveModifier;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        Vector2 pz = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        float posX = Mathf.Lerp(transform.position.x, startPos.x + (pz.x * moveModifier), 2f * Time.deltaTime);
        float posY = Mathf.Lerp(transform.position.y, startPos.y + (pz.y * moveModifier), 2f * Time.deltaTime);

        transform.position = new Vector3 (posX, posY, 0);
    }
}
