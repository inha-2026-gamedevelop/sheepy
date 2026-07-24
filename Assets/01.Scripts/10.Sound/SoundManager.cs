// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.Sound
{
    // 지속형 SFX 한 채널의 상태를 묶어 관리하는 데이터 클래스
    public class DurationAudioData
    {
        /****************************************
        *                Fields
        ****************************************/

        public ESfxState State    { get; private set; } // 재생 중인 SFX 카테고리 키
        public int       Index    { get; private set; } // 카테고리 내 클립 인덱스
        public int       Identity { get; private set; } // 호출자 식별자 (같은 사운드라도 주체별 구분용, -1이면 미지정)

        public AudioSource Source { get; private set; } // 실제 재생을 담당하는 AudioSource

        /****************************************
        *                Methods
        ****************************************/

        public DurationAudioData(AudioSource source)
        {
            Clear();
            Source = source;
            Source.playOnAwake = false;
        }

        /// <summary> 지정 클립을 재생하고 식별 키(카테고리/인덱스/식별자)를 기록 </summary>
        public void Play(AudioClip clip, ESfxState state, int index, int identity, float pitch, bool isLoop)
        {
            State    = state;
            Index    = index;
            Identity = identity;

            if (Source.isPlaying)
            {
                Source.Stop();
            }

            Source.loop  = isLoop;
            Source.pitch = pitch;
            Source.clip  = clip; // Pause/Resume 스냅샷을 위해 PlayOneShot 대신 clip 직접 할당
            Source.time  = 0f;
            Source.Play();
        }

        /// <summary> 키가 일치하는 사운드가 재생 중이면 정지. 반환값은 키 일치 여부 </summary>
        public bool TryStop(ESfxState state, int index, int identity)
        {
            if ((State != state) || (Index != index) || (Identity != identity))
            {
                return false;
            }

            // 씬 전환/Destroy 중에는 Source가 fake-null이 될 수 있음
            if (Source == null)
            {
                Clear();
                return true;
            }

            if (Source.isPlaying)
            {
                Source.Stop();
            }
            Clear();

            return true;
        }

        /// <summary> 식별 키를 미사용 상태로 되돌려 채널을 재사용 가능하게 만듦 </summary>
        public void Clear()
        {
            State    = ESfxState.NONE;
            Index    = -1;
            Identity = -1;
        }
    }

    // 게임 전역 사운드(BGM/SFX) 재생을 총괄하는 싱글톤 매니저
    [AddComponentMenu("Minsung/Sound Manager")]
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        // 씬에 직접 배치할 때만 인스펙터로 지정. 비어 있으면 Resources에서 자동 로드
        [SerializeField] private SoundData _soundDB;
        [SerializeField] private float _bgmCrossFadeDuration = 1f;

        [SerializeField] private float _bgmVolume = Constants.Audio.DEFAULT_BGM_VOLUME; // 현재 BGM 볼륨 (0~1)
        [SerializeField] private float _sfxVolume = Constants.Audio.DEFAULT_SFX_VOLUME; // 현재 SFX 볼륨 (0~1)

        private AudioSource _bgmSource;                                  // BGM 전용 채널 (loop 재생)
        private readonly List<AudioSource> _oneShotSources = new();      // 단발 SFX 풀 (겹침 재생 허용)
        private readonly List<DurationAudioData> _durationSources = new(); // 지속형 SFX 풀 (시작/정지를 직접 제어)

        // 같은 프레임대에 동일 클립이 여러 번 울려 소리가 뭉치는 현상 방지
        private AudioSource _bgmCrossFadeSource;
        private AudioSource _activeBgmSource;
        private AudioSource _inactiveBgmSource;

        private readonly Dictionary<AudioClip, int> _lastPlayFrame = new();

        // Resume하려면 무엇을 어디까지 어떤 설정으로 재생 중이었는지 알아야 해서 스냅샷으로 저장
        private class PausedSfxData
        {
            public AudioSource Source;
            public AudioClip   Clip;
            public float       Pitch;
            public float       Volume;
            public float       Time;
        }
        private readonly List<PausedSfxData> _pausedSfx = new();

        private Coroutine _coBgmCrossFade;
        private float     _bgmCrossFadeProgress = 1f;
        private float     _outgoingBgmGain      = 1f;

        private Transform _root;      // 모든 AudioSource를 묶는 부모 ("@Sound")
        private bool _isSfxPaused;    // SFX 전체 일시정지 상태 (중복 Pause/Resume 방지)

        /// <summary> BGM 볼륨 변경 통지 (설정 UI 연동용) </summary>
        public static event Action<float> OnBgmVolumeChanged;

        /// <summary> SFX 볼륨 변경 통지 (설정 UI 연동용) </summary>
        public static event Action<float> OnSfxVolumeChanged;

        public float BgmVolume => _bgmVolume;
        public float SfxVolume => _sfxVolume;

        private float BgmOutputVolume => _bgmVolume * Constants.Audio.BASE_BGM_VOLUME;
        private float SfxOutputVolume => _sfxVolume * Constants.Audio.BASE_SFX_VOLUME;

        /// <summary> 마지막으로 PlayBGM에 성공한 카테고리. BGM 존 트리거처럼 이전 곡으로 되돌아가야 하는 경우 조회용 </summary>
        public EBgm CurrentBgm { get; private set; }

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance            = null;
            OnBgmVolumeChanged  = null;
            OnSfxVolumeChanged  = null;
        }

        // 씬에 배치하지 않아도 동작하도록 씬 로드 후 자동 생성
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("SoundManager").AddComponent<SoundManager>();
            }
        }

        protected override void OnSingletonAwake()
        {
            if (_soundDB == null)
            {
                _soundDB = Resources.Load<SoundData>(SoundData.RESOURCES_PATH);
                if (_soundDB == null)
                {
                    Debug.LogError($"[SoundManager] SoundDB를 찾을 수 없습니다 (Resources/{SoundData.RESOURCES_PATH})");
                }
            }

            CreateAudioSources();
            PreloadAllSounds();
        }

        /****************************************
        *              BGM Methods
        ****************************************/

        /// <summary> BGM 재생. 이미 재생 중이면 새 클립과 크로스페이드한다. 카테고리에 클립이 여러 개면 그 중 하나를 무작위로 재생 </summary>
        public void PlayBGM(EBgm bgm, bool isLoop = true, float pitch = 1f)
        {
            PlayBGM(bgm, -1, isLoop, pitch);
        }

        /// <summary> BGM 재생. clipIndex로 카테고리 내 특정 클립을 지정 (-1이면 무작위) </summary>
        public void PlayBGM(EBgm bgm, int clipIndex, bool isLoop = true, float pitch = 1f)
        {
            AudioClip clip = _soundDB != null ? _soundDB.GetBgmClip(bgm, clipIndex) : null;
            if (clip == null)
            {
                return;
            }

            if ((_activeBgmSource == null) || !_activeBgmSource.isPlaying || (_bgmCrossFadeDuration <= 0f))
            {
                PlayBgmImmediately(clip, isLoop, pitch);
            }
            else
            {
                StartBgmCrossFade(clip, isLoop, pitch);
            }

            CurrentBgm = bgm;
        }

        /// <summary> BGM 일시정지 (메뉴 열림 등에서 호출) </summary>
        public void PauseBGM()
        {
            _activeBgmSource?.Pause();
            _inactiveBgmSource?.Pause();
        }

        /// <summary> 일시정지한 BGM 재개 </summary>
        public void UnPauseBGM()
        {
            _activeBgmSource?.UnPause();
            _inactiveBgmSource?.UnPause();
        }

        /// <summary> BGM 완전 정지 </summary>
        public void StopBGM()
        {
            if (_coBgmCrossFade != null)
            {
                StopCoroutine(_coBgmCrossFade);
                _coBgmCrossFade = null;
            }
            _activeBgmSource?.Stop();
            _inactiveBgmSource?.Stop();
            _bgmCrossFadeProgress = 1f;
        }

        /// <summary> BGM 카테고리에서 클립만 조회 (재생하지 않음). 라디오처럼 BGM 카테고리의 클립을 SFX 채널로 재생하고 싶을 때 클립 해석용으로 사용 </summary>
        public AudioClip GetBgmClip(EBgm bgm, int clipIndex = -1)
        {
            return _soundDB != null ? _soundDB.GetBgmClip(bgm, clipIndex) : null;
        }

        /****************************************
        *              SFX Methods
        ****************************************/

        /// <summary> DB 기반 단발 SFX 재생 </summary>
        public void PlaySFX(ESfxState state, int index, float pitch = 1f)
        {
            AudioClip clip = _soundDB != null ? _soundDB.GetSfxClip(state, index) : null;
            PlaySFX(clip, pitch);
        }

        /// <summary> 클립 직접 지정 단발 SFX 재생 </summary>
        public void PlaySFX(AudioClip clip, float pitch = 1f)
        {
            if ((clip == null) || IsDuplicatePlay(clip))
            {
                return;
            }

            AudioSource source = GetOneShotSource();
            source.pitch  = pitch;
            source.volume = SfxOutputVolume;
            source.clip   = clip; // Pause 시점에 클립을 알 수 있도록 PlayOneShot 대신 직접 할당
            source.time   = 0f;
            source.Play();
        }

        /// <summary> 볼륨 배율을 별도로 지정하는 단발 SFX 재생 (전역 SFX 볼륨 x 배율) </summary>
        public void PlaySFXVolume(ESfxState state, int index, float volumeScale = 1f)
        {
            AudioClip clip = _soundDB != null ? _soundDB.GetSfxClip(state, index) : null;
            if ((clip == null) || IsDuplicatePlay(clip))
            {
                return;
            }

            AudioSource source = GetOneShotSource();
            source.pitch  = 1f;
            source.volume = Mathf.Clamp01(SfxOutputVolume * volumeScale);
            source.clip   = clip;
            source.time   = 0f;
            source.Play();
        }

        /// <summary> 지속형 SFX 재생 (리와인드 루프, 레버 회전음 등). 같은 키로 StopSFX_Duration을 호출해 수동 정지하며, identity는 같은 사운드를 여러 주체가 쓸 때 구분용(예: GetInstanceID) </summary>
        public void PlaySFX_Duration(ESfxState state, int index, int identity = -1, float pitch = 1f, bool isLoop = false)
        {
            AudioClip clip = _soundDB != null ? _soundDB.GetSfxClip(state, index) : null;
            if ((clip == null) || IsDuplicatePlay(clip))
            {
                return;
            }

            DurationAudioData data = GetDurationAudio();
            data.Source.volume = SfxOutputVolume;
            data.Play(clip, state, index, identity, pitch, isLoop);
        }

        /// <summary> 클립을 직접 지정하는 지속형 SFX 재생. 라디오처럼 BGM 카테고리에서 고른 클립 등 SFX DB 인덱스에 매이지 않는 클립을 SFX 채널로 재생할 때 사용. StopSFX_Duration(ESfxState.NONE, -1, identity)로 정지 </summary>
        public void PlaySFX_Duration(AudioClip clip, int identity = -1, float pitch = 1f, bool isLoop = false)
        {
            if ((clip == null) || IsDuplicatePlay(clip))
            {
                return;
            }

            DurationAudioData data = GetDurationAudio();
            data.Source.volume = SfxOutputVolume;
            data.Play(clip, ESfxState.NONE, -1, identity, pitch, isLoop);
        }

        /// <summary> 키가 일치하는 지속형 SFX를 찾아 정지. 첫 일치 채널만 처리 </summary>
        public void StopSFX_Duration(ESfxState state, int index, int identity = -1)
        {
            for (int i = 0; i < _durationSources.Count; ++i)
            {
                if (_durationSources[i].TryStop(state, index, identity))
                {
                    return;
                }
            }
        }

        /****************************************
        *           Pause / Resume
        ****************************************/

        /// <summary> 재생 중인 모든 SFX를 스냅샷(클립/피치/볼륨/재생 위치) 저장 후 일시 정지 - ResumeAllSFX로 이어서 재생 가능 </summary>
        public void PauseAllSFX()
        {
            if (_isSfxPaused)
            {
                return;
            }
            _isSfxPaused = true;

            _pausedSfx.Clear();

            for (int i = 0; i < _oneShotSources.Count; ++i)
            {
                CaptureAndStopIfPlaying(_oneShotSources[i]);
            }

            for (int i = 0; i < _durationSources.Count; ++i)
            {
                CaptureAndStopIfPlaying(_durationSources[i].Source);
            }
        }

        /// <summary> PauseAllSFX로 저장한 스냅샷 기반으로 효과음을 동일 상태로 재개 </summary>
        public void ResumeAllSFX()
        {
            if (!_isSfxPaused)
            {
                return;
            }
            _isSfxPaused = false;

            for (int i = 0; i < _pausedSfx.Count; ++i)
            {
                PausedSfxData data = _pausedSfx[i];
                if ((data.Source == null) || (data.Clip == null))
                {
                    continue;
                }

                data.Source.clip   = data.Clip;
                data.Source.pitch  = data.Pitch;
                data.Source.volume = data.Volume;
                data.Source.time   = Mathf.Clamp(data.Time, 0f, data.Clip.length);
                data.Source.Play();
            }

            _pausedSfx.Clear();
        }

        /****************************************
        *              Volume
        ****************************************/

        /// <summary> BGM 볼륨 설정 (설정 UI 슬라이더에서 호출) </summary>
        public void SetBgmVolume(float value)
        {
            _bgmVolume = Mathf.Clamp01(value);

            if (_coBgmCrossFade != null)
            {
                _activeBgmSource.volume = _bgmCrossFadeProgress * BgmOutputVolume;
                _inactiveBgmSource.volume = (1f - _bgmCrossFadeProgress) * _outgoingBgmGain * BgmOutputVolume;
            }
            else if (_activeBgmSource != null)
            {
                _activeBgmSource.volume = BgmOutputVolume;
            }

            OnBgmVolumeChanged?.Invoke(_bgmVolume);
        }

        /// <summary> SFX 볼륨 설정. 재생 중인 SFX에도 즉시 반영 </summary>
        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);

            for (int i = 0; i < _oneShotSources.Count; ++i)
            {
                _oneShotSources[i].volume = SfxOutputVolume;
            }

            for (int i = 0; i < _durationSources.Count; ++i)
            {
                _durationSources[i].Source.volume = SfxOutputVolume;
            }

            OnSfxVolumeChanged?.Invoke(_sfxVolume);
        }

        /****************************************
        *           private Methods
        ****************************************/

        // "@Sound" 루트 아래에 채널/풀별 AudioSource를 생성
        private void CreateAudioSources()
        {
            _root = new GameObject("@Sound").transform;
            _root.SetParent(transform);

            _bgmSource             = CreateSource("BGM");
            _bgmSource.loop        = true;
            _bgmSource.volume      = BgmOutputVolume;
            _bgmCrossFadeSource = CreateSource("BGM Crossfade");
            _activeBgmSource     = _bgmSource;
            _inactiveBgmSource   = _bgmCrossFadeSource;

            for (int i = 0; i < Constants.Audio.ONESHOT_POOL_SIZE; ++i)
            {
                _oneShotSources.Add(CreateSource($"OneShot[{i}]"));
            }

            for (int i = 0; i < Constants.Audio.DURATION_POOL_SIZE; ++i)
            {
                _durationSources.Add(new DurationAudioData(CreateSource($"Duration[{i}]")));
            }
        }

        // 루트 자식으로 2D AudioSource 오브젝트를 생성
        private AudioSource CreateSource(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root);

            AudioSource source  = go.AddComponent<AudioSource>();
            source.playOnAwake  = false;
            source.loop         = false;
            source.spatialBlend = 0f; // 2D 프로젝트라 공간 음향 미사용
            return source;
        }

        /****************************************
        *           BGM Cross Fade
        ****************************************/

        private void PlayBgmImmediately(AudioClip clip, bool isLoop, float pitch)
        {
            if (_coBgmCrossFade != null)
            {
                StopCoroutine(_coBgmCrossFade);
                _coBgmCrossFade = null;
            }

            _inactiveBgmSource.Stop();
            ConfigureBgmSource(_activeBgmSource, clip, isLoop, pitch, BgmOutputVolume);
            _bgmCrossFadeProgress = 1f;
        }

        private void StartBgmCrossFade(AudioClip clip, bool isLoop, float pitch)
        {
            if (_coBgmCrossFade != null)
            {
                StopCoroutine(_coBgmCrossFade);
                _coBgmCrossFade = null;
            }

            _inactiveBgmSource.Stop();

            AudioSource outgoing = _activeBgmSource;
            AudioSource incoming = _inactiveBgmSource;
            _outgoingBgmGain = (BgmOutputVolume > 0f) ? Mathf.Clamp01(outgoing.volume / BgmOutputVolume) : 1f;

            ConfigureBgmSource(incoming, clip, isLoop, pitch, 0f);

            _activeBgmSource      = incoming;
            _inactiveBgmSource    = outgoing;
            _bgmCrossFadeProgress = 0f;
            _coBgmCrossFade = StartCoroutine(CoCrossFadeBgm(outgoing, incoming, _outgoingBgmGain));
        }

        private void ConfigureBgmSource(AudioSource source, AudioClip clip, bool isLoop, float pitch, float volume)
        {
            source.loop   = isLoop;
            source.pitch  = pitch;
            source.volume = volume;
            source.clip   = clip;
            source.time   = 0f;
            source.Play();
        }

        private IEnumerator CoCrossFadeBgm(AudioSource outgoing, AudioSource incoming, float outgoingGain)
        {
            while (_bgmCrossFadeProgress < 1f)
            {
                _bgmCrossFadeProgress += Time.unscaledDeltaTime / _bgmCrossFadeDuration;
                float progress = Mathf.Clamp01(_bgmCrossFadeProgress);
                outgoing.volume = (1f - progress) * outgoingGain * BgmOutputVolume;
                incoming.volume = progress * BgmOutputVolume;
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            _coBgmCrossFade = null;
        }

        // 단발 SFX 풀에서 재생 중이 아닌 소스를 반환. 모두 사용 중이면 동적 추가
        private AudioSource GetOneShotSource()
        {
            for (int i = 0; i < _oneShotSources.Count; ++i)
            {
                if (!_oneShotSources[i].isPlaying)
                {
                    return _oneShotSources[i];
                }
            }

            AudioSource added = CreateSource($"OneShot[{_oneShotSources.Count}]");
            _oneShotSources.Add(added);
            return added;
        }

        // 지속형 SFX 풀에서 사용 가능한 채널을 반환. 모두 사용 중이면 동적 추가
        private DurationAudioData GetDurationAudio()
        {
            for (int i = 0; i < _durationSources.Count; ++i)
            {
                if (!_durationSources[i].Source.isPlaying)
                {
                    _durationSources[i].Clear();
                    return _durationSources[i];
                }
            }

            DurationAudioData added = new DurationAudioData(CreateSource($"Duration[{_durationSources.Count}]"));
            _durationSources.Add(added);
            return added;
        }

        // 동일 클립이 DEDUPE_FRAME_GAP 프레임 이내에 재요청되면 true (중복으로 판정해 무시)
        private bool IsDuplicatePlay(AudioClip clip)
        {
            int currentFrame = Time.frameCount;

            if (_lastPlayFrame.TryGetValue(clip, out int lastFrame)
                && (currentFrame - lastFrame < Constants.Audio.DEDUPE_FRAME_GAP))
            {
                return true;
            }

            _lastPlayFrame[clip] = currentFrame;
            return false;
        }

        // 재생 중인 소스의 상태를 스냅샷으로 저장하고 정지
        private void CaptureAndStopIfPlaying(AudioSource source)
        {
            if ((source == null) || !source.isPlaying || (source.clip == null))
            {
                return;
            }

            _pausedSfx.Add(new PausedSfxData
            {
                Source = source,
                Clip   = source.clip,
                Pitch  = source.pitch,
                Volume = source.volume,
                Time   = source.time,
            });

            source.Stop();
        }

        // 모든 클립의 오디오 데이터를 비동기로 미리 디코딩 (첫 재생 끊김 방지)
        private void PreloadAllSounds()
        {
            if (_soundDB == null)
            {
                return;
            }

            BgmData[] bgmDatas = _soundDB.BgmDatas;
            if (bgmDatas != null)
            {
                for (int i = 0; i < bgmDatas.Length; ++i)
                {
                    AudioClip[] clips = bgmDatas[i]?.Clips;
                    if (clips == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < clips.Length; ++j)
                    {
                        PreloadClip(clips[j]);
                    }
                }
            }

            SfxData[] sfxDatas = _soundDB.SfxDatas;
            if (sfxDatas != null)
            {
                for (int i = 0; i < sfxDatas.Length; ++i)
                {
                    AudioClip[] clips = sfxDatas[i]?.Clips;
                    if (clips == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < clips.Length; ++j)
                    {
                        PreloadClip(clips[j]);
                    }
                }
            }
        }

        // 아직 로드되지 않은 클립만 비동기 로드
        private void PreloadClip(AudioClip clip)
        {
            if ((clip != null) && (clip.loadState != AudioDataLoadState.Loaded))
            {
                clip.LoadAudioData();
            }
        }
    }
}
