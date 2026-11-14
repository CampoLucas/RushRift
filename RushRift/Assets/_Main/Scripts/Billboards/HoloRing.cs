using UnityEngine;

[DisallowMultipleComponent]
public class HoloRing : MonoBehaviour
{
    [Header("MESH")]
    [Tooltip("Transform del objeto que contiene el MeshRenderer.")]
    public Transform meshTransform;

    [Header("ROTATION")]
    public bool autoRotate = true;

    [Tooltip("Eje de rotacion local.")]
    public Vector3 rotationAxis = Vector3.forward;

    [Range(-360f, 360f)]
    public float rotationSpeed = 30f;

    [Tooltip("Si esta activo, rota sobre ejes locales.")]
    public bool localRotation = true;

    [Header("OPTIONAL")]
    [Tooltip("Permite probar otra orientacion del mesh.")]
    public bool rotateAroundCenter = false;

    private void Awake()
    {
        if (meshTransform == null)
        {
            MeshRenderer renderer =
                GetComponentInChildren<MeshRenderer>();

            if (renderer != null)
                meshTransform = renderer.transform;
            else
                meshTransform = transform;
        }
    }

    private void Update()
    {
        if (!autoRotate || meshTransform == null)
            return;

        if (rotationAxis.sqrMagnitude < 0.0001f)
            return;

        Vector3 axis = rotationAxis.normalized;

        float angle = rotationSpeed * Time.deltaTime;

        if (rotateAroundCenter)
        {
            Renderer renderer =
                meshTransform.GetComponent<Renderer>();

            if (renderer != null)
            {
                meshTransform.RotateAround(
                    renderer.bounds.center,
                    localRotation
                        ? meshTransform.TransformDirection(axis)
                        : axis,
                    angle
                );

                return;
            }
        }

        meshTransform.Rotate(
            axis,
            angle,
            localRotation ? Space.Self : Space.World
        );
    }
}
