using UnityEngine;
using System.Collections.Generic;


public struct SpatialAudio
{
    public GameObject sourceObject;
    public AudioSource audioSource;

    public SpatialAudio(GameObject sourceObj, AudioSource sourceAudio)
    {
    sourceObject = sourceObj;
    audioSource = sourceAudio;
    }
}


public class audioManager : MonoBehaviour, IAudioManInterface
{

    [Header("Audio Manager")]
    public static audioManager instance = null;

    private List<AudioSource> _globalAudioSources = new List<AudioSource>();
    private List<SpatialAudio> _spatialAudioSources = new List<SpatialAudio>();

    
    void Awake()
    {
        if(instance == null){
             instance = this;
             DontDestroyOnLoad(gameObject);
        } else {
             Destroy(this.gameObject);
             return;
        }
    }

    public void playAudio(AudioClip audioClip)
    {
    var audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.clip = audioClip;
    audioSource.spatialBlend = 0f;
    audioSource.Play();

    _globalAudioSources.Add(audioSource);
    }

    public void play3dAudio(AudioClip audioClip, GameObject source)
    {
    var audioSource = source.AddComponent<AudioSource>();
    audioSource.clip = audioClip;
    audioSource.spatialBlend = 1f;
    audioSource.Play();

    _spatialAudioSources.Add(new SpatialAudio(source, audioSource));
    }

}
