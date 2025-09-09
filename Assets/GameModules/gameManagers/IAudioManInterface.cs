using UnityEngine;

public interface IAudioManInterface
{

    // Audio Player
    void playAudio(AudioClip audioClip);
    void play3dAudio(AudioClip audioClip, GameObject source);

}
