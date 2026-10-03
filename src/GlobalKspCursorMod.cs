using System;
using System.IO;
using System.Reflection;
using KSP.Game;
using KSP.OAB;
using SpaceWarp2.API.Mods;
using UnityEngine;

namespace GlobalKspCursor
{
    public sealed class GlobalKspCursorMod : MonoBehaviourMod
    {
        private const string BundleFileName = "oab_assets_all.bundle";
        private const string CursorName = "cursor_arrow@2x";
        private Sprite cursorSprite;
        private Texture2D cursorTexture;
        private Texture2D leftPressedTexture;
        private Texture2D rightPressedTexture;
        private Vector2 cursorHotspot;

        public override void OnInitialized()
        {
            try
            {
                cursorSprite = LoadStockCursor();
                if (cursorSprite == null || cursorSprite.texture == null)
                {
                    Debug.LogError("[GlobalKspCursor] Stock KSP 2 cursor could not be found; leaving the system cursor unchanged.");
                    return;
                }

                cursorTexture = cursorSprite.texture;
                cursorHotspot = new Vector2(cursorSprite.pivot.x, cursorTexture.height - cursorSprite.pivot.y);
                try
                {
                    Color32[] pixels = ReadCursorPixels(cursorTexture);
                    leftPressedTexture = TintCursor(pixels, cursorTexture.width, cursorTexture.height,
                        new Color32(62, 206, 103, 255));
                    rightPressedTexture = TintCursor(pixels, cursorTexture.width, cursorTexture.height,
                        new Color32(170, 170, 170, 255));
                }
                catch (Exception error)
                {
                    // Keep the working stock cursor if this game's texture is
                    // unreadable or its graphics backend rejects the copy.
                    Debug.LogWarning("[GlobalKspCursor] Pressed colors unavailable; using the stock cursor: " + error);
                }
                ApplyCursor();
                Debug.Log("[GlobalKspCursor] Stock KSP 2 cursor enabled globally from the installed game assets.");
            }
            catch (Exception error)
            {
                Debug.LogError("[GlobalKspCursor] Could not load the stock cursor: " + error);
            }
        }

        private static Sprite LoadStockCursor()
        {
            // Addressables may already own this bundle. Never unload a game-owned bundle.
            foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                Sprite loadedCursor = FindCursorInBundle(loadedBundle);
                if (loadedCursor != null)
                {
                    return loadedCursor;
                }
            }

            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Debug.LogError("[GlobalKspCursor] StreamingAssets folder is missing: " + streamingAssetsPath);
                return null;
            }

            string[] bundlePaths = Directory.GetFiles(streamingAssetsPath, BundleFileName, SearchOption.AllDirectories);
            if (bundlePaths.Length == 0)
            {
                Debug.LogError("[GlobalKspCursor] " + BundleFileName + " was not found under " + streamingAssetsPath);
                return null;
            }

            for (int index = 0; index < bundlePaths.Length; index++)
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePaths[index]);
                if (bundle == null)
                {
                    Debug.LogError("[GlobalKspCursor] Unity could not open bundle: " + bundlePaths[index]);
                    continue;
                }

                try
                {
                    Sprite cursor = FindCursorInBundle(bundle);
                    if (cursor != null)
                    {
                        return cursor;
                    }
                }
                finally
                {
                    // Keep loaded assets, but release this temporary bundle handle so
                    // Addressables can load the same bundle when the player enters the OAB.
                    bundle.Unload(false);
                }
            }

            Debug.LogError("[GlobalKspCursor] The OAB bundle did not contain the expected cursor: " + CursorName);
            return null;
        }

        private static Sprite FindCursorInBundle(AssetBundle bundle)
        {
            if (bundle == null)
            {
                return null;
            }

            // The OAB Addressables bundle exposes one prefab named "2". The cursor
            // is a referenced Sprite on its builder component, not a top-level asset.
            string[] names = bundle.GetAllAssetNames();
            bool hasOabPrefab = false;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], "2", StringComparison.OrdinalIgnoreCase))
                {
                    hasOabPrefab = true;
                    break;
                }
            }

            if (!hasOabPrefab)
            {
                return null;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>("2");
            if (prefab == null)
            {
                return null;
            }

            foreach (Component component in prefab.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                FieldInfo assetsField = component.GetType().GetField("BuilderAssets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (assetsField == null)
                {
                    continue;
                }

                object assets = assetsField.GetValue(component);
                if (assets == null)
                {
                    continue;
                }

                PropertyInfo cursorProperty = assets.GetType().GetProperty("DefaultCursor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Sprite cursor = cursorProperty == null ? null : cursorProperty.GetValue(assets, null) as Sprite;
                if (cursor != null && cursor.name == CursorName)
                {
                    return cursor;
                }
            }

            return null;
        }

        private void LateUpdate()
        {
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            if (cursorTexture != null && !ShouldYieldToOabTransformCursor())
            {
                Texture2D active = Input.GetMouseButton(0) && leftPressedTexture != null
                    ? leftPressedTexture
                    : Input.GetMouseButton(1) && rightPressedTexture != null
                        ? rightPressedTexture : cursorTexture;
                Cursor.SetCursor(active, cursorHotspot, CursorMode.Auto);
            }
        }

        private static bool ShouldYieldToOabTransformCursor()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.Game == null || manager.Game.OAB == null)
                return false;

            ObjectAssemblyBuilder builder = manager.Game.OAB.Current;
            return builder != null && builder.IsLoaded &&
                builder.GetCurrentToolType() == OABToolType.TRANSLATE_ROTATE;
        }

        private static Color32[] ReadCursorPixels(Texture2D source)
        {
            if (source.isReadable)
                return source.GetPixels32();

            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height,
                0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                return copy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (copy != null)
                    UnityEngine.Object.Destroy(copy);
            }
        }

        private static Texture2D TintCursor(Color32[] source, int width, int height, Color32 tint)
        {
            Color32[] pixels = new Color32[source.Length];
            for (int index = 0; index < pixels.Length; index++)
            {
                Color32 original = source[index];
                byte brightness = Math.Max(original.r, Math.Max(original.g, original.b));
                pixels[index] = new Color32(
                    (byte)(brightness * tint.r / 255),
                    (byte)(brightness * tint.g / 255),
                    (byte)(brightness * tint.b / 255), original.a);
            }
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply(false, false);
            return result;
        }

        private void OnDestroy()
        {
            if (cursorTexture != null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            if (leftPressedTexture != null)
                UnityEngine.Object.Destroy(leftPressedTexture);
            if (rightPressedTexture != null)
                UnityEngine.Object.Destroy(rightPressedTexture);
        }
    }
}
