using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoMapGatlingLogic : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        HookUpGatling();
    }

    [InitializeOnLoadMethod]
    private static void HookUpGatling()
    {
        EditorApplication.delayCall += () =>
        {
            string jpgPath = "Assets/Sprites/Weapons/gatlinggun.jpg";
            string pngPath = "Assets/Sprites/Weapons/GatlingGun_Clear.png";

            // 自动去黑底：把JPG转成PNG
            if (File.Exists(jpgPath) && !File.Exists(pngPath))
            {
                Texture2D jpgTex = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(jpgPath);
                if (jpgTex.LoadImage(fileData))
                {
                    Texture2D pngTex = new Texture2D(jpgTex.width, jpgTex.height, TextureFormat.RGBA32, false);
                    Color[] pixels = jpgTex.GetPixels();
                    Color[] newPixels = new Color[pixels.Length];
                    
                    for(int i = 0; i < pixels.Length; i++)
                    {
                        Color c = pixels[i];
                        // RGB都小于一定阈值认为是纯黑或接近黑的背景，设置透明
                        if (c.r < 0.15f && c.g < 0.15f && c.b < 0.15f)
                        {
                            newPixels[i] = new Color(c.r, c.g, c.b, 0f); // alpha = 0
                        }
                        else
                        {
                            newPixels[i] = new Color(c.r, c.g, c.b, 1f); // alpha = 1
                        }
                    }
                    
                    pngTex.SetPixels(newPixels);
                    pngTex.Apply();
                    
                    File.WriteAllBytes(pngPath, pngTex.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
            }

            string targetPath = File.Exists(pngPath) ? pngPath : jpgPath;

            TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; // 确保识别透明通道
                importer.SaveAndReimport();
                return; // wait for reimport
            }

            PlayerAttack[] attacks = Object.FindObjectsOfType<PlayerAttack>(true);
            if (attacks.Length == 0) return;

            Sprite gatlingSp = AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);

            GameObject gbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/GatlingBullet.prefab");
            if (gbPrefab == null && attacks[0].rifleBulletPrefab != null)
            {
                string riflePrefabPath = AssetDatabase.GetAssetPath(attacks[0].rifleBulletPrefab);
                if (!string.IsNullOrEmpty(riflePrefabPath))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
                        AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
                    
                    AssetDatabase.CopyAsset(riflePrefabPath, "Assets/Prefabs/Weapons/GatlingBullet.prefab");
                    gbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/GatlingBullet.prefab");
                }
            }
            
            if (gbPrefab != null)
            {
                var sr = gbPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.8f, 0.2f, 1f); 
                
                var trail = gbPrefab.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.startColor = new Color(1f, 0.8f, 0.2f, 1f);
                    trail.endColor = new Color(1f, 0.5f, 0f, 0f);
                    trail.startWidth = 0.05f;
                }

                gbPrefab.transform.localScale = new Vector3(0.5f, 0.15f, 1f);
                EditorUtility.SetDirty(gbPrefab);
            }

            foreach (var pa in attacks)
            {
                Undo.RecordObject(pa, "Map Gatling");
                
                Transform gatlingObj = pa.transform.Find("GatlingVisual");
                if (gatlingObj == null)
                {
                    GameObject c = new GameObject("GatlingVisual");
                    c.transform.SetParent(pa.transform);
                    gatlingObj = c.transform;
                }

                var cSr = gatlingObj.GetComponent<SpriteRenderer>();
                if (cSr == null) cSr = gatlingObj.gameObject.AddComponent<SpriteRenderer>();
                if (gatlingSp != null) cSr.sprite = gatlingSp;
                cSr.sortingOrder = 12;

                // 可以稍微调到0.15，如果去底后边缘稍弱
                gatlingObj.localScale = new Vector3(0.15f, 0.15f, 1f); 

                // --- 确保强行应用大炮规模更新 ---
                Transform canonObj = pa.transform.Find("CannonVisual");
                if (canonObj != null)
                {
                   canonObj.localScale = new Vector3(2.5f, 2.5f, 1f);
                }

                pa.gatlingVisual = gatlingObj.gameObject;
                if (gbPrefab != null) pa.gatlingBulletPrefab = gbPrefab;

                EditorUtility.SetDirty(pa);
            }

            AssetDatabase.SaveAssets();
        };
    }
}
