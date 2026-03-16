using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tools.CameraBaker.Editor
{
    public static class CameraBakerTool
    {
        private const string FolderPath = "Assets/_LevelPreview";

        // Bakes from the currently selected Camera.
        [MenuItem("Tools/Level Previews/Quick Menu/Bake Selected Camera Preview (1:1)")]
        private static void BakeSquare()
        {
            Bake(512, 512);
        }

        [MenuItem("Tools/Level Previews/Quick Menu/Bake Selected Camera Preview (16:10)")]
        private static void BakeWidescreen()
        {
            Bake(1920, 1200);
        }

        private static void Bake(int width, int height)
        {
            var cam = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Camera>() : null;
            if (!cam)
            {
                EditorUtility.DisplayDialog("Cannot Bake", "Select a GameObject with a Camera component.", "OK"); 
                Debug.LogError("Select a GameObject with a Camera component.");
                return;
            }

            var path = Bake(cam, width, height, FolderPath, cam.gameObject.scene.name + "(" + width + "x" + height + ")" + ".png",
                true, false, true, true);
            
            if (!string.IsNullOrEmpty(path))
            {
                CameraBakerPreviewWindow.ShowPreview(path);
            }
        }

        public static string Bake(
            Camera cam,
            int width,
            int height,
            string folderPath,
            string fileName,
            bool importAsSprite,
            bool mipmaps,
            bool srgb,
            bool alphaIsTransparency)
        {
            if (!cam)
            {
                Debug.LogError("Camera is null.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Debug.LogError("Folder path is empty.");
                return null;
            }

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName).Replace('\\', '/');

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };

            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            try
            {
                cam.targetTexture = rt;
                RenderTexture.active = rt;
                cam.Render();

                var tex = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps, !srgb);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply(mipmaps, false);

                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = importAsSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                    importer.mipmapEnabled = mipmaps;
                    importer.sRGBTexture = srgb;
                    importer.alphaIsTransparency = alphaIsTransparency;
                    importer.SaveAndReimport();
                }

                Debug.Log($"Saved preview: {fullPath}");
                return fullPath;
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;

                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
    
    
}
