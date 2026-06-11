using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace Complete
{
    public class TankHealth : MonoBehaviourPun
    {
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;

        private AudioSource m_ExplosionAudio;
        private ParticleSystem m_ExplosionParticles;
        private float m_CurrentHealth;
        private bool m_Dead;

        // Seguro antiquemaduras para no morir dos veces en el mismo frame
        private bool m_DeathRPCEnviado;

        private void Awake()
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_DeathRPCEnviado = false; // Reiniciamos el seguro al reaparecer
            SetHealthUI();
        }

        public void TakeDamage(float amount)
        {
            if(!m_Dead && !m_DeathRPCEnviado)
            {
                photonView.RPC("RPC_RecibirDano", RpcTarget.AllBuffered, amount);
            }
        }

        private void SetHealthUI()
        {
            if (m_Slider != null) m_Slider.value = m_CurrentHealth;
            if (m_FillImage != null)
            {
                m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
            }
        }

        [PunRPC]
        void RPC_RecibirDano(float amount)
        {
            if (m_Dead) return;

            m_CurrentHealth -= amount;
            SetHealthUI();

            if (m_CurrentHealth <= 0f && !m_DeathRPCEnviado)
            {
                m_DeathRPCEnviado = true; // Bloqueamos para no enviar el RPC de muerte dos veces

                if (photonView.IsMine)
                {
                    photonView.RPC("RPC_OnDeath", RpcTarget.AllBuffered);
                }
            }
        }

        [PunRPC]
        void RPC_OnDeath()
        {
            if (m_Dead) return;
            m_Dead = true;

            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            if (PhotonNetwork.IsMasterClient)
            {
                // COSAS DEL JUGADOR REAL
                if (gameObject.CompareTag("Player") && photonView.Owner != null)
                {
                    if (PlayerStats.Instance != null)
                    {
                        PlayerStats.Instance.RestarVida(photonView.Owner);
                    }
                }
                // COSAS DE LA IA (No tocamos nada de su apariencia)
                else if (!gameObject.CompareTag("Player"))
                {
                    if (PlayerStats.Instance != null && PhotonNetwork.LocalPlayer != null)
                    {
                        PlayerStats.Instance.SumarPuntos(PhotonNetwork.LocalPlayer, 100);
                    }
                    if (EnemySpawner.Instance != null)
                    {
                        EnemySpawner.Instance.EnemigoEliminado();
                    }
                }
            }

            // VOLVEMOS AL FUNCIONAMIENTO ORIGINAL:
            // Apagamos el tanque antiguo para que tu PlayerStats instancie el nuevo limpiamente
            gameObject.SetActive(false);
        }

        [PunRPC]
        public void RPC_SetTankColor(int actorNumber)
        {
            if (GameManager.Instance != null)
            {
                Color colorAsignado = GameManager.Instance.m_PlayerColors[(actorNumber - 1) % GameManager.Instance.m_PlayerColors.Length];

                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    r.material.color = colorAsignado;
                }
            }
        }
    }
}