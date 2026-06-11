using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Complete
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviourPun
    {
        public AudioSource m_MovementAudio;         // Arrastra aquí el AudioSource del motor del enemigo
        public AudioClip m_EngineIdling;            // Clip de motor al ralentí
        public AudioClip m_EngineDriving;           // Clip de motor avanzando
        public float m_PitchRange = 0.2f;           // Variación del tono para que suene realista

        public ParticleSystem[] m_ParticleSystems;   // Arrastra aquí los sistemas de partículas de polvo de las orugas

        private NavMeshAgent m_Agent;
        private float m_OriginalPitch;

        private void Awake()
        {
            m_Agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            m_OriginalPitch = m_MovementAudio.pitch;

            // Encendemos las partículas de polvo del enemigo al nacer
            for (int i = 0; i < m_ParticleSystems.Length; ++i)
            {
                m_ParticleSystems[i].Play();
            }
        }

        private void Update()
        {
            // El audio del motor lo calcula cada cliente localmente para escuchar a los enemigos pasar
            EngineAudio();
        }

        private void EngineAudio()
        {
            // En vez de usar Inputs, miramos la velocidad real del NavMeshAgent
            // Si la velocidad actual es casi cero, es que está quieto
            if (m_Agent.velocity.magnitude < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Si el agente se está moviendo por el NavMesh, cambiamos al sonido de acelerar
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }
    }
}