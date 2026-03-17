using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (音频播放器)")]
    [Tooltip("专门用于播放背景音乐")]
    public AudioSource bgmSource;
    [Tooltip("专门用于播放环境音效（如持续的雨声、机器嗡嗡声）")]
    public AudioSource ambientSource;
    [Tooltip("专门用于播放UI音效，使用PlayOneShot支持多音效重叠")]
    public AudioSource uiSource;

    [Header("Audio Mixer (混音器设置)")]
    public AudioMixer masterMixer;

    private Tween bgmFadeTween;
    private Tween ambientFadeTween;

    private void Awake()
    {
        // 标准单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region BGM 控制
    /// <summary>
    /// 播放背景音乐（带淡入效果）
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null) return;
        
        // 如果当前正在播放同一首曲子，则不重新播放
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmFadeTween?.Kill(); // 停止当前可能正在进行的渐变动画

        bgmSource.clip = clip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        bgmFadeTween = bgmSource.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 停止背景音乐（带淡出效果）
    /// </summary>
    public void StopBGM(float fadeDuration = 1f)
    {
        bgmFadeTween?.Kill();

        bgmFadeTween = bgmSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => bgmSource.Stop());
    }
    #endregion

    #region 环境音 (Ambient) 控制
    /// <summary>
    /// 播放环境音效（带淡入效果）
    /// </summary>
    public void PlayAmbient(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null) return;

        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientFadeTween?.Kill();

        ambientSource.clip = clip;
        ambientSource.volume = 0f;
        ambientSource.Play();

        ambientFadeTween = ambientSource.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 停止环境音效（带淡出效果）
    /// </summary>
    public void StopAmbient(float fadeDuration = 1f)
    {
        ambientFadeTween?.Kill();

        ambientFadeTween = ambientSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => ambientSource.Stop());
    }
    #endregion

    #region UI 音效控制
    /// <summary>
    /// 播放单次UI音效
    /// </summary>
    public void PlayUISFX(AudioClip clip, float pitch = 1f)
    {
        if (clip != null)
        {
            uiSource.pitch = pitch; // 改变播放器的音高
            uiSource.PlayOneShot(clip);
        }
    }
    #endregion

    #region 音量控制 (供设置面板调用)
    // 注意：Mixer 的音量是是对数控制的，通常范围是 -80dB 到 0dB（或 +20dB）
    // 这里传入的 volume 参数通常是 UI Slider 的值 (0.0001f 到 1f)
    public void SetMasterVolume(float volume)
    {
        masterMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
    }

    public void SetBGMVolume(float volume)
    {
        masterMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
    }

    public void SetSFXVolume(float volume) // 包含 UI 和 Ambient
    {
        masterMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
    }
    #endregion
}