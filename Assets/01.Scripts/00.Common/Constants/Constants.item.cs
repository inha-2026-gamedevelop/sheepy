namespace Minsung.Common
{
    public static partial class Constants
    {
        public static class Item
        {
            // 포션
            public const float HP_POTION_RESTORE   = 30f;    // HP 포션 회복량
            public const float MP_POTION_RESTORE   = 40f;    // MP 포션 회복량

            // 드랍 확률 (0~1)
            public const float DROP_COMMON_RATE    = 0.60f;
            public const float DROP_RARE_RATE      = 0.25f;
            public const float DROP_EPIC_RATE      = 0.10f;
            public const float DROP_UNIQUE_RATE    = 0.04f;
            public const float DROP_LEGENDARY_RATE = 0.009f;
            public const float DROP_CELESTIAL_RATE = 0.001f;

            // 인벤토리
            public const int   INVENTORY_CAPACITY  = 32;
        }
    }
}
