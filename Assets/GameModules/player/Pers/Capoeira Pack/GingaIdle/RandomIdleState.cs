using UnityEngine;

public class RandomIdleState : StateMachineBehaviour
{
    // Esta função é chamada automaticamente quando a animação do estado TERMINA
    // e o Animator está prestes a sair para outro estado.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Sorteia um novo número aleatório para a próxima animação de idle.
        // Se você tem 3 animações (índices 1, 2, 3), o range é de 1 a 4.
        int randomIndex = UnityEngine.Random.Range(1, 5);

        // Define o parâmetro 'IdleIndex' no Animator com o novo número sorteado.
        animator.SetInteger("IdleIndex", randomIndex);
    }
}