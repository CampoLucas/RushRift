using UnityEngine;

public class HoloRing : MonoBehaviour
{
    public float rotationSpeed = 20f;

    void Update()
    {
        // Rota sobre el eje Y, es decir, alrededor
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}