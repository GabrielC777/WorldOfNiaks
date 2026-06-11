using Photon.Pun;
using UnityEngine;
using System.IO;

public class PlayerManager : MonoBehaviourPun
{
    private void Start()
    {
        if (photonView.IsMine)
        {
            CreateController();
        }
    }

    private void CreateController()
    {
        Complete.GameManager gameManager = FindObjectOfType<Complete.GameManager>();
        GameObject myTank = null;

        // Intentamos spawnear en el punto del manager
        if (gameManager != null && gameManager.m_Tanks != null && gameManager.m_Tanks.Length > 0)
        {
            int playerIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % gameManager.m_Tanks.Length;
            Transform spawnPoint = gameManager.m_Tanks[playerIndex].m_SpawnPoint;
            myTank = PhotonNetwork.Instantiate("CompleteTank", spawnPoint.position, spawnPoint.rotation);
        }
        else // Fallback
        {
            myTank = PhotonNetwork.Instantiate("CompleteTank", Vector3.zero, Quaternion.identity);
        }

        // Activamos componentes solo para el dueño
        if (myTank != null)
        {
            SetupTank(myTank);
        }
    }

    private void SetupTank(GameObject tank)
    {
        bool isMine = tank.GetComponent<PhotonView>().IsMine;

        tank.GetComponent<Complete.TankMovement>().enabled = isMine;
        tank.GetComponent<Complete.TankShooting>().enabled = isMine;

        if (isMine)
        {
            tank.GetComponent<Complete.TankMovement>().m_PlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            tank.GetComponent<Complete.TankShooting>().m_PlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            // Buscamos la cámara dentro de la estructura
            Transform cam = tank.transform.Find("CameraPivot/TankCamera");
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                cam.tag = "MainCamera";

                // Le decimos al script de la cámara: "Toma, este es tu tanque, síguelo"
                CameraOrbit orbitScript = cam.GetComponent<CameraOrbit>();
                if (orbitScript != null)
                {
                    orbitScript.AsignarTanque(tank.transform);
                }
            }

            if (Complete.GameManager.Instance != null)
            {
                Complete.GameManager.Instance.ColorizarTanque(tank, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            AudioListener audio = tank.GetComponentInChildren<AudioListener>();
            if (audio) Destroy(audio);
        }
    }
    [PunRPC]
    public void RPC_RecreateController()
    {
        if (photonView.IsMine)
        {
            // Volvemos a llamar a la función que busca el SpawnPoint e instancia el CompleteTank
            CreateController();
        }
    }
}