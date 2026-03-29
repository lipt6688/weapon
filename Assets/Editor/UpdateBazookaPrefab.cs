using UnityEngine;
using UnityEditor;

public class UpdateBazookaPrefab
{
    [InitializeOnLoadMethod]
    private static void UpdatePrefab()
    {
        EditorApplication.delayCall += () =>
        {
            GameObject cbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/BazookaBomb.prefab");
            if (cbPrefab != null)
            {
                Cannonball cbScript = cbPrefab.GetComponent<Cannonball>();
                if (cbScript != null)
                {
                    cbScript.speed = 30f;
                    cbScript.aoeRadius = 8f;
                    cbScript.minDamage = 150;
                    cbScript.maxDamage = 300;
                    EditorUtility.SetDirty(cbPrefab);
                    AssetDatabase.SaveAssets();
                }
            }
        };
    }
}
