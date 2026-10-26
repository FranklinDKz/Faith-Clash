using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Training
{
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        public static readonly List<TrainingDummy> Todos = new List<TrainingDummy>();

        [SerializeField] Renderer body;
        [SerializeField] float respawnDelay = 2f;
        [Header("Identidade (HUD)")]
        [SerializeField] string nome = "Boneco de Treino";
        [SerializeField] string subtitulo = "Fé Maculada";
        [SerializeField] int nivel = 26;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Health health;
        Color baseColor = Color.white;
        Vector3 home;
        MaterialPropertyBlock block;
        Vector3 empurrao;
        float presoAte;
        InimigoIA ia;   // se existir, o boneco é um inimigo com corpo e animações

        public string Nome => nome;
        public string Subtitulo => subtitulo;
        public int Nivel => nivel;
        public Health Vida => health;
        public bool Preso => Time.time < presoAte;

        void Awake()
        {
            health = GetComponent<Health>();
            ia = GetComponent<InimigoIA>();
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
            Todos.Add(this);
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            Todos.Remove(this);
        }

        void Update()
        {
            if (empurrao.sqrMagnitude > 0.0001f)
            {
                if (ia != null) ia.Mover(empurrao * Time.deltaTime); else transform.position += empurrao * Time.deltaTime;
                empurrao = Vector3.Lerp(empurrao, Vector3.zero, 5f * Time.deltaTime);
            }
        }

        /// <summary>Empurra o boneco (vento, explosões). Ele volta sozinho ao ponto inicial ao renascer.</summary>
        public void Empurrar(Vector3 impulso)
        {
            if (Preso) return;
            impulso.y = 0f;
            empurrao += impulso;
        }

        /// <summary>Imobiliza o boneco (Prisão de Água).</summary>
        public void Prender(float duracao)
        {
            presoAte = Time.time + duracao;
            empurrao = Vector3.zero;
        }

        void OnDamaged(float amount, Vector3 point, GameObject source)
        {
            if (TrainingManager.Instance != null) TrainingManager.Instance.RegisterDamage(amount);
            NumeroDano.Criar(transform.position + Vector3.up * 1.8f, amount, amount >= 40f ? new Color(1f, 0.35f, 0.2f) : amount >= 18f ? new Color(1f, 0.8f, 0.3f) : Color.white);
            Vector3 dirGolpe = source != null ? Vector3.ProjectOnPlane(transform.position - source.transform.position, Vector3.up).normalized : Vector3.zero;
            if (health.IsDead) { if (ia != null) ia.Morrer(dirGolpe); return; }
            if (ia != null) { ia.Reagir(-dirGolpe, amount); if (amount >= 28f) empurrao += dirGolpe * Mathf.Min(6f, amount * 0.15f); return; }
            StopAllCoroutines();
            StartCoroutine(Flash(dirGolpe));
        }

        void OnDied()
        {
            StopAllCoroutines();
            StartCoroutine(Respawn());
        }

        IEnumerator Flash(Vector3 dirGolpe)
        {
            SetColor(Color.white);
            transform.localScale = new Vector3(1.1f, 0.95f, 1.1f);
            // balança para longe do golpe e volta (reação ao impacto)
            transform.rotation = Quaternion.Euler(dirGolpe.z * 10f, 0f, -dirGolpe.x * 10f);
            yield return new WaitForSeconds(0.1f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 0.6f);
            yield return new WaitForSeconds(0.05f);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetColor(baseColor);
        }

        IEnumerator Respawn()
        {
            if (ia != null)
            {
                yield return new WaitForSeconds(respawnDelay + 3f);
                empurrao = Vector3.zero;
                health.ResetHealth();
                ia.Renascer(home);
                yield break;
            }
            SetColor(Color.red);
            transform.rotation = Quaternion.Euler(80f, transform.eulerAngles.y, 0f);
            yield return new WaitForSeconds(respawnDelay);
            transform.rotation = Quaternion.identity;
            transform.position = home;
            transform.localScale = Vector3.one;
            empurrao = Vector3.zero;
            health.ResetHealth();
            SetColor(baseColor);
        }

        void SetColor(Color c)
        {
            if (body == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            body.SetPropertyBlock(block);
        }
    }
}
