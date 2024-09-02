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

        public Action OnPlay;
        public Action OnEnd;
        public Action OnPause;
        public Action OnUnpause;

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
                OnPlay?.Invoke();
            }
        }

        public void Stop()
        {
            source.Stop();
            OnEnd?.Invoke();
        }

        public void Pause()
        {
            source.Pause();
            OnPause?.Invoke();
        }

        public void Unpause()
        {
            source.UnPause();
            OnUnpause?.Invoke();
        }

        private IEnumerator PlayDelayedCoroutine(float delaySeconds)
        {
            if (delaySeconds > 0) yield return new WaitForSeconds(delaySeconds);

            Play();
            OnPlay?.Invoke();

            yield return new WaitUntil(() => !IsPlaying);

            OnEnd?.Invoke();
        }
    }
}
