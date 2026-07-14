// Unity
using UnityEngine;

namespace Minsung.Utility
{
    public static class UtilCoroutine
    {
        /// <summary> 돌고 있던 코루틴이 있으면 멈추고 새 코루틴으로 교체한다. </summary>
        public static void CheckRunCoroutine(ref Coroutine co, Coroutine newCo, MonoBehaviour mono)
        {
            if (co != null)
            {
                mono.StopCoroutine(co);
            }
            co = newCo;
        }

        /// <summary> 돌고 있던 코루틴이 있으면 멈추고 참조를 비운다. </summary>
        public static void CheckStopCoroutine(ref Coroutine co, MonoBehaviour mono)
        {
            if (co != null)
            {
                mono.StopCoroutine(co);
                co = null;
            }
        }
    }
}
