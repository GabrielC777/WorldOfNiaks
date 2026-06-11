using UnityEngine;
using Photon.Pun;

public class CameraOrbit : MonoBehaviourPun
{
    [Header("Objetivo")]
    public Transform target;          // El tanque al que seguirá

    [Header("Configuración de Distancia")]
    public float distance = 10.0f;
    public float scrollSpeed = 2.0f;

    [Header("Sensibilidad del Ratón")]
    [Range(10f, 500f)]
    public float mouseSensitivity = 150f;

    private float x = 0.0f;
    private float y = 0.0f;

    // Esta función la llamaremos desde el PlayerManager en cuanto el tanque aparezca
    public void AsignarTanque(Transform miTanque)
    {
        target = miTanque;

        // La soltamos en la escena para evitar tirones de la física del padre
        transform.SetParent(null);

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        Debug.Log("<color=green>[CAMERA ORBIT]</color> Tanque asignado con éxito. Cámara independizada.");
    }

    void LateUpdate()
    {
        // CONTROL COJONUDO: Si no hay tanque asignado o no es nuestro, no hacemos nada
        if (target == null || !photonView.IsMine) return;

        // 1. Giro orbital con el Clic Derecho
        if (Input.GetMouseButton(1))
        {
            x += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            y = Mathf.Clamp(y, -5f, 60f);
        }

        // Zoom con la rueda
        distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * scrollSpeed, 3f, 20f);

        // 2. Cálculo de la posición (Apuntando 1.5 metros por encima de las orugas)
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 targetCenter = target.position + Vector3.up * 1.5f;
        Vector3 desiredPosition = targetCenter + (rotation * new Vector3(0.0f, 0.0f, -distance));

        // 3. Filtro para que no atraviese el suelo
        RaycastHit hit;
        if (Physics.Linecast(targetCenter, desiredPosition, out hit))
        {
            transform.position = hit.point + (hit.normal * 0.2f);
        }
        else
        {
            transform.position = desiredPosition;
        }

        transform.rotation = rotation;
    }
}