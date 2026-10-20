using System.Collections;
using UnityEngine;
using FaithClash.Core;

namespace FaithClash.Training
{
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] Renderer body;
        [SerializeField] float respawnDelay = 2f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Health health;
        Color baseColor = Color.white;
        Vector3 home;
        MaterialPropertyBlock block;

        void Awake()
        {
            health = GetComponent<Health>();
            home = transform.position;
            block = new MaterialPropertyBlock();
            if (body != null && body.sharedMaterial.HasProperty(BaseColorId))
                baseColor = body.sharedMaterial.GetColor(BaseColorId);
        }

        void OnEnable()
        {
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(float amount, Vector3 point, GameObject source)
        {
            if (TrainingManager.Instance != null) TrainingManager.Instance.RegisterDamage(amount);
            StopAllCoroutines();
            StartCoroutine(Flash());
        }

        void OnDied()
        {
            StopAllCoroutines();
            StartCoroutine(Respawn());
        }

        IEnumerator Flash()
        {
            SetColor(Color.white);
            transform.localScale = new Vector3(1.1f, 0.95f, 1.1f);
            yield return new WaitForSeconds(0.1f);
            transform.localScale = Vector3.one;
            SetColor(baseColor);
        }

        IEnumerator Respawn()
        {
            SetColor(Color.red);
            transform.rotation = Quaternion.Euler(80f, transform.eulerAngles.y, 0f);
            yield return new WaitForSeconds(respawnDelay);
            transform.rotation = Quaternion.identity;
            transform.position = home;
            transform.localScale = Vector3.one;
            health.ResetHealth();
            SetColor(baseColor);
        }

        void SetColor(Color c)
        {
            if (body == null) return;
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            body.SetPropertyBlock(block);
        }
    }
}
