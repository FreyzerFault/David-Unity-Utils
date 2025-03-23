using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace DavidUtils.Audio
{
    [Serializable]
    public class Audio
    {
        [HideInInspector] public AudioSource source;

        public string name;

        public AudioClip clip;
        public AudioMixerGroup mixerGroup;

        [Range(0, 1)] public float volume = .5f;

        [Range(-3, 3)] public float pitch = 1;
        public bool loop;

        public bool IsPlaying => source.isPlaying;

        public Action onPlay;
        public Action onEnd;
        public Action onPause;
        public Action onUnpause;

        public void SetSource(AudioSource newSource)
        {
            source = newSource;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = loop;
            source.outputAudioMixerGroup = mixerGroup;
            source.playOnAwake = false;
        }

        public void Play(float delayInSeconds = 0)
        {
            if (delayInSeconds > 0)
            {
                AudioManager.Instance.StartCoroutine(PlayDelayedCoroutine(delayInSeconds));
            }
            else
            {
                source.Play();
                onPlay?.Invoke();
            }
        }

        public void Stop()
        {
            source.Stop();
            onEnd?.Invoke();
        }

        public void Pause()
        {
            source.Pause();
            onPause?.Invoke();
        }

        public void Unpause()
        {
            source.UnPause();
            onUnpause?.Invoke();
        }

        private IEnumerator PlayDelayedCoroutine(float delaySeconds)
        {
            if (delaySeconds > 0) yield return new WaitForSeconds(delaySeconds);

            Play();
            onPlay?.Invoke();

            yield return new WaitUntil(() => !IsPlaying);

            onEnd?.Invoke();
        }
    }
}
