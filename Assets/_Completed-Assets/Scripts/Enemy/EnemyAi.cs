using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Complete
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviourPun
    {
        public enum State { Patrullar, Perseguir, Disparar }
        [Header("Estado Actual")]
        public State estadoActual = State.Patrullar;

        [Header("Rangos de la IA")]
        public float radioDeteccion = 15f;
        public float radioAtaque = 10f;
        public float tiempoEntreDisparos = 2f;

        [Header("Ajustes de Patrulla")]
        public float radioDePatrulla = 20f;

        [Header("Referencias del Prefab")]
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_FireClip;
        public float m_LaunchForce = 20f;

        private NavMeshAgent agente;
        private Transform targetJugador;
        private float cronometroDisparo;

        private void Awake()
        {
            agente = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            cronometroDisparo = tiempoEntreDisparos;

            // Buscamos un punto de patrulla inicial
            if (PhotonNetwork.IsMasterClient)
            {
                BuscarNuevoPuntoPatrulla();
            }
        }

        private void Update()
        {
            // Solo el MasterClient calcula la IA de los bots.
            // Los clientes normales solo verán al bot moverse gracias al PhotonTransformView.
            if (!PhotonNetwork.IsMasterClient) return;

            cronometroDisparo += Time.deltaTime;

            BuscarJugadorMasCercano();

            // MÁQUINA DE ESTADOS
            switch (estadoActual)
            {
                case State.Patrullar:
                    HistorialPatrulla();
                    break;
                case State.Perseguir:
                    HistorialPersecucion();
                    break;
                case State.Disparar:
                    HistorialAtaque();
                    break;
            }
        }

        private void BuscarJugadorMasCercano()
        {
            // Buscamos absolutamente todos los tanques con vida del mapa
            TankHealth[] todosLosTanques = FindObjectsOfType<TankHealth>();
            float distanciaMasCercana = Mathf.Infinity;
            Transform jugadorCercano = null;

            foreach (TankHealth tanque in todosLosTanques)
            {
                //  Si el tanque está muerto o desactivado, lo ignoramos
                if (!tanque.gameObject.activeSelf) continue;

                // Si el tanque soy YO MISMO, lo ignoramos
                if (tanque.gameObject == this.gameObject) continue;

                // Si el objeto NO tiene la etiqueta "Player", significa que es otro bot enemigo, ¡así que pasamos de él!
                if (!tanque.CompareTag("Player")) continue;

                // Si pasa los filtros, calculamos la distancia porque es un jugador real
                float distancia = Vector3.Distance(transform.position, tanque.transform.position);
                if (distancia < distanciaMasCercana)
                {
                    distanciaMasCercana = distancia;
                    jugadorCercano = tanque.transform;
                }
            }

            targetJugador = jugadorCercano;

            // Transiciones de estado basadas en la distancia del jugador real
            if (targetJugador != null)
            {
                if (distanciaMasCercana <= radioAtaque)
                {
                    estadoActual = State.Disparar;
                }
                else if (distanciaMasCercana <= radioDeteccion)
                {
                    estadoActual = State.Perseguir;
                }
                else
                {
                    estadoActual = State.Patrullar;
                }
            }
            else
            {
                // Si no hay ningún jugador real en su rango, se mantiene patrullando el desierto
                estadoActual = State.Patrullar;
            }
        }

        private void HistorialPatrulla()
        {
            //Volvemos a encender el motor de movimiento por si veníamos del estado de Disparar
            agente.isStopped = false;

            // Si el agente llega cerca de su punto de destino, busca otro
            if (!agente.hasPath || agente.remainingDistance < 1.5f)
            {
                BuscarNuevoPuntoPatrulla();
            }
        }

        private void BuscarNuevoPuntoPatrulla()
        {
            Vector3 direccionAleatoria = Random.insideUnitSphere * radioDePatrulla;
            direccionAleatoria += transform.position;
            NavMeshHit hit;

            // Buscamos un punto válido dentro del NavMesh horneado
            if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioDePatrulla, 1))
            {
                agente.SetDestination(hit.position);
            }
        }

        private void HistorialPersecucion()
        {
            if (targetJugador != null)
            {
                //Volvemos a encender el motor de movimiento para que corra tras el jugador
                agente.isStopped = false;

                agente.SetDestination(targetJugador.position);
            }
        }

        private void HistorialAtaque()
        {
            if (targetJugador == null) return;

            // Frenamos al tanque para que apunte y dispare con precisión
            agente.SetDestination(transform.position);

            // Rotamos suavemente el chasis hacia la posición del jugador objetivo
            Vector3 direccionHaciaJugador = (targetJugador.position - transform.position).normalized;
            direccionHaciaJugador.y = 0; // Evitamos que el tanque cabecee hacia arriba o abajo
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHaciaJugador);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);

            // Sistema de cadencia de tiro
            if (cronometroDisparo >= tiempoEntreDisparos)
            {
                cronometroDisparo = 0f;
                // Mandamos un RPC para que todos los clientes instancien la bala del enemigo
                photonView.RPC("RPC_EnemigoDispara", RpcTarget.All);
            }
        }

        [PunRPC]
        void RPC_EnemigoDispara()
        {
            // Instanciamos la bala localmente en cada pantalla (puedes usar PhotonNetwork.Instantiate si prefieres, 
            // pero para los enemigos de la IA, instanciar local en cascada mediante RPC ahorra mucho ancho de banda)
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.velocity = m_LaunchForce * m_FireTransform.forward;

            if (m_ShootingAudio != null && m_FireClip != null)
            {
                m_ShootingAudio.clip = m_FireClip;
                m_ShootingAudio.Play();
            }
        }

        // Esta función dibuja las esferas de rango en la pestaña Scene al seleccionar el enemigo
        private void OnDrawGizmosSelected()
        {
            //Radio de Patrulla (Verde) - El área donde busca puntos aleatorios
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Verde con transparencia
            Gizmos.DrawWireSphere(transform.position, radioDePatrulla);

            //Radio de Detección (Amarillo) - Cuándo te ve y empieza a perseguirte
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Amarillo
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);

            //Radio de Ataque (Rojo) - Cuándo se frena para apuntar y disparar
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // Rojo
            Gizmos.DrawWireSphere(transform.position, radioAtaque);
        }
    }
}