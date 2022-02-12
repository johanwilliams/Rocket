using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    public Sound[] sounds;

    public static AudioManager instance;

    // Use this for initialization
    void Awake()
    {
        // Make sure there is only one AudioManager if we change scenes
        if (instance == null)
            instance = this;
        else if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Don't destroy the AudioManager if we change scenes
        DontDestroyOnLoad(gameObject);

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.playOnAwake = sound.playOnAwake;
            sound.source.spatialBlend = sound.spatialBlend;
        }
    }

    private void Update()
    {
        foreach (Sound sound in sounds)
        {            
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.spatialBlend = sound.spatialBlend;
        }
    }

    public void Play(string name)
    {
        Sound s = GetSoundByName(name);
        if (s != null)
            s.source.Play();
    }

    public void PlayClipAtPoint(string name, Vector3 position)
    {
        Sound s = GetSoundByName(name);
        if (s != null)
            AudioSource.PlayClipAtPoint(s.source.clip, position);
    }

    public void Stop(string name)
    {
        Sound s = GetSoundByName(name);
        if (s != null)
            s.source.Stop();
    }

    public void Pause(string name)
    {
        Sound s = GetSoundByName(name);
        if (s != null)
            s.source.Pause();
    }

    public bool IsPlaying(string name)
    {
        Sound s = GetSoundByName(name);
        if (s != null)
            return s.source.isPlaying;
        return false;
    }

    public Sound GetSoundByName(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
            Debug.LogWarning("Sound '" + name + "' could not be found");
        return s;
    }
}
