using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool tạo NPC_Farmer tự động: cắt sprite, tạo animation, tạo Animator và tạo 4 anchor.
/// Mở bằng menu Tools > Setup NPC Farmer.
/// </summary>
public class NPCFarmerSetupTool : EditorWindow
{
    private const string AbsoluteSpriteFolder = "E:\\game\\My project\\Assets\\N\u00f4ng d\u00e2n\\NV_01";
    private const string ProjectSpriteFolder = "Assets/N\u00f4ng d\u00e2n/NV_01";
    private const string GeneratedFolder = "Assets/N\u00f4ng d\u00e2n/NV_01/Generated/NPCFarmer";
    private const string ControllerPath = GeneratedFolder + "/NPC_Farmer.controller";

    private const string DirectionX = "Direction X";
    private const string DirectionY = "Direction Y";
    private const string Speed = "Speed";
    private const string Water = "Water";
    private const string Celebrate = "Celebrate";

    private const int Columns = 6;
    private const int Rows = 4;
    private const int SortingOrder = 100;

    private float pixelsPerUnit = 100f;
    private float walkFrameRate = 10f;
    private float actionFrameRate = 8f;
    private float waypointRadiusX = 2.5f;
    private float waypointRadiusY = 1.25f;

    [MenuItem("Tools/Setup NPC Farmer")]
    public static void ShowWindow()
    {
        NPCFarmerSetupTool window = GetWindow<NPCFarmerSetupTool>("NPC Farmer Setup");
        window.minSize = new Vector2(440f, 280f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("One-click NPC Farmer Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.HelpBox(
            "Tool sẽ đọc sprite sheet tại đúng thư mục:\n" + AbsoluteSpriteFolder +
            "\n\nSau khi tạo NPC_Farmer, bạn có thể kéo 4 Anchor con trong Scene để chỉnh đường tuần tra.",
            MessageType.Info);

        pixelsPerUnit = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit", "Đổi kích thước NPC trong world."), pixelsPerUnit);
        walkFrameRate = EditorGUILayout.FloatField(new GUIContent("Walk FPS", "Tốc độ animation đi bộ."), walkFrameRate);
        actionFrameRate = EditorGUILayout.FloatField(new GUIContent("Action FPS", "Tốc độ animation hành động."), actionFrameRate);
        waypointRadiusX = EditorGUILayout.FloatField(new GUIContent("Anchor Width", "Khoảng cách ngang của 4 Anchor mặc định."), waypointRadiusX);
        waypointRadiusY = EditorGUILayout.FloatField(new GUIContent("Anchor Height", "Khoảng cách dọc của 4 Anchor mặc định."), waypointRadiusY);

        pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        walkFrameRate = Mathf.Max(1f, walkFrameRate);
        actionFrameRate = Mathf.Max(1f, actionFrameRate);
        waypointRadiusX = Mathf.Max(0.1f, waypointRadiusX);
        waypointRadiusY = Mathf.Max(0.1f, waypointRadiusY);

        EditorGUILayout.Space(10f);

        if (GUILayout.Button("Setup NPC Farmer", GUILayout.Height(42f)))
        {
            SetupNPCFarmer();
        }
    }

    private void SetupNPCFarmer()
    {
        try
        {
            EnsureGeneratedFolder();

            string texturePath = FindSpriteSheetAssetPath();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Không load được Texture2D tại: " + texturePath);
            }

            SliceSpriteSheet(texturePath, texture);

            Dictionary<string, Sprite[]> spritesByDirection = LoadDirectionalSprites(texturePath);
            AnimationClip idleClip = CreateSpriteClip("NPCFarmer_Idle", new[] { spritesByDirection["Down"][0] }, 1f, true);
            AnimationClip walkRightClip = CreateSpriteClip("NPCFarmer_Walk_Right", spritesByDirection["Right"], walkFrameRate, true);
            AnimationClip walkLeftClip = CreateSpriteClip("NPCFarmer_Walk_Left", spritesByDirection["Left"], walkFrameRate, true);
            AnimationClip walkDownClip = CreateSpriteClip("NPCFarmer_Walk_Down", spritesByDirection["Down"], walkFrameRate, true);
            AnimationClip walkUpClip = CreateSpriteClip("NPCFarmer_Walk_Up", spritesByDirection["Up"], walkFrameRate, true);

            // Sheet hiện tại có 4 hàng đi bộ; Water/Celebrate dùng lại frame sẵn có để có clip mẫu.
            // Khi có sprite hành động riêng, bạn chỉ cần thay clip trong Animator.
            AnimationClip waterClip = CreateSpriteClip("NPCFarmer_WaterPlants", spritesByDirection["Down"], actionFrameRate, false);
            AnimationClip celebrateClip = CreateSpriteClip("NPCFarmer_Celebrate", CreateCelebrateFrames(spritesByDirection), actionFrameRate, false);

            AnimatorController controller = CreateAnimatorController(
                idleClip,
                walkRightClip,
                walkLeftClip,
                walkDownClip,
                walkUpClip,
                waterClip,
                celebrateClip);

            GameObject npc = CreateSceneNPC(texture, spritesByDirection["Down"][0], controller);
            Selection.activeGameObject = npc;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("NPC Farmer Setup Complete", "Đã tạo NPC_Farmer và asset animation tại:\n" + GeneratedFolder, "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("NPC Farmer Setup Failed", exception.Message, "OK");
        }
    }

    private void SliceSpriteSheet(string texturePath, Texture2D texture)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Asset không phải TextureImporter: " + texturePath);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        int frameWidth = texture.width / Columns;
        int frameHeight = texture.height / Rows;

        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new InvalidOperationException("Kích thước sprite sheet không hợp lệ.");
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();

        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        Dictionary<string, GUID> existingIdsByName = dataProvider.GetSpriteRects()
            .ToDictionary(spriteRect => spriteRect.name, spriteRect => spriteRect.spriteID, StringComparer.Ordinal);

        List<SpriteRect> spriteRects = new List<SpriteRect>();
        string[] directionNames = { "Right", "Left", "Down", "Up" };

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                string spriteName = $"Farmer_{directionNames[row]}_{column}";
                Rect rect = new Rect(
                    column * frameWidth,
                    texture.height - ((row + 1) * frameHeight),
                    frameWidth,
                    frameHeight);

                spriteRects.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = rect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = existingIdsByName.TryGetValue(spriteName, out GUID existingId) ? existingId : GUID.Generate()
                });
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            SpriteNameFileIdPair[] pairs = spriteRects
                .Select(spriteRect => new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID))
                .ToArray();
            nameFileIdProvider.SetNameFileIdPairs(pairs);
        }

        dataProvider.Apply();
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
    }

    private Dictionary<string, Sprite[]> LoadDirectionalSprites(string texturePath)
    {
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();

        Dictionary<string, Sprite[]> result = new Dictionary<string, Sprite[]>();
        foreach (string direction in new[] { "Right", "Left", "Down", "Up" })
        {
            Sprite[] directionSprites = allSprites
                .Where(sprite => sprite.name.StartsWith("Farmer_" + direction + "_", StringComparison.Ordinal))
                .OrderBy(sprite => ExtractFrameIndex(sprite.name))
                .ToArray();

            if (directionSprites.Length != Columns)
            {
                throw new InvalidOperationException($"Hướng {direction} cần {Columns} frame nhưng tìm thấy {directionSprites.Length}.");
            }

            result[direction] = directionSprites;
        }

        return result;
    }

    private AnimationClip CreateSpriteClip(string clipName, IReadOnlyList<Sprite> frames, float frameRate, bool loop)
    {
        if (frames == null || frames.Count == 0)
        {
            throw new InvalidOperationException("Clip " + clipName + " không có frame nào.");
        }

        string clipPath = $"{GeneratedFolder}/{clipName}.anim";
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            frameRate = frameRate
        };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private AnimatorController CreateAnimatorController(
        AnimationClip idleClip,
        AnimationClip walkRightClip,
        AnimationClip walkLeftClip,
        AnimationClip walkDownClip,
        AnimationClip walkUpClip,
        AnimationClip waterClip,
        AnimationClip celebrateClip)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(DirectionX, AnimatorControllerParameterType.Float);
        controller.AddParameter(DirectionY, AnimatorControllerParameterType.Float);
        controller.AddParameter(Speed, AnimatorControllerParameterType.Float);
        controller.AddParameter(Water, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(Celebrate, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.name = "NPC Farmer Locomotion";

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(250f, 120f, 0f));
        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;

        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(250f, 260f, 0f));
        BlendTree walkBlendTree = new BlendTree
        {
            name = "Walk_4Direction_BlendTree",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = DirectionX,
            blendParameterY = DirectionY,
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(walkBlendTree, ControllerPath);
        walkBlendTree.AddChild(walkRightClip, new Vector2(1f, 0f));
        walkBlendTree.AddChild(walkLeftClip, new Vector2(-1f, 0f));
        walkBlendTree.AddChild(walkDownClip, new Vector2(0f, -1f));
        walkBlendTree.AddChild(walkUpClip, new Vector2(0f, 1f));
        walkState.motion = walkBlendTree;

        AnimatorState waterState = stateMachine.AddState("WaterPlants", new Vector3(560f, 120f, 0f));
        waterState.motion = waterClip;

        AnimatorState celebrateState = stateMachine.AddState("Celebrate", new Vector3(560f, 260f, 0f));
        celebrateState.motion = celebrateClip;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.05f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, Speed);

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.05f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, Speed);

        AnimatorStateTransition anyToWater = stateMachine.AddAnyStateTransition(waterState);
        anyToWater.hasExitTime = false;
        anyToWater.duration = 0.05f;
        anyToWater.canTransitionToSelf = false;
        anyToWater.AddCondition(AnimatorConditionMode.If, 0f, Water);

        AnimatorStateTransition waterToIdle = waterState.AddTransition(idleState);
        waterToIdle.hasExitTime = true;
        waterToIdle.exitTime = 0.95f;
        waterToIdle.duration = 0.05f;

        AnimatorStateTransition anyToCelebrate = stateMachine.AddAnyStateTransition(celebrateState);
        anyToCelebrate.hasExitTime = false;
        anyToCelebrate.duration = 0.05f;
        anyToCelebrate.canTransitionToSelf = false;
        anyToCelebrate.AddCondition(AnimatorConditionMode.If, 0f, Celebrate);

        AnimatorStateTransition celebrateToIdle = celebrateState.AddTransition(idleState);
        celebrateToIdle.hasExitTime = true;
        celebrateToIdle.exitTime = 0.95f;
        celebrateToIdle.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private GameObject CreateSceneNPC(Texture2D texture, Sprite defaultSprite, AnimatorController controller)
    {
        GameObject npc = new GameObject("NPC_Farmer");
        Undo.RegisterCreatedObjectUndo(npc, "Create NPC_Farmer");

        SpriteRenderer spriteRenderer = npc.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.sortingOrder = SortingOrder;

        Animator animator = npc.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        NPCFarmController npcController = npc.AddComponent<NPCFarmController>();
        npcController.waypoints = CreateAnchors(npc.transform);

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.MoveGameObjectToScene(npc, activeScene);
        }

        npc.transform.position = Vector3.zero;
        return npc;
    }

    private Transform[] CreateAnchors(Transform npcRoot)
    {
        GameObject anchorsRoot = new GameObject("Anchors");
        anchorsRoot.transform.SetParent(npcRoot);
        anchorsRoot.transform.localPosition = Vector3.zero;
        anchorsRoot.transform.localRotation = Quaternion.identity;
        anchorsRoot.transform.localScale = Vector3.one;

        Vector3[] localPositions =
        {
            new Vector3(waypointRadiusX, 0f, 0f),
            new Vector3(0f, waypointRadiusY, 0f),
            new Vector3(-waypointRadiusX, 0f, 0f),
            new Vector3(0f, -waypointRadiusY, 0f)
        };

        Transform[] anchors = new Transform[localPositions.Length];
        for (int i = 0; i < localPositions.Length; i++)
        {
            GameObject anchor = new GameObject($"Anchor_{i + 1:00}");
            anchor.transform.SetParent(anchorsRoot.transform);
            anchor.transform.localPosition = localPositions[i];
            anchor.transform.localRotation = Quaternion.identity;
            anchor.transform.localScale = Vector3.one;
            anchors[i] = anchor.transform;
        }

        return anchors;
    }

    private static Sprite[] CreateCelebrateFrames(Dictionary<string, Sprite[]> spritesByDirection)
    {
        List<Sprite> frames = new List<Sprite>();
        frames.AddRange(spritesByDirection["Down"]);
        frames.AddRange(spritesByDirection["Up"].Reverse());
        return frames.ToArray();
    }

    private static string FindSpriteSheetAssetPath()
    {
        if (!Directory.Exists(AbsoluteSpriteFolder))
        {
            throw new DirectoryNotFoundException("Không tìm thấy thư mục sprite sheet bắt buộc: " + AbsoluteSpriteFolder);
        }

        string[] candidates = Directory.GetFiles(AbsoluteSpriteFolder)
            .Where(path =>
            {
                string extension = Path.GetExtension(path);
                return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".psd", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".psb", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException("Không có file sprite sheet .png/.psd/.psb trong: " + AbsoluteSpriteFolder);
        }

        string normalizedDataPath = Application.dataPath.Replace('\\', '/');
        string normalizedCandidate = candidates[0].Replace('\\', '/');

        if (normalizedCandidate.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets" + normalizedCandidate.Substring(normalizedDataPath.Length);
        }

        return ProjectSpriteFolder + "/" + Path.GetFileName(candidates[0]);
    }

    private static void EnsureGeneratedFolder()
    {
        EnsureFolder("Assets/N\u00f4ng d\u00e2n");
        EnsureFolder(ProjectSpriteFolder);
        EnsureFolder("Assets/N\u00f4ng d\u00e2n/NV_01/Generated");
        EnsureFolder(GeneratedFolder);
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        string[] parts = normalized.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static int ExtractFrameIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        if (underscore < 0 || underscore >= spriteName.Length - 1)
        {
            return 0;
        }

        return int.TryParse(spriteName.Substring(underscore + 1), out int index) ? index : 0;
    }
}
