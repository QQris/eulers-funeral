using System.Collections;
using EulersFuneral.Core.Data;
using UnityEngine;

namespace EulersFuneral.Audio
{
    /// <summary>
    /// Sistema de Audio y Música Global de Euler's Funeral:
    /// - Música global continua para todo el juego en el orden:
    ///   MainTheme -> LevelsSong -> LevelsSong2 -> repetir (sin interrupciones al cambiar de escena).
    /// - Efectos procedurales: rodadura, rebote, estrella, victoria y derrota.
    /// </summary>
    public class ProceduralAudioSystem : MonoBehaviour
    {
        public static ProceduralAudioSystem Instance { get; private set; }

        private const int SampleRate = 44100;
        private const float BaseMusicVolume = 0.55f;

        [Header("Playlist Global (MainTheme -> LevelsSong -> LevelsSong2)")]
        public AudioClip MainThemeClip;
        public AudioClip LevelsSong1Clip;
        public AudioClip LevelsSong2Clip;

        private AudioSource _musicSource;
        private AudioSource _rollingSource;
        private AudioSource _sfxSource;

        private AudioClip _rollingClip;
        private AudioClip _bounceClip;
        private AudioClip _starClip;
        private AudioClip _victoryClip;
        private AudioClip _defeatClip;

        private float _masterVolume = 0.8f;
        private Coroutine _playlistRoutine;

        public float MasterVolume => _masterVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.MainThemeClip == null && MainThemeClip != null) Instance.MainThemeClip = MainThemeClip;
                if (Instance.LevelsSong1Clip == null && LevelsSong1Clip != null) Instance.LevelsSong1Clip = LevelsSong1Clip;
                if (Instance.LevelsSong2Clip == null && LevelsSong2Clip != null) Instance.LevelsSong2Clip = LevelsSong2Clip;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            _masterVolume = SaveSystem.Load().MasterVolume;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = BaseMusicVolume * _masterVolume;

            _rollingSource = gameObject.AddComponent<AudioSource>();
            _rollingSource.loop = true;
            _rollingSource.playOnAwake = false;
            _rollingSource.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            EnsureMusicClipsLoaded();
            SynthesizeAllClips();
            _rollingSource.clip = _rollingClip;
        }

        private void Start()
        {
            StartGlobalMusicPlaylistIfNeeded();
        }

        private void EnsureMusicClipsLoaded()
        {
            if (MainThemeClip == null)
                MainThemeClip = Resources.Load<AudioClip>("Music/MainTheme");

            if (LevelsSong1Clip == null)
                LevelsSong1Clip = Resources.Load<AudioClip>("Music/LevelsSong");

            if (LevelsSong2Clip == null)
                LevelsSong2Clip = Resources.Load<AudioClip>("Music/LevelsSong2");
        }

        public void StartGlobalMusicPlaylistIfNeeded()
        {
            EnsureMusicClipsLoaded();
            if (_playlistRoutine != null)
                return;

            _playlistRoutine = StartCoroutine(GlobalPlaylistLoopRoutine());
        }

        private IEnumerator GlobalPlaylistLoopRoutine()
        {
            AudioClip[] playlist = { MainThemeClip, LevelsSong1Clip, LevelsSong2Clip };
            int currentTrackIndex = 0;

            while (true)
            {
                EnsureMusicClipsLoaded();
                playlist[0] = MainThemeClip;
                playlist[1] = LevelsSong1Clip;
                playlist[2] = LevelsSong2Clip;

                AudioClip clip = playlist[currentTrackIndex];
                if (clip != null && _musicSource != null)
                {
                    _musicSource.clip = clip;
                    _musicSource.loop = false;
                    _musicSource.volume = BaseMusicVolume * _masterVolume;
                    _musicSource.Play();

                    // Esperar mientras la pista actual se reproduce
                    while (_musicSource != null && _musicSource.isPlaying)
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1.0f);
                }

                currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
            }
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);

            if (_musicSource != null)
            {
                _musicSource.volume = BaseMusicVolume * _masterVolume;
            }

            SaveProgressData data = SaveSystem.Load();
            data.MasterVolume = _masterVolume;
            SaveSystem.Save();
        }

        public void UpdateRollingSound(double speed, bool isOnSurfaceAndRunning)
        {
            if (_rollingSource == null) return;

            if (!isOnSurfaceAndRunning || speed < 0.02)
            {
                _rollingSource.volume = Mathf.MoveTowards(_rollingSource.volume, 0f, Time.deltaTime * 8f);
                if (_rollingSource.volume <= 0.001f && _rollingSource.isPlaying)
                {
                    _rollingSource.Pause();
                }
                return;
            }

            if (!_rollingSource.isPlaying)
            {
                _rollingSource.Play();
            }

            float normalizedSpeed = Mathf.Clamp01((float)(speed / 8.0));
            float targetVol = normalizedSpeed * 0.45f * _masterVolume;
            _rollingSource.volume = Mathf.Lerp(_rollingSource.volume, targetVol, Time.deltaTime * 12f);
            _rollingSource.pitch = Mathf.Lerp(0.75f, 1.65f, normalizedSpeed);
        }

        public void StopRollingSound()
        {
            if (_rollingSource != null)
            {
                _rollingSource.volume = 0f;
                _rollingSource.Stop();
            }
        }

        public void PlayBounce(double impactSpeed, double restitution)
        {
            if (restitution <= 0.0 || _sfxSource == null || _bounceClip == null)
                return;

            float intensity = Mathf.Clamp01((float)(impactSpeed / 6.0));
            _sfxSource.PlayOneShot(_bounceClip, Mathf.Max(0.15f, intensity) * _masterVolume);
        }

        public void PlayStarPickup(int starNumber)
        {
            if (_sfxSource == null || _starClip == null) return;
            _sfxSource.pitch = 1.0f + (starNumber - 1) * 0.15f;
            _sfxSource.PlayOneShot(_starClip, 0.7f * _masterVolume);
            _sfxSource.pitch = 1.0f;
        }

        public void PlayVictory()
        {
            StopRollingSound();
            if (_sfxSource != null && _victoryClip != null)
            {
                _sfxSource.PlayOneShot(_victoryClip, 0.8f * _masterVolume);
            }
        }

        public void PlayDefeat()
        {
            StopRollingSound();
            if (_sfxSource != null && _defeatClip != null)
            {
                _sfxSource.PlayOneShot(_defeatClip, 0.75f * _masterVolume);
            }
        }

        private void SynthesizeAllClips()
        {
            _rollingClip = CreateRollingLoopClip();
            _bounceClip = CreateBounceClip();
            _starClip = CreateStarChimeClip();
            _victoryClip = CreateVictoryClip();
            _defeatClip = CreateDefeatClip();
        }

        private static AudioClip CreateRollingLoopClip()
        {
            int samples = SampleRate;
            float[] data = new float[samples];
            var rng = new System.Random(1729);
            float filterState = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                filterState = 0.94f * filterState + 0.06f * white;
                float hum = 0.25f * Mathf.Sin(2f * Mathf.PI * 110f * t)
                          + 0.12f * Mathf.Sin(2f * Mathf.PI * 220f * t);
                data[i] = (filterState * 0.65f + hum) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("ProceduralRolling", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBounceClip()
        {
            int samples = SampleRate / 5;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-28f * t);
                float freq = Mathf.Lerp(260f, 85f, t / 0.2f);
                data[i] = env * Mathf.Sin(2f * Mathf.PI * freq * t);
            }

            AudioClip clip = AudioClip.Create("ProceduralBounce", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateStarChimeClip()
        {
            int samples = (int)(SampleRate * 0.35f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-9f * t);
                float tone = 0.6f * Mathf.Sin(2f * Mathf.PI * 880f * t)
                           + 0.4f * Mathf.Sin(2f * Mathf.PI * 1318.5f * t);
                data[i] = env * tone;
            }

            AudioClip clip = AudioClip.Create("ProceduralStarChime", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateVictoryClip()
        {
            int samples = (int)(SampleRate * 0.9f);
            float[] data = new float[samples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIdx = Mathf.Min(notes.Length - 1, (int)(t / 0.22f));
                float localT = t - noteIdx * 0.25f;
                float env = Mathf.Exp(-6f * Mathf.Max(0f, localT));
                data[i] = 0.6f * env * Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t);
            }

            AudioClip clip = AudioClip.Create("ProceduralVictory", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDefeatClip()
        {
            int samples = (int)(SampleRate * 0.75f);
            float[] data = new float[samples];
            float[] notes = { 392.0f, 349.23f, 311.13f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIdx = Mathf.Min(notes.Length - 1, (int)(t / 0.25f));
                float localT = t - noteIdx * 0.25f;
                float env = Mathf.Exp(-5f * localT);
                data[i] = 0.6f * env * Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t);
            }

            AudioClip clip = AudioClip.Create("ProceduralDefeat", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
