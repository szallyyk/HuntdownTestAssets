#if UNITY_SERVICES || STEAM_SERVICES
using UnityEngine;
using UnityEngine.Pool;
namespace Ignitives.MultiplayerEngine
{
    public class DamageEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem damageParticlesPrefab;

        private ObjectPool<ParticleSystem> _damageParticlesPool;

        private void Awake()
        {
            if (_damageParticlesPool == null)
            {
                _damageParticlesPool = new ObjectPool<ParticleSystem>(CreateDamageParticle, OnGet, OnRelease, OnDestroyParticle, true, 10, 100);
            }
        }

        private ParticleSystem CreateDamageParticle()
        {
            ParticleSystem particleInstance = Instantiate(damageParticlesPrefab, transform);
            var returnToPool = particleInstance.gameObject.AddComponent<ReturnToPool>();
            returnToPool.Pool = _damageParticlesPool;
            return particleInstance;
        }

        private static void OnGet(ParticleSystem particleInstance)
        {
            particleInstance.gameObject.SetActive(true);
        }

        private static void OnRelease(ParticleSystem particleInstance)
        {
            particleInstance.gameObject.SetActive(false);
        }

        private static void OnDestroyParticle(ParticleSystem particleInstance)
        {
            Destroy(particleInstance.gameObject);
        }

        public void TakeDamage(DamageMessage damageMessage)
        {
            Vector3 hitPoint = damageMessage.hitPoint;
            Transform sender = damageMessage.sender;
            PlayDamageEffect(hitPoint, sender);
        }

        public void PlayDamageEffect(Vector3 position, Transform sender)
        {
            var particles = _damageParticlesPool.Get();
            particles.transform.position = position;
            particles.transform.LookAt(sender);
            particles.Play();
        }
    }

    public class ReturnToPool : MonoBehaviour
    {
        public IObjectPool<ParticleSystem> Pool;
        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped()
        {
            if (Pool != null)
            {
                Pool.Release(_particleSystem);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
#endif
