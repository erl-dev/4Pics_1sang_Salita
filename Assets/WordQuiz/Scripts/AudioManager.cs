using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public Sounds[] sounds;
    public static bool isMusicPlaying = false;
    void Awake()
    {
        foreach (Sounds s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    void Start()
    {
        if (isMusicPlaying){
            Play("BGMusic");   
        }
          
    }

    public void Play(string name)
    {
        Sounds s = Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            s.source.Play();
            isMusicPlaying = true;
        }
        else
        {
            Debug.LogWarning("Sound " + name + " not found!");
        }
    }

    public void Stop(string name)
    {
        Sounds s = Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            s.source.Stop();
            isMusicPlaying = false;
        }
        else
        {
            Debug.LogWarning("Sound " + name + " not found!");
        }
    }
}
