using UnityEngine;

namespace FaithClash.Combat
{
    /// <summary>Número de dano que sobe e some sobre o inimigo atingido (sempre virado para a câmera).</summary>
    public class NumeroDano : MonoBehaviour
    {
        TextMesh texto;
        float t, duracao = 0.9f;
        Vector3 vel;
        Color cor;

        public static void Criar(Vector3 pos, float valor, Color cor)
        {
            var go = new GameObject("Número de Dano");
            go.transform.position = pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, Random.Range(-0.3f, 0.3f));
            var n = go.AddComponent<NumeroDano>();
            n.texto = go.AddComponent<TextMesh>();
            n.texto.text = Mathf.RoundToInt(valor).ToString();
            n.texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            n.texto.GetComponent<MeshRenderer>().sharedMaterial = n.texto.font.material;
            n.texto.anchor = TextAnchor.MiddleCenter;
            n.texto.alignment = TextAlignment.Center;
            n.texto.fontSize = 64;
            n.texto.characterSize = 0.05f * Mathf.Lerp(1f, 1.8f, Mathf.InverseLerp(10f, 60f, valor));
            n.texto.fontStyle = FontStyle.Bold;
            n.cor = cor;
            n.vel = new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, Random.Range(-0.4f, 0.4f));
            n.texto.color = cor;
        }

        void LateUpdate()
        {
            t += Time.deltaTime;
            vel.y = Mathf.Lerp(vel.y, 0.3f, 4f * Time.deltaTime);
            transform.position += vel * Time.deltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            float a = Mathf.Clamp01(1f - (t - duracao * 0.5f) / (duracao * 0.5f));
            texto.color = new Color(cor.r, cor.g, cor.b, a);
            if (t >= duracao) Destroy(gameObject);
        }
    }
}