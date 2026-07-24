using UnityEngine;
using UnityEditor;
using Minsung.Boss;

[InitializeOnLoad]
public class AddForwarderUtility
{
    static AddForwarderUtility()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        // Scene objects
        var meleeUnits = Object.FindObjectsByType<BossMeleeUnitBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var unit in meleeUnits)
        {
            AddForwarderToVisual(unit.gameObject);
        }

        // Prefab objects
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var unit = prefab.GetComponentInChildren<BossMeleeUnitBase>(true);
                if (unit != null)
                {
                    AddForwarderToVisual(unit.gameObject);
                }
            }
        }
        
        Debug.Log("[MCP] Successfully added BossAnimationEventForwarder to all Visual objects!");
    }

    static void AddForwarderToVisual(GameObject bossRoot)
    {
        // Try to find a child named "Visual" or one with an Animator
        var animator = bossRoot.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.gameObject != bossRoot)
        {
            var forwarder = animator.gameObject.GetComponent<BossAnimationEventForwarder>();
            if (forwarder == null)
            {
                animator.gameObject.AddComponent<BossAnimationEventForwarder>();
                EditorUtility.SetDirty(animator.gameObject);
                Debug.Log($"Added BossAnimationEventForwarder to {animator.gameObject.name} in {bossRoot.name}");
            }
        }
    }
}
