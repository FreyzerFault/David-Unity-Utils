using System;
using UnityEngine;

namespace DavidUtils.Audio
{
    [Serializable]
    public class Sound
    {
        public string name = "";
    
        public AudioClip clip;

        [Range(0,1)]
        public float volume = .5f;
        [Range(-3, 3)]
        public float pitch = 1;

        public bool loop = false;

    
        public AudioSource source;

        public AudioSource Source
        {
            get => source;
            set
            {
                source = value;
                source.clip = clip;
                source.volume = volume;
                source.pitch = pitch;
                source.loop = loop;
            }
        }

        public bool IsPlaying => source.isPlaying;

        public void Play(float volume = -1, float pitch = -20, float delay = -1)
        {
            source.volume = volume < 0 ? this.volume : volume;
            source.pitch = pitch < -10 ? this.pitch : pitch;
        
            if (delay < 0)
                source.Play();
            else
                source.PlayDelayed(delay);
        }

        public void PlayOnce(float volume = -1, float pitch = -20)
        {
            source.pitch = pitch < -10 ? source.pitch : pitch;
            source.volume = volume < 0 ? this.volume : volume;
            source.PlayOneShot(clip);
        }

        public void Stop() => source.Stop();
    }
}
