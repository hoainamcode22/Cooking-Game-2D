using Day_Night;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.VFX;

namespace Day_Night.Editor
{
    public static class DayNightSetupTool
    {
        private const string PrefabPath = "Assets/Day_Night/Prefabs/DayNightWeatherSetup.prefab";
        private const string DayAudioPath = "Assets/Day_Night/Audio/Ambience/Background ambience outside - Day.wav";
        private const string NightAudioPath = "Assets/Day_Night/Audio/Ambience/Background ambience outside - Night.wav";
        private const string RainAudioPath = "Assets/Day_Night/Audio/Ambience/Rain.wav";
        private const string ThunderAudioPath = "Assets/Day_Night/Audio/Ambience/Thunder.wav";
        private const string WaterAudioPath = "Assets/Day_Night/Audio/Ambience/Water flowing.wav";
        private const string RainVfxPath = "Assets/Day_Night/VFX/Rain/VFX_2DRain.vfx";
        private const string RainForegroundVfxPath = "Assets/Day_Night/VFX/Rain/VFX_RainForeground.vfx";
        private const string WaterDropVfxPath = "Assets/Day_Night/VFX/Rain/VFX_WaterDrop.vfx";
        private const string WaterLinesVfxPath = "Assets/Day_Night/VFX/Water/VFX_WaterLines.vfx";
        private const string WaterLinesStormVfxPath = "Assets/Day_Night/VFX/Water/VFX_WaterLinesStorm.vfx";
        private const string LeavesVfxName = "VFX_Leaves";
        private const string DustVfxName = "VFX_DustParticles";
        private const string LightPackPrefabPath = "Assets/Day_Night/Prefabs/DayNightLightPack.prefab";
        private const string StreetLampPrefabPath = "Assets/Day_Night/Lights/Art/Environment/Lamps/StreetLamp/Prefab_Streetlamp.prefab";
        private const string LanternPrefabPath = "Assets/Day_Night/Lights/Art/Environment/Lamps/LampAndLantern/Prefab_Lantern.prefab";
        private const string LanternHangingPrefabPath = "Assets/Day_Night/Lights/Art/Environment/Lamps/LampAndLantern/Prefab_Lantern_Hanging.prefab";
        private const string HouseLampPrefabPath = "Assets/Day_Night/Lights/Art/Environment/Lamps/HouseLamp/Prefab_Sprite_HouseLamp.prefab";
        private const string WindowLightPrefabPath = "Assets/Day_Night/Lights/Art/Interior/VFX/Light_Windows.prefab";
        private const string WarehouseLightPrefabPath = "Assets/Day_Night/Lights/Prefabs/Light 2D_Warehouse.prefab";
        private const string FireplacePrefabPath = "Assets/Day_Night/Lights/Art/Interior/Fireplace/Prefab_Fireplace.prefab";
        private const string FireVfxPrefabPath = "Assets/Day_Night/Lights/VFX/Fire/VFX_Fire.prefab";
        private const string FireCircleTexturePath = "Assets/Day_Night/Lights/VFX/Fire/circle.png";
        private const string MothsPrefabPath = "Assets/Day_Night/Lights/VFX/Moth/P_VFX_Moths.prefab";
        private const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";
        private const string BottomSortingLayerName = "Bottom";
        private const string WaterSortingLayerName = "Water";
        private const string BuildingSortingLayerName = "CongTrinh";
        private const string ObjectsFrontSortingLayerName = "ObjectsFront";
        private const string DefaultSortingLayerName = "Default";
        private const string ForegroundSortingLayerName = "Foreground";

        [MenuItem("Tools/Day Night/Create Or Refresh Setup Prefab")]
        public static void CreateOrRefreshPrefab()
        {
            GameObject root = BuildSetup();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Day_Night setup prefab refreshed: " + PrefabPath);
        }

        [MenuItem("Tools/Day Night/Spawn Setup In Current Scene")]
        public static void SpawnSetupInCurrentScene()
        {
            CreateOrRefreshPrefab();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            GameObject instance;
            if (prefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
            else
            {
                instance = BuildSetup();
            }

            if (instance == null)
            {
                Debug.LogError("Could not create Day_Night setup.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Day Night Weather Setup");
            Selection.activeGameObject = instance;
            Debug.Log("Spawned Day_Night setup in the current scene.");
        }

        [MenuItem("Tools/Day Night/Fix Existing Setups In Scene")]
        public static void FixExistingSetupsInScene()
        {
            DayNightCycleController[] controllers = Object.FindObjectsByType<DayNightCycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                FixSetupInScene(controllers[i], true);
            }

            Debug.Log("Fixed Day_Night setups in scene: " + controllers.Length);
        }

        [MenuItem("Tools/Day Night/Fix SCN_Farm Scene Asset")]
        public static void FixFarmSceneAsset()
        {
            const string scenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            DayNightCycleController[] controllers = Object.FindObjectsByType<DayNightCycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                FixSetupInScene(controllers[i], false);
            }

            AttachWaterAudioInCurrentScene();
            FixVisibleVfxInCurrentScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Fixed and saved scene: " + scenePath);
        }

        [MenuItem("Tools/Day Night/Create Or Refresh Light Pack Prefab")]
        public static void CreateOrRefreshLightPackPrefab()
        {
            GameObject root = BuildLightPack();
            PrefabUtility.SaveAsPrefabAsset(root, LightPackPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Day_Night light pack prefab refreshed: " + LightPackPrefabPath);
        }

        [MenuItem("Tools/Day Night/Spawn Light Pack In Current Scene")]
        public static void SpawnLightPackInCurrentScene()
        {
            GameObject instance = BuildLightPack();
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Day Night Light Pack");
            Selection.activeGameObject = instance;
            Debug.Log("Spawned Day_Night light pack in the current scene.");
        }

        [MenuItem("Tools/Day Night/Restore Deleted Light Pack In Current Scene")]
        public static void RestoreDeletedLightPackInCurrentScene()
        {
            SpawnLightPackInCurrentScene();
        }

        [MenuItem("Tools/Day Night/Fix Light Packs In Current Scene")]
        public static void FixLightPacksInCurrentScene()
        {
            int fixedCount = 0;
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                string objectName = transforms[i].name;
                if (objectName != "DayNightLightPack" && objectName != "Day_Night_Light_Pack")
                {
                    continue;
                }

                ConfigureLightInstance(transforms[i].gameObject);
                DisableWarehouseSample(transforms[i]);
                fixedCount++;
            }

            DayNightDayEventHandler[] handlers = Object.FindObjectsByType<DayNightDayEventHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i].RefreshNow();
            }

            Debug.Log("Fixed Day_Night light packs in scene: " + fixedCount);
        }

        [MenuItem("Tools/Day Night/Fix Fire VFX In Current Scene")]
        public static void FixFireVfxInCurrentScene()
        {
            int fixedCount = 0;
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (!transforms[i].name.Contains("VFX_Fire"))
                {
                    continue;
                }

                ConfigureFireInstance(transforms[i].gameObject);
                fixedCount++;
            }

            Debug.Log("Fixed Day_Night fire VFX in scene: " + fixedCount);
        }

        [MenuItem("Tools/Day Night/Fix Farm Scene Lighting Sorting UI")]
        public static void FixFarmSceneLightingSortingUi()
        {
            Material spriteLitMaterial = LoadSpriteLitMaterial();
            int litCount = 0;
            int tilemapCount = 0;
            int trainCount = 0;
            int buildingCount = 0;

            DayNightCycleController[] controllers = Object.FindObjectsByType<DayNightCycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                FixSetupInScene(controllers[i], false);
                ConfigureControllerLights(controllers[i]);
            }

            TilemapRenderer[] tilemaps = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                TilemapRenderer renderer = tilemaps[i];
                if (renderer == null)
                {
                    continue;
                }

                Undo.RecordObject(renderer, "Fix Farm Tilemap Sorting");
                string name = renderer.gameObject.name;
                bool isWaterTilemap = name == "Water_Tilemap" || name == "Nước" || name == "Nuoc";
                if (spriteLitMaterial != null && !isWaterTilemap)
                {
                    renderer.sharedMaterial = spriteLitMaterial;
                    litCount++;
                }

                if (name == "Underwater_Tilemap")
                {
                    renderer.sortingLayerName = WaterSortingLayerName;
                    renderer.sortingOrder = 0;
                    tilemapCount++;
                }
                else if (isWaterTilemap)
                {
                    renderer.sortingLayerName = WaterSortingLayerName;
                    renderer.sortingOrder = 10;
                    tilemapCount++;
                }
                else if (name == "Dat_Nen")
                {
                    renderer.sortingLayerName = BottomSortingLayerName;
                    renderer.sortingOrder = 0;
                    tilemapCount++;
                }
                else if (name == "Co_Grass")
                {
                    renderer.sortingLayerName = BottomSortingLayerName;
                    renderer.sortingOrder = 1;
                    tilemapCount++;
                }

                EditorUtility.SetDirty(renderer);
            }

            SpriteRenderer[] spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer == null || renderer.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                Undo.RecordObject(renderer, "Fix Farm Sprite Sorting");
                if (spriteLitMaterial != null)
                {
                    renderer.sharedMaterial = spriteLitMaterial;
                    litCount++;
                }

                if (IsUnderNamedParent(renderer.transform, "TrainVisualRoot") || IsUnderNamedParent(renderer.transform, "TrainVisualRoot2"))
                {
                    renderer.sortingLayerName = ObjectsFrontSortingLayerName;
                    renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 650);
                    trainCount++;
                }
                else if (renderer.sortingOrder >= 500 || HasComponentInParentByName(renderer.transform, "PermanentBuilding", "EditableBuilding", "HouseOrderController", "TrainStationBuilding"))
                {
                    renderer.sortingLayerName = BuildingSortingLayerName;
                    renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 500);
                    buildingCount++;
                }

                EditorUtility.SetDirty(renderer);
            }

            FixMarketPopupBlocking();
            AttachWaterAudioInCurrentScene();
            FixVisibleVfxInCurrentScene();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Fixed farm lighting/sorting/UI. DayNight={controllers.Length}, Tilemaps={tilemapCount}, TrainSprites={trainCount}, BuildingSprites={buildingCount}, LitMaterials={litCount}.");
        }

        [MenuItem("Tools/Day Night/Attach Water Audio In Current Scene")]
        public static void AttachWaterAudioInCurrentScene()
        {
            AudioClip waterClip = AssetDatabase.LoadAssetAtPath<AudioClip>(WaterAudioPath);
            if (waterClip == null)
            {
                Debug.LogWarning("Water ambience clip not found: " + WaterAudioPath);
                return;
            }

            Transform target = FindWaterAudioTarget();
            if (target == null)
            {
                Debug.LogWarning("Could not find a water object to attach ambience to.");
                return;
            }

            GameObject audioObject = EnsureChild(target, "Day_Night_Water_Ambience");
            AudioSource source = EnsureComponent<AudioSource>(audioObject);
            Undo.RecordObject(source, "Attach Water Ambience");
            source.clip = waterClip;
            source.playOnAwake = true;
            source.loop = true;
            source.volume = 0.35f;
            source.spatialBlend = 0f;
            source.priority = 160;
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(audioObject);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Day Night/Fix Visible VFX In Current Scene")]
        public static void FixVisibleVfxInCurrentScene()
        {
            Camera sceneCamera = FindSceneCamera();
            EnsureSceneAudioListener(sceneCamera);
            RemoveDuplicateRainFollowersInScene();

            DayNightCycleController[] controllers = Object.FindObjectsByType<DayNightCycleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                FixWeatherVfxVisibility(controllers[i], sceneCamera, true);
            }

            FixAmbientVfxVisibility(sceneCamera);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Fixed visible rain/leaves/dust VFX in current scene.");
        }

        private static GameObject BuildSetup()
        {
            GameObject root = new GameObject("Day_Night_Setup");
            DayNightCycleController controller = root.AddComponent<DayNightCycleController>();
            controller.ResetToHappyHarvestDefaults();

            GameObject lights = CreateChild(root.transform, "Lights");
            GameObject ambientObject = CreateChild(lights.transform, "Ambient Light");
            Light2D ambientLight = AddLight(ambientObject, "Global", 4, 0, 1.25f, 0f, 1f, 360f, 360f);

            GameObject lightsRotator = CreateChild(lights.transform, "LightsRotator");
            lightsRotator.transform.localPosition = new Vector3(0f, 0f, 10f);

            GameObject dayLightObject = CreateChild(lightsRotator.transform, "DayLight");
            dayLightObject.transform.localPosition = new Vector3(0f, -25f, 0f);
            Light2D dayLight = AddLight(dayLightObject, "Point", 3, 0, 1.57f, 31.32f, 43.59f, 360f, 360f);

            GameObject nightLightObject = CreateChild(lightsRotator.transform, "NightLight");
            nightLightObject.transform.localPosition = new Vector3(0f, 25f, 0f);
            Light2D nightLight = AddLight(nightLightObject, "Point", 3, 0, 1.57f, 31.32f, 43.59f, 360f, 360f);

            GameObject sunRimObject = CreateChild(lightsRotator.transform, "DayLightRim");
            sunRimObject.transform.localPosition = new Vector3(0f, -25f, 0f);
            Light2D sunRimLight = AddLight(sunRimObject, "Point", 3, 3, 1.57f, 31.32f, 43.59f, 84.38f, 84.38f);

            GameObject moonRimObject = CreateChild(lightsRotator.transform, "NightLightRim");
            moonRimObject.transform.localPosition = new Vector3(0f, 25f, 0f);
            moonRimObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Light2D moonRimLight = AddLight(moonRimObject, "Point", 3, 3, 1.57f, 31.32f, 43.59f, 360f, 360f);

            GameObject weatherRoot = CreateChild(root.transform, "Weather");
            DayNightWeatherSystem weatherSystem = root.AddComponent<DayNightWeatherSystem>();
            weatherSystem.SearchRoot = weatherRoot.transform;
            weatherSystem.StartingWeather = DayNightWeatherType.Sun;

            GameObject rainRoot = CreateChild(weatherRoot.transform, "Rain");
            rainRoot.AddComponent<DayNightRainFollower>().FollowInEditMode = false;
            CreateRainVfx(rainRoot.transform, "Visual Effect Rain", RainVfxPath, DayNightWeatherType.Rain | DayNightWeatherType.Thunder, DefaultSortingLayerName, 0, 45f, 4.6f, new Vector4(0.9f, 0.9f, 1.25f, 0.58f));
            CreateRainVfx(rainRoot.transform, "Visual Effect Rain Foreground", RainForegroundVfxPath, DayNightWeatherType.Rain, ForegroundSortingLayerName, 90, 320f, 5.8f, new Vector4(0.9f, 0.9f, 1.25f, 0.58f));
            CreateRainOverlay(rainRoot.transform, "Rain Lines Visible", DayNightWeatherType.Rain | DayNightWeatherType.Thunder);
            CreateRainParticles(rainRoot.transform, "Rain Particles Fallback", DayNightWeatherType.Rain | DayNightWeatherType.Thunder, ForegroundSortingLayerName, 92);

            GameObject thunderVfx = CreateRainVfx(rainRoot.transform, "Visual Effect Rain Foreground Thunder", RainForegroundVfxPath, DayNightWeatherType.Thunder, ForegroundSortingLayerName, 90, 85f, 5.8f, new Vector4(0.93f, 0.97f, 1f, 0.72f));
            VisualEffect thunderEffect = thunderVfx.GetComponent<VisualEffect>();
            if (thunderEffect != null)
            {
                thunderEffect.SetBool("Lightnings", true);
            }

            GameObject waterDrop = CreateRainVfx(rainRoot.transform, "VFX_WaterDrop", WaterDropVfxPath, DayNightWeatherType.Rain | DayNightWeatherType.Thunder, DefaultSortingLayerName, 5, 180f, 5f, new Vector4(1f, 1f, 1.25f, 0.78f));
            waterDrop.transform.localPosition = new Vector3(-4.33f, 4.613f, 0f);

            GameObject wetEffectsRoot = CreateChild(weatherRoot.transform, "Map Wet Effects");
            CreateWeatherVfx(wetEffectsRoot.transform, "VFX_WaterLines", WaterLinesVfxPath, DayNightWeatherType.Rain, DefaultSortingLayerName, 8);
            CreateWeatherVfx(wetEffectsRoot.transform, "VFX_WaterLinesStorm", WaterLinesStormVfxPath, DayNightWeatherType.Thunder, DefaultSortingLayerName, 8);

            GameObject audioRoot = CreateChild(root.transform, "Audio");
            AudioSource dayAmbience = CreateAudioSource(audioRoot.transform, "Day Ambience", DayAudioPath, 0f);
            AudioSource nightAmbience = CreateAudioSource(audioRoot.transform, "Night Ambience", NightAudioPath, 0f);
            CreateAudioSource(audioRoot.transform, "Water Ambience", WaterAudioPath, 0.25f);
            AudioSource rainAmbience = CreateAudioSource(rainRoot.transform, "RainSound", RainAudioPath, 0f);
            rainAmbience.gameObject.AddComponent<DayNightWeatherElement>().WeatherType = DayNightWeatherType.Rain | DayNightWeatherType.Thunder;
            AudioSource thunderSource = CreateAudioSource(rainRoot.transform, "ThunderSound", ThunderAudioPath, 0f);
            thunderSource.playOnAwake = false;
            thunderSource.loop = false;
            DayNightThunderAudio thunderAudio = thunderSource.gameObject.AddComponent<DayNightThunderAudio>();
            thunderAudio.ThunderSource = thunderSource;
            thunderSource.gameObject.AddComponent<DayNightWeatherElement>().WeatherType = DayNightWeatherType.Thunder;

            controller.LightsRoot = lightsRotator.transform;
            controller.DayLight = dayLight;
            controller.NightLight = nightLight;
            controller.AmbientLight = ambientLight;
            controller.SunRimLight = sunRimLight;
            controller.MoonRimLight = moonRimLight;
            controller.WeatherSystem = weatherSystem;
            controller.DayAmbience = dayAmbience;
            controller.NightAmbience = nightAmbience;
            controller.RainAmbience = rainAmbience;
            controller.SetTimeOfDay(controller.StartingTime);
            weatherSystem.ChangeWeather(weatherSystem.StartingWeather);

            return root;
        }

        private static GameObject BuildLightPack()
        {
            GameObject root = new GameObject("Day_Night_Light_Pack");

            InstantiateLightPrefab(root.transform, StreetLampPrefabPath, new Vector3(0f, 0f, 0f));
            InstantiateLightPrefab(root.transform, LanternPrefabPath, new Vector3(5f, 0f, 0f));
            InstantiateLightPrefab(root.transform, LanternHangingPrefabPath, new Vector3(10f, 0f, 0f));
            InstantiateLightPrefab(root.transform, HouseLampPrefabPath, new Vector3(-5f, 0f, 0f));
            InstantiateLightPrefab(root.transform, WindowLightPrefabPath, new Vector3(-10f, 0f, 0f));
            GameObject warehouseSample = InstantiateLightPrefab(root.transform, WarehouseLightPrefabPath, new Vector3(0f, 6f, 0f));
            if (warehouseSample != null)
            {
                warehouseSample.SetActive(false);
            }

            InstantiateLightPrefab(root.transform, FireplacePrefabPath, new Vector3(5f, 6f, 0f));
            GameObject fireVfx = InstantiateLightPrefab(root.transform, FireVfxPrefabPath, new Vector3(10f, 6f, 0f));
            if (fireVfx != null)
            {
                ConfigureFireInstance(fireVfx);
            }

            InstantiateLightPrefab(root.transform, MothsPrefabPath, new Vector3(-5f, 6f, 0f));

            return root;
        }

        private static GameObject InstantiateLightPrefab(Transform parent, string prefabPath, Vector3 localPosition)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("Missing Day_Night light prefab: " + prefabPath);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            ConfigureLightInstance(instance);
            return instance;
        }

        private static void ConfigureLightInstance(GameObject instance)
        {
            Light2D[] lights = instance.GetComponentsInChildren<Light2D>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                ConfigurePlacedLight(lights[i]);
            }

            ShadowCaster2D[] shadowCasters = instance.GetComponentsInChildren<ShadowCaster2D>(true);
            for (int i = 0; i < shadowCasters.Length; i++)
            {
                Undo.RecordObject(shadowCasters[i], "Configure Day Night Shadow Caster");
                shadowCasters[i].enabled = true;
                EditorUtility.SetDirty(shadowCasters[i]);
            }

            if (instance.name.Contains("VFX_Fire"))
            {
                ConfigureFireInstance(instance);
            }
        }

        private static void ConfigureFireInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Undo.RecordObject(instance, "Configure Day Night Fire");
            instance.SetActive(true);

            VisualEffect visualEffect = instance.GetComponent<VisualEffect>();
            if (visualEffect != null)
            {
                Undo.RecordObject(visualEffect, "Configure Day Night Fire VFX");
                visualEffect.enabled = true;
                visualEffect.Play();
                EditorUtility.SetDirty(visualEffect);
            }

            DayNightProceduralFire fire = EnsureComponent<DayNightProceduralFire>(instance);
            fire.Width = 1.05f;
            fire.Height = 1.05f;
            fire.FlickerSpeed = 2.2f;
            fire.FlickerAmount = 0.25f;
            fire.FlameTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FireCircleTexturePath);
            fire.SortingLayerName = ForegroundSortingLayerName;
            fire.SortingOrder = 95;
            EditorUtility.SetDirty(fire);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Undo.RecordObject(renderers[i], "Configure Day Night Fire Renderer");
                renderers[i].enabled = true;
                renderers[i].sortingLayerName = renderers[i].gameObject == instance
                    ? ForegroundSortingLayerName
                    : DefaultSortingLayerName;
                renderers[i].sortingOrder = renderers[i].gameObject == instance ? 95 : 8;
                EditorUtility.SetDirty(renderers[i]);
            }

            Light2D[] lights = instance.GetComponentsInChildren<Light2D>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                ConfigureFireLight(lights[i]);
            }

            EditorUtility.SetDirty(instance);
        }

        private static void ConfigureFireLight(Light2D light)
        {
            if (light == null)
            {
                return;
            }

            Undo.RecordObject(light, "Configure Day Night Fire Light");
            SerializedObject serializedLight = new SerializedObject(light);
            SetColor(serializedLight, "m_Color", new Color(1f, 0.48f, 0.08f, 1f));
            SetFloatMin(serializedLight, "m_Intensity", 5.5f);
            SetFloat(serializedLight, "m_FalloffIntensity", 0.72f);
            SetFloatMin(serializedLight, "m_PointLightInnerRadius", 1.2f);
            SetFloatMin(serializedLight, "m_PointLightOuterRadius", 8.5f);
            SetBool(serializedLight, "m_ShadowsEnabled", true);
            SetFloat(serializedLight, "m_ShadowIntensity", 0.75f);
            SetFloat(serializedLight, "m_ShadowSoftness", 0.45f);
            ApplyToAllSortingLayers(serializedLight);
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        private static void DisableWarehouseSample(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.Contains("Warehouse"))
                {
                    continue;
                }

                Undo.RecordObject(children[i].gameObject, "Disable Warehouse Light Sample");
                children[i].gameObject.SetActive(false);
                EditorUtility.SetDirty(children[i].gameObject);
            }
        }

        private static void ConfigurePlacedLight(Light2D light)
        {
            if (light == null)
            {
                return;
            }

            Undo.RecordObject(light, "Configure Day Night Light");
            SerializedObject serializedLight = new SerializedObject(light);
            SerializedProperty lightType = serializedLight.FindProperty("m_LightType");
            bool isPointLight = lightType == null || lightType.intValue == 3;
            bool isShapeLight = lightType != null && lightType.intValue == 2;

            SetColor(serializedLight, "m_Color", isShapeLight
                ? new Color(1f, 0.86f, 0.45f, 1f)
                : new Color(1f, 0.66f, 0.28f, 1f));
            SetFloatMin(serializedLight, "m_Intensity", isPointLight ? 12.4f : 1.15f);
            SetFloat(serializedLight, "m_FalloffIntensity", 0.65f);
            SetBool(serializedLight, "m_ShadowsEnabled", true);
            SetFloat(serializedLight, "m_ShadowIntensity", 0.9f);
            SetFloat(serializedLight, "m_ShadowSoftness", 0.35f);
            ApplyToAllSortingLayers(serializedLight);

            if (isPointLight)
            {
                SetFloatMin(serializedLight, "m_PointLightInnerRadius", 1.6f);
                SetFloatMin(serializedLight, "m_PointLightOuterRadius", 12f);
            }
            else if (isShapeLight)
            {
                SetFloat(serializedLight, "m_ShapeLightFalloffSize", 1.25f);
            }

            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        private static void ConfigureControllerLights(DayNightCycleController controller)
        {
            if (controller == null)
            {
                return;
            }

            ConfigureDayNightLight(controller.DayLight);
            ConfigureDayNightLight(controller.NightLight);
            ConfigureDayNightLight(controller.AmbientLight);
            ConfigureDayNightLight(controller.SunRimLight);
            ConfigureDayNightLight(controller.MoonRimLight);
        }

        private static void ConfigureDayNightLight(Light2D light)
        {
            if (light == null)
            {
                return;
            }

            Undo.RecordObject(light, "Configure Day Night Sorting Layers");
            SerializedObject serializedLight = new SerializedObject(light);
            ApplyToAllSortingLayers(serializedLight);
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        public static void FixSetupInScene(DayNightCycleController controller, bool previewBrightDay)
        {
            if (controller == null)
            {
                return;
            }

            Undo.RecordObject(controller, "Fix Day Night Setup");
            controller.ResetToHappyHarvestDefaults();
            ConfigureControllerLights(controller);

            DayNightWeatherSystem weatherSystem = controller.WeatherSystem;
            if (weatherSystem == null)
            {
                weatherSystem = controller.GetComponent<DayNightWeatherSystem>();
                if (weatherSystem == null)
                {
                    weatherSystem = Undo.AddComponent<DayNightWeatherSystem>(controller.gameObject);
                }

                controller.WeatherSystem = weatherSystem;
            }

            Transform weatherRoot = weatherSystem.SearchRoot;
            if (weatherRoot == null)
            {
                weatherRoot = EnsureChild(controller.transform, "Weather").transform;
                weatherSystem.SearchRoot = weatherRoot;
            }

            GameObject rainRoot = EnsureChild(weatherRoot, "Rain");
            EnsureSingleComponent<DayNightRainFollower>(rainRoot);

            EnsureRainVfx(rainRoot.transform, "Visual Effect Rain", RainVfxPath, DayNightWeatherType.Rain | DayNightWeatherType.Thunder, DefaultSortingLayerName, 0, 45f, 4.6f, new Vector4(0.9f, 0.9f, 1.25f, 0.58f));
            EnsureRainVfx(rainRoot.transform, "Visual Effect Rain Foreground", RainForegroundVfxPath, DayNightWeatherType.Rain, ForegroundSortingLayerName, 90, 320f, 5.8f, new Vector4(0.9f, 0.9f, 1.25f, 0.58f));
            GameObject thunderVfx = EnsureRainVfx(rainRoot.transform, "Visual Effect Rain Foreground Thunder", RainForegroundVfxPath, DayNightWeatherType.Thunder, ForegroundSortingLayerName, 90, 85f, 5.8f, new Vector4(0.93f, 0.97f, 1f, 0.72f));
            VisualEffect thunderEffect = thunderVfx.GetComponent<VisualEffect>();
            if (thunderEffect != null)
            {
                thunderEffect.SetBool("Lightnings", true);
            }

            GameObject waterDrop = EnsureRainVfx(rainRoot.transform, "VFX_WaterDrop", WaterDropVfxPath, DayNightWeatherType.Rain | DayNightWeatherType.Thunder, DefaultSortingLayerName, 5, 180f, 5f, new Vector4(1f, 1f, 1.25f, 0.78f));
            if (waterDrop.transform.localPosition == Vector3.zero)
            {
                Undo.RecordObject(waterDrop.transform, "Position Water Drop VFX");
                waterDrop.transform.localPosition = new Vector3(-4.33f, 4.613f, 0f);
            }

            FixRainVfxSorting(rainRoot.transform);
            EnsureRainOverlay(rainRoot.transform, "Rain Lines Visible");

            if (FindDirectChild(rainRoot.transform, "Rain Particles Fallback") == null)
            {
                CreateRainParticles(rainRoot.transform, "Rain Particles Fallback", DayNightWeatherType.Rain | DayNightWeatherType.Thunder, ForegroundSortingLayerName, 92);
            }

            Transform wetEffectsRoot = EnsureChild(weatherRoot, "Map Wet Effects").transform;
            if (FindDirectChild(wetEffectsRoot, "VFX_WaterLines") == null)
            {
                CreateWeatherVfx(wetEffectsRoot, "VFX_WaterLines", WaterLinesVfxPath, DayNightWeatherType.Rain, DefaultSortingLayerName, 8);
            }

            if (FindDirectChild(wetEffectsRoot, "VFX_WaterLinesStorm") == null)
            {
                CreateWeatherVfx(wetEffectsRoot, "VFX_WaterLinesStorm", WaterLinesStormVfxPath, DayNightWeatherType.Thunder, DefaultSortingLayerName, 8);
            }

            Transform audioRoot = EnsureChild(controller.transform, "Audio").transform;
            if (FindDirectChild(audioRoot, "Water Ambience") == null)
            {
                CreateAudioSource(audioRoot, "Water Ambience", WaterAudioPath, 0.25f);
            }

            Transform rainSound = FindDirectChild(rainRoot.transform, "RainSound");
            if (rainSound == null)
            {
                AudioSource rainSource = CreateAudioSource(rainRoot.transform, "RainSound", RainAudioPath, 0f);
                rainSource.gameObject.AddComponent<DayNightWeatherElement>().WeatherType = DayNightWeatherType.Rain | DayNightWeatherType.Thunder;
                controller.RainAmbience = rainSource;
            }
            else
            {
                AudioSource rainSource = EnsureComponent<AudioSource>(rainSound.gameObject);
                rainSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RainAudioPath);
                rainSource.playOnAwake = true;
                rainSource.loop = true;
                DayNightWeatherElement rainElement = EnsureComponent<DayNightWeatherElement>(rainSound.gameObject);
                rainElement.WeatherType = DayNightWeatherType.Rain | DayNightWeatherType.Thunder;
                controller.RainAmbience = rainSource;
            }

            if (FindDirectChild(rainRoot.transform, "ThunderSound") == null)
            {
                AudioSource thunderSource = CreateAudioSource(rainRoot.transform, "ThunderSound", ThunderAudioPath, 0f);
                thunderSource.playOnAwake = false;
                thunderSource.loop = false;
                DayNightThunderAudio thunderAudio = thunderSource.gameObject.AddComponent<DayNightThunderAudio>();
                thunderAudio.ThunderSource = thunderSource;
                thunderSource.gameObject.AddComponent<DayNightWeatherElement>().WeatherType = DayNightWeatherType.Thunder;
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(weatherSystem);
            AttachWaterAudioInCurrentScene();
            FixWeatherVfxVisibility(controller, FindSceneCamera(), false);

            if (previewBrightDay)
            {
                weatherSystem.ChangeWeather(DayNightWeatherType.Sun);
                controller.SetTimeOfDay(0.5f);
            }
            else
            {
                weatherSystem.RefreshElements();
                DayNightWeatherType currentWeather = weatherSystem.CurrentWeather == 0
                    ? weatherSystem.StartingWeather
                    : weatherSystem.CurrentWeather;
                weatherSystem.ChangeWeather(currentWeather);
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Light2D AddLight(GameObject target, string lightLabel, int lightType, int blendStyle, float intensity, float innerRadius, float outerRadius, float innerAngle, float outerAngle)
        {
            Light2D light = target.AddComponent<Light2D>();
            SerializedObject serializedLight = new SerializedObject(light);
            SetInt(serializedLight, "m_LightType", lightType);
            SetInt(serializedLight, "m_BlendStyleIndex", blendStyle);
            SetFloat(serializedLight, "m_Intensity", intensity);
            SetFloat(serializedLight, "m_PointLightInnerRadius", innerRadius);
            SetFloat(serializedLight, "m_PointLightOuterRadius", outerRadius);
            SetFloat(serializedLight, "m_PointLightInnerAngle", innerAngle);
            SetFloat(serializedLight, "m_PointLightOuterAngle", outerAngle);
            ApplyToAllSortingLayers(serializedLight);
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            light.name = lightLabel;
            return light;
        }

        private static GameObject CreateRainVfx(Transform parent, string name, string assetPath, DayNightWeatherType weatherType, string sortingLayerName, int sortingOrder, float rainRate, float rainSpeed, Vector4 rainTint)
        {
            GameObject target = CreateWeatherVfx(parent, name, assetPath, weatherType, sortingLayerName, sortingOrder);
            VisualEffect effect = target.GetComponent<VisualEffect>();
            if (effect != null)
            {
                SetVfxFloat(effect, "RainSpeed", rainSpeed);
                SetVfxFloat(effect, "RainRate", rainRate);
                SetVfxVector2(effect, "RainDirection", new Vector2(1f, 7.65f));
                SetVfxVector3(effect, "RainDirectionV3", new Vector3(0f, -2.6f, 0f));
                SetVfxVector4(effect, "RainTintColor", rainTint);
            }

            return target;
        }

        private static GameObject EnsureRainVfx(Transform parent, string name, string assetPath, DayNightWeatherType weatherType, string sortingLayerName, int sortingOrder, float rainRate, float rainSpeed, Vector4 rainTint)
        {
            GameObject target = EnsureWeatherVfx(parent, name, assetPath, weatherType, sortingLayerName, sortingOrder);
            VisualEffect effect = target.GetComponent<VisualEffect>();
            if (effect != null)
            {
                SetVfxFloat(effect, "RainSpeed", rainSpeed);
                SetVfxFloat(effect, "RainRate", rainRate);
                SetVfxVector2(effect, "RainDirection", new Vector2(1f, 7.65f));
                SetVfxVector3(effect, "RainDirectionV3", new Vector3(0f, -2.6f, 0f));
                SetVfxVector4(effect, "RainTintColor", rainTint);
                SetVfxVector4(effect, "RainColor", rainTint);
            }

            return target;
        }

        private static GameObject CreateWeatherVfx(Transform parent, string name, string assetPath, DayNightWeatherType weatherType, string sortingLayerName, int sortingOrder)
        {
            GameObject target = CreateChild(parent, name);
            VisualEffect effect = target.AddComponent<VisualEffect>();
            effect.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
            }

            target.AddComponent<DayNightWeatherElement>().WeatherType = weatherType;
            return target;
        }

        private static GameObject EnsureWeatherVfx(Transform parent, string name, string assetPath, DayNightWeatherType weatherType, string sortingLayerName, int sortingOrder)
        {
            GameObject target = EnsureChild(parent, name);
            VisualEffect effect = EnsureComponent<VisualEffect>(target);
            Undo.RecordObject(effect, "Configure Weather VFX");
            effect.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Configure Weather VFX Sorting");
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                EditorUtility.SetDirty(renderer);
            }

            DayNightWeatherElement element = EnsureComponent<DayNightWeatherElement>(target);
            element.WeatherType = weatherType;
            EditorUtility.SetDirty(effect);
            EditorUtility.SetDirty(element);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static GameObject CreateRainParticles(Transform parent, string name, DayNightWeatherType weatherType, string sortingLayerName, int sortingOrder)
        {
            GameObject target = CreateChild(parent, name);
            ParticleSystem particles = target.AddComponent<ParticleSystem>();
            ConfigureRainParticles(particles, sortingLayerName, sortingOrder, FindSceneCamera());

            target.AddComponent<DayNightWeatherElement>().WeatherType = weatherType;
            return target;
        }

        private static void ConfigureRainParticles(ParticleSystem particles, string sortingLayerName, int sortingOrder, Camera sceneCamera)
        {
            if (particles == null)
            {
                return;
            }

            Undo.RecordObject(particles, "Configure Visible Rain Particles");
            float cameraHeight = sceneCamera != null && sceneCamera.orthographic ? sceneCamera.orthographicSize * 2f : 2400f;
            float cameraWidth = sceneCamera != null && sceneCamera.orthographic ? cameraHeight * sceneCamera.aspect : 3800f;

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.94f, 1f, 0.9f), new Color(1f, 1f, 1f, 1f));
            main.maxParticles = 12000;
            main.prewarm = true;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 5200f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(cameraWidth * 1.35f, 10f, 1f);
            shape.position = new Vector3(0f, cameraHeight * 0.65f, 0f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-460f, -220f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2600f, -1850f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return;
            }

            Undo.RecordObject(renderer, "Configure Visible Rain Particle Sorting");
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 13f;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            Material particleMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (particleMaterial != null)
            {
                renderer.sharedMaterial = particleMaterial;
            }

            if (Application.isPlaying)
            {
                particles.Play(true);
            }

            EditorUtility.SetDirty(particles);
            EditorUtility.SetDirty(renderer);
        }

        private static GameObject CreateRainOverlay(Transform parent, string name, DayNightWeatherType weatherType)
        {
            GameObject target = CreateChild(parent, name);
            DayNightRainOverlay overlay = target.AddComponent<DayNightRainOverlay>();
            ConfigureRainOverlay(overlay);
            target.AddComponent<DayNightWeatherElement>().WeatherType = weatherType;
            return target;
        }

        private static void EnsureRainOverlay(Transform parent, string name)
        {
            GameObject target = EnsureChild(parent, name);
            DayNightRainOverlay overlay = EnsureComponent<DayNightRainOverlay>(target);
            ConfigureRainOverlay(overlay);
            DayNightWeatherElement element = EnsureComponent<DayNightWeatherElement>(target);
            element.WeatherType = DayNightWeatherType.Rain | DayNightWeatherType.Thunder;
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureRainOverlay(DayNightRainOverlay overlay)
        {
            overlay.AreaSize = new Vector2(2600f, 1600f);
            overlay.DropCount = 550;
            overlay.FallSpeed = 850f;
            overlay.DropLength = 78f;
            overlay.DropWidth = 2.2f;
            overlay.DropHeadSize = 4.5f;
            overlay.DropColor = new Color(0.86f, 0.96f, 1f, 0.72f);
            overlay.SortingLayerName = ForegroundSortingLayerName;
            overlay.SortingOrder = 260;
        }

        private static void FixRainVfxSorting(Transform rainRoot)
        {
            SetChildRendererSorting(rainRoot, "Visual Effect Rain", DefaultSortingLayerName, 0);
            SetChildRendererSorting(rainRoot, "Visual Effect Rain Foreground", ForegroundSortingLayerName, 90);
            SetChildRendererSorting(rainRoot, "Visual Effect Rain Foreground Thunder", ForegroundSortingLayerName, 90);
            SetChildRendererSorting(rainRoot, "VFX_WaterDrop", DefaultSortingLayerName, 5);
            SetChildRendererSorting(rainRoot, "Rain Particles Fallback", ForegroundSortingLayerName, 240);
            SetChildRendererSorting(rainRoot, "Rain Lines Visible", ForegroundSortingLayerName, 260);
        }

        private static void SetChildRendererSorting(Transform parent, string childName, string sortingLayerName, int sortingOrder)
        {
            Transform child = FindDirectChild(parent, childName);
            if (child == null)
            {
                return;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void FixWeatherVfxVisibility(DayNightCycleController controller, Camera sceneCamera, bool previewRain)
        {
            if (controller == null || controller.WeatherSystem == null)
            {
                return;
            }

            DayNightWeatherSystem weatherSystem = controller.WeatherSystem;
            FixSetupSortingGroup(controller.gameObject);
            Transform weatherRoot = weatherSystem.SearchRoot;
            Transform rainRoot = weatherRoot == null ? null : FindDirectChild(weatherRoot, "Rain");
            if (rainRoot == null)
            {
                return;
            }

            DayNightRainFollower follower = EnsureSingleComponent<DayNightRainFollower>(rainRoot.gameObject);
            Undo.RecordObject(follower, "Fix Rain Camera Follow");
            follower.Target = sceneCamera != null ? sceneCamera.transform : null;
            follower.Offset = Vector3.zero;
            follower.FollowZ = false;
            follower.FollowInEditMode = false;
            EditorUtility.SetDirty(follower);

            if (sceneCamera != null)
            {
                Undo.RecordObject(rainRoot, "Move Rain VFX To Camera");
                Vector3 position = sceneCamera.transform.position;
                position.z = rainRoot.position.z;
                rainRoot.position = position;
            }

            ConfigureRainVisualEffect(rainRoot, "Visual Effect Rain", DefaultSortingLayerName, 100, 220f);
            ConfigureRainVisualEffect(rainRoot, "Visual Effect Rain Foreground", ForegroundSortingLayerName, 230, 220f);
            ConfigureRainVisualEffect(rainRoot, "Visual Effect Rain Foreground Thunder", ForegroundSortingLayerName, 235, 240f);
            ConfigureRainVisualEffect(rainRoot, "VFX_WaterDrop", ForegroundSortingLayerName, 238, 220f);

            Transform fallback = FindDirectChild(rainRoot, "Rain Particles Fallback");
            if (fallback != null)
            {
                ConfigureRainParticles(fallback.GetComponent<ParticleSystem>(), ForegroundSortingLayerName, 240, sceneCamera);
            }

            Transform overlayTransform = FindDirectChild(rainRoot, "Rain Lines Visible");
            if (overlayTransform != null)
            {
                DayNightRainOverlay overlay = overlayTransform.GetComponent<DayNightRainOverlay>();
                if (overlay != null)
                {
                    Undo.RecordObject(overlay, "Fix Visible Rain Overlay");
                    ConfigureRainOverlay(overlay);
                    EditorUtility.SetDirty(overlay);
                }
            }

            if (previewRain)
            {
                Undo.RecordObject(weatherSystem, "Preview Rain Weather");
                weatherSystem.StartingWeather = DayNightWeatherType.Rain;
                weatherSystem.ChangeWeather(DayNightWeatherType.Rain);
                EditorUtility.SetDirty(weatherSystem);
            }
        }

        private static void ConfigureRainVisualEffect(Transform rainRoot, string childName, string sortingLayerName, int sortingOrder, float scale)
        {
            Transform child = FindDirectChild(rainRoot, childName);
            if (child == null)
            {
                return;
            }

            Undo.RecordObject(child, "Scale Rain VFX");
            child.localScale = Vector3.one * scale;
            EditorUtility.SetDirty(child);

            VisualEffect effect = child.GetComponent<VisualEffect>();
            if (effect != null)
            {
                Undo.RecordObject(effect, "Tune Rain VFX");
                bool isWaterDrop = childName.Contains("WaterDrop");
                bool isThunder = childName.Contains("Thunder");
                bool isForeground = childName.Contains("Foreground");
                float rainRate = isWaterDrop ? 180f : isThunder ? 85f : isForeground ? 320f : 45f;
                float rainSpeed = isWaterDrop ? 5f : isForeground || isThunder ? 5.8f : 4.6f;
                Vector4 tint = isWaterDrop ? new Vector4(1f, 1f, 1.25f, 0.78f) :
                    isThunder ? new Vector4(0.93f, 0.97f, 1f, 0.72f) :
                    new Vector4(0.9f, 0.9f, 1.25f, 0.58f);
                SetVfxFloat(effect, "RainRate", rainRate);
                SetVfxFloat(effect, "RainSpeed", rainSpeed);
                SetVfxFloat(effect, "EffectSizeMultiplier", 1.15f);
                SetVfxFloat(effect, "Alpha", tint.w);
                SetVfxVector4(effect, "RainTintColor", tint);
                SetVfxVector4(effect, "RainColor", tint);
                effect.Reinit();
                EditorUtility.SetDirty(effect);
            }

            Renderer renderer = child.GetComponent<Renderer>();
            SetRendererSorting(renderer, sortingLayerName, sortingOrder);
        }

        private static void FixAmbientVfxVisibility(Camera sceneCamera)
        {
            VisualEffect[] effects = Object.FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int dustIndex = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                VisualEffect effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                string objectName = effect.gameObject.name;
                if (objectName.Contains(LeavesVfxName))
                {
                    ConfigureAmbientVfx(effect, sceneCamera, Vector3.zero, 220f, ForegroundSortingLayerName, 80, 30f, 45f);
                }
                else if (objectName.Contains(DustVfxName))
                {
                    Vector3 offset = GetDustCameraOffset(sceneCamera, dustIndex++);
                    ConfigureAmbientVfx(effect, sceneCamera, offset, 620f, ForegroundSortingLayerName, 110, 26f, 160f);
                }
            }
        }

        private static void ConfigureAmbientVfx(VisualEffect effect, Camera sceneCamera, Vector3 cameraOffset, float scale, string sortingLayerName, int sortingOrder, float spawnRadius, float spawnRate)
        {
            GameObject target = effect.gameObject;
            Undo.RecordObject(target, "Enable Ambient VFX");
            target.SetActive(true);

            Undo.RecordObject(target.transform, "Move Ambient VFX To Camera");
            target.transform.localScale = Vector3.one * scale;
            if (sceneCamera != null)
            {
                Vector3 position = sceneCamera.transform.position + cameraOffset;
                position.z = target.transform.position.z;
                target.transform.position = position;
            }
            EditorUtility.SetDirty(target.transform);

            DayNightRainFollower follower = EnsureSingleComponent<DayNightRainFollower>(target);
            Undo.RecordObject(follower, "Follow Camera For Ambient VFX");
            follower.Target = sceneCamera != null ? sceneCamera.transform : null;
            follower.Offset = cameraOffset;
            follower.FollowZ = false;
            follower.FollowInEditMode = false;
            EditorUtility.SetDirty(follower);

            Undo.RecordObject(effect, "Boost Ambient VFX");
            SetVfxFloat(effect, "Spawn Radius", spawnRadius);
            SetVfxFloat(effect, "Spawn Rate", spawnRate);
            SetVfxFloat(effect, "SpawnRate", spawnRate * 120f);
            SetVfxFloat(effect, "EffectSizeMultiplier", target.name.Contains(DustVfxName) ? 3.8f : 3f);
            SetVfxFloat(effect, "Alpha", target.name.Contains(DustVfxName) ? 0.58f : 0.9f);
            if (target.name.Contains(DustVfxName))
            {
                SetDustVfxColor(effect);
            }
            effect.Reinit();
            EditorUtility.SetDirty(effect);

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SetRendererSorting(renderers[i], sortingLayerName, sortingOrder);
            }

            SortingGroup sortingGroup = target.GetComponent<SortingGroup>();
            if (sortingGroup != null)
            {
                Undo.RecordObject(sortingGroup, "Fix Ambient VFX Sorting");
                sortingGroup.sortingLayerName = sortingLayerName;
                sortingGroup.sortingOrder = sortingOrder;
                EditorUtility.SetDirty(sortingGroup);
            }

            EditorUtility.SetDirty(target);
        }

        private static Vector3 GetDustCameraOffset(Camera sceneCamera, int index)
        {
            float cameraHeight = sceneCamera != null && sceneCamera.orthographic ? sceneCamera.orthographicSize * 2f : 2400f;
            float cameraWidth = sceneCamera != null && sceneCamera.orthographic ? cameraHeight * sceneCamera.aspect : 3800f;
            int column = index % 4;
            int row = (index / 4) % 3;
            float x = Mathf.Lerp(-0.42f, 0.42f, column / 3f) * cameraWidth;
            float y = Mathf.Lerp(-0.32f, 0.32f, row / 2f) * cameraHeight;
            return new Vector3(x, y, 0f);
        }

        private static Camera FindSceneCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return cameras.Length > 0 ? cameras[0] : null;
        }

        private static void EnsureSceneAudioListener(Camera sceneCamera)
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (listeners.Length > 0 || sceneCamera == null)
            {
                return;
            }

            Undo.AddComponent<AudioListener>(sceneCamera.gameObject);
            EditorUtility.SetDirty(sceneCamera.gameObject);
        }

        private static void FixSetupSortingGroup(GameObject setup)
        {
            SortingGroup sortingGroup = setup.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                return;
            }

            Undo.RecordObject(sortingGroup, "Fix Day Night Setup Sorting Group");
            sortingGroup.sortingLayerName = ForegroundSortingLayerName;
            sortingGroup.sortingOrder = 200;
            EditorUtility.SetDirty(sortingGroup);
        }

        private static void SetDustVfxColor(VisualEffect effect)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.9f, 0.78f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 0.92f), 0.45f),
                    new GradientColorKey(new Color(0.72f, 0.88f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.05f, 0f),
                    new GradientAlphaKey(0.62f, 0.45f),
                    new GradientAlphaKey(0.02f, 1f)
                });

            SetVfxGradient(effect, "ColorGradient", gradient);
            SetVfxVector4(effect, "Color", new Vector4(0.95f, 0.9f, 0.76f, 0.58f));
            SetVfxVector4(effect, "Tint", new Vector4(0.95f, 0.9f, 0.76f, 0.58f));
        }

        private static void RemoveDuplicateRainFollowersInScene()
        {
            DayNightRainFollower[] followers = Object.FindObjectsByType<DayNightRainFollower>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < followers.Length; i++)
            {
                DayNightRainFollower follower = followers[i];
                if (follower == null)
                {
                    continue;
                }

                DayNightRainFollower singleFollower = EnsureSingleComponent<DayNightRainFollower>(follower.gameObject);
                Undo.RecordObject(singleFollower, "Disable Edit Mode VFX Follow");
                singleFollower.FollowInEditMode = false;
                EditorUtility.SetDirty(singleFollower);
            }
        }

        private static void SetRendererSorting(Renderer renderer, string sortingLayerName, int sortingOrder)
        {
            if (renderer == null)
            {
                return;
            }

            Undo.RecordObject(renderer, "Fix VFX Sorting");
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(renderer);
        }

        private static AudioSource CreateAudioSource(Transform parent, string name, string clipPath, float volume)
        {
            GameObject target = CreateChild(parent, name);
            AudioSource source = target.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.playOnAwake = true;
            source.loop = true;
            source.volume = volume;
            source.spatialBlend = 0f;
            return source;
        }

        private static Transform FindWaterAudioTarget()
        {
            TilemapRenderer[] tilemaps = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform fallback = null;
            for (int i = 0; i < tilemaps.Length; i++)
            {
                TilemapRenderer tilemap = tilemaps[i];
                if (tilemap == null)
                {
                    continue;
                }

                string normalizedName = NormalizeName(tilemap.gameObject.name);
                if (normalizedName.Contains("underwater"))
                {
                    continue;
                }

                bool preferred = normalizedName == "water" || normalizedName == "water tilemap" || normalizedName == "nuoc";
                bool waterLike = preferred || normalizedName.Contains("water") || normalizedName.Contains("nuoc");
                if (!waterLike)
                {
                    continue;
                }

                if (preferred)
                {
                    return tilemap.transform;
                }

                if (fallback == null)
                {
                    fallback = tilemap.transform;
                }
            }

            if (fallback != null)
            {
                return fallback;
            }

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                string normalizedName = NormalizeName(transforms[i].name);
                if ((normalizedName == "water" || normalizedName == "nuoc") && !normalizedName.Contains("underwater"))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Normalize(NormalizationForm.FormD).ToLowerInvariant();
            char[] chars = new char[normalized.Length];
            int index = 0;
            for (int i = 0; i < normalized.Length; i++)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalized[i]);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                char c = normalized[i];
                chars[index++] = c == '_' || c == '-' ? ' ' : c;
            }

            return new string(chars, 0, index).Trim();
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = CreateChild(parent, name);
            Undo.RegisterCreatedObjectUndo(child, "Create Day Night Child");
            return child;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(target);
        }

        private static T EnsureSingleComponent<T>(GameObject target) where T : Component
        {
            T[] components = target.GetComponents<T>();
            if (components.Length == 0)
            {
                return Undo.AddComponent<T>(target);
            }

            T keep = components[0];
            for (int i = 1; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(components[i]);
            }

            return keep;
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        private static void SetFloatMin(SerializedObject serializedObject, string propertyName, float minValue)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = Mathf.Max(property.floatValue, minValue);
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void ApplyToAllSortingLayers(SerializedObject serializedObject)
        {
            SerializedProperty property = serializedObject.FindProperty("m_ApplyToSortingLayers");
            if (property == null || !property.isArray)
            {
                return;
            }

            SortingLayer[] layers = SortingLayer.layers;
            property.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = layers[i].id;
            }
        }

        private static Material LoadSpriteLitMaterial()
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid);
            return string.IsNullOrEmpty(materialPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        private static bool IsUnderNamedParent(Transform transform, string parentName)
        {
            Transform cursor = transform;
            while (cursor != null)
            {
                if (cursor.name == parentName)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static bool HasComponentInParentByName(Transform transform, params string[] componentTypeNames)
        {
            Transform cursor = transform;
            while (cursor != null)
            {
                MonoBehaviour[] behaviours = cursor.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    string typeName = behaviour.GetType().Name;
                    for (int j = 0; j < componentTypeNames.Length; j++)
                    {
                        if (typeName == componentTypeNames[j])
                        {
                            return true;
                        }
                    }
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static void FixMarketPopupBlocking()
        {
            GameObject marketCanvas = GameObject.Find("Canvas_MarketPopup");
            if (marketCanvas == null)
            {
                return;
            }

            Undo.RecordObject(marketCanvas, "Fix Market Canvas Blocking");
            marketCanvas.SetActive(true);
            Undo.RecordObject(marketCanvas.transform, "Fix Market Canvas Scale");
            marketCanvas.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(marketCanvas.transform);

            CanvasGroup canvasGroup = marketCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Undo.RecordObject(canvasGroup, "Fix Market Canvas Group");
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                EditorUtility.SetDirty(canvasGroup);
            }

            Transform panel = FindDirectChild(marketCanvas.transform, "Panel_Background");
            if (panel != null)
            {
                Undo.RecordObject(panel.gameObject, "Keep Market Popup Panel Active");
                panel.gameObject.SetActive(true);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorUtility.SetDirty(marketCanvas);
        }

        private static void SetVfxFloat(VisualEffect effect, string propertyName, float value)
        {
            if (effect.HasFloat(propertyName))
            {
                effect.SetFloat(propertyName, value);
            }
        }

        private static void SetVfxVector2(VisualEffect effect, string propertyName, Vector2 value)
        {
            if (effect.HasVector2(propertyName))
            {
                effect.SetVector2(propertyName, value);
            }
        }

        private static void SetVfxVector3(VisualEffect effect, string propertyName, Vector3 value)
        {
            if (effect.HasVector3(propertyName))
            {
                effect.SetVector3(propertyName, value);
            }
        }

        private static void SetVfxVector4(VisualEffect effect, string propertyName, Vector4 value)
        {
            if (effect.HasVector4(propertyName))
            {
                effect.SetVector4(propertyName, value);
            }
        }

        private static void SetVfxGradient(VisualEffect effect, string propertyName, Gradient value)
        {
            if (effect.HasGradient(propertyName))
            {
                effect.SetGradient(propertyName, value);
            }
        }
    }

    public class DayNightSetupWindow : EditorWindow
    {
        [MenuItem("Tools/Day Night/Open Setup Tool")]
        public static void Open()
        {
            GetWindow<DayNightSetupWindow>("Day Night");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Day Night Weather Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Create Or Refresh Setup Prefab"))
            {
                DayNightSetupTool.CreateOrRefreshPrefab();
            }

            if (GUILayout.Button("Spawn Setup In Current Scene"))
            {
                DayNightSetupTool.SpawnSetupInCurrentScene();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Light Prefabs", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Or Refresh Light Pack Prefab"))
            {
                DayNightSetupTool.CreateOrRefreshLightPackPrefab();
            }

            if (GUILayout.Button("Spawn Light Pack In Current Scene"))
            {
                DayNightSetupTool.SpawnLightPackInCurrentScene();
            }

            if (GUILayout.Button("Restore Deleted Light Pack In Current Scene"))
            {
                DayNightSetupTool.RestoreDeletedLightPackInCurrentScene();
            }

            if (GUILayout.Button("Fix Light Packs In Current Scene"))
            {
                DayNightSetupTool.FixLightPacksInCurrentScene();
            }

            if (GUILayout.Button("Fix Fire VFX In Current Scene"))
            {
                DayNightSetupTool.FixFireVfxInCurrentScene();
            }

            if (GUILayout.Button("Fix Farm Scene Lighting Sorting UI"))
            {
                DayNightSetupTool.FixFarmSceneLightingSortingUi();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Creates the full hierarchy: DayNight controller, bright day/night/rim Light2D setup, rain/thunder VFX, visible particle rain fallback, water-line weather effects, day/night/rain/thunder audio, and a separate sample pack for copied light prefabs.",
                MessageType.Info);
        }
    }

    [CustomEditor(typeof(DayNightCycleController))]
    public class DayNightCycleControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DayNightCycleController controller = (DayNightCycleController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (GUILayout.Button("Fix This Setup + Bright Day"))
            {
                DayNightSetupTool.FixSetupInScene(controller, true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Day"))
                {
                    SetPreview(controller, 0.5f, DayNightWeatherType.Sun);
                }

                if (GUILayout.Button("Morning"))
                {
                    SetPreview(controller, 0.34f, DayNightWeatherType.Sun);
                }

                if (GUILayout.Button("Evening"))
                {
                    SetPreview(controller, 0.76f, DayNightWeatherType.Sun);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Night"))
                {
                    SetPreview(controller, 0.05f, DayNightWeatherType.Sun);
                }

                if (GUILayout.Button("Rain"))
                {
                    SetPreview(controller, 0.5f, DayNightWeatherType.Rain);
                    DayNightSetupTool.FixSetupInScene(controller, false);
                    SetPreview(controller, 0.5f, DayNightWeatherType.Rain);
                }

                if (GUILayout.Button("Thunder"))
                {
                    SetPreview(controller, 0.5f, DayNightWeatherType.Thunder);
                }
            }
        }

        private static void SetPreview(DayNightCycleController controller, float time, DayNightWeatherType weather)
        {
            Undo.RecordObject(controller, "Preview Day Night Weather");
            controller.SetTimeOfDay(time);
            controller.SetWeather(weather);
            EditorUtility.SetDirty(controller);

            if (controller.WeatherSystem != null)
            {
                EditorUtility.SetDirty(controller.WeatherSystem);
            }
        }
    }
}
