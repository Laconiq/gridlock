#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIWE.Editor
{
    public static class InteractionHUDSetup
    {
        [MenuItem("AIWE/Setup Interaction HUD")]
        public static void Setup()
        {
            if (GameObject.Find("---UI_INTERACTION---") != null)
            {
                Debug.Log("Interaction HUD already exists");
                return;
            }

            // Canvas
            var canvasGO = new GameObject("---UI_INTERACTION---");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Crosshair (center dot)
            var crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasGO.transform, false);
            var chRT = crosshair.AddComponent<RectTransform>();
            chRT.anchorMin = new Vector2(0.5f, 0.5f);
            chRT.anchorMax = new Vector2(0.5f, 0.5f);
            chRT.sizeDelta = new Vector2(4, 4);
            var chImg = crosshair.AddComponent<Image>();
            chImg.color = new Color(1f, 1f, 1f, 0.7f);
            chImg.raycastTarget = false;

            // Prompt root (below crosshair)
            var promptRoot = new GameObject("InteractionPrompt");
            promptRoot.transform.SetParent(canvasGO.transform, false);
            var prRT = promptRoot.AddComponent<RectTransform>();
            prRT.anchorMin = new Vector2(0.5f, 0.5f);
            prRT.anchorMax = new Vector2(0.5f, 0.5f);
            prRT.anchoredPosition = new Vector2(0, -60);
            prRT.sizeDelta = new Vector2(200, 50);

            // Horizontal layout
            var hlg = promptRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Key background (the "E" button visual)
            var keyBG = new GameObject("KeyBackground");
            keyBG.transform.SetParent(promptRoot.transform, false);
            var kbRT = keyBG.AddComponent<RectTransform>();
            kbRT.sizeDelta = new Vector2(40, 40);
            var kbLayout = keyBG.AddComponent<LayoutElement>();
            kbLayout.preferredWidth = 40;
            kbLayout.preferredHeight = 40;
            var kbImg = keyBG.AddComponent<Image>();
            kbImg.color = new Color(0.9f, 0.9f, 0.9f);
            kbImg.raycastTarget = false;

            // Rounded corners via outline
            var outline = keyBG.AddComponent<Outline>();
            outline.effectColor = new Color(0.6f, 0.6f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            // Key text "E"
            var keyTextGO = new GameObject("KeyText");
            keyTextGO.transform.SetParent(keyBG.transform, false);
            var ktRT = keyTextGO.AddComponent<RectTransform>();
            ktRT.anchorMin = Vector2.zero;
            ktRT.anchorMax = Vector2.one;
            ktRT.offsetMin = Vector2.zero;
            ktRT.offsetMax = Vector2.zero;
            var ktTMP = keyTextGO.AddComponent<TextMeshProUGUI>();
            ktTMP.text = "E";
            ktTMP.fontSize = 22;
            ktTMP.fontStyle = FontStyles.Bold;
            ktTMP.alignment = TextAlignmentOptions.Center;
            ktTMP.color = Color.black;
            ktTMP.raycastTarget = false;

            // Action text
            var actionTextGO = new GameObject("ActionText");
            actionTextGO.transform.SetParent(promptRoot.transform, false);
            var atLayout = actionTextGO.AddComponent<LayoutElement>();
            atLayout.preferredWidth = 140;
            atLayout.preferredHeight = 40;
            var atTMP = actionTextGO.AddComponent<TextMeshProUGUI>();
            atTMP.text = "Edit Tower";
            atTMP.fontSize = 16;
            atTMP.alignment = TextAlignmentOptions.MidlineLeft;
            atTMP.color = Color.white;
            atTMP.raycastTarget = false;

            // Progress fill (under the key)
            var progressBG = new GameObject("ProgressBG");
            progressBG.transform.SetParent(keyBG.transform, false);
            var pbRT = progressBG.AddComponent<RectTransform>();
            pbRT.anchorMin = new Vector2(0, 0);
            pbRT.anchorMax = new Vector2(1, 0.08f);
            pbRT.offsetMin = new Vector2(2, 0);
            pbRT.offsetMax = new Vector2(-2, 0);
            pbRT.sizeDelta = new Vector2(-4, 3);
            var pbImg = progressBG.AddComponent<Image>();
            pbImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            pbImg.raycastTarget = false;

            var progressFill = new GameObject("ProgressFill");
            progressFill.transform.SetParent(progressBG.transform, false);
            var pfRT = progressFill.AddComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero;
            pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = Vector2.zero;
            pfRT.offsetMax = Vector2.zero;
            var pfImg = progressFill.AddComponent<Image>();
            pfImg.color = new Color(0.3f, 0.85f, 0.4f);
            pfImg.raycastTarget = false;
            pfImg.type = Image.Type.Filled;
            pfImg.fillMethod = Image.FillMethod.Horizontal;
            pfImg.fillAmount = 0f;

            // Add InteractionHUD component and wire
            var hud = canvasGO.AddComponent<AIWE.Player.InteractionHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("promptRoot").objectReferenceValue = promptRoot;
            so.FindProperty("keyBackground").objectReferenceValue = kbImg;
            so.FindProperty("keyText").objectReferenceValue = ktTMP;
            so.FindProperty("actionText").objectReferenceValue = atTMP;
            so.FindProperty("progressFill").objectReferenceValue = pfImg;
            so.ApplyModifiedProperties();

            promptRoot.SetActive(false);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameScene.unity");
            Debug.Log("Interaction HUD created and scene saved.");
        }
    }
}
#endif
