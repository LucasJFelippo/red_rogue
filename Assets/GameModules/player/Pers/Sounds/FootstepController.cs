using UnityEngine;

public class FootstepController : MonoBehaviour
{
    // Componente para tocar os sons
    public AudioSource audioSource;

    // Lista de sons de passos para caminhada
    public AudioClip[] walkingClips;

    // Esta função será chamada pela Animação
    public void PlayFootstepSound()
    {
        // Se não tiver clipes, não faz nada
        if (walkingClips.Length == 0) return;

        // Escolhe um som aleatório da lista para não ficar repetitivo
        int index = UnityEngine.Random.Range(0, walkingClips.Length);
        AudioClip clip = walkingClips[index];

        // Toca o som escolhido
        audioSource.PlayOneShot(clip);
    }
}