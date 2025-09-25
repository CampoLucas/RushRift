using UnityEngine;

public class HoloRing : MonoBehaviour
{
    public float rotationSpeed = 20f;

    void Update()
    {
       
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}