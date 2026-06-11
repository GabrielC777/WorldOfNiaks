using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;

public class PlayerStats : MonoBehaviourPunCallbacks
{
    public static PlayerStats Instance;

    // Memoria local para saber si nuestras propias vidas han bajado
    private int m_MisVidasAnteriores = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            m_MisVidasAnteriores = 3;
            Hashtable propiedadesIniciales = new Hashtable
            {
                { "Vidas", 3 },
                { "Puntos", 0 }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(propiedadesIniciales);
        }
    }

    // Esta función AHORA SOLO RESTA EL NÚMERO. No invoca respawns.
    public void RestarVida(Player jugador)
    {
        if (jugador.CustomProperties.TryGetValue("Vidas", out object v))
        {
            int vidasActuales = (int)v;
            vidasActuales--;

            Hashtable nuevaVida = new Hashtable { { "Vidas", vidasActuales } };
            jugador.SetCustomProperties(nuevaVida);
        }
    }


    // Salta automáticamente en tu ordenador cuando la red detecta que tu vida ha cambiado
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Solo nos ponemos la cuenta atrás si el que ha perdido vida somos NOSOTROS
        if (targetPlayer != null && targetPlayer.IsLocal)
        {
            if (changedProps.ContainsKey("Vidas"))
            {
                int vidasNuevas = (int)changedProps["Vidas"];

                if (vidasNuevas < m_MisVidasAnteriores)
                {
                    m_MisVidasAnteriores = vidasNuevas;

                    Debug.Log($"<color=cyan>[STATS]</color> Me quedan {vidasNuevas} vidas.");

                    if (vidasNuevas > 0)
                    {
                        // Cada uno se pone sus propios 3 segundos, aunque muráis a la vez
                        Invoke("InvocarRespawn", 3f);
                    }
                    else
                    {
                        Debug.Log($"<color=red>[GAME OVER]</color> Me he quedado sin vidas.");
                    }
                }
            }
        }
    }

    private void InvocarRespawn()
    {
        // Como hay varios PlayerManagers invisibles, buscamos todos y elegimos el nuestro.
        PlayerManager[] todosLosManagers = FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager manager in todosLosManagers)
        {
            if (manager.GetComponent<PhotonView>().IsMine)
            {
                Debug.Log("<color=green>[RESPAWN]</color> Invocando nuevo tanque en MI manager...");
                manager.GetComponent<PhotonView>().RPC("RPC_RecreateController", RpcTarget.AllViaServer);
                break; // Una vez encontrado el nuestro, dejamos de buscar
            }
        }
    }

    public void SumarPuntos(Player jugador, int cantidad)
    {
        if (jugador.CustomProperties.TryGetValue("Puntos", out object p))
        {
            int puntosActuales = (int)p;
            puntosActuales += cantidad;

            Hashtable nuevosPuntos = new Hashtable { { "Puntos", puntosActuales } };
            jugador.SetCustomProperties(nuevosPuntos);
        }
    }
}