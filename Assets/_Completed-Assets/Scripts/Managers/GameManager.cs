using UnityEngine;
using Photon.Pun;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        // Estructura simple para el inspector
        [System.Serializable]
        public struct TankSpawnConfig
        {
            public Transform m_SpawnPoint;
        }

        public TankSpawnConfig[] m_Tanks; // Mantenemos el array para no romper las referencias del inspector del profesor
        public Color[] m_PlayerColors = { Color.red, Color.blue, Color.green, Color.yellow };

        public static GameManager Instance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        // Función que llamará tu PlayerManager al instanciar un tanque para pintarlo de su color único
        public void ColorizarTanque(GameObject tank, int actorNumber)
        {
            // Buscamos el componente PhotonView del tanque
            PhotonView pv = tank.GetComponent<PhotonView>();
            if (pv != null)
            {
                // Pasamos el ID del dueño por RPC a todos para que el color sea idéntico en todas las pantallas
                pv.RPC("RPC_SetTankColor", RpcTarget.AllBuffered, actorNumber);
            }
        }
    }
}