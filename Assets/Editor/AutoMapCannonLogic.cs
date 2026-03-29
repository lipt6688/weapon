using UnityEngine;
using UnityEditor;

public class AutoMapCannonLogic
{
    [InitializeOnLoadMethod]
    private static void HookUpCannonLogic()
    {
        // 强制重新执行匹配，因为之前可能名字没对上
        EditorApplication.delayCall += () =>
        {
            PlayerAttack[] attacks = Object.FindObjectsOfType<PlayerAttack>(true);
            if (attacks.Length == 0) return;

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");

            // --- 重新匹配真正的大炮(Bazooka)和炮弹(Bomb)图片 ---
            // 之前由于我说的是 Cannon，但实际上你的文件叫 Bazooka.png 和 Bomb.png 
            Sprite bazookaSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/Bazooka.png");
            Sprite bombSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/Bomb.png");

            // 1. 生成大炮子弹的预制体 BazookaBomb
            GameObject cbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/BazookaBomb.prefab");
            if (cbPrefab == null)
            {
                GameObject temp = new GameObject("BazookaBomb");
                var sr = temp.AddComponent<SpriteRenderer>();
                
                if (bombSp != null) sr.sprite = bombSp;
                else sr.color = Color.black; 

                var cbScript = temp.AddComponent<Cannonball>();
                cbScript.speed = 15f; 
                cbScript.aoeRadius = 3f;
                cbScript.minDamage = 30;
                cbScript.maxDamage = 50;

                cbPrefab = PrefabUtility.SaveAsPrefabAsset(temp, "Assets/Prefabs/Weapons/BazookaBomb.prefab");
                Object.DestroyImmediate(temp);
            }

            foreach (var pa in attacks)
            {
                Undo.RecordObject(pa, "Map Bazooka");
                
                // 2. 将大炮模型赋予Player
                Transform cannonObj = pa.transform.Find("CannonVisual");
                if (cannonObj == null)
                {
                    GameObject c = new GameObject("CannonVisual");
                    c.transform.SetParent(pa.transform);
                    cannonObj = c.transform;
                }

                var cSr = cannonObj.GetComponent<SpriteRenderer>();
                if (cSr == null) cSr = cannonObj.gameObject.AddComponent<SpriteRenderer>();
                if (bazookaSp != null) cSr.sprite = bazookaSp;
                cSr.sortingOrder = 11;

                // 放大
                cannonObj.localScale = new Vector3(2.5f, 2.5f, 1f); 
                cannonObj.localPosition = new Vector3(0, 0.8f, 0);

                pa.cannonVisual = cannonObj.gameObject;
                pa.cannonballPrefab = cbPrefab;
                pa.cannonCooldown = 4f;

                EditorUtility.SetDirty(pa);
            }

            AssetDatabase.SaveAssets();
        };
    }
}
