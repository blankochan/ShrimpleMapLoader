using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.ResourceLocations;

[assembly: MelonInfo(
    typeof(ShrimpleMapLoader.Loader),
    "ShrimpleMapLoader",
    "0.0.2",
    "ZabelTheBanal"
)]
namespace ShrimpleMapLoader;

public class Loader : MelonMod
{
    static MapPatcher patcher = new();

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("haaaaai :3");
        MelonEvents.OnSceneWasInitialized.Subscribe(patcher.OnSceneWasInitialized);
    }

    public override void OnDeinitializeMelon()
    {
        MelonEvents.OnSceneWasInitialized.Unsubscribe(patcher.OnSceneWasInitialized);
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName is not "Splashes") return;

        LoggerInstance.Msg("Installing Extra Maps");
        var locator = Addressables.Instance.m_ResourceLocators[0].Locator.Cast<UnityEngine.AddressableAssets.ResourceLocators.ResourceLocationMap>();
        var originalMaps = locator.Locations["Maps"].Cast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>>();
        string[] mapPaths = Directory.GetDirectories("./UserData/Maps/");

        List<IResourceLocation> newMaps = new();
        foreach (string mapPath in mapPaths)
        {
            if (TryLoadCustomMap(mapPath, out var info))
            {
                newMaps.Add(locator.Locations[info.ScenePrimaryKey][0]);
            }
            else
            {
                LoggerInstance.Warning("failed to load: " + mapPath);
            }
        }

        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<IResourceLocation> arr =
            new(3 + newMaps.Count);

        for (int i = 0; i < originalMaps.Count; i++)
        {
            arr[i] = originalMaps[i];
        }

        for (int i = 0; i < newMaps.Count; i++)
        {
            arr[3 + i] = newMaps[i];
        }

        LoggerInstance.Msg("Registering Extra Maps");
        locator.locations.System_Collections_IDictionary_set_Item(
            "Maps",
            arr.Cast<Il2CppSystem.Object>()
        );
    }

    public bool TryLoadCustomMap(string path, out MapInfo mapInfo)
    { // messy asf code
        string mapBasePath = Path.GetFullPath(path).Replace('\\', '/');
        try
        {
            string mapJSON = File.ReadAllText($"{mapBasePath}/Manifest.json");
            mapInfo = JsonConvert.DeserializeObject<MapInfo>(mapJSON);
            LoggerInstance.Msg($"Installing: {mapInfo.ScenePrimaryKey}");
            if (mapInfo.ScenePrimaryKey == null)
                return false;

            var locator = Addressables
                .Instance.m_ResourceLocators[0]
                .Locator.Cast<UnityEngine.AddressableAssets.ResourceLocators.ResourceLocationMap>();

            if (locator.Locations.ContainsKey(mapInfo.ScenePrimaryKey))
                return false;

            Il2CppSystem.Collections.Generic.List<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> depList = new();

            for (int i = 0; i < mapInfo.Dependencies.Length; i++)
            {
                var dep = mapInfo.Dependencies[i];
                UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions req =
                    new();
                req.BundleName = dep.Data.BundleName;
                req.BundleSize = dep.Data.BundleSize;
                req.UseCrcForCachedBundle = true;

                if (locator.Locations.ContainsKey(dep.PrimaryKey))
                {
                    var preExistingAssets = locator
                        .Locations[dep.PrimaryKey]
                        .Cast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>>();
                    foreach (var asset in preExistingAssets)
                        depList.Add(asset);
                    continue;
                }

                UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData.CompactLocation depLocation =
                    new(
                        locator,
                        $"{mapBasePath}/Dependencies/{dep.InternalId}",
                        dep.ProviderId,
                        null,
                        req,
                        0,
                        dep.PrimaryKey,
                        Il2CppSystem.Type.GetType(
                            "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource"
                        )
                    );
                depList.Add(
                    depLocation.Cast<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>()
                );
            }

            UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData.CompactLocation Scenelocation =
                new(
                    locator,
                    mapInfo.SceneInternalKey,
                    "UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider",
                    mapInfo.ScenePrimaryKey + "_deps",
                    null,
                    0,
                    mapInfo.ScenePrimaryKey,
                    Il2CppSystem.Type.GetType(
                        "UnityEngine.ResourceManagement.ResourceProviders.SceneInstance"
                    )
                );

            locator.Add(
                mapInfo.ScenePrimaryKey + "_deps",
                depList.Cast<Il2CppSystem.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>>()
            );
            locator.Add(
                mapInfo.ScenePrimaryKey,
                Scenelocation.Cast<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>()
            );
            return true;
        }
        catch (FileNotFoundException)
        {
            mapInfo = null;
            LoggerInstance.Warning(
                $"The map at \"{path}\" does not have a manifest.json and will not be loaded"
            );
            return false;
        }
    }
}

public struct AssetBundleRequestOptions
{
    public string BundleName;
    public long BundleSize;
    public string AssetLoadMode;
}

public struct SimpleDependency
{
    public string InternalId;
    public string PrimaryKey;
    public string ProviderId;

    public AssetBundleRequestOptions Data;
}

public sealed class MapInfo
{
    public string SceneInternalKey;
    public string ScenePrimaryKey;
    public SimpleDependency[] Dependencies;
}
