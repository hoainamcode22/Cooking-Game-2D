#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

public static class FarmerManualRigHelper
{
    private const string PsbPath = "Assets/N\u00f4ng d\u00e2n/Chibi_Farmer_Watering_Rig_Package.psb";
    private const string PrefabDir = "Assets/N\u00f4ng d\u00e2n/Prefabs";
    private const string AnimDir = "Assets/N\u00f4ng d\u00e2n/Animations";
    private const string BackupDir = "Assets/_Game/Farm/Scripts/Editor/_Backup_FarmerRigOld";
    private const string ManualRootName = "Farmer_Manual_Final";

    private static readonly PartDef[] Parts =
    {
        new PartDef("05_Hair_Back", "SR_Hair_Back", -20, "B_Head"),
        new PartDef("04_Hat", "SR_Hat", 7, "B_Head"),
        new PartDef("06_Hair_Front", "SR_Hair_Front", 6, "B_Head"),
        new PartDef("07_Head", "SR_Head", 5, "B_Head"),
        new PartDef("08_Neckerchief", "SR_Neckerchief", 2, "B_Neck"),
        new PartDef("09_Body_Torso", "SR_Body_Torso", 0, "B_Body"),
        new PartDef("03_Belt_Pouch", "SR_Belt_Pouch", 1, "B_Body"),
        new PartDef("10_Left_Arm", "SR_Left_Arm", 4, "B_Left_Arm"),
        new PartDef("11_Right_Arm_Holding_Can", "SR_Right_Arm_Holding_Can", 3, "B_Right_Arm"),
        new PartDef("12_Left_Leg", "SR_Left_Leg", -10, "B_Left_Leg"),
        new PartDef("13_Right_Leg", "SR_Right_Leg", -12, "B_Right_Leg"),
    };

    [MenuItem("Tools/Farm/Farmer Rig/Create Manual Farmer Rig Helper")]
    public static void CreateManualFarmerRigHelper()
    {
        List<string> backedUp = BackupOldAutoTools();
        string psbPath = ResolvePsbPath();
        var errors = new List<string>();
        var failureReasons = new List<string>();
        List<string> spriteNames = new List<string>();
        List<string> matchedSprites = new List<string>();
        List<string> missingSprites = new List<string>();
        int totalSprites = 0;
        string created = "NO";
        string rootObject = "NONE";

        try
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(AnimDir);

            Dictionary<string, Sprite> sprites = LoadSprites(psbPath, errors, spriteNames);
            totalSprites = sprites.Count;
            Dictionary<string, Sprite> resolvedSprites = ResolveSpritesForManualLayout(sprites.Values, matchedSprites, missingSprites);
            if (errors.Count > 0)
                failureReasons.AddRange(errors);
            if (missingSprites.Count > 0)
                failureReasons.Add("Missing sprites will be created as empty MISSING_SR_* placeholders.");

            GameObject existing = GameObject.Find(ManualRootName);
            if (existing != null)
            {
                Debug.LogWarning($"[FarmerManualRigHelper] Existing '{ManualRootName}' found in scene. Replacing it.");
                UnityEngine.Object.DestroyImmediate(existing);
            }

            string sortingLayer = FindBestSortingLayer();
            var root = new GameObject(ManualRootName);
            rootObject = root.name;

            var sg = root.AddComponent<SortingGroup>();
            sg.sortingLayerName = sortingLayer;

            Transform referenceRoot = MakeChild("Reference", root.transform);
            Transform bonesRoot = MakeChild("Bones", root.transform);
            Transform partsRoot = MakeChild("Parts", root.transform);

            CreateReference(referenceRoot, GetResolvedSprite(resolvedSprites, "01_Full_Character_Preview"), sortingLayer);
            CreateEditableBones(bonesRoot);
            CreateLooseParts(partsRoot, resolvedSprites, sortingLayer, missingSprites);

            SafeSelectPingAndFrame(root);
            created = "YES";

            PrintCreateReport(psbPath, totalSprites, spriteNames, matchedSprites, missingSprites, created, rootObject, failureReasons, errors);
            PrintReport(backedUp, "YES", root.name, "YES", "NONE", "NONE", "NONE", errors);
        }
        catch (Exception ex)
        {
            string detail = ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace;
            failureReasons.Add(detail);
            errors.Add(detail);
            Debug.LogWarning("[FarmerManualRigHelper] Exception while creating manual helper:\n" + detail);
            PrintCreateReport(psbPath, totalSprites, spriteNames, matchedSprites, missingSprites, created, rootObject, failureReasons, errors);
            PrintReport(backedUp, "NO", "FAILED", "YES", "NONE", "NONE", "NONE", errors);
        }
    }

    [MenuItem("Tools/Farm/Farmer Rig/Bake Manual Farmer Rig")]
    public static void BakeManualFarmerRig()
    {
        List<string> backedUp = BackupOldAutoTools();
        EnsureFolder(PrefabDir);
        EnsureFolder(AnimDir);

        GameObject sourceRoot = GameObject.Find(ManualRootName);
        if (sourceRoot == null)
        {
            PrintReport(backedUp, "NO", "NOT FOUND", "YES", "NONE", "NONE", "NONE",
                new[] { "Scene object Farmer_Manual_Final not found. Run Create Manual Farmer Rig Helper first." });
            return;
        }

        Transform sourceBonesRoot = sourceRoot.transform.Find("Bones");
        Transform sourcePartsRoot = sourceRoot.transform.Find("Parts");
        if (sourceBonesRoot == null || sourcePartsRoot == null)
        {
            PrintReport(backedUp, "NO", sourceRoot.name, "YES", "NONE", "NONE", "NONE",
                new[] { "Farmer_Manual_Final must contain Bones and Parts children." });
            return;
        }

        var errors = new List<string>();
        Dictionary<string, Transform> sourceBones = CollectRecursive(sourceBonesRoot);
        Dictionary<string, Transform> sourceParts = CollectDirect(sourcePartsRoot);

        foreach (string boneName in RequiredBoneNames())
        {
            if (!sourceBones.ContainsKey(boneName))
                errors.Add("Missing bone: " + boneName);
        }

        foreach (PartDef part in Parts)
        {
            if (!sourceParts.TryGetValue(part.ObjectName, out Transform partTransform))
            {
                errors.Add("Missing part: " + part.ObjectName);
                continue;
            }

            SpriteRenderer sr = partTransform.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
                errors.Add("Missing SpriteRenderer/sprite on part: " + part.ObjectName);
        }

        if (sourceParts.ContainsKey("SR_Accessory_Watering_Can"))
            errors.Add("Duplicate watering can detected: SR_Accessory_Watering_Can must not be used.");

        if (errors.Count > 0)
        {
            PrintReport(backedUp, "NO", sourceRoot.name, "YES", "NONE", "NONE", "NONE", errors);
            return;
        }

        string prefabPath = $"{PrefabDir}/Farmer_Manual_Final.prefab";
        string controllerPath = $"{AnimDir}/Farmer_Manual.controller";
        string idlePath = $"{AnimDir}/Farmer_Manual_Idle.anim";
        string walkPath = $"{AnimDir}/Farmer_Manual_Walk.anim";
        string wateringPath = $"{AnimDir}/Farmer_Manual_Watering.anim";

        DeleteAssetIfExists(prefabPath);
        string sortingLayer = FindBestSortingLayer();
        var prefabRoot = new GameObject(ManualRootName);
        int srCount = 0;

        try
        {
            var sg = prefabRoot.AddComponent<SortingGroup>();
            sg.sortingLayerName = sortingLayer;
            prefabRoot.AddComponent<Animator>();

            Transform prefabBonesRoot = MakeChild("Bones", prefabRoot.transform);
            Transform prefabPartsRoot = MakeChild("Parts", prefabRoot.transform);
            Transform prefabReferenceRoot = MakeChild("Reference", prefabRoot.transform);
            prefabReferenceRoot.gameObject.SetActive(false);

            Dictionary<string, Transform> bakedBones = CloneBoneTree(sourceBonesRoot, prefabBonesRoot);
            CopyReference(sourceRoot.transform.Find("Reference"), prefabReferenceRoot, sortingLayer);

            foreach (PartDef part in Parts)
            {
                Transform sourcePart = sourceParts[part.ObjectName];
                SpriteRenderer sourceSr = sourcePart.GetComponent<SpriteRenderer>();
                GameObject clone = CloneSpriteObject(sourcePart, sourceSr, prefabPartsRoot, sortingLayer);
                srCount++;

                Transform targetBone = bakedBones[part.BoneName];
                Vector3 worldBefore = clone.transform.position;
                clone.transform.SetParent(targetBone, true);
                Debug.Log($"[FarmerManualRigBake] {part.ObjectName} -> {part.BoneName}, world=({worldBefore.x:F3},{worldBefore.y:F3}), local=({clone.transform.localPosition.x:F3},{clone.transform.localPosition.y:F3})");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool ok);
            if (!ok)
                errors.Add("Failed to save prefab: " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefabRoot);
        }

        if (errors.Count == 0)
        {
            SaveOrReplaceAsset(CreateIdleClip(), idlePath);
            SaveOrReplaceAsset(CreateWalkClip(), walkPath);
            SaveOrReplaceAsset(CreateWateringClip(), wateringPath);

            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            AnimationClip walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkPath);
            AnimationClip watering = AssetDatabase.LoadAssetAtPath<AnimationClip>(wateringPath);
            AnimatorController controller = BuildController(controllerPath, idle, walk, watering);
            AssignControllerToPrefab(prefabPath, controller);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string anims = string.Join(", ", new[] { idlePath, walkPath, wateringPath });
        PrintReport(backedUp, "NO", sourceRoot.name, "YES",
            errors.Count == 0 ? $"{prefabPath} (SpriteRenderers: {srCount})" : "FAILED",
            errors.Count == 0 ? anims : "FAILED",
            errors.Count == 0 ? controllerPath : "FAILED",
            errors);
    }

    private static void CreateReference(Transform parent, Sprite preview, string sortingLayer)
    {
        var go = new GameObject("SR_Full_Character_Preview");
        go.transform.SetParent(parent, false);
        if (preview != null)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = preview;
            sr.color = new Color(1f, 1f, 1f, 0.35f);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = -1000;
            ApplySafeSpriteMaterial(sr);
        }
        else
        {
            go.name = "MISSING_SR_Full_Character_Preview";
        }
    }

    private static void CreateLooseParts(Transform parent, Dictionary<string, Sprite> sprites, string sortingLayer, List<string> missingSprites)
    {
        for (int i = 0; i < Parts.Length; i++)
        {
            PartDef part = Parts[i];
            Sprite sprite = GetResolvedSprite(sprites, part.SpriteName);
            bool missing = sprite == null;
            var go = new GameObject(missing ? "MISSING_" + part.ObjectName : part.ObjectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(3.2f + (i % 3) * 1.0f, 2.2f - (i / 3) * 0.9f, 0f);

            if (missing)
                continue;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = part.SortOrder;
            sr.maskInteraction = SpriteMaskInteraction.None;
            ApplySafeSpriteMaterial(sr);
        }
    }

    private static void CreateEditableBones(Transform bonesRoot)
    {
        Transform root = CreateBone("B_Root", bonesRoot, new Vector3(0f, -1.15f, 0f));
        Transform pelvis = CreateBone("B_Pelvis", root, new Vector3(0f, -1.15f, 0f));
        Transform body = CreateBone("B_Body", pelvis, new Vector3(0f, -0.25f, 0f));
        Transform neck = CreateBone("B_Neck", body, new Vector3(0f, 0.45f, 0f));
        CreateBone("B_Head", neck, new Vector3(0f, 0.78f, 0f));
        CreateBone("B_Left_Arm", body, new Vector3(-0.55f, 0.2f, 0f));
        CreateBone("B_Right_Arm", body, new Vector3(0.55f, 0.2f, 0f));
        CreateBone("B_Left_Leg", pelvis, new Vector3(-0.25f, -1.45f, 0f));
        CreateBone("B_Right_Leg", pelvis, new Vector3(0.25f, -1.45f, 0f));
    }

    private static Transform CreateBone(string name, Transform parent, Vector3 worldPosition)
    {
        var go = new GameObject(name);
        go.transform.position = worldPosition;
        go.transform.SetParent(parent, true);
        return go.transform;
    }

    private static Dictionary<string, Transform> CloneBoneTree(Transform sourceBonesRoot, Transform prefabBonesRoot)
    {
        Transform sourceRootBone = sourceBonesRoot.Find("B_Root");
        var baked = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        CloneBoneRecursive(sourceRootBone, prefabBonesRoot, baked);
        return baked;
    }

    private static Transform CloneBoneRecursive(Transform source, Transform parent, Dictionary<string, Transform> baked)
    {
        var go = new GameObject(source.name);
        go.transform.position = source.position;
        go.transform.rotation = source.rotation;
        go.transform.localScale = source.lossyScale;
        go.transform.SetParent(parent, true);
        baked[source.name] = go.transform;

        foreach (Transform child in source)
            CloneBoneRecursive(child, go.transform, baked);

        return go.transform;
    }

    private static GameObject CloneSpriteObject(Transform source, SpriteRenderer sourceSr, Transform parent, string sortingLayer)
    {
        var go = new GameObject(source.name);
        go.transform.position = source.position;
        go.transform.rotation = source.rotation;
        go.transform.localScale = source.lossyScale;
        go.transform.SetParent(parent, true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sourceSr.sprite;
        sr.color = sourceSr.color;
        sr.flipX = sourceSr.flipX;
        sr.flipY = sourceSr.flipY;
        sr.sortingLayerName = string.IsNullOrEmpty(sourceSr.sortingLayerName) ? sortingLayer : sourceSr.sortingLayerName;
        sr.sortingOrder = sourceSr.sortingOrder;
        sr.maskInteraction = SpriteMaskInteraction.None;
        ApplySafeSpriteMaterial(sr);
        return go;
    }

    private static void CopyReference(Transform sourceReference, Transform targetReference, string sortingLayer)
    {
        if (sourceReference == null)
            return;

        foreach (Transform child in sourceReference)
        {
            SpriteRenderer sourceSr = child.GetComponent<SpriteRenderer>();
            if (sourceSr == null || sourceSr.sprite == null)
                continue;

            GameObject clone = CloneSpriteObject(child, sourceSr, targetReference, sortingLayer);
            clone.SetActive(child.gameObject.activeSelf);
        }
    }

    private static AnimationClip CreateIdleClip()
    {
        AnimationClip clip = NewClip("Farmer_Manual_Idle", true);
        SetCurve(clip, BonePath("B_Body"), "localPosition.y", (0f, 0f), (0.5f, 0.035f), (1f, 0f));
        SetCurve(clip, BonePath("B_Head"), "localEulerAngles.z", (0f, -2f), (0.5f, 2f), (1f, -2f));
        return clip;
    }

    private static AnimationClip CreateWalkClip()
    {
        AnimationClip clip = NewClip("Farmer_Manual_Walk", true);
        SetCurve(clip, BonePath("B_Left_Leg"), "localEulerAngles.z", (0f, -10f), (0.4f, 10f), (0.8f, -10f));
        SetCurve(clip, BonePath("B_Right_Leg"), "localEulerAngles.z", (0f, 10f), (0.4f, -10f), (0.8f, 10f));
        SetCurve(clip, BonePath("B_Left_Arm"), "localEulerAngles.z", (0f, 8f), (0.4f, -8f), (0.8f, 8f));
        SetCurve(clip, BonePath("B_Right_Arm"), "localEulerAngles.z", (0f, -8f), (0.4f, 8f), (0.8f, -8f));
        SetCurve(clip, BonePath("B_Body"), "localPosition.y", (0f, 0f), (0.2f, 0.025f), (0.4f, 0f), (0.6f, 0.025f), (0.8f, 0f));
        return clip;
    }

    private static AnimationClip CreateWateringClip()
    {
        AnimationClip clip = NewClip("Farmer_Manual_Watering", true);
        SetCurve(clip, BonePath("B_Right_Arm"), "localEulerAngles.z", (0f, 0f), (0.45f, -28f), (0.9f, -20f), (1.2f, 0f));
        SetCurve(clip, BonePath("B_Body"), "localEulerAngles.z", (0f, 0f), (0.45f, 3f), (0.9f, 3f), (1.2f, 0f));
        return clip;
    }

    private static AnimationClip NewClip(string name, bool loop)
    {
        AnimationClip clip = new AnimationClip { name = name, wrapMode = loop ? WrapMode.Loop : WrapMode.Once };
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = loop });
        return clip;
    }

    private static void SetCurve(AnimationClip clip, string path, string property, params (float time, float value)[] keys)
    {
        AnimationCurve curve = new AnimationCurve();
        foreach ((float time, float value) key in keys)
            curve.AddKey(new Keyframe(key.time, key.value));

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }

        clip.SetCurve(path, typeof(Transform), property, curve);
    }

    private static AnimatorController BuildController(string path, AnimationClip idle, AnimationClip walk, AnimationClip watering)
    {
        DeleteAssetIfExists(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState("Idle", new Vector3(250, 0));
        AnimatorState walkState = sm.AddState("Walk", new Vector3(250, 70));
        AnimatorState wateringState = sm.AddState("Watering", new Vector3(250, 140));
        idleState.motion = idle;
        walkState.motion = walk;
        wateringState.motion = watering;
        sm.defaultState = idleState;

        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsWatering", AnimatorControllerParameterType.Bool);

        AddBoolTransition(idleState, walkState, "IsWalking", true);
        AddBoolTransition(walkState, idleState, "IsWalking", false);
        AddBoolTransition(idleState, wateringState, "IsWatering", true);
        AddBoolTransition(wateringState, idleState, "IsWatering", false);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AssignControllerToPrefab(string prefabPath, RuntimeAnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string BonePath(string boneName)
    {
        switch (boneName)
        {
            case "B_Root": return "Bones/B_Root";
            case "B_Pelvis": return "Bones/B_Root/B_Pelvis";
            case "B_Body": return "Bones/B_Root/B_Pelvis/B_Body";
            case "B_Neck": return "Bones/B_Root/B_Pelvis/B_Body/B_Neck";
            case "B_Head": return "Bones/B_Root/B_Pelvis/B_Body/B_Neck/B_Head";
            case "B_Left_Arm": return "Bones/B_Root/B_Pelvis/B_Body/B_Left_Arm";
            case "B_Right_Arm": return "Bones/B_Root/B_Pelvis/B_Body/B_Right_Arm";
            case "B_Left_Leg": return "Bones/B_Root/B_Pelvis/B_Left_Leg";
            case "B_Right_Leg": return "Bones/B_Root/B_Pelvis/B_Right_Leg";
            default: return "Bones/B_Root";
        }
    }

    private static Dictionary<string, Sprite> LoadSprites(string psbPath, List<string> errors, List<string> spriteNames)
    {
        var sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(psbPath))
        {
            errors.Add("PSB not found. Expected path: " + PsbPath);
            return sprites;
        }

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(psbPath) == null)
        {
            errors.Add("AssetDatabase cannot load PSB at path: " + psbPath);
            return sprites;
        }

        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(psbPath))
        {
            Sprite sprite = obj as Sprite;
            if (sprite != null && !sprites.ContainsKey(sprite.name))
                sprites.Add(sprite.name, sprite);
        }

        spriteNames.AddRange(sprites.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

        if (sprites.Count == 0)
        {
            errors.Add("PSB loaded but no Sprite assets were found. Check PSD Importer / PSB import settings.");
            return sprites;
        }

        return sprites;
    }

    private static Dictionary<string, Sprite> ResolveSpritesForManualLayout(IEnumerable<Sprite> sourceSprites, List<string> matchedSprites, List<string> missingSprites)
    {
        var resolved = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        List<Sprite> allSprites = sourceSprites == null
            ? new List<Sprite>()
            : sourceSprites.Where(s => s != null).ToList();

        ResolveOne("01_Full_Character_Preview", "SR_Full_Character_Preview", allSprites, resolved, matchedSprites, missingSprites);

        foreach (PartDef part in Parts)
            ResolveOne(part.SpriteName, part.ObjectName, allSprites, resolved, matchedSprites, missingSprites);

        return resolved;
    }

    private static void ResolveOne(string wantedSpriteName, string objectName, List<Sprite> allSprites, Dictionary<string, Sprite> resolved, List<string> matchedSprites, List<string> missingSprites)
    {
        Sprite sprite = FindSpriteFlexible(allSprites, wantedSpriteName);
        if (sprite != null)
        {
            resolved[wantedSpriteName] = sprite;
            matchedSprites.Add($"{objectName} <- {sprite.name}");
        }
        else
        {
            resolved[wantedSpriteName] = null;
            missingSprites.Add($"{objectName} expected '{wantedSpriteName}'");
        }
    }

    private static Sprite FindSpriteFlexible(IEnumerable<Sprite> sprites, string wantedName)
    {
        if (sprites == null)
            return null;

        string wanted = NormalizeSpriteName(wantedName);
        string wantedNoNumber = StripLeadingNumber(wanted);
        foreach (Sprite sprite in sprites)
            if (string.Equals(sprite.name, wantedName, StringComparison.OrdinalIgnoreCase))
                return sprite;

        foreach (Sprite sprite in sprites)
        {
            string candidate = NormalizeSpriteName(sprite.name);
            if (candidate == wanted)
                return sprite;
        }

        foreach (Sprite sprite in sprites)
        {
            string candidate = NormalizeSpriteName(sprite.name);
            string candidateNoNumber = StripLeadingNumber(candidate);
            if (candidateNoNumber == wantedNoNumber ||
                candidate.Contains(wantedNoNumber) ||
                wantedNoNumber.Contains(candidateNoNumber))
                return sprite;
        }

        return null;
    }

    private static string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (char c in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static string StripLeadingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        int i = 0;
        while (i < value.Length && char.IsDigit(value[i]))
            i++;
        return value.Substring(i);
    }

    private static Sprite GetResolvedSprite(Dictionary<string, Sprite> sprites, string spriteName)
    {
        if (sprites != null && sprites.TryGetValue(spriteName, out Sprite sprite))
            return sprite;
        return null;
    }

    private static string ResolvePsbPath()
    {
        if (File.Exists(PsbPath))
            return PsbPath;

        foreach (string guid in AssetDatabase.FindAssets("Chibi_Farmer_Watering_Rig_Package"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".psb", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    private static IEnumerable<string> RequiredBoneNames()
    {
        yield return "B_Root";
        yield return "B_Pelvis";
        yield return "B_Body";
        yield return "B_Neck";
        yield return "B_Head";
        yield return "B_Left_Arm";
        yield return "B_Right_Arm";
        yield return "B_Left_Leg";
        yield return "B_Right_Leg";
    }

    private static Dictionary<string, Transform> CollectDirect(Transform parent)
    {
        var dict = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        foreach (Transform child in parent)
            dict[child.name] = child;
        return dict;
    }

    private static Dictionary<string, Transform> CollectRecursive(Transform parent)
    {
        var dict = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        foreach (Transform child in parent)
        {
            dict[child.name] = child;
            foreach (KeyValuePair<string, Transform> kv in CollectRecursive(child))
                dict[kv.Key] = kv.Value;
        }
        return dict;
    }

    private static List<string> BackupOldAutoTools()
    {
        EnsureFolder(BackupDir);
        var moved = new List<string>();
        foreach (string path in OldAutoToolPaths())
        {
            if (!File.Exists(path))
                continue;

            string fileName = path.Replace("/", "__").Replace("\\", "__");
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                fileName += ".bak";

            string target = $"{BackupDir}/{fileName}";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
                target = $"{BackupDir}/{fileName}.{DateTime.Now:yyyyMMddHHmmss}";

            if (AssetDatabase.MoveAsset(path, target) == string.Empty)
                moved.Add($"{path} -> {target}");
        }

        return moved;
    }

    private static IEnumerable<string> OldAutoToolPaths()
    {
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerAutoRigFromPreviewBuilder.cs";
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerAutoRigFromPreviewBuilder.cs.meta";
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerCompleteRigBuilder.cs";
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerCompleteRigBuilder.cs.meta";
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerRigCleanBuilder.cs";
        yield return "Assets/_Game/Farm/Scripts/Editor/FarmerRigCleanBuilder.cs.meta";
        yield return "Assets/Editor/FarmerWateringRigBuilder.cs";
        yield return "Assets/Editor/FarmerWateringRigBuilder.cs.meta";
    }

    private static void PrintCreateReport(
        string psbPath,
        int totalSprites,
        IEnumerable<string> spriteNames,
        IEnumerable<string> matchedSprites,
        IEnumerable<string> missingSprites,
        string layoutCreated,
        string rootSceneObject,
        IEnumerable<string> failureReasons,
        IEnumerable<string> consoleErrors)
    {
        string[] names = spriteNames == null ? Array.Empty<string>() : spriteNames.ToArray();
        string[] matched = matchedSprites == null ? Array.Empty<string>() : matchedSprites.ToArray();
        string[] missing = missingSprites == null ? Array.Empty<string>() : missingSprites.ToArray();
        string[] failures = failureReasons == null ? Array.Empty<string>() : failureReasons.Where(e => !string.IsNullOrEmpty(e)).ToArray();
        string[] errors = consoleErrors == null ? Array.Empty<string>() : consoleErrors.Where(e => !string.IsNullOrEmpty(e)).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("[FARMER_MANUAL_RIG_HELPER_REPORT]");
        sb.AppendLine("PSB path: " + (string.IsNullOrEmpty(psbPath) ? "NOT FOUND" : psbPath));
        sb.AppendLine("Total sprites found: " + totalSprites);
        sb.AppendLine("Sprite names:");
        if (names.Length == 0)
            sb.AppendLine("  NONE");
        else
            foreach (string name in names)
                sb.AppendLine("  " + name);
        sb.AppendLine("Matched sprites:");
        if (matched.Length == 0)
            sb.AppendLine("  NONE");
        else
            foreach (string match in matched)
                sb.AppendLine("  " + match);
        sb.AppendLine("Missing sprites: " + (missing.Length == 0 ? "NONE" : string.Join(", ", missing)));
        sb.AppendLine("Manual layout object created: " + layoutCreated);
        sb.AppendLine("Root scene object: " + (string.IsNullOrEmpty(rootSceneObject) ? "NONE" : rootSceneObject));
        sb.AppendLine("Failure reason: " + (failures.Length == 0 ? "NONE" : string.Join(" | ", failures)));
        sb.AppendLine("Console errors: " + (errors.Length == 0 ? "NONE" : string.Join(" | ", errors)));
        Debug.Log(sb.ToString());
    }

    private static void PrintReport(
        List<string> backedUp,
        string helperCreated,
        string layoutObject,
        string bakeMenu,
        string prefabCreated,
        string animationsCreated,
        string controllerCreated,
        IEnumerable<string> errors)
    {
        string errorText = errors == null || !errors.Any() ? "NONE" : string.Join(" | ", errors);
        var sb = new StringBuilder();
        sb.AppendLine("[FARMER_MANUAL_RIG_HELPER_REPORT]");
        sb.AppendLine("Old auto tools backed up: " + (backedUp == null || backedUp.Count == 0 ? "already backed up or absent" : string.Join(" | ", backedUp)));
        sb.AppendLine("Manual helper created: " + helperCreated);
        sb.AppendLine("Manual layout object created: " + layoutObject);
        sb.AppendLine("Bake menu created: " + bakeMenu);
        sb.AppendLine("Prefab created: " + prefabCreated);
        sb.AppendLine("Animations created: " + animationsCreated);
        sb.AppendLine("Controller created: " + controllerCreated);
        sb.AppendLine("Console errors: " + errorText);
        Debug.Log(sb.ToString());
    }

    private static Transform MakeChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void ApplySafeSpriteMaterial(SpriteRenderer sr)
    {
        Material mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (mat != null)
            sr.sharedMaterial = mat;
    }

    private static void SafeSelectPingAndFrame(GameObject root)
    {
        if (root == null)
        {
            Debug.LogWarning("[FarmerManualRigHelper] Cannot select/ping/frame because root object is null.");
            return;
        }

        try
        {
            Selection.activeGameObject = root;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FarmerManualRigHelper] Could not select root object:\n" + ex);
        }

        try
        {
            EditorGUIUtility.PingObject(root);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FarmerManualRigHelper] Could not ping root object:\n" + ex);
        }

        try
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            sceneView.Frame(new Bounds(root.transform.position, Vector3.one * 5f), false);
            sceneView.Repaint();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FarmerManualRigHelper] Could not frame root object in Scene view:\n" + ex);
        }
    }

    private static string FindBestSortingLayer()
    {
        string[] preferred = { "Characters", "Character", "Player", "NPC" };
        foreach (string name in preferred)
            foreach (SortingLayer layer in SortingLayer.layers)
                if (layer.name == name)
                    return name;
        return "Default";
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void SaveOrReplaceAsset(UnityEngine.Object asset, string path)
    {
        DeleteAssetIfExists(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void DeleteAssetIfExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private readonly struct PartDef
    {
        public readonly string SpriteName;
        public readonly string ObjectName;
        public readonly int SortOrder;
        public readonly string BoneName;

        public PartDef(string spriteName, string objectName, int sortOrder, string boneName)
        {
            SpriteName = spriteName;
            ObjectName = objectName;
            SortOrder = sortOrder;
            BoneName = boneName;
        }
    }
}
#endif
