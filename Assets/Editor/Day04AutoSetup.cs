#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day04AutoSetup
{
    [MenuItem("Tools/Game Platform/Build DAY04 Save + Safe Area")]
    public static void BuildDay04()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "DAY04 Setup",
                "Play 모드를 먼저 종료한 뒤 다시 실행하세요.",
                "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || scene.name != "PlatformMain")
        {
            EditorUtility.DisplayDialog(
                "DAY04 Setup",
                "PlatformMain 씬을 연 상태에서 실행하세요.",
                "확인");
            return;
        }

        GameObject sessionGO = GameObject.Find("GameSession");
        GameSession session = sessionGO != null ? sessionGO.GetComponent<GameSession>() : null;
        GameObject safeAreaGO = GameObject.Find("Canvas/SafeAreaRoot");

        if (session == null || safeAreaGO == null)
        {
            EditorUtility.DisplayDialog(
                "DAY04 Setup",
                "DAY02~03 구성이 필요합니다.\nGameSession 또는 Canvas/SafeAreaRoot를 찾지 못했습니다.",
                "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        RectTransform safeArea = safeAreaGO.GetComponent<RectTransform>();

        // SafeAreaRoot는 화면 전체 Stretch / offset 0으로 시작합니다.
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.pivot = new Vector2(0.5f, 0.5f);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        safeArea.localScale = Vector3.one;

        SafeAreaFitter fitter = safeAreaGO.GetComponent<SafeAreaFitter>();
        if (fitter == null)
        {
            fitter = safeAreaGO.AddComponent<SafeAreaFitter>();
        }

        // 중복 실행 시 기존 DAY04 UI를 다시 정확하게 구성합니다.
        Transform oldBest = safeArea.Find("BestText");
        if (oldBest != null)
        {
            Object.DestroyImmediate(oldBest.gameObject);
        }

        Transform oldSave = safeArea.Find("SaveButton");
        if (oldSave != null)
        {
            Object.DestroyImmediate(oldSave.gameObject);
        }

        TMP_Text bestText = CreateText(
            "BestText",
            safeArea,
            new Vector2(300f, 50f),
            new Vector2(590f, 375f),
            "Best: 0",
            26f);

        Button saveButton = CreateButton(
            "SaveButton",
            safeArea,
            new Vector2(220f, 50f),
            new Vector2(590f, 310f),
            "SAVE BEST");

        // GameSession의 Best Text 참조 연결
        SerializedObject sessionSO = new SerializedObject(session);
        SerializedProperty bestTextProp = sessionSO.FindProperty("bestText");

        if (bestTextProp == null)
        {
            EditorUtility.DisplayDialog(
                "DAY04 Setup",
                "GameSession.cs에 bestText 필드가 없습니다.\nAssets/Scripts/Runtime/GameSession.cs가 DAY04 버전으로 교체됐는지 확인하세요.",
                "확인");
            return;
        }

        bestTextProp.objectReferenceValue = bestText;
        sessionSO.ApplyModifiedPropertiesWithoutUndo();

        // SAVE BEST 버튼 이벤트 연결
        UnityEventTools.AddPersistentListener(saveButton.onClick, session.SaveBest);

        // Canvas 설정도 DAY02 요구값으로 다시 확인/보정
        Canvas canvas = safeAreaGO.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600f, 900f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = safeAreaGO;

        EditorUtility.DisplayDialog(
            "DAY04 Setup 완료",
            "DAY04 구성이 완료되었습니다.\n\n" +
            "• GameSession 최고 점수 저장\n" +
            "• BestText = Best: 0\n" +
            "• SAVE BEST 버튼\n" +
            "• PlayerPrefs 저장\n" +
            "• 결과 화면 자동 SaveBest()\n" +
            "• OnApplicationPause 저장\n" +
            "• SafeAreaFitter 연결\n" +
            "• Canvas 1600x900 / Match 0.5 확인\n\n" +
            "Play → START → 점수 획득 → SAVE BEST 또는 시간 종료 후 저장을 확인하세요.",
            "확인");
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        string value,
        float fontSize)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

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
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        string label)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        SetCenteredRect(rt, size, position);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.62f, 0.64f, 1f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(
            "Text (TMP)",
            rt,
            size,
            Vector2.zero,
            label,
            26f);

        text.raycastTarget = false;

        return button;
    }

    private static void SetCenteredRect(
        RectTransform rt,
        Vector2 size,
        Vector2 position)
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
