// Unity
using UnityEngine;
using UnityEngine.Serialization;

using Minsung.Utility;

namespace Minsung.UI
{
    // UI가 참조하는 공용 스프라이트 보관소 (키 가이드 등).
    public class SpriteReference : PersistentSingleton<SpriteReference>
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("키 가이드 스프라이트")]
        [FormerlySerializedAs("spInteractive")]
        [SerializeField] private Sprite _interactiveKeySprite; // 상호작용 (E키)

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 키 가이드 종류에 대응하는 스프라이트 반환. 미등록 종류면 null. </summary>
        public Sprite GetKeySprite(EKeyGuide keyGuide)
        {
            switch (keyGuide)
            {
                case EKeyGuide.Interactive:
                    return _interactiveKeySprite;
                default:
                    return null;
            }
        }
    }
}
