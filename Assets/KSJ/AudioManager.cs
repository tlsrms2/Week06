using System.Net;
using UnityEngine;
using static AudioManager;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("#BGM")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    public int channels_bgm;
    AudioSource[] bgmPlayers;
    int channelIndex_bgm;

    [Header("#SFX")]
    public AudioClip[] sfxClips;
    public float sfxVolume;
    public int channels;
    AudioSource[] sfxPlayers;
    int channelIndex;

    public enum Bgm { ingame, damaged };
    public enum Sfx { cursor, hit, startButton , stay , cardSlide , cardFlip, cardReturn, 
        dealerStab, dealer, whoosh, Ending, ingame, damaged, S1, S2, M1, M2, L, eye1, eye2, eye3, eye4 };
    void Awake()
    {
        instance = this;
        Init();
    }

    void Init()
    {
        //배경음 플레이어 초기화
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayers = new AudioSource[channels];
        for (int index = 0; index < bgmPlayers.Length; index++)
        {
            bgmPlayers[index] = bgmObject.AddComponent<AudioSource>();
            bgmPlayers[index].playOnAwake = false;
            bgmPlayers[index].volume = bgmVolume;
        }

        //효과음 플레이어 초기화
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
        }

    }

    public void PlayBgm(Bgm bgm)
    {
        for (int index = 0; index < bgmPlayers.Length; index++)
        {
            int loopIndex = (index + channelIndex) % bgmPlayers.Length;

            if (bgmPlayers[loopIndex].isPlaying)
                continue;
            channelIndex = loopIndex;
            bgmPlayers[loopIndex].clip = bgmClips[(int)bgm];
            bgmPlayers[loopIndex].Play();
            break;
        }
    }

    public void PlaySfx(Sfx sfx)
    {
        for (int index = 0; index < sfxPlayers.Length; index++){
            int loopIndex = (index + channelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying)
                continue;
            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }

    //Bgm이나 Sfx 사용하고 싶은 곳에 넣을 코드
    //AudioManager.instance.PlayBgm(true);
    //AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);

}
