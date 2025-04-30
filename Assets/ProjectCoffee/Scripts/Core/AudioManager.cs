using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio manager for sound effects
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;
    
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.5f, 1.5f)]
        public float pitch = 1f;
    }
    
    [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();
    
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        
        // Create audio sources for each sound effect
        foreach (SoundEffect soundEffect in soundEffects)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = soundEffect.clip;
            source.volume = soundEffect.volume;
            source.pitch = soundEffect.pitch;
            source.playOnAwake = false;
            
            audioSources[soundEffect.name] = source;
        }
    }
    
    public void PlaySound(string soundName)
    {
        if (audioSources.TryGetValue(soundName, out AudioSource source))
        {
            source.Play();
        }
    }
    
    public void StopSound(string soundName)
    {
        if (audioSources.TryGetValue(soundName, out AudioSource source))
        {
            source.Stop();
        }
    }
    
    public AudioSource GetAudioSource(string soundName)
    {
        if (audioSources.TryGetValue(soundName, out AudioSource source))
        {
            return source;
        }
        
        return null;
    }
}