#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day02AutoSetup
{
    private const string SceneName = "PlatformMain";

    [MenuItem("Tools/Game Platform/Build DAY02 Scene")]
    public static void BuildDay02Scene()
    {
        string scenePath = FindPlatformMainScene();
        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog("DAY02 Setup", "PlatformMain 씬을 찾지 못했습니다.\nAssets/Scenes/PlatformMain.unity가 있는지 확인하세요.", "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        DestroyRootIfExists(scene, "GameSession");
        DestroyRootIfExists(scene, "StageMain");
        DestroyRootIfExists(scene, "Player");
        DestroyRootIfExists(scene, "Canvas");
        DestroyRootIfExists(scene, "EventSystem");

        Material playerMaterial = GetOrCreateMaterial("Assets/Materials/PlayerMaterial.mat", new Color(0.20f, 0.80f, 0.35f, 1f));
        Material coinMaterial = GetOrCreateMaterial("Assets/Materials/CoinMaterial.mat", new Color(1.00f, 0.66f, 0.10f, 1f));

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.5f, -7f);
        player.transform.rotation = Quaternion.identity;
        player.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        player.GetComponent<MeshRenderer>().sharedMaterial = playerMaterial;

        CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
        playerCollider.isTrigger = false;
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = true;

        GameObject stageMain = new GameObject("StageMain");
        stageMain.transform.position = Vector3.zero;
        stageMain.transform.rotation = Quaternion.identity;
        stageMain.transform.localScale = Vector3.one;

        CoinPickup[] coins = new CoinPickup[10];
        float[] xs = { -6f, -3f, 0f, 3f, 6f };

        for (int i = 0; i < 10; i++)
        {
            int row = i / 5;
            int col = i % 5;

            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = $"Coin{i + 1:00}";
            coin.transform.SetParent(stageMain.transform, false);
            coin.transform.localPosition = new Vector3(xs[col], 0f, row == 0 ? -3f : 3f);
            coin.transform.localRotation = Quaternion.identity;
            coin.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);
            coin.GetComponent<MeshRenderer>().sharedMaterial = coinMaterial;

            CapsuleCollider coinCollider = coin.GetComponent<CapsuleCollider>();
            coinCollider.center = Vector3.zero;
            coinCollider.radius = 0.5f;
            coinCollider.height = 2f;
            coinCollider.direction = 1;
            coinCollider.isTrigger = true;

            CoinPickup pickup = coin.AddComponent<CoinPickup>();
            SerializedObject coinSO = new SerializedObject(pickup);
            coinSO.FindProperty("coinId").intValue = i + 1;
            coinSO.ApplyModifiedPropertiesWithoutUndo();
            coins[i] = pickup;
        }

        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeAreaRoot = CreateRect("SafeAreaRoot", canvasGO.transform);
        StretchFull(safeAreaRoot);

        Image menuPanelImage;
        RectTransform menuPanel = CreatePanel("MenuPanel", safeAreaRoot, new Vector2(480f, 600f), Vector2.zero, out menuPanelImage);
        Button startButton = CreateButton("StartButton", menuPanel, new Vector2(340f, 54f), new Vector2(0f, 80f), "START");
        TMP_Text statusText = CreateText("StatusText", menuPanel, new Vector2(440f, 80f), new Vector2(0f, -190f), "Ready", 24f);

        Image hudPanelImage;
        RectTransform hudPanel = CreatePanel("HudPanel", safeAreaRoot, new Vector2(520f, 70f), new Vector2(0f, 370f), out hudPanelImage);
        TMP_Text hudText = CreateText("HudText", hudPanel, new Vector2(500f, 60f), Vector2.zero, "Score: 0  Time: 30", 28f);

        Image resultPanelImage;
        RectTransform resultPanel = CreatePanel("ResultPanel", safeAreaRoot, new Vector2(480f, 360f), Vector2.zero, out resultPanelImage);
        TMP_Text resultText = CreateText("ResultText", resultPanel, new Vector2(440f, 60f), new Vector2(0f, 45f), "Score: 0", 28f);
        Button restartButton = CreateButton("RestartButton", resultPanel, new Vector2(340f, 54f), new Vector2(0f, -35f), "RESTART");
        Button menuButton = CreateButton("MenuButton", resultPanel, new Vector2(340f, 54f), new Vector2(0f, -105f), "MENU");

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject gameSessionGO = new GameObject("GameSession");
        AudioSource audioSource = gameSessionGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.4f;

        GameSession session = gameSessionGO.AddComponent<GameSession>();
        SerializedObject sessionSO = new SerializedObject(session);
        sessionSO.FindProperty("player").objectReferenceValue = player.transform;
        sessionSO.FindProperty("collectAudio").objectReferenceValue = audioSource;
        sessionSO.FindProperty("menuPanel").objectReferenceValue = menuPanel.gameObject;
        sessionSO.FindProperty("hudPanel").objectReferenceValue = hudPanel.gameObject;
        sessionSO.FindProperty("resultPanel").objectReferenceValue = resultPanel.gameObject;
        sessionSO.FindProperty("hud").objectReferenceValue = hudText;
        sessionSO.FindProperty("resultText").objectReferenceValue = resultText;
        sessionSO.FindProperty("statusText").objectReferenceValue = statusText;
        sessionSO.FindProperty("startButton").objectReferenceValue = startButton;

        SerializedProperty coinsProp = sessionSO.FindProperty("coins");
        coinsProp.arraySize = coins.Length;
        for (int i = 0; i < coins.Length; i++)
            coinsProp.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
        sessionSO.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(startButton.onClick, session.BeginRound);
        UnityEventTools.AddPersistentListener(restartButton.onClick, session.BeginRound);
        UnityEventTools.AddPersistentListener(menuButton.onClick, session.ReturnToMenu);

        menuPanel.gameObject.SetActive(true);
        hudPanel.gameObject.SetActive(false);
        resultPanel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = gameSessionGO;

        EditorUtility.DisplayDialog("DAY02 Setup 완료",
            "DAY02 구성이 완료되었습니다.\n\nPlayer / Coin01~Coin10 / GameSession / Canvas / EventSystem\n메뉴·HUD·결과 UI / 버튼 이벤트 / AudioSource / 머티리얼 연결 완료\n\nPlay 후 START 버튼으로 테스트하세요.",
            "확인");
    }

    private static string FindPlatformMainScene()
    {
        string[] guids = AssetDatabase.FindAssets($"{SceneName} t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == SceneName)
                return path;
        }
        return null;
    }

    private static void DestroyRootIfExists(Scene scene, string objectName)
    {
        GameObject target = scene.GetRootGameObjects().FirstOrDefault(go => go.name == objectName);
        if (target != null)
            Object.DestroyImmediate(target);
    }

    private static Material GetOrCreateMaterial(string assetPath, Color color)
    {
        string directory = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, out Image image)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, size, position);
        image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.10f, 0.18f, 0.88f);
        image.raycastTarget = false;
        return rt;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 size, Vector2 position, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, size, position);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.62f, 0.64f, 1f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText("Text (TMP)", rt, size, Vector2.zero, label, 26f);
        text.raycastTarget = false;
        return button;
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 size, Vector2 position, string value, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, size, position);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private static void SetCenteredRect(RectTransform rt, Vector2 size, Vector2 position)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}
#endif
