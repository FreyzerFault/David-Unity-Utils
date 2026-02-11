using System.Collections.Generic;
using System.Linq;
using DavidUtils.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace DavidUtils
{
    public class AudioManager : Singleton<AudioManager>
    {
        public enum AudioType { Music, Sfx }
    
        public Transform musicParent;
        public Transform sfxParent;
    
        public AudioMixerGroup musicMixerGroup;
        public AudioMixerGroup sfxMixerGroup;
    
        private List<AudioSource> _musicSources;
        private List<AudioSource> _sfxSources;
    
        private AudioSource _playingMusic;
        private bool IsPlayingMusic => _playingMusic != null && _playingMusic.isPlaying;
    
        private List<AudioSource> SourceListByType(AudioType type) => type switch
        {
            AudioType.Music => _musicSources,
            AudioType.Sfx => _sfxSources,
            _ => null
        };

        protected override void Awake()
        {
            base.Awake();
        
            _musicSources = musicParent.GetComponentsInChildren<AudioSource>().ToList();
            _sfxSources = sfxParent.GetComponentsInChildren<AudioSource>().ToList();
        
            musicMixerGroup ??= _musicSources.First()?.outputAudioMixerGroup;
            sfxMixerGroup ??= _sfxSources.First()?.outputAudioMixerGroup;
        }


        #region SFX
    
        public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f) => Play(clip, AudioType.Sfx, volume, pitch);

        public void PlaySfx(int index) => _sfxSources[index].Play();
        public void PlaySfx(string clipName) => 
            _sfxSources.First(source => source.clip.name == clipName).Play();

        public void StopSfx(int index) => _sfxSources[index].Stop();
        public void StopSfx(string clipName) => 
            _sfxSources.First(source => source.clip.name == clipName).Stop();
    
        public void StopAllSfxs()
        {
            foreach (AudioSource sfxSource in _sfxSources) 
                sfxSource.Stop();
        }
    
        private AudioSource AddSfx(AudioClip clip) => AddAudio(clip, AudioType.Sfx);
        private AudioSource InstantiateSfxSource() => InstantiateSource(AudioType.Sfx);

        #endregion
    


        #region MUSIC
    
        public void  PlayMusic(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (IsPlayingMusic)
                StopMusic();
        
            Play(clip, AudioType.Music, volume, pitch);
        }

        public void PlayMusic(int index) => PlayMusic(_musicSources[index]);
        public void PlayMusic(string clipName) => 
            PlayMusic(_musicSources.First(source => source.clip.name == clipName));
    
        private void PlayMusic(AudioSource musicSource)
        {
            if (IsPlayingMusic)
                StopMusic();

            _playingMusic = musicSource;
            musicSource.Play();
        }

        public void StopMusic()
        {
            _playingMusic?.Stop();
            _playingMusic = null;
        }
    
        private AudioSource AddMusic(AudioClip clip) => AddAudio(clip, AudioType.Music);
        private AudioSource InstantiateMusicSource() => InstantiateSource(AudioType.Music);
    
        #endregion


        #region AUDIO CREATION
    
        private AudioSource AddAudio(AudioClip clip, AudioType type)
        {
            AudioSource source = type switch
            {
                AudioType.Music => InstantiateMusicSource(),
                AudioType.Sfx => InstantiateSfxSource(),
                _ => null
            };
        
            if (source == null) return null;
        
            source.clip = clip;
        
            return source;
        }
    

        private AudioSource InstantiateSource(AudioType type)
        {
            string objName = type switch
            {
                AudioType.Music => $"Music Source {_musicSources.Count}",
                AudioType.Sfx => $"SFX Source {_sfxSources.Count}",
                _ => ""
            };
            Transform parent = type switch
            {
                AudioType.Music => musicParent,
                AudioType.Sfx => sfxParent,
                _ => null
            };
            AudioMixerGroup mixerGroup = type switch
            {
                AudioType.Music => musicMixerGroup,
                AudioType.Sfx => sfxMixerGroup,
                _ => null
            };
            bool playOnAwake = false;
            bool loop = type == AudioType.Music;
        
            // INSTANTIATE GameObject
            GameObject sourceObj = new(objName);
            sourceObj.transform.SetParent(parent);
        
            // ADD AudioSource COMPONENT
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixerGroup;
            source.playOnAwake = playOnAwake;
            source.loop = loop;
        
            // ADD to AudioSource LIST
            SourceListByType(type)?.Add(source);
        
            return source;
        }

        #endregion


        #region PLAYING

        private AudioSource Play(AudioClip clip, AudioType type = AudioType.Sfx, float volume = 1f, float pitch = 1f)
        {
            // Search for the Clip in Sources
            AudioSource source = SourceListByType(type).FirstOrDefault(source => source.clip == clip);
        
            // If not found, search for an empty source to set the clip
            if (source == null)
            {
                source = SourceListByType(type).FirstOrDefault(s => s.clip == null);
                if (source != null) source.clip = clip;
            }
        
            // If no empty source, create a new one
            if (source == null) source = AddAudio(clip, type);

            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            return source;
        }

        #endregion
    }
}
