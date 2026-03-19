#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace AIWE.Editor
{
    public static class UISetup
    {
        [MenuItem("AIWE/Setup Game UI")]
        public static void SetupUI()
        {
            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Debug.Log("Created EventSystem");
            }

            CreateMainMenuUI();
            CreateNodeEditorUI();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameScene.unity");
            Debug.Log("UI Setup complete. Scene saved.");
        }

        private static void CreateMainMenuUI()
        {
            // Check if already exists
            if (GameObject.Find("---UI_MAINMENU---") != null)
            {
                Debug.Log("MainMenu UI already exists, skipping");
                return;
            }

            // Canvas
            var canvasGO = new GameObject("---UI_MAINMENU---");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
            var mainMenuUI = canvasGO.AddComponent<AIWE.UI.MainMenuUI>();

            // === Main Menu Panel ===
            var mainPanel = CreatePanel(canvasGO.transform, "MainMenuPanel", Color.clear);
            SetAnchors(mainPanel, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f));

            // Background
            var bg = CreatePanel(mainPanel.transform, "Background", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            SetAnchors(bg, Vector2.zero, Vector2.one);

            // Title
            var title = CreateText(mainPanel.transform, "Title", "AIWE", 48, TextAlignmentOptions.Center);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.8f);
            titleRT.anchorMax = new Vector2(1, 0.95f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Host Button
            var hostBtn = CreateButton(mainPanel.transform, "HostButton", "HOST GAME");
            var hostRT = hostBtn.GetComponent<RectTransform>();
            hostRT.anchorMin = new Vector2(0.15f, 0.55f);
            hostRT.anchorMax = new Vector2(0.85f, 0.65f);
            hostRT.offsetMin = Vector2.zero;
            hostRT.offsetMax = Vector2.zero;

            // Join Code Input
            var joinInput = CreateInputField(mainPanel.transform, "JoinCodeInput", "Enter lobby code...");
            var joinRT = joinInput.GetComponent<RectTransform>();
            joinRT.anchorMin = new Vector2(0.15f, 0.38f);
            joinRT.anchorMax = new Vector2(0.85f, 0.48f);
            joinRT.offsetMin = Vector2.zero;
            joinRT.offsetMax = Vector2.zero;

            // Join Button
            var joinBtn = CreateButton(mainPanel.transform, "JoinButton", "JOIN GAME");
            var joinBtnRT = joinBtn.GetComponent<RectTransform>();
            joinBtnRT.anchorMin = new Vector2(0.15f, 0.25f);
            joinBtnRT.anchorMax = new Vector2(0.85f, 0.35f);
            joinBtnRT.offsetMin = Vector2.zero;
            joinBtnRT.offsetMax = Vector2.zero;

            // Status Text
            var statusText = CreateText(mainPanel.transform, "StatusText", "", 18, TextAlignmentOptions.Center);
            var statusRT = statusText.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.1f, 0.1f);
            statusRT.anchorMax = new Vector2(0.9f, 0.2f);
            statusRT.offsetMin = Vector2.zero;
            statusRT.offsetMax = Vector2.zero;
            statusText.color = Color.yellow;

            // === Lobby Panel ===
            var lobbyPanel = CreatePanel(canvasGO.transform, "LobbyPanel", Color.clear);
            SetAnchors(lobbyPanel, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f));
            lobbyPanel.SetActive(false);

            var lobbyBg = CreatePanel(lobbyPanel.transform, "Background", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            SetAnchors(lobbyBg, Vector2.zero, Vector2.one);

            var lobbyTitle = CreateText(lobbyPanel.transform, "LobbyTitle", "LOBBY", 36, TextAlignmentOptions.Center);
            var lobbyTitleRT = lobbyTitle.GetComponent<RectTransform>();
            lobbyTitleRT.anchorMin = new Vector2(0, 0.85f);
            lobbyTitleRT.anchorMax = new Vector2(1, 0.95f);
            lobbyTitleRT.offsetMin = Vector2.zero;
            lobbyTitleRT.offsetMax = Vector2.zero;

            var lobbyCode = CreateText(lobbyPanel.transform, "LobbyCodeText", "Code: ------", 24, TextAlignmentOptions.Center);
            var lobbyCodeRT = lobbyCode.GetComponent<RectTransform>();
            lobbyCodeRT.anchorMin = new Vector2(0.1f, 0.65f);
            lobbyCodeRT.anchorMax = new Vector2(0.9f, 0.75f);
            lobbyCodeRT.offsetMin = Vector2.zero;
            lobbyCodeRT.offsetMax = Vector2.zero;

            var playerCount = CreateText(lobbyPanel.transform, "PlayerCountText", "Players: 0 / 4", 20, TextAlignmentOptions.Center);
            var pcRT = playerCount.GetComponent<RectTransform>();
            pcRT.anchorMin = new Vector2(0.1f, 0.5f);
            pcRT.anchorMax = new Vector2(0.9f, 0.6f);
            pcRT.offsetMin = Vector2.zero;
            pcRT.offsetMax = Vector2.zero;

            var startBtn = CreateButton(lobbyPanel.transform, "StartGameButton", "START GAME");
            var startRT = startBtn.GetComponent<RectTransform>();
            startRT.anchorMin = new Vector2(0.15f, 0.3f);
            startRT.anchorMax = new Vector2(0.85f, 0.4f);
            startRT.offsetMin = Vector2.zero;
            startRT.offsetMax = Vector2.zero;

            var disconnectBtn = CreateButton(lobbyPanel.transform, "DisconnectButton", "DISCONNECT");
            var discRT = disconnectBtn.GetComponent<RectTransform>();
            discRT.anchorMin = new Vector2(0.15f, 0.15f);
            discRT.anchorMax = new Vector2(0.85f, 0.25f);
            discRT.offsetMin = Vector2.zero;
            discRT.offsetMax = Vector2.zero;
            disconnectBtn.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f);

            // Wire references via SerializedObject
            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
            so.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
            so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
            so.FindProperty("joinButton").objectReferenceValue = joinBtn.GetComponent<Button>();
            so.FindProperty("joinCodeInput").objectReferenceValue = joinInput.GetComponent<TMP_InputField>();
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("lobbyCodeText").objectReferenceValue = lobbyCode;
            so.FindProperty("playerCountText").objectReferenceValue = playerCount;
            so.FindProperty("startGameButton").objectReferenceValue = startBtn.GetComponent<Button>();
            so.FindProperty("disconnectButton").objectReferenceValue = disconnectBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Debug.Log("Created MainMenu UI");
        }

        private static void CreateNodeEditorUI()
        {
            if (GameObject.Find("---UI_NODEEDITOR---") != null)
            {
                Debug.Log("NodeEditor UI already exists, skipping");
                return;
            }

            // Canvas
            var canvasGO = new GameObject("---UI_NODEEDITOR---");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var screen = canvasGO.AddComponent<AIWE.NodeEditor.UI.NodeEditorScreen>();
            canvasGO.AddComponent<AIWE.NodeEditor.NodeEditorController>();

            // Editor Panel (starts hidden)
            var editorPanel = CreatePanel(canvasGO.transform, "EditorPanel", new Color(0.12f, 0.12f, 0.16f, 0.98f));
            SetAnchors(editorPanel, Vector2.zero, Vector2.one);
            editorPanel.SetActive(false);

            // Title bar
            var titleBar = CreatePanel(editorPanel.transform, "TitleBar", new Color(0.2f, 0.2f, 0.25f));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 0.95f);
            tbRT.anchorMax = Vector2.one;
            tbRT.offsetMin = Vector2.zero;
            tbRT.offsetMax = Vector2.zero;

            var titleText = CreateText(titleBar.transform, "Title", "NODE EDITOR", 20, TextAlignmentOptions.MidlineLeft);
            var ttRT = titleText.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero;
            ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = new Vector2(20, 0);
            ttRT.offsetMax = new Vector2(-200, 0);

            // Save button
            var saveBtn = CreateButton(titleBar.transform, "SaveButton", "SAVE & CLOSE");
            var saveRT = saveBtn.GetComponent<RectTransform>();
            saveRT.anchorMin = new Vector2(0.82f, 0.1f);
            saveRT.anchorMax = new Vector2(0.99f, 0.9f);
            saveRT.offsetMin = Vector2.zero;
            saveRT.offsetMax = Vector2.zero;
            saveBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);

            // Add onClick for save
            var saveBtnComp = saveBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                saveBtnComp.onClick,
                new UnityEngine.Events.UnityAction(screen.OnSaveButtonClicked));

            // Module Palette (left panel)
            var palette = CreatePanel(editorPanel.transform, "ModulePalette", new Color(0.15f, 0.15f, 0.2f));
            var palRT = palette.GetComponent<RectTransform>();
            palRT.anchorMin = new Vector2(0, 0);
            palRT.anchorMax = new Vector2(0.15f, 0.95f);
            palRT.offsetMin = Vector2.zero;
            palRT.offsetMax = Vector2.zero;
            palette.AddComponent<AIWE.NodeEditor.UI.ModulePalette>();

            // Palette sections
            var triggersLabel = CreateText(palette.transform, "TriggersLabel", "TRIGGERS", 14, TextAlignmentOptions.TopLeft);
            var tlRT = triggersLabel.GetComponent<RectTransform>();
            tlRT.anchorMin = new Vector2(0.05f, 0.85f);
            tlRT.anchorMax = new Vector2(0.95f, 0.9f);
            tlRT.offsetMin = Vector2.zero;
            tlRT.offsetMax = Vector2.zero;
            triggersLabel.color = new Color(1f, 0.6f, 0.2f);

            var triggersSection = new GameObject("TriggersSection");
            triggersSection.transform.SetParent(palette.transform, false);
            var tsRT = triggersSection.AddComponent<RectTransform>();
            tsRT.anchorMin = new Vector2(0.05f, 0.7f);
            tsRT.anchorMax = new Vector2(0.95f, 0.85f);
            tsRT.offsetMin = Vector2.zero;
            tsRT.offsetMax = Vector2.zero;
            var tsLayout = triggersSection.AddComponent<VerticalLayoutGroup>();
            tsLayout.spacing = 4;

            var zonesLabel = CreateText(palette.transform, "ZonesLabel", "ZONES", 14, TextAlignmentOptions.TopLeft);
            var zlRT = zonesLabel.GetComponent<RectTransform>();
            zlRT.anchorMin = new Vector2(0.05f, 0.55f);
            zlRT.anchorMax = new Vector2(0.95f, 0.6f);
            zlRT.offsetMin = Vector2.zero;
            zlRT.offsetMax = Vector2.zero;
            zonesLabel.color = new Color(0.3f, 0.5f, 1f);

            var zonesSection = new GameObject("ZonesSection");
            zonesSection.transform.SetParent(palette.transform, false);
            var zsRT = zonesSection.AddComponent<RectTransform>();
            zsRT.anchorMin = new Vector2(0.05f, 0.4f);
            zsRT.anchorMax = new Vector2(0.95f, 0.55f);
            zsRT.offsetMin = Vector2.zero;
            zsRT.offsetMax = Vector2.zero;
            var zsLayout = zonesSection.AddComponent<VerticalLayoutGroup>();
            zsLayout.spacing = 4;

            var effectsLabel = CreateText(palette.transform, "EffectsLabel", "EFFECTS", 14, TextAlignmentOptions.TopLeft);
            var elRT = effectsLabel.GetComponent<RectTransform>();
            elRT.anchorMin = new Vector2(0.05f, 0.25f);
            elRT.anchorMax = new Vector2(0.95f, 0.3f);
            elRT.offsetMin = Vector2.zero;
            elRT.offsetMax = Vector2.zero;
            effectsLabel.color = new Color(0.3f, 0.85f, 0.4f);

            var effectsSection = new GameObject("EffectsSection");
            effectsSection.transform.SetParent(palette.transform, false);
            var esRT = effectsSection.AddComponent<RectTransform>();
            esRT.anchorMin = new Vector2(0.05f, 0.05f);
            esRT.anchorMax = new Vector2(0.95f, 0.25f);
            esRT.offsetMin = Vector2.zero;
            esRT.offsetMax = Vector2.zero;
            var esLayout = effectsSection.AddComponent<VerticalLayoutGroup>();
            esLayout.spacing = 4;

            // Canvas area (the node editing space)
            var canvasArea = new GameObject("CanvasArea");
            canvasArea.transform.SetParent(editorPanel.transform, false);
            var caRT = canvasArea.AddComponent<RectTransform>();
            caRT.anchorMin = new Vector2(0.15f, 0);
            caRT.anchorMax = new Vector2(1, 0.95f);
            caRT.offsetMin = Vector2.zero;
            caRT.offsetMax = Vector2.zero;
            canvasArea.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);
            canvasArea.AddComponent<Mask>();

            var nodeCanvas = canvasArea.AddComponent<AIWE.NodeEditor.UI.NodeEditorCanvas>();

            // Scrollable content
            var content = new GameObject("Content");
            content.transform.SetParent(canvasArea.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(4000, 4000);
            contentRT.anchoredPosition = Vector2.zero;

            // Wire NodeEditorScreen references
            var screenSO = new SerializedObject(screen);
            screenSO.FindProperty("editorPanel").objectReferenceValue = editorPanel;
            screenSO.FindProperty("canvas").objectReferenceValue = nodeCanvas;
            screenSO.FindProperty("palette").objectReferenceValue = palette.GetComponent<AIWE.NodeEditor.UI.ModulePalette>();
            screenSO.ApplyModifiedProperties();

            // Wire NodeEditorCanvas content reference
            var canvasSO = new SerializedObject(nodeCanvas);
            canvasSO.FindProperty("content").objectReferenceValue = contentRT;
            canvasSO.ApplyModifiedProperties();

            // Wire NodeEditorController
            var controller = canvasGO.GetComponent<AIWE.NodeEditor.NodeEditorController>();
            var ctrlSO = new SerializedObject(controller);
            ctrlSO.FindProperty("screen").objectReferenceValue = screen;
            ctrlSO.ApplyModifiedProperties();

            // Wire ModulePalette sections
            var palComp = palette.GetComponent<AIWE.NodeEditor.UI.ModulePalette>();
            var palSO = new SerializedObject(palComp);
            palSO.FindProperty("triggerSection").objectReferenceValue = triggersSection.transform;
            palSO.FindProperty("zoneSection").objectReferenceValue = zonesSection.transform;
            palSO.FindProperty("effectSection").objectReferenceValue = effectsSection.transform;
            palSO.ApplyModifiedProperties();

            Debug.Log("Created NodeEditor UI");
        }

        // === Helper Methods ===

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.5f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f);
            btn.colors = colors;

            var text = CreateText(go.transform, "Label", label, 18, TextAlignmentOptions.Center);
            return go;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f);

            // Text Area
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(go.transform, false);
            var taRT = textArea.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(10, 2);
            taRT.offsetMax = new Vector2(-10, -2);

            // Placeholder
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(textArea.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = placeholder;
            phTMP.fontSize = 16;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.color = new Color(0.5f, 0.5f, 0.5f);
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Text
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(textArea.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
            txtTMP.fontSize = 16;
            txtTMP.color = Color.white;
            txtTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = taRT;
            input.textComponent = txtTMP;
            input.placeholder = phTMP;
            input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;

            return go;
        }
    }
}
#endif
