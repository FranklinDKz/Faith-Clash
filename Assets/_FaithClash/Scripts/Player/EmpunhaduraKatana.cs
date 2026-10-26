using UnityEngine;
using FaithClash.Combat;

namespace FaithClash.Player
{
    /// <summary>
    /// Katana com as duas mãos: a mão esquerda é presa por IK na pegada traseira do cabo (objeto "PegadaEsquerda" da katana) enquanto
    /// a empunhadura é de duas mãos. Com uma mão a esquerda fica livre (escudo opcional). Fica no objeto do Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EmpunhaduraKatana : MonoBehaviour
    {
        Animator animator;
        ModoCombate modo;
        Transform pegada;
        float peso;

        void Awake()
        {
            animator = GetComponent<Animator>();
            modo = GetComponentInParent<ModoCombate>();
        }

        void OnAnimatorIK(int camada)
        {
            if (animator == null || !animator.isHuman || modo == null) return;
            if (pegada == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "PegadaEsquerda") { pegada = t; break; }
                if (pegada == null) return;
            }
            bool ativo = modo.DuasMaos && pegada.gameObject.activeInHierarchy;
            peso = Mathf.MoveTowards(peso, ativo ? 1f : 0f, Time.deltaTime * 8f);
            if (peso <= 0.001f) { animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f); animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f); return; }
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, peso);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, peso * 0.85f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, pegada.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, pegada.rotation);
        }
    }
}
