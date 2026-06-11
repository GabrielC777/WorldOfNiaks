using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameUI : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoVidas;
    public TextMeshProUGUI textoPuntos;

    [Header("Referencias de Enemigos")]
    public TextMeshProUGUI m_EnemigosTexto;

    void Start()
    {
        // Al arrancar la partida, forzamos la primera actualización manual 
        // para pintar el nombre y los datos iniciales (0 puntos, 3 vidas, enemigos)
        ActualizarInterfazTodo();
    }

    // Este método es un "Callback" oficial de Photon.
    // ¡Solo se ejecuta UN FRAME cuando alguien modifica sus CustomProperties!
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Solo nos interesa actualizar nuestra pantalla si las propiedades que han cambiado
        // pertenecen al jugador local (nosotros mismos)
        if (targetPlayer != null && targetPlayer.IsLocal)
        {
            // Si en el cambio de propiedades venían las Vidas, actualizamos el texto de vidas
            if (changedProps.ContainsKey("Vidas"))
            {
                textoVidas.text = "VIDAS: " + changedProps["Vidas"].ToString();
            }

            // Si en el cambio venían los Puntos, actualizamos el texto de puntos
            if (changedProps.ContainsKey("Puntos"))
            {
                textoPuntos.text = "PUNTOS: " + changedProps["Puntos"].ToString();
            }
        }
    }

    // Se ejecuta automáticamente en TODOS los clientes cuando el MasterClient altera una propiedad de la SALA.
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("EnemigosRestantes") && m_EnemigosTexto != null)
        {
            m_EnemigosTexto.text = "ENEMIGOS: " + propertiesThatChanged["EnemigosRestantes"].ToString();
        }
    }

    private void ActualizarInterfazTodo()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            // Nombre
            if (!string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
                textoNombre.text = PhotonNetwork.LocalPlayer.NickName;
            else
                textoNombre.text = "Jugador " + PhotonNetwork.LocalPlayer.ActorNumber;

            // Vidas iniciales
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Vidas", out object v))
                textoVidas.text = "VIDAS: " + v.ToString();
            else
                textoVidas.text = "VIDAS: 3";

            // Puntos iniciales
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Puntos", out object p))
                textoPuntos.text = "PUNTOS: " + p.ToString();
            else
                textoPuntos.text = "PUNTOS: 0";
        }

        // Carga inicial del contador de enemigos (por si un cliente conecta un poco más tarde)
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("EnemigosRestantes", out object e))
        {
            if (m_EnemigosTexto != null) m_EnemigosTexto.text = "ENEMIGOS: " + e.ToString();
        }
        else
        {
            // Texto provisional durante los primeros 10 segundos antes de que spawneen
            if (m_EnemigosTexto != null) m_EnemigosTexto.text = "ENEMIGOS: --";
        }
    }
}