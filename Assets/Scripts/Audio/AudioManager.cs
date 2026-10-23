using UnityEngine;

namespace GlitchRun.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music Clips")]
        [SerializeField] private AudioClip bgmMenu;
        [SerializeField] private AudioClip bgmGameplay;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip sfxJump;
        [SerializeField] private AudioClip sfxPickup;
        [SerializeField] private AudioClip sfxGlitchOn;
        [SerializeField] private AudioClip sfxGlitchOff;
        [SerializeField] private AudioClip sfxDamage;
        [SerializeField] private AudioClip sfxDeath;
        [SerializeField] private AudioClip sfxCheckpoint;
        [SerializeField] private AudioClip sfxVictory;
        [SerializeField] private AudioClip sfxClick;

        private float masterVolume = 1f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 1f;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            LoadSettings();
            LoadDefaultClips();
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.9f);
            ApplyVolumes();
        }

        private void LoadDefaultClips()
        {
            if (bgmMenu == null) bgmMenu = Resources.Load<AudioClip>("Audio/Music/bgm_menu");
            if (bgmGameplay == null) bgmGameplay = Resources.Load<AudioClip>("Audio/Music/bgm_gameplay");
            if (sfxJump == null) sfxJump = Resources.Load<AudioClip>("Audio/SFX/sfx_jump");
            if (sfxPickup == null) sfxPickup = Resources.Load<AudioClip>("Audio/SFX/sfx_pickup");
            if (sfxGlitchOn == null) sfxGlitchOn = Resources.Load<AudioClip>("Audio/SFX/sfx_glitch_on");
            if (sfxGlitchOff == null) sfxGlitchOff = Resources.Load<AudioClip>("Audio/SFX/sfx_glitch_off");
            if (sfxDamage == null) sfxDamage = Resources.Load<AudioClip>("Audio/SFX/sfx_damage");
            if (sfxDeath == null) sfxDeath = Resources.Load<AudioClip>("Audio/SFX/sfx_death");
            if (sfxCheckpoint == null) sfxCheckpoint = Resources.Load<AudioClip>("Audio/SFX/sfx_checkpoint");
            if (sfxVictory == null) sfxVictory = Resources.Load<AudioClip>("Audio/SFX/sfx_victory");
            if (sfxClick == null) sfxClick = Resources.Load<AudioClip>("Audio/SFX/sfx_ui_click");
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
            if (sfxSource != null) sfxSource.volume = masterVolume * sfxVolume;
        }

        public void PlayMusic(string track)
        {
            AudioClip clip = (track.ToLower() == "menu") ? bgmMenu : bgmGameplay;
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlaySfx(string soundName)
        {
            if (sfxSource == null) return;

            AudioClip clip = soundName.ToLower() switch
            {
                "jump" => sfxJump,
                "pickup" => sfxPickup,
                "glitch_on" => sfxGlitchOn,
                "glitch_off" => sfxGlitchOff,
                "damage" => sfxDamage,
                "death" => sfxDeath,
                "checkpoint" => sfxCheckpoint,
                "victory" => sfxVictory,
                "click" => sfxClick,
                _ => null
            };

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
            }
        }
    }
}
