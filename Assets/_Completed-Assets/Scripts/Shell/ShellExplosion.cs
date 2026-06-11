using UnityEngine;
using Photon.Pun;

namespace Complete
{
    public class ShellExplosion : MonoBehaviourPun, IPunInstantiateMagicCallback
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 1000f;
        public float m_MaxLifeTime = 2f;
        public float m_ExplosionRadius = 5f;

        private bool m_YaHaExplotado = false;

        private void Start()
        {
            // Programamos la autodestrucción por tiempo de seguridad
            if (photonView != null && photonView.IsMine)
            {
                Invoke("DestroyBullet", m_MaxLifeTime);
            }
            // Si es una bala local de la IA (ViewID == 0), borrado tradicional
            else if (photonView == null || photonView.ViewID == 0)
            {
                Destroy(gameObject, m_MaxLifeTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Evitamos rebotes o que la bala choque dos veces en el mismo frame
            if (m_YaHaExplotado) return;
            m_YaHaExplotado = true;

            // Apagamos el colisionador de inmediato para que deje de interactuar físicamente
            Collider miColisionador = GetComponent<Collider>();
            if (miColisionador != null) miColisionador.enabled = false;

            // CLASIFICAMOS LA BALA DE FORMA ESTRICTA:
            bool esMiBalaDeJugador = photonView != null && photonView.IsMine;
            bool esBalaDeLaIA = photonView == null || photonView.ViewID == 0;
            bool soyMasterClient = PhotonNetwork.IsMasterClient;

            // SOLO calculamos el daño a los tanques si la bala es MÍA, o si es de la IA y soy el MasterClient
            if (esMiBalaDeJugador || (esBalaDeLaIA && soyMasterClient))
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                    if (!targetRigidbody) continue;

                    targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

                    TankHealth targetHealth = targetRigidbody.GetComponentInParent<TankHealth>();
                    if (targetHealth != null)
                    {
                        float damage = CalculateDamage(targetRigidbody.position);
                        targetHealth.TakeDamage(damage);
                    }
                }
            }

            // 🎯 EL TRUCO PARA QUE PHOTON NO DE ERROR NUNCA MÁS:
            if (esMiBalaDeJugador)
            {
                // 1. Es mi bala de red: La borro oficialmente por Photon.
                PhotonNetwork.Destroy(gameObject);
            }
            else if (esBalaDeLaIA)
            {
                // 2. Es una bala local de un bot: La borro yo mismo.
                Destroy(gameObject);
            }
            else
            {
                // 3. Es la bala de red de OTRO jugador: 
                // ¡PROHIBIDO USAR DESTROY AQUÍ! Simplemente la hago invisible.
                // Photon la borrará de verdad cuando llegue el mensaje del otro jugador.
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        // Se ejecuta cuando el objeto es destruido (sea por red o por código local)
        private void OnDestroy()
        {
            if (m_ExplosionParticles != null)
            {
                // Desvinculamos las partículas para que no se borren
                m_ExplosionParticles.transform.parent = null;
                m_ExplosionParticles.Play();

                // Filtro para evitar el error amarillo del audio
                if (m_ExplosionAudio != null && m_ExplosionAudio.isActiveAndEnabled)
                {
                    m_ExplosionAudio.Play();
                }

                Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.main.duration);
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            object[] datos = info.photonView.InstantiationData;
            if (datos != null && datos.Length > 0)
            {
                Vector3 velocidadRecibida = (Vector3)datos[0];
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = velocidadRecibida;
                }
            }
        }

        void DestroyBullet()
        {
            if (photonView != null && photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
            float damage = relativeDistance * m_MaxDamage;
            return Mathf.Max(0f, damage);
        }
    }
}