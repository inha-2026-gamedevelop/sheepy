// Unity
using Unity.Cinemachine;
using UnityEngine;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.CameraSystem
{
    // 포커스 연출(라디오 등) 전용 카메라 매니저. 씬에 하나만 존재하며 상호작용 오브젝트는 알지 못한다.
    public class CameraManager : PersistentSingleton<CameraManager>
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("가상 카메라")]
        [SerializeField] private CinemachineCamera _playerCamera; // 평소 플레이 카메라
        [SerializeField] private CinemachineCamera _focusCamera;  // 포커스 연출 전용 카메라 (씬에 하나, 여러 상호작용 오브젝트가 공유)
        [SerializeField] private CinemachineBrain  _brain;        // 블렌드 시간 조절용

        /****************************************
        *              Unity Event
        ****************************************/

        protected override void OnSingletonAwake()
        {
            SetPriority(_playerCamera, Constants.Camera.PRIORITY_DEFAULT);
            SetPriority(_focusCamera, Constants.Camera.PRIORITY_FOCUS_IDLE);

            SetOrthographicSize(_playerCamera, Constants.Camera.PLAYER_ORTHOGRAPHIC_SIZE);
            SetOrthographicSize(_focusCamera, Constants.Camera.FOCUS_ORTHOGRAPHIC_SIZE);
        }

        /****************************************
        *                Methods
        ****************************************/
        // 포커스 카메라를 특정 위치/회전으로 이동시키고 우선순위를 높여 포커스 연출을 시작한다.
        public void Focus(Transform cameraTip, float orthographicSize, float blendTime = Constants.Camera.DEFAULT_BLEND_TIME)
        {
            if ((_focusCamera == null) || (cameraTip == null))
            {
                return;
            }

            Vector3 focusPosition = cameraTip.position;
            focusPosition.z = _focusCamera.transform.position.z;
            _focusCamera.transform.position = focusPosition;

            SetOrthographicSize(_focusCamera, orthographicSize);
            SetBlendTime(blendTime);
            SetPriority(_focusCamera, Constants.Camera.PRIORITY_FOCUS);
        }

        // 포커스를 해제하고 플레이어 카메라로 복귀한다.
        public void UnFocus()
        {
            if (_focusCamera == null)
            {
                return;
            }
            SetPriority(_focusCamera, Constants.Camera.PRIORITY_FOCUS_IDLE);
        }

        // 플레이어 카메라 렌즈(Orthographic Size)를 임시로 변경한다 - 보스 기믹 등 연출용
        public void SetPlayerOrthographicSize(float size)
        {
            SetOrthographicSize(_playerCamera, size);
        }

        // 플레이어 카메라 렌즈를 기본값(PLAYER_ORTHOGRAPHIC_SIZE)으로 되돌린다
        public void ResetPlayerOrthographicSize()
        {
            SetOrthographicSize(_playerCamera, Constants.Camera.PLAYER_ORTHOGRAPHIC_SIZE);
        }

        // 카메라 우선순위 설정
        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
            {
                return;
            }

            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value   = priority;
            camera.Priority  = settings;
        }

        // 카메라 렌즈 설정
        private static void SetOrthographicSize(CinemachineCamera camera, float size)
        {
            if (camera == null)
            {
                return;
            }

            LensSettings lens = camera.Lens;
            lens.OrthographicSize = size;
            camera.Lens = lens;

            Debug.Log($"[{nameof(CameraManager)}] SetOrthographicSize: {camera.name} = {size}");
        }
    
        // 카메라 블렌드 시간 설정
        private void SetBlendTime(float blendTime)
        {
            if (_brain == null)
            {
                return;
            }

            CinemachineBlendDefinition blend = _brain.DefaultBlend;
            blend.Time      = blendTime;
            _brain.DefaultBlend = blend;
        }

        /// <summary> 플레이어 카메라 줌 변경 (보스전 아레나 줌아웃 등). 되돌릴 땐 ResetPlayerZoom 호출 </summary>
        public void SetPlayerZoom(float orthographicSize)
        {
            SetOrthographicSize(_playerCamera, orthographicSize);
        }

        /// <summary> 플레이어 카메라 줌을 평소 값(PLAYER_ORTHOGRAPHIC_SIZE)으로 복귀 </summary>
        public void ResetPlayerZoom()
        {
            SetOrthographicSize(_playerCamera, Constants.Camera.PLAYER_ORTHOGRAPHIC_SIZE);
        }
    }
}
