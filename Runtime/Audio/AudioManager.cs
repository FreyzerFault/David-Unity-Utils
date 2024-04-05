using System.Collections.Generic;
using System.Linq;
using DavidUtils.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace DavidUtils.Audio
{
    public class AudioManager : SingletonPersistent<AudioManager>
    {
        private static SettingsManager Settings => SettingsManager.Instance;

        #region Mixer and MixerGroups

        public AudioMixer mixer;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup menuSfxGroup;

        private const string MIXER_MASTER_TAG = "MasterVolume";
        private const string MIXER_MUSIC_TAG = "MusicVolume";
        private const string MIXER_SFX_TAG = "SFXVolume";
        private const string MIXER_MENUSFX_TAG = "MenuSFXVolume";

        #endregion

        #region Reserved Audios

        [SerializeField] private Audio musicAudio;

        [SerializeField] private Audio ambientAudio;

        [SerializeField] private Audio auxAudio;

        #endregion

        // Volume is [0,1]
        public float MasterVolume
        {
            get => AudioListener.volume;
            set => AudioListener.volume = value / 100;
        }

        // List needed to be editable in Inspector, and map to query by name
        public List<Audio> sounds = new();
        private readonly Dictionary<string, Audio> _soundMap = new();

        protected override void Awake()
        {
            base.Awake();

            foreach (var sound in sounds)
            {
                sound.SetSource(gameObject.AddComponent<AudioSource>());
                _soundMap.Add(sound.name, sound);
            }
        }

        private void Start()
        {
            LoadVolumeSettings();

            PlayMusic();

            Settings.OnLoad += LoadVolumeSettings;
            Settings.OnSave += SaveVolumeSettings;
        }

        #region Volume

        public static float GlobalVolume
        {
            get => Settings.GetSetting<float>(MIXER_MASTER_TAG);
            private set => Settings.SetSetting(MIXER_MASTER_TAG, value);
        }
        public static float MusicVolume
        {
            get => Settings.GetSetting<float>(MIXER_MUSIC_TAG);
            private set => Settings.SetSetting(MIXER_MUSIC_TAG, value);
        }
        public static float SfxVolume
        {
            get => Settings.GetSetting<float>(MIXER_SFX_TAG);
            private set => Settings.SetSetting(MIXER_SFX_TAG, value);
        }

        private static void UpdateMixerVolumes()
        {
            Instance.mixer.SetFloat(MIXER_MASTER_TAG, LinearToLogAudio(GlobalVolume));
            Instance.mixer.SetFloat(MIXER_MUSIC_TAG, LinearToLogAudio(MusicVolume));
            Instance.mixer.SetFloat(MIXER_SFX_TAG, LinearToLogAudio(SfxVolume));
            Instance.mixer.SetFloat(MIXER_MENUSFX_TAG, LinearToLogAudio(SfxVolume));
        }

        #endregion

        #region Music

        public static void PlayMusic(string musicName)
        {
            Instance.musicAudio = GetAudio("Music/" + musicName);
            PlayMusic();
        }

        public static void PlayMusic() => Instance.musicAudio.Play();

        public static void StopMusic() => Instance.musicAudio.Stop();

        #endregion

        #region Ambient

        public static void PlayAmbient(string ambientName)
        {
            Instance.ambientAudio = GetAudio("Ambient/" + ambientName);
            PlayAmbient();
        }

        public static void PlayAmbient() => Instance.ambientAudio.Play();

        public static void StopAmbient() => Instance.ambientAudio.Stop();

        #endregion

        #region Play/Stop/Pause Sound

        public static Audio Play(string audioName, float delaySeconds = 0)
        {
            var audio = GetAudio(audioName);
            if (audio == null) return null;

            audio.Play(delaySeconds);
            return audio;
        }

        public static void Stop(string soundName)
        {
            var sound = GetAudio(soundName);
            if (sound != null && sound.IsPlaying) sound.Stop();
        }

        public static void Pause(string soundName)
        {
            var sound = GetAudio(soundName);
            if (sound != null) sound.Pause();
        }

        public static void UnPause(string soundName)
        {
            var sound = GetAudio(soundName);
            if (sound != null) sound.Unpause();
        }

        public static AudioClip GetAudioClip(string audioName)
        {
            var audio = GetAudio(audioName);
            return audio?.clip;
        }

        private static Audio GetAudio(string audioName)
        {
            if (Instance._soundMap.TryGetValue(audioName, out var audio)) return audio;

            Debug.LogWarning("[AudioManager] Clip not found: " + audioName);
            return null;
        }

        #endregion


        #region PauseGame

        // Lista TEMPORAL de Audios que se pausan
        private static readonly List<AudioSource> PausedSources = new();

        // Cuando se Pause el Juego, debemos parar TODOS excepto la música, que estaría bien bajarle el volumen
        private static void OnPauseGame()
        {
            // Bajamos el volumen de la música
            Instance.mixer.SetFloat(MIXER_MUSIC_TAG, LinearToLogAudio(GlobalVolume / 10));

            // Muteamos los SFX que no sean del menu
            var nonMusicAudioPlaying = Instance._soundMap.Values.Where(
                audio =>
                    audio.IsPlaying && audio != Instance.musicAudio
            );
            foreach (var audio in nonMusicAudioPlaying)
            {
                PausedSources.Add(audio.source);
                audio.Pause();
            }

            // Muteamos los sonidos espaciales
            var spatialSources = FindSpatialAudioSources();
            foreach (var source in spatialSources)
                if (source.isPlaying)
                {
                    PausedSources.Add(source);
                    source.Pause();
                }
        }

        private static void OnUnpauseGame()
        {
            // Quita la pausa a todos los sonidos pausados anteriormente, y limpia la lista
            foreach (var source in PausedSources) source.UnPause();
            PausedSources.Clear();

            // Vuelve el volumen de la música a su valor original
            UpdateMixerVolumes();
        }

        #endregion

        #region Settings

        private void LoadVolumeSettings()
        {
            Settings.Load();
            UpdateMixerVolumes();
        }

        private void SaveVolumeSettings() => Settings.Save();

        #endregion

        #region Spatial Sounds

        // Audios instanciados por la escena, no asociados al AudioManager
        private static AudioSource[] FindSpatialAudioSources()
        {
            var spatialSources = FindObjectsOfType<AudioSource>().ToList();

            // Quita todos los sonidos que no sean espaciales (Musica, ambiente y aux)
            spatialSources.Remove(Instance.musicAudio.source);
            spatialSources.Remove(Instance.ambientAudio.source);
            spatialSources.Remove(Instance.auxAudio.source);

            // Y quita también sonidos asociados al AudioManager (no deben ser espaciales)
            foreach (var audio in Instance.sounds) spatialSources.Remove(audio.source);

            return spatialSources.ToArray();
        }

        #endregion

        #region UTILITIES

        private static float LinearToLogAudio(float t) => Mathf.Log10(Mathf.Max(t, .00001f)) * 20;

        public static bool IsPlaying(string clipName) => GetAudio(clipName).IsPlaying;

        #endregion
    }
}