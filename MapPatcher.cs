using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ShrimpleMapLoader;

public class MapPatcher
{
    public bool IsTerrianSeeThroughForceEnabled = false;

    public void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (IsTerrianSeeThroughForceEnabled)
        {
            IsTerrianSeeThroughForceEnabled = false;
            MelonEvents.OnUpdate.Unsubscribe(stormDrain_Update);
        }
        if (sceneName is "Splashes" or "Bootstrap")
            return;
        var shaderFetch =
            Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                "b75fae7cc98036167227c7451fa91add.bundle"
            );

        shaderFetch.WaitForCompletion();
        Shader shader = shaderFetch
            .Result.GetAssetBundle()
            .LoadAsset<Shader>("Assets/Shaders/GameShaders/InvisibleWall.shader");
        foreach (
            MeshRenderer wall in Resources
                .FindObjectsOfTypeAll<MeshRenderer>()
                .Where(mesh => mesh.name.StartsWith("InvisibleWall"))
        )
            wall.material.shader = shader;

        if (sceneName is "StormDrain")
        {
            IsTerrianSeeThroughForceEnabled = true;
            MelonEvents.OnUpdate.Subscribe(stormDrain_Update);
        }

        if (sceneName is "NightRide")
        {
            var skyboxShaderFetch =
                Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                    "dd64db8a6e3d524e4f801ea1c20e5231.bundle"
                );
            var manager = Il2CppView_Lighting.GlobalLightingManager.instance;
            Material material = manager.skyboxMat;
            manager.fog = true;
            manager.baseLightingProfile.fogFadeEndDist = float.MaxValue;
            manager.baseLightingProfile.UpdateShaderParams();

            System.Collections.IEnumerator EnableFogRenderer()
            {
                while (true)
                {
                    yield return new WaitForEndOfFrame();
                    if (Camera.current is Camera) // wait for a camera to be assigned (inconsistent timing on multiplayer)
                    {
                        foreach (Camera camera in Camera.allCameras)
                        {
                            if (
                                camera.gameObject.GetComponent<Il2CppView_Lighting.FogRenderer>()
                                is Il2CppView_Lighting.FogRenderer fogRenderer
                            )
                            {
                                fogRenderer.enabled = true;
                                break;
                            }
                        }
                    }
                }
            }
            MelonCoroutines.Start(EnableFogRenderer());

            skyboxShaderFetch.WaitForCompletion();
            var skyboxShader = skyboxShaderFetch
                .Result.GetAssetBundle()
                .LoadAsset<Shader>("Assets/Shaders/GameShaders/SkyboxShaders/CubeSkybox.shader");
            material.shader = skyboxShader;
        }

        if (sceneName is "NightRide" or "Quarry")
        {
            var nightRideOSTFetch =
                Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                    "1d427297405c7ef905bae9dbedf5abb2.bundle"
                );
            var quarryOSTFetch =
                Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                    "7f48d042845aa8d546c4f67fccb2c1ac.bundle"
                );
            nightRideOSTFetch.WaitForCompletion();
            quarryOSTFetch.WaitForCompletion();

            AudioClip nightRideOST = nightRideOSTFetch
                .Result.GetAssetBundle()
                .LoadAsset<AudioClip>("Assets/Audio/Music/AFU - LB1 - NightRide.mp3");
            AudioClip quarryOST = quarryOSTFetch
                .Result.GetAssetBundle()
                .LoadAsset<AudioClip>("Assets/Audio/Music/AFU - DnB6 - Quarry.mp3");
            if (
                UnityEngine.Resources.FindObjectsOfTypeAll<Il2CppView_Music.SongsLibrary>().First()
                is Il2CppView_Music.SongsLibrary songLibrary
            )
            {
                songLibrary.quarry = quarryOST;
                songLibrary.nightride = nightRideOST;
            }
        }

        //   if (sceneName is "Warehouse"){
        //     LoggerInstance.Msg("Fixing Water");
        //     MelonCoroutines.Start(FixWater());
        //   }
    }

    private void stormDrain_Update()
    {
        try
        {
            Camera.current.gameObject.GetComponent<Il2CppView_Main.Camera_View>().terrainOverlapChecker.terrainOverlap = true;
        }
        catch (NullReferenceException)
        {

        }
    }

    internal Texture2D TexDownload(Texture gpuTex)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            gpuTex.width,
            gpuTex.height,
            0,
            UnityEngine.SystemInfo.GetCompatibleFormat(
                gpuTex.graphicsFormat,
                UnityEngine.Experimental.Rendering.FormatUsage.Render
            ),
            1
        );

        Texture2D readableTexture = new(gpuTex.width, gpuTex.height, TextureFormat.RGBA32, false);

        Graphics.Blit(gpuTex, renderTexture);
        RenderTexture.active = renderTexture;
        readableTexture.ReadPixels(new Rect(0, 0, gpuTex.width, gpuTex.height), 0, 0);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        return readableTexture;
    }

    private System.Collections.IEnumerator FixWater()
    {
        var waterTextureFetch =
            Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                "9ad2ca64b4d80a7f228034d0624f0c42.bundle"
            );
        var waterShaderFetch =
            Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                "13e5358245c2906af4ba4841428bf87f.bundle"
            );
        var waterMaterialFetch =
            Addressables.LoadAsset<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>(
                "aaf2c64c87f04347138362261802769c.bundle"
            );
        waterShaderFetch.WaitForCompletion();
        waterShaderFetch.WaitForCompletion();
        waterMaterialFetch.WaitForCompletion();

        Texture2D waterTex = waterTextureFetch
            .Result.GetAssetBundle()
            .LoadAsset<Texture2D>("Assets/Materials/Terrain/Water_Caustic.png");
        Shader waterShader = waterShaderFetch
            .Result.GetAssetBundle()
            .LoadAsset<Shader>("Assets/Shaders/GameShaders/Water.shader");
        Material waterMat = waterMaterialFetch
            .Result.GetAssetBundle()
            .LoadAsset<Material>("Assets/Materials/Terrain/Water.mat");

        yield return new WaitForEndOfFrame();
        Texture2D tex = TexDownload(waterMat.mainTexture.Cast<Texture2D>());
        waterMat.mainTexture = tex;
        tex.name = "ReimportedWater";
        tex.Apply(true, false);
        foreach (
            MeshRenderer renderer in Resources
                .FindObjectsOfTypeAll<MeshRenderer>()
                .Where(renderer => renderer.name.StartsWith("Water"))
        )
        {
            renderer.material = Material.Instantiate(waterMat);
            renderer.material.SetTexture("_MainTex", tex);
        }
    }
}
