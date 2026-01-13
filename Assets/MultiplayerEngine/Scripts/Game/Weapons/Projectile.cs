#if UNITY_SERVICES || STEAM_SERVICES
using System.Collections;
using UnityEngine;
namespace Ignitives.MultiplayerEngine
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float maxLifetime = 5f;

        public int Damage => damage;

        private TrailRenderer trailRenderer;
        private Coroutine moveCoroutine;

        private void Awake()
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }

        /// <summary>
        /// Clears the trail renderer. Call before reusing from pool.
        /// </summary>
        public void ClearTrail()
        {
            if (trailRenderer != null)
                trailRenderer.Clear();
        }

        public void MoveTo(Vector3 target, float speed, System.Action<Projectile> onArrive)
        {
            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveRoutine(target, speed, onArrive));
        }

        private IEnumerator MoveRoutine(Vector3 target, float speed, System.Action<Projectile> onArrive)
        {
            float elapsedTime = 0f;

            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                elapsedTime += Time.deltaTime;

                // Safety timeout to prevent stuck projectiles
                if (elapsedTime > maxLifetime)
                {
                    break;
                }

                yield return null;
            }

            // Spawn hit effect if assigned
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            onArrive?.Invoke(this);
        }
    }
}
#endif
