using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PocketBloom.Editor
{
    public static class BloomProjectBuilder
    {
        public const string ScenePath = "Assets/PocketBloom/Scenes/PocketBloom.unity";
        [MenuItem("Pocket Bloom/Create or Update Game Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene before building.");
            Directory.CreateDirectory("Assets/PocketBloom/Scenes");
            Directory.CreateDirectory("Assets/PocketBloom/Resources");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PocketBloom/Resources/BloomFont.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PocketBloom/Resources/SunlitFont.asset") ?? font;
            if (!font)
            {
                if (TMP_Settings.instance == null)
                {
                    TMP_PackageResourceImporter.ImportResources(true, false, false);
                    Debug.Log("TMP 기본 리소스를 가져옵니다. 임포트가 끝나면 씬 생성을 다시 실행하세요.");
                    return;
                }
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/PocketBloom/Art/NotoSansKR.ttf");
                if (!source) throw new InvalidOperationException("NotoSansKR source font missing.");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 7, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "BloomFont";
                AssetDatabase.CreateAsset(font, "Assets/PocketBloom/Resources/BloomFont.asset");
                font.material.name = "BloomFont Material"; AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) { texture.name = "BloomFont Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
                string characters = string.Concat(Directory.GetFiles("Assets/PocketBloom/Scripts", "*.cs").Select(File.ReadAllText));
                font.TryAddCharacters(characters);
                EditorUtility.SetDirty(font);
            }
            var config = AssetDatabase.LoadAssetAtPath<BloomAdConfig>("Assets/PocketBloom/Resources/AdConfig.asset");
            if (!config) { config = ScriptableObject.CreateInstance<BloomAdConfig>(); AssetDatabase.CreateAsset(config, "Assets/PocketBloom/Resources/AdConfig.asset"); }
            var artImporter = AssetImporter.GetAtPath("Assets/PocketBloom/Art/GardenKeyArt.png") as TextureImporter;
            if (artImporter) { artImporter.maxTextureSize = 1536; artImporter.textureCompression = TextureImporterCompression.CompressedHQ; artImporter.mipmapEnabled = false; artImporter.SaveAndReimport(); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
            var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 6.4f; cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.07f, .247f, .25f);
            var cameraData = camera.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = true;
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global; light.intensity = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.5f); bloom.threshold.Override(.9f);
            profile.Add<Vignette>(true).intensity.Override(.2f);
            const string volumePath = "Assets/PocketBloom/Resources/GardenVolume.asset";
            var savedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if (!savedProfile)
            {
                AssetDatabase.CreateAsset(profile, volumePath);
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
                savedProfile = profile;
            }
            else UnityEngine.Object.DestroyImmediate(profile);
            var volume = new GameObject("Garden Atmosphere").AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = savedProfile;
            var go = new GameObject("Pocket Bloom"); var ads = go.AddComponent<BloomAds>(); ads.config = config;
            var game = go.AddComponent<BloomGame>(); game.font = font; game.ads = ads;
            game.gardenArt = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PocketBloom/Art/GardenKeyArt.png");
            PlayerSettings.companyName = "Pocket Bloom Studio"; PlayerSettings.productName = "Pocket Bloom";
            PlayerSettings.bundleVersion = "1.0.0";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PocketBloom/Art/AppIcon.png");
            if (icon) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.pocketbloom.garden");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            QualitySettings.vSyncCount = 0;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); SetGameViewSize(540, 960);
            Debug.Log("Pocket Bloom scene saved: " + ScenePath);
        }
        public static void SetGameViewSize(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null);
            var groupType = asm.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupType, EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "Android" : "Standalone") });
            var sizeType = asm.GetType("UnityEditor.GameViewSize"); var mode = asm.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(mode, "FixedResolution"), width, height, "Bloom " + width + "x" + height });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var viewType = asm.GetType("UnityEditor.GameView"); var window = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(window, count - 1);
        }
        [MenuItem("Pocket Bloom/Build Windows Preview")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/PocketBloom.exe", false);
        [MenuItem("Pocket Bloom/Build Android Test APK")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/PocketBloom-test.apk", false);
        [MenuItem("Pocket Bloom/Build Android Release AAB")]
        public static void BuildRelease()
        {
            var config = AssetDatabase.LoadAssetAtPath<BloomAdConfig>("Assets/PocketBloom/Resources/AdConfig.asset");
            if (!config || !config.enableAds || string.IsNullOrEmpty(config.androidAppKey) || string.IsNullOrEmpty(config.privacyPolicyUrl))
                throw new InvalidOperationException("실제 광고 계정, 개인정보처리방침, 동의 제공자 연결과 서명을 먼저 검증해야 합니다. Docs/ReleaseChecklist.md 참조.");
            if (!PlayerSettings.Android.useCustomKeystore) throw new InvalidOperationException("Release signing is not configured.");
            Build(BuildTarget.Android, "Builds/Android/PocketBloom.aab", true);
        }
        static void Build(BuildTarget target, string path, bool bundle)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = bundle;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = target, locationPathName = path, options = BuildOptions.DetailedBuildReport });
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/Build-" + target + ".json", JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(), errors = report.summary.totalErrors, warnings = report.summary.totalWarnings, bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds, output = report.summary.outputPath, utc = DateTime.UtcNow.ToString("O") }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + report.summary.result);
        }
        [Serializable] sealed class BuildEvidence { public string result, output, utc; public int errors, warnings; public ulong bytes; public double seconds; }
    }
}
