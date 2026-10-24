using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FaithClash.Training
{
    /// <summary>
    /// Teste automático das animações (Faith Clash > Ferramentas > Testar Animações, dentro do Editor).
    /// Toca cada estado do Animator no chão e mede, a cada 0,05 s: altura do pé mais baixo em relação ao chão real
    /// (negativo = pé atravessando o chão), altura da bacia e da cabeça, e quanto o personagem saiu do lugar.
    /// Resultado em Ferramentas/analise/teste_animacoes.txt.
    /// </summary>
    public class TestadorAnimacoes : MonoBehaviour
    {
        public const string ChaveSessao = "FC_TESTAR_ANIMACOES";

        IEnumerator Start()
        {
#if UNITY_EDITOR
            if (SessionState.GetInt(ChaveSessao, 0) != 1) yield break;
            SessionState.SetInt(ChaveSessao, 0);
            yield return new WaitForSeconds(1.5f);

            var jogador = FindAnyObjectByType<FaithClash.Player.PlayerController>();
            if (jogador == null) { Debug.LogError("[Teste] Jogador não encontrado."); yield break; }
            var animator = jogador.GetComponentInChildren<Animator>();
            var animadorScript = jogador.GetComponent<FaithClash.Player.AnimadorProtagonista>();
            jogador.Congelado = true;
            animadorScript.enabled = false;
            var pes = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot),
                              animator.GetBoneTransform(HumanBodyBones.LeftToes), animator.GetBoneTransform(HumanBodyBones.RightToes) };
            var quadril = animator.GetBoneTransform(HumanBodyBones.Hips);
            var cabeca = animator.GetBoneTransform(HumanBodyBones.Head);
            var maos = new[] { animator.GetBoneTransform(HumanBodyBones.LeftHand), animator.GetBoneTransform(HumanBodyBones.RightHand) };

            var ctrl = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
            var cc = jogador.GetComponent<CharacterController>();

            var sb = new StringBuilder();
            sb.AppendLine("estado;ik;duracao;pe_min_acima_do_chao;pe_max;bacia_min;bacia_max;cabeca_min;maos_min;deslocamento_xz;ficou_sob_o_chao");
            var pesIk = jogador.GetComponentInChildren<FaithClash.Player.PesNoChao>();
            // o nome do estado do Animator = nome do clipe nos estados soltos; Locomocao é blend tree
            var nomes = new List<string>();
            foreach (var s in ctrl.layers[0].stateMachine.states) nomes.Add(s.state.name);
            foreach (bool ikLigado in new[] { false, true })
            foreach (var nome in nomes)
            {
                if (pesIk != null) pesIk.enabled = ikLigado;
                int hash = Animator.StringToHash(nome);
                if (!animator.HasState(0, hash)) continue;
                var vels = nome == "Locomocao" ? new[] { 0f, 2.5f, 5.5f, 9f } : new[] { 0f };
                foreach (var vel in vels)
                {
                    cc.enabled = false;
                    jogador.transform.position = new Vector3(0, 0.1f, -3f);
                    jogador.transform.rotation = Quaternion.identity;
                    cc.enabled = true;
                    yield return null;
                    animator.SetFloat("Velocidade", vel);
                    animator.Play(hash, 0, 0f);
                    yield return null;
                    float dur = Mathf.Clamp(animator.GetCurrentAnimatorStateInfo(0).length, 0.5f, 2.4f);
                    float peMin = 9f, peMax = -9f, bMin = 9f, bMax = -9f, cMin = 9f, mMin = 9f;
                    Vector3 ini = quadril.position;
                    float maiorDesloc = 0f;
                    for (float t = 0f; t < dur; t += 0.05f)
                    {
                        yield return new WaitForSeconds(0.05f);
                        float chao = jogador.transform.position.y;
                        if (Physics.Raycast(jogador.transform.position + Vector3.up * 0.5f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore)) chao = h.point.y;
                        foreach (var p in pes) { float v = p.position.y - chao; peMin = Mathf.Min(peMin, v); peMax = Mathf.Max(peMax, v); }
                        bMin = Mathf.Min(bMin, quadril.position.y - chao); bMax = Mathf.Max(bMax, quadril.position.y - chao);
                        cMin = Mathf.Min(cMin, cabeca.position.y - chao);
                        foreach (var m in maos) mMin = Mathf.Min(mMin, m.position.y - chao);
                        Vector3 d = quadril.position - ini; d.y = 0;
                        maiorDesloc = Mathf.Max(maiorDesloc, d.magnitude);
                    }
                    string rotulo = vels.Length > 1 ? nome + "@" + vel : nome;
                    bool sob = peMin < -0.06f || mMin < -0.05f;
                    sb.AppendLine($"{rotulo};{(ikLigado ? "com" : "cru")};{dur:0.00};{peMin:0.000};{peMax:0.000};{bMin:0.00};{bMax:0.00};{cMin:0.00};{mMin:0.00};{maiorDesloc:0.00};{(sob ? "SIM" : "nao")}");
                }
            }
            File.WriteAllText("Ferramentas/analise/teste_animacoes.txt", sb.ToString(), Encoding.UTF8);
            Debug.Log("[Teste] Animações testadas: " + nomes.Count + " estados. Veja Ferramentas/analise/teste_animacoes.txt");
            EditorApplication.isPlaying = false;
#else
            yield break;
#endif
        }
    }
}
