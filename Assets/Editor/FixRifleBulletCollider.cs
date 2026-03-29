using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FixRifleBulletCollider
{
    static FixRifleBulletCollider()
    {
        EditorApplication.delayCall += DoFix;
    }

    static void DoFix()
    {
        string[] guids = AssetDatabase.FindAssets("RifleBullet t:GameObject");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    collider.size = new Vector2(0.8f, 0.8f); // 增大碰撞盒
                }

                Rigidbody2D rb = prefab.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 开启连续碰撞检测，防止子弹速度过快穿透敌人
                }

                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
