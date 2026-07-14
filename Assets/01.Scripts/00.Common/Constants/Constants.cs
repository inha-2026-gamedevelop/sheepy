// Unity
using UnityEngine;

namespace Minsung.Common
{
    // 프로젝트 전역 상수 관리 파일.
    public static partial class Constants
    {
        /****************************************
        *           Layer / Tag / Scene
        ****************************************/

        public static class Layer
        {
            public const string GROUND   = "Ground";
            public const string PLAYER   = "Player";
            public const string CLONE    = "Clone";
            public const string ENEMY    = "Enemy";
            public const string ITEM     = "Item";
            public const string HAZARD   = "Hazard";
        }

        public static class Tag
        {
            public const string PLAYER   = "Player";
            public const string CLONE    = "Clone";
            public const string ENEMY    = "Enemy";
            public const string BOSS     = "Boss";
            public const string ITEM     = "Item";
        }

        public static class Scene
        {
            public const string MAIN_MENU = "MainMenu";
            public const string LOADING   = "Loading";
            public const string PAUSE     = "Pause";
            public const string MAP_1     = "Map1";
            public const string MAP_2     = "Map2";
            public const string BOSS      = "Boss";
        }

        // 시스템 전역 입력 (플레이어 입력이 아닌 UI/흐름 제어용 키는 Constants.Player와 분리)
        public static class System
        {
            public const KeyCode KEY_PAUSE = KeyCode.Escape;
        }
    }
}
