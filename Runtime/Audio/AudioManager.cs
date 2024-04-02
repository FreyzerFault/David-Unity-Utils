using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DavidUtils.Audio
{
    public class AudioManager : SingletonPersistent<AudioManager>
    {
        // Volume is [0,1]
        public float MasterVolume
        {
            get => AudioListener.volume;
            set => AudioListener.volume = value / 100;
        }

        // List to show in Inspector
        public List<Sound> sounds = new List<Sound>();
        
        // Map to access by name
        private readonly Dictionary<string, Sound> _audioMap = new();


        private void Start()
        {
            foreach (Sound sound in sounds.Where(sound => sound.name != ""))
            {
                // Create AudioSource
                sound.Source = gameObject.AddComponent<AudioSource>();
            
                // Bind to Map
                _audioMap.Add(sound.name, sound);
            }
        }

        public void Play(string clipName, float volume = -1, float pitch = -20,  float delaySeconds = -1)
        {
            if (_audioMap.TryGetValue(clipName, out Sound sound))
                sound.Play(volume, pitch, delaySeconds);
            else
                Debug.LogWarning("[AudioManager] Clip not found: " + clipName);
        }
    
        public void Stop(string clipName) => _audioMap[clipName].Stop();

        // No loopeable (Si el volumen o el pitch es -1, usamos el Volumen por defecto
        public void PlayOnce(string clipName, float volume = -1, float pitch = -20, float delaySeconds = -1)
        {
            if (_audioMap.ContainsKey(clipName))
                StartCoroutine(PlayOnceCoroutine(clipName, volume, pitch, delaySeconds));
            else
                Debug.LogWarning("[AudioManager] Clip not found: " + clipName);
        }
    
        private IEnumerator PlayOnceCoroutine(string clipName, float volume, float pitch, float delaySeconds)
        {
            if (delaySeconds > 0)
                yield return new WaitForSeconds(delaySeconds);
        
            _audioMap[clipName].PlayOnce(volume, pitch);

            yield return new WaitUntil(() => _audioMap[clipName].IsPlaying);
            // Sound ended
        }

        public bool IsPlaying(string clipName) => _audioMap[clipName].IsPlaying;
    }
}