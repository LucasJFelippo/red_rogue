using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioClip[] walkingClips;
    public AudioClip[] runningClips;
    public AudioClip[] jumpingClips;

    // Função para tocar som de caminhada
    public void PlayWalkSound()
    {
        if (walkingClips.Length == 0) return;
        int index = UnityEngine.Random.Range(0, walkingClips.Length);
        audioSource.PlayOneShot(walkingClips[index]);
    }

    // Função para tocar som de corrida
    public void PlayRunSound()
    {
        if (runningClips.Length == 0) return;
        int index = UnityEngine.Random.Range(0, runningClips.Length);
        audioSource.PlayOneShot(runningClips[index]);
    }

    // Função para tocar som de pulo
    public void PlayJumpSound()
    {
        if (jumpingClips.Length == 0) return;
        int index = UnityEngine.Random.Range(0, jumpingClips.Length);
        audioSource.PlayOneShot(jumpingClips[index]);
    }
}