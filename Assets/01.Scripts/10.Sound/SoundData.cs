// System
using System;

// Unity
using UnityEngine;

namespace Minsung.Sound
{
    // SFX 분류. SoundData의 SfxDatas 배열 인덱스로 사용
    public enum ESfxState
    {
        Player,
        Time,
        Monster,
        Boss,
        Object,
        UI,
        NONE,
    }

    // BGM 클립 인덱스. SoundDB 에셋의 BgmDatas 배열 순서와 일치시킬 것
    public enum EBgm
    {
        Menu,
        Map1,
        Map2,
        Boss,
        BossPhase2,
        Ending,
        Radio, // 라디오 상호작용 시 재생되는 저장된 BGM
    }

    // 아래 enum들은 각 ESfxState 카테고리 안에서의 클립 인덱스
    // SoundDB 에셋의 clips 배열 순서와 일치시킬 것
    public enum EPlayerSfx
    {
        Jump,
        Land,
        Attack,
        ChargeAttack,
        Hit,
        Death,
    }

    public enum ETimeSfx
    {
        Rewind,
        CloneSpawn,
        CloneDeath,
        SlowOn,
        SlowOff,
    }

    public enum EMonsterSfx
    {
        Attack,
        Hit,
        Death,
    }

    public enum EBossSfx
    {
        Hit,
        PhaseChange,
        Laser,
    }

    public enum EObjectSfx
    {
        ItemPickup,
        EnhanceSuccess,
        EnhanceFail,
        Lever,
        Radio,
    }

    public enum EUISfx
    {
        ButtonClick,
        AchievementToast,
    }

    // 한 SFX 카테고리(ESfxState)가 보유한 클립 묶음
    [Serializable]
    public class SfxData
    {
        [SerializeField] private string _sfxName;    // 인스펙터 식별용 카테고리 이름
        [SerializeField] private AudioClip[] _clips; // 카테고리 내 클립 목록 (enum 인덱스 순서)

        public AudioClip[] Clips => _clips;
    }

    // 한 BGM 카테고리(EBgm)가 보유한 클립 묶음. 라디오처럼 곡이 여러 개면 재생할 때마다 그 중 하나를 무작위로 고른다
    [Serializable]
    public class BgmData
    {
        [SerializeField] private AudioClip[] _clips; // 카테고리 내 클립 목록. 1개면 항상 그 곡, 여러 개면 무작위 재생

        public AudioClip[] Clips => _clips;
    }

    // BGM/SFX 클립을 보관하는 사운드 DB
    // Resources/SoundDB 경로에 두면 SoundManager가 자동 로드한다
    [CreateAssetMenu(fileName = "SoundDB", menuName = "TheLastRewind/SoundDB")]
    public class SoundData : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        public const string RESOURCES_PATH = "SoundDB";

        [SerializeField] private BgmData[] _bgmDatas; // EBgm 순서와 일치
        [SerializeField] private SfxData[] _sfxDatas; // ESfxState 순서와 일치

        public BgmData[] BgmDatas => _bgmDatas;
        public SfxData[] SfxDatas => _sfxDatas;

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> BGM 클립 조회. 카테고리에 클립이 여러 개면 그 중 하나를 무작위로 반환 </summary>
        public AudioClip GetBgmClip(EBgm bgm)
        {
            return GetBgmClip(bgm, -1);
        }

        /// <summary> BGM 클립 조회. clipIndex가 유효한 범위면 해당 클립을, 아니면(-1 등) 카테고리 내 무작위 클립을 반환 </summary>
        public AudioClip GetBgmClip(EBgm bgm, int clipIndex)
        {
            int index = (int)bgm;
            if ((_bgmDatas == null) || (index < 0) || (index >= _bgmDatas.Length))
            {
                Debug.LogWarning($"[SoundData] BGM 카테고리 없음: {bgm}");
                return null;
            }

            AudioClip[] clips = _bgmDatas[index].Clips;
            if ((clips == null) || (clips.Length == 0))
            {
                Debug.LogWarning($"[SoundData] BGM 클립 없음: {bgm}");
                return null;
            }

            if ((clipIndex < 0) || (clipIndex >= clips.Length))
            {
                return clips[UnityEngine.Random.Range(0, clips.Length)];
            }

            return clips[clipIndex];
        }

        /// <summary> SFX 클립 조회. 카테고리/인덱스가 비어 있으면 null 반환 </summary>
        public AudioClip GetSfxClip(ESfxState state, int index)
        {
            int stateIndex = (int)state;
            if ((_sfxDatas == null) || (stateIndex < 0) || (stateIndex >= _sfxDatas.Length))
            {
                Debug.LogWarning($"[SoundData] SFX 카테고리 없음: {state}");
                return null;
            }

            AudioClip[] clips = _sfxDatas[stateIndex].Clips;
            if ((clips == null) || (index < 0) || (index >= clips.Length))
            {
                Debug.LogWarning($"[SoundData] SFX 클립 없음: {state}[{index}]");
                return null;
            }

            return clips[index];
        }
    }
}
