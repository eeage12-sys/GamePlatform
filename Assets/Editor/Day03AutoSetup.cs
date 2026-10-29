#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day03AutoSetup
{
    private const string ControlsPath = "Assets/Input/PlatformControls.inputactions";
    private const string MoveReferencePath = "Assets/Input/MoveActionReference.asset";

    [MenuItem("Tools/Game Platform/Build DAY03 Input")]
    public static void BuildDay03()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "DAY03 Setup",
                "Play 모드를 먼저 종료한 뒤 다시 실행하세요.",
                "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "PlatformMain")
        {
            EditorUtility.DisplayDialog(
                "DAY03 Setup",
                "PlatformMain 씬을 연 상태에서 실행하세요.",
                "확인");
            return;
        }

        GameObject player = GameObject.Find("Player");
        GameObject gameSessionGO = GameObject.Find("GameSession");
        GameSession session = gameSessionGO != null ? gameSessionGO.GetComponent<GameSession>() : null;
        GameObject safeAreaGO = GameObject.Find("Canvas/SafeAreaRoot");

        if (player == null || session == null || safeAreaGO == null)
        {
            EditorUtility.DisplayDialog(
                "DAY03 Setup",
                "DAY02 구성이 필요합니다.\nPlayer, GameSession, Canvas/SafeAreaRoot를 찾지 못했습니다.",
                "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        InputActionAsset controls = CreateOrReplaceControlsAsset();
        if (controls == null)
            return;

        InputAction moveAction = controls.FindAction("Gameplay/Move", true);
        InputActionReference moveReference = CreateOrReplaceMoveReference(moveAction);

        // Player 입력 이동
        PlatformMover mover = player.GetComponent<PlatformMover>();
        if (mover == null)
            mover = player.AddComponent<PlatformMover>();

        SerializedObject moverSO = new SerializedObject(mover);
        moverSO.FindProperty("move").objectReferenceValue = moveReference;
        moverSO.FindProperty("session").objectReferenceValue = session;
        moverSO.FindProperty("speed").floatValue = 5f;
        moverSO.ApplyModifiedPropertiesWithoutUndo();

        // 기존 MobileControls가 있으면 다시 정확하게 구성
        Transform oldMobile = safeAreaGO.transform.Find("MobileControls");
        if (oldMobile != null)
            UnityEngine.Object.DestroyImmediate(oldMobile.gameObject);

        RectTransform mobile = CreateRect("MobileControls", safeAreaGO.transform);
        mobile.anchorMin = new Vector2(0.15f, 0.18f);
        mobile.anchorMax = new Vector2(0.15f, 0.18f);
        mobile.pivot = new Vector2(0.5f, 0.5f);
        mobile.anchoredPosition = Vector2.zero;
        mobile.sizeDelta = new Vector2(260f, 220f);
        mobile.localScale = Vector3.one;

        GameObject up = CreateDirectionButton("Up", mobile, "W", new Vector2(0f, 65f), "<Keyboard>/w");
        GameObject down = CreateDirectionButton("Down", mobile, "S", new Vector2(0f, -10f), "<Keyboard>/s");
        GameObject left = CreateDirectionButton("Left", mobile, "A", new Vector2(-80f, -10f), "<Keyboard>/a");
        GameObject right = CreateDirectionButton("Right", mobile, "D", new Vector2(80f, -10f), "<Keyboard>/d");

        // 추가 실습: Joystick
        GameObject joystickBase = CreateImage(
            "JoystickBase",
            mobile,
            new Vector2(200f, 200f),
            Vector2.zero,
            new Color(0.12f, 0.12f, 0.15f, 0.85f),
            false);

        GameObject joystickHandle = CreateImage(
            "JoystickHandle",
            joystickBase.transform,
            new Vector2(64f, 64f),
            Vector2.zero,
            new Color(0.78f, 0.78f, 0.82f, 1f),
            true);

        OnScreenStick stick = joystickHandle.AddComponent<OnScreenStick>();
        SetSerializedString(stick, "m_ControlPath", "<Gamepad>/leftStick");
        SetSerializedFloat(stick, "m_MovementRange", 60f);
        SetSerializedBool(stick, "m_UseIsolatedInputActions", false);
        SetSerializedEnumByName(stick, "m_Behaviour", "RelativePositionWithStaticOrigin");

        // 전체 DAY03 완료 상태는 추가 실습인 조이스틱 표시로 둡니다.
        // 방향 버튼 테스트는 Tools > Game Platform > DAY03 Input Mode 메뉴에서 즉시 전환할 수 있습니다.
        up.SetActive(false);
        down.SetActive(false);
        left.SetActive(false);
        right.SetActive(false);
        joystickBase.SetActive(true);

        // 실행 환경 표시
        PlatformInfo info = gameSessionGO.GetComponent<PlatformInfo>();
        if (info == null)
            info = gameSessionGO.AddComponent<PlatformInfo>();

        SerializedObject infoSO = new SerializedObject(info);
        infoSO.FindProperty("mobileControls").objectReferenceValue = mobile.gameObject;
        infoSO.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "DAY03 Setup 완료",
            "DAY03 입력 구성이 완료되었습니다.\n\n" +
            "• PlatformControls.inputactions\n" +
            "• WASD / 방향키 / Gamepad Left Stick\n" +
            "• Player PlatformMover + Speed 5\n" +
            "• 화면 방향 버튼 4개\n" +
            "• 가상 조이스틱\n" +
            "• PlatformInfo + 실행환경 로그\n\n" +
            "현재는 추가 실습 상태로 조이스틱이 표시됩니다.\n" +
            "방향 버튼 테스트는 Tools > Game Platform > DAY03 Input Mode > Direction Buttons 를 누르세요.",
            "확인");
    }

    [MenuItem("Tools/Game Platform/DAY03 Input Mode/Direction Buttons")]
    public static void ShowDirectionButtons()
    {
        SetInputMode(true);
    }

    [MenuItem("Tools/Game Platform/DAY03 Input Mode/Joystick")]
    public static void ShowJoystick()
    {
        SetInputMode(false);
    }

    private static void SetInputMode(bool directionButtons)
    {
        GameObject mobile = GameObject.Find("Canvas/SafeAreaRoot/MobileControls");
        if (mobile == null)
        {
            EditorUtility.DisplayDialog("DAY03 Input Mode", "MobileControls를 찾지 못했습니다.", "확인");
            return;
        }

        string[] buttonNames = { "Up", "Down", "Left", "Right" };
        foreach (string name in buttonNames)
        {
            Transform t = mobile.transform.Find(name);
            if (t != null)
                t.gameObject.SetActive(directionButtons);
        }

        Transform joystick = mobile.transform.Find("JoystickBase");
        if (joystick != null)
            joystick.gameObject.SetActive(!directionButtons);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = mobile;
    }

    private static InputActionAsset CreateOrReplaceControlsAsset()
    {
        EnsureFolder("Assets/Input");

        if (File.Exists(ControlsPath))
            AssetDatabase.DeleteAsset(ControlsPath);

        InputActionAsset temp = ScriptableObject.CreateInstance<InputActionAsset>();
        temp.name = "PlatformControls";

        InputActionMap gameplay = temp.AddActionMap("Gameplay");
        InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");

        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        move.AddBinding("<Gamepad>/leftStick");

        string json = temp.ToJson();
        UnityEngine.Object.DestroyImmediate(temp);

        File.WriteAllText(ControlsPath, json);
        AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        InputActionAsset imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
        if (imported == null)
        {
            EditorUtility.DisplayDialog(
                "DAY03 Setup",
                "PlatformControls.inputactions 생성에 실패했습니다.",
                "확인");
        }

        return imported;
    }

    private static InputActionReference CreateOrReplaceMoveReference(InputAction moveAction)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MoveReferencePath) != null)
            AssetDatabase.DeleteAsset(MoveReferencePath);

        InputActionReference reference = InputActionReference.Create(moveAction);
        reference.name = "Gameplay_Move";
        AssetDatabase.CreateAsset(reference, MoveReferencePath);
        AssetDatabase.SaveAssets();
        return reference;
    }

    private static GameObject CreateDirectionButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        string controlPath)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(OnScreenButton));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, new Vector2(70f, 65f), position);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.62f, 0.64f, 0.95f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        OnScreenButton onScreen = go.GetComponent<OnScreenButton>();
        SetSerializedString(onScreen, "m_ControlPath", controlPath);

        TMP_Text text = CreateText("Text (TMP)", rt, new Vector2(70f, 65f), Vector2.zero, label, 28f);
        text.raycastTarget = false;

        return go;
    }

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color,
        bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, size, position);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return go;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        string value,
        float fontSize)
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

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
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

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = es.GetComponent<EventSystem>();
        }

        GameObject go = eventSystem.gameObject;

        StandaloneInputModule oldModule = go.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
            UnityEngine.Object.DestroyImmediate(oldModule);

        InputSystemUIInputModule module = go.GetComponent<InputSystemUIInputModule>();
        if (module == null)
            module = go.AddComponent<InputSystemUIInputModule>();

        // 새 InputSystemUIInputModule은 기본 UI Actions를 자동 구성합니다.
        EditorUtility.SetDirty(go);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void SetSerializedString(UnityEngine.Object target, string name, string value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
        {
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedFloat(UnityEngine.Object target, string name, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
        {
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedBool(UnityEngine.Object target, string name, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
        {
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedEnumByName(UnityEngine.Object target, string name, string enumName)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p != null && p.propertyType == SerializedPropertyType.Enum)
        {
            int index = Array.IndexOf(p.enumNames, enumName);
            if (index >= 0)
            {
                p.enumValueIndex = index;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
