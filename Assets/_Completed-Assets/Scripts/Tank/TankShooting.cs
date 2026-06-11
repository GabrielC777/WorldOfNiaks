using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // Necesario para controlar la red con photonView

namespace Complete
{
    public class TankShooting : MonoBehaviourPun // Heredamos de MonoBehaviourPun
    {
        public int m_PlayerNumber = 1;
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;


        private string m_FireButton;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;


        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }


        private void Start()
        {
    
            m_FireButton = "Fire";

            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update()
        {
            // ¡BLINDAJE CRÍTICO! Si este tanque no es el mío, no leo el teclado ni ejecuto el disparo.
            if (!photonView.IsMine) return;

            // El slider debe mostrar por defecto el valor mínimo de fuerza
            m_AimSlider.value = m_MinLaunchForce;

            // Si supera la fuerza máxima y no ha disparado...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            // Si acaba de pulsar el botón de disparo...
            else if (Input.GetButtonDown(m_FireButton))
            {
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            // Si mantiene pulsado el botón de disparo sin haber soltado la bala...
            else if (Input.GetButton(m_FireButton) && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Si suelta el botón de disparo...
            else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
            {
                Fire();
            }
        }


        private void Fire()
        {
            m_Fired = true;

            // Calculamos la velocidad que tendrá la bala
            Vector3 velocidadCalculada = m_CurrentLaunchForce * m_FireTransform.forward;

            // Metemos la velocidad en un array de objetos para enviarla por red
            object[] datosDeInstanciacion = new object[] { velocidadCalculada };

            // Pasamos los datos al Instantiate de Photon
            PhotonNetwork.Instantiate(
                "CompleteShell",
                m_FireTransform.position,
                m_FireTransform.rotation,
                0,
                datosDeInstanciacion
            );

            // Audio local
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            m_CurrentLaunchForce = m_MinLaunchForce;
        }
    }
}