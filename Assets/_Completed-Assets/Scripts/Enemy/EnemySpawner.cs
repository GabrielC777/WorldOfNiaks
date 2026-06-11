using UnityEngine;
using System.Collections;
using Photon.Pun;

public class EnemySpawner : MonoBehaviourPunCallbacks
{
    public static EnemySpawner Instance;

    [Header("Configuración del Spawner")]
    public GameObject prefabEnemigo;
    public int totalEnemigos = 20;
    public float rangoSpawn = 25f; // Radio máximo desde el centro para esparcir los tanques
    public float distanciaSeguridadJugador = 27f; // Evita que aparezcan encima del jugador

    private int enemigosRestantes;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void IniciarSpawning()
    {
        // Solo el MasterClient genera los objetos de la sala
        if (!PhotonNetwork.IsMasterClient) return;

        enemigosRestantes = totalEnemigos;
        StartCoroutine(SpawnEnemigosConRetraso());
    }

    private IEnumerator SpawnEnemigosConRetraso()
    {
        // Esperamos un segundo para asegurarnos de que el mapa procedural ha terminado de hornear el NavMesh
        yield return new WaitForSeconds(10f);

        int enemigosCreados = 0;
        int intentosMaximos = 100; // Para evitar bucles infinitos si el mapa es pequeño

        while (enemigosCreados < totalEnemigos && intentosMaximos > 0)
        {
            intentosMaximos--;

            // Calculamos una posición aleatoria en el mapa
            float xRand = Random.Range(-rangoSpawn, rangoSpawn);
            float zRand = Random.Range(-rangoSpawn, rangoSpawn);
            Vector3 posicionPropuesta = new Vector3(xRand, 0f, zRand);

            // FILTRO DE SEGURIDAD CENTRAL: Evitamos que aparezcan en el centro (punto de spawn de los jugadores)
            if (Vector3.Distance(posicionPropuesta, Vector3.zero) < distanciaSeguridadJugador)
            {
                continue; // Demasiado cerca del centro, descartamos e intentamos otra vez
            }

            // Buscamos un punto válido real sobre el NavMesh horneado para que no aparezcan dentro de los edificios
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(posicionPropuesta, out hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
            {
                if (prefabEnemigo != null)
                {//Leemos directamente el nombre del prefab
                    PhotonNetwork.InstantiateRoomObject(prefabEnemigo.name, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    enemigosCreados++;
                }
            }
        }
        enemigosRestantes = enemigosCreados;

        // Subimos el número inicial a las propiedades de la sala de Photon para la UI optimizada por eventos
        ExitGames.Client.Photon.Hashtable propSala = new ExitGames.Client.Photon.Hashtable { { "EnemigosRestantes", enemigosRestantes } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(propSala);

        Debug.Log($"<color=orange>[SPAWNER]</color> ¡Tiempo concluido! Se han esparcido {enemigosCreados} enemigos.");
    }

    // Esta función la llamará el enemigo al morir
    public void EnemigoEliminado()
    {

        if (!PhotonNetwork.IsMasterClient) return;

        enemigosRestantes--;
        Debug.Log($"<color=orange>[SPAWNER]</color> Enemigo destruido. Quedan: {enemigosRestantes}");

        ExitGames.Client.Photon.Hashtable propSala = new ExitGames.Client.Photon.Hashtable { { "EnemigosRestantes", enemigosRestantes } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(propSala);

        if (enemigosRestantes <= 0)
        {
            // Fin de la partida: Mandamos a todos a la pantalla de resultados
            photonView.RPC("RPC_FinalizarPartida", RpcTarget.AllViaServer);
        }
    }

    [PunRPC]
    void RPC_FinalizarPartida()
    {
        Debug.Log("<color=red>[VICTORIA]</color> ¡Todos los enemigos han sido eliminados! Regresando al menú...");

        // Desconectamos de la sala de Photon de forma limpia
        PhotonNetwork.LeaveRoom();
    }

    // Callback automático de Photon que se dispara cuando sales de la sala con LeaveRoom
    public override void OnLeftRoom()
    {
        // Cargamos la escena del menú principal (asegúrate de que en el Launcher pusiste el índice o nombre correcto, ej: 0)
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}