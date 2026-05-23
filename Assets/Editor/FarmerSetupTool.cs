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
/// Tool tao nhanh nhan vat Farmer_Player, cat sprite sheet 4x6 va tao Animator.
/// Dat file nay trong thu muc Editor de khong bi dua vao ban build game.
/// </summary>
public class FarmerSetupTool : EditorWindow
{
    private const string AbsoluteSpriteFolder = "E:\\game\\My project\\Assets\\N\u00f4ng d\u00e2n\\NV_01";
    private const string ProjectSpriteFolder = "Assets/N\u00f4ng d\u00e2n/NV_01";
    private const string GeneratedFolder = "Assets/N\u00f4ng d\u00e2n/NV_01/Generated";
    private const string ControllerPath = GeneratedFolder + "/Farmer_Player.controller";

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

    [MenuItem("Tools/Setup Farmer Character")]
    public static void ShowWindow()
    {
        FarmerSetupTool window = GetWindow<FarmerSetupTool>("Farmer Setup");
        window.minSize = new Vector2(420f, 230f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("One-click Farmer Character Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.HelpBox(
            "Tool nay doc sprite sheet trong dung thu muc:\n" + AbsoluteSpriteFolder +
            "\n\nNhan nut ben duoi de cat 24 frame, tao animation, tao Animator Controller va tao GameObject Farmer_Player trong scene hien tai.",
            MessageType.Info);

        pixelsPerUnit = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit", "Tang/giam de doi kich thuoc nhan vat trong world."), pixelsPerUnit);
        walkFrameRate = EditorGUILayout.FloatField(new GUIContent("Walk FPS", "Toc do animation di bo."), walkFrameRate);
        actionFrameRate = EditorGUILayout.FloatField(new GUIContent("Action FPS", "Toc do animation tuoi cay/nhay an mung."), actionFrameRate);

        pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        walkFrameRate = Mathf.Max(1f, walkFrameRate);
        actionFrameRate = Mathf.Max(1f, actionFrameRate);

        EditorGUILayout.Space(10f);

        if (GUILayout.Button("Setup Farmer Character", GUILayout.Height(42f)))
        {
            SetupFarmerCharacter();
        }
    }

    private void SetupFarmerCharacter()
    {
        try
        {
            EnsureGeneratedFolder();

            string texturePath = FindSpriteSheetAssetPath();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Khong load duoc Texture2D tai: " + texturePath);
            }

            SliceSpriteSheet(texturePath, texture);

            Dictionary<string, Sprite[]> spritesByDirection = LoadDirectionalSprites(texturePath);
            AnimationClip idleClip = CreateSpriteClip("Farmer_Idle", new[] { spritesByDirection["Down"][0] }, 1f, true);
            AnimationClip walkRightClip = CreateSpriteClip("Farmer_Walk_Right", spritesByDirection["Right"], walkFrameRate, true);
            AnimationClip walkLeftClip = CreateSpriteClip("Farmer_Walk_Left", spritesByDirection["Left"], walkFrameRate, true);
            AnimationClip walkDownClip = CreateSpriteClip("Farmer_Walk_Down", spritesByDirection["Down"], walkFrameRate, true);
            AnimationClip walkUpClip = CreateSpriteClip("Farmer_Walk_Up", spritesByDirection["Up"], walkFrameRate, true);

            // Sprite sheet hien tai chi co 4 huong di bo, nen 2 clip hanh dong duoc tao tu cac frame san co.
            // Sau nay co sheet action rieng, ban chi can thay clip trong Animator ma khong can sua runtime script.
            AnimationClip waterClip = CreateSpriteClip("Farmer_WaterPlants", spritesByDirection["Down"], actionFrameRate, false);
            AnimationClip celebrateClip = CreateSpriteClip("Farmer_Celebrate", CreateCelebrateFrames(spritesByDirection), actionFrameRate, false);

            AnimatorController controller = CreateAnimatorController(
                idleClip,
                walkRightClip,
                walkLeftClip,
                walkDownClip,
                walkUpClip,
                waterClip,
                celebrateClip);

            GameObject player = CreateSceneFarmer(texture, spritesByDirection["Down"][0], controller);
            Selection.activeGameObject = player;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Farmer Setup Complete", "Da tao Farmer_Player va cac asset animation trong:\n" + GeneratedFolder, "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Farmer Setup Failed", exception.Message, "OK");
        }
    }

    private void SliceSpriteSheet(string texturePath, Texture2D texture)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Asset khong phai TextureImporter: " + texturePath);
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
            throw new InvalidOperationException("Kich thuoc sprite sheet khong hop le.");
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();

        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        List<SpriteRect> spriteRects = new List<SpriteRect>();
        string[] directionNames = { "Right", "Left", "Down", "Up" };

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                Rect rect = new Rect(
                    column * frameWidth,
                    texture.height - ((row + 1) * frameHeight),
                    frameWidth,
                    frameHeight);

                spriteRects.Add(new SpriteRect
                {
                    name = $"Farmer_{directionNames[row]}_{column}",
                    rect = rect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
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
                throw new InvalidOperationException($"Huong {direction} can {Columns} frame nhung tim thay {directionSprites.Length}.");
            }

            result[direction] = directionSprites;
        }

        return result;
    }

    private AnimationClip CreateSpriteClip(string clipName, IReadOnlyList<Sprite> frames, float frameRate, bool loop)
    {
        if (frames == null || frames.Count == 0)
        {
            throw new InvalidOperationException("Clip " + clipName + " khong co frame nao.");
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
        stateMachine.name = "Farmer Locomotion";

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

    private GameObject CreateSceneFarmer(Texture2D texture, Sprite defaultSprite, AnimatorController controller)
    {
        GameObject player = new GameObject("Farmer_Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Farmer_Player");

        SpriteRenderer spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.sortingOrder = SortingOrder;

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        Vector2 spriteSize = defaultSprite != null ? defaultSprite.bounds.size : new Vector2(texture.width / pixelsPerUnit, texture.height / pixelsPerUnit);
        collider.size = new Vector2(spriteSize.x * 0.45f, spriteSize.y * 0.28f);
        collider.offset = new Vector2(0f, -spriteSize.y * 0.28f);

        player.AddComponent<FarmerActionController>();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.MoveGameObjectToScene(player, activeScene);
        }

        player.transform.position = Vector3.zero;
        return player;
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
            throw new DirectoryNotFoundException("Khong tim thay thu muc sprite sheet bat buoc: " + AbsoluteSpriteFolder);
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
            throw new FileNotFoundException("Khong co file sprite sheet .png/.psd/.psb trong: " + AbsoluteSpriteFolder);
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
