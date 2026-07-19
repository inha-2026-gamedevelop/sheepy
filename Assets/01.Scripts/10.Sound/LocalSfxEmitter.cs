// Unity
using UnityEngine;

namespace Minsung.Sound
{
    // 개별 오브젝트용 SFX 재생기. 프리팹/인스턴스마다 클립을 독립적으로 지정할 수 있다
    [RequireComponent(typeof(AudioSource))]
    public class LocalSfxEmitter : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private AudioSource _audioSource;

        [Header("단발 SFX")]
        [SerializeField] private AudioClip _interactClip;
        [SerializeField] private AudioClip _activateClip;
        [SerializeField] private AudioClip _deactivateClip;
        [SerializeField] private AudioClip _attackClip;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _deathClip;

        [Header("지속 SFX")]
        [SerializeField] private AudioClip _loopClip;

        [SerializeField, Range(0f, 1f)] private float _volumeScale = 1f;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_audioSource == null)
            {
                TryGetComponent(out _audioSource);
            }
        }

        private void OnDisable()
        {
            StopLoop();
        }

        /****************************************
        *                Methods
        ****************************************/

        public void PlayInteract() { PlayOneShot(_interactClip); }
        public void PlayActivate() { PlayOneShot(_activateClip); }
        public void PlayDeactivate() { PlayOneShot(_deactivateClip); }
        public void PlayAttack() { PlayOneShot(_attackClip); }
        public void PlayHit() { PlayOneShot(_hitClip); }
        public void PlayDeath() { PlayOneShot(_deathClip); }

        public void PlayLoop()
        {
            if ((_audioSource == null) || (_loopClip == null) ||
                ((_audioSource.isPlaying) && (_audioSource.clip == _loopClip)))
            {
                return;
            }

            _audioSource.clip   = _loopClip;
            _audioSource.loop   = true;
            _audioSource.volume = GetVolume();
            _audioSource.Play();
        }

        public void StopLoop()
        {
            if ((_audioSource == null) || (!_audioSource.loop))
            {
                return;
            }

            _audioSource.Stop();
            _audioSource.loop = false;
            _audioSource.clip = null;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if ((_audioSource == null) || (clip == null))
            {
                return;
            }

            _audioSource.PlayOneShot(clip, GetVolume());
        }

        private float GetVolume()
        {
            if (SoundManager.Instance != null)
            {
                return _volumeScale * SoundManager.Instance.SfxVolume;
            }

            return _volumeScale;
        }
    }
}
