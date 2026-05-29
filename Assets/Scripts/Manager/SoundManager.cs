using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FxType
{
    Xe = 0,
    Do = 1,
    Pickup1 = 2,
    Place1 = 3,
    Pickup2 = 4,
    Pickup3 = 5,
    Soup = 6,
    OvenClose = 7,
    OvenDone = 8,
    OvenRunning = 9,
    Fire = 10,

    EmojiPositive = 11,
    EmojiNegative = 12,
    None = 20,
}

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource SfxSource { get; private set; }
    public AudioClip[] audioClips;
    public AudioSource bgm;
    private AudioSource[] fx = new AudioSource[30];
    bool isMute = false;
    public bool IsMute => isMute;
    protected override void Awake()
    {
        base.Awake();

        if (SfxSource == null)
        {
            SfxSource = gameObject.AddComponent<AudioSource>();
            SfxSource.playOnAwake = false;
        }
    }

    public void PlayFx(FxType fxType)
    {
        if (fxType == FxType.None) return;
        if (!isMute)
        {
            if (fx[(int)fxType] == null)
            {
                fx[(int)fxType] = new GameObject().AddComponent<AudioSource>();
                fx[(int)fxType].clip = audioClips[(int)fxType];
            }

            fx[(int)fxType].Play();
        }
    }
    public static AudioSource PlaySfx(AudioClip clip, float volume = 1f, bool isLoop = false)
    {
        Instance.SfxSource.loop = isLoop;
        Instance.SfxSource.volume = volume;
        Instance.SfxSource.clip = clip;
        Instance.SfxSource.Play();
        return Instance.SfxSource;
    }
    public static void PlaySFX(params AudioClip[] clips)
    {
        if (Instance == null) return;
        if (Instance.SfxSource == null) return;
        if (clips == null || clips.Length == 0) return;

        AudioClip c = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (c == null) return;

        Instance.SfxSource.clip = c;
        Instance.SfxSource.Play();
    }


    public static void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (Instance == null) return;
        if (Instance.SfxSource == null) return;
        if (clip == null) return;

        Instance.SfxSource.volume = volume;
        Instance.SfxSource.clip = clip;
        Instance.SfxSource.Play();
    }
    public static void PlaySFXOneShot(AudioClip clip, float volume = 1f)
    {
        if (Instance == null) return;
        if (Instance.SfxSource == null) return;
        if (clip == null) return;

        Instance.SfxSource.PlayOneShot(clip, volume);
    }
    public void PlayFxIfNotPlay(FxType fxType)
    {
        if (fxType == FxType.None) return;
        if (!isMute)
        {
            if (fx[(int)fxType] == null)
            {
                fx[(int)fxType] = new GameObject().AddComponent<AudioSource>();
                fx[(int)fxType].clip = audioClips[(int)fxType];
            }

            if (!fx[(int)fxType].isPlaying) fx[(int)fxType].Play();
        }
    }

    public void PlaySoundLoop(FxType fxType)
    {
        if (fxType == FxType.None) return;
        if (!isMute)
        {
            if (fx[(int)fxType] == null)
            {
                fx[(int)fxType] = new GameObject().AddComponent<AudioSource>();
                fx[(int)fxType].clip = audioClips[(int)fxType];
                fx[(int)fxType].loop = true;
            }

            fx[(int)fxType].Play();
        }
    }

    public void StopSoundLoop(FxType fxType)
    {
        if (fxType == FxType.None) return;
        if (fx[(int)fxType] != null)
        {
            fx[(int)fxType].Stop();
        }
    }

    public IEnumerator IE_PlayFxAfterTime(FxType fxType, float time)
    {
        yield return new WaitForSeconds(time);
        if (!isMute)
        {
            if (fx[(int)fxType] == null)
            {
                fx[(int)fxType] = new GameObject().AddComponent<AudioSource>();
                fx[(int)fxType].clip = audioClips[(int)fxType];
            }

            fx[(int)fxType].Play();
        }
    }

    public void PlayFxAfterTime(FxType fxType, float time)
    {
        if (fxType == FxType.None) return;
        StartCoroutine(IE_PlayFxAfterTime(fxType, time));
    }

    public void Mute()
    {
        isMute = true;
        bgm.Stop();
        for (int i = 0; i < fx.Length; i++)
        {
            if (fx[i] != null)
            {
                fx[i].Stop();
            }
        }
    }

    public void PlayBgm()
    {
        if (isMute) return;
        bgm.Play();
    }
}