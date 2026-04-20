using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Builds nickname and leaderboard screens under the same parent as the main menu when the scene has no prefabs yet
    /// (useful before running Tools/LudumDare/Build Template in the editor).
    /// </summary>
    public static class LeaderboardUiRuntimeBuilder
    {
        /// <summary>Finds a <see cref="UIScreen"/> among descendants whose GameObject has the given name.</summary>
        public static UIScreen FindChildScreen(Transform root, string gameObjectName)
        {
            if (root == null || string.IsNullOrEmpty(gameObjectName)) return null;
            foreach (var s in root.GetComponentsInChildren<UIScreen>(true))
            {
                if (s.gameObject.name == gameObjectName) return s;
            }

            return null;
        }

        public static void EnsureMainMenuScreens(Transform mainMenuScreenTransform)
        {
            var parent = mainMenuScreenTransform.parent;
            if (parent == null) return;

            if (FindChildScreen(parent, "NicknameEntryScreen") == null)
            {
                BuildNicknamePanel(parent);
            }

            if (FindChildScreen(parent, "LeaderboardScreen") == null)
            {
                BuildLeaderboardPanel(parent);
            }

            if (FindChildScreen(parent, "TutorialScreen") == null)
            {
                BuildTutorialPanel(parent);
            }
        }

        public static UIScreen EnsureGameLeaderboard(Transform uiElementUnderCanvas)
        {
            var canvas = uiElementUnderCanvas.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            var existing = FindChildScreen(canvas.transform, "LeaderboardScreen");
            if (existing != null) return existing;

            return BuildLeaderboardPanel(canvas.transform);
        }

        private static UIScreen BuildNicknamePanel(Transform parent)
        {
            var panel = CreateOverlayPanel(parent, "NicknameEntryScreen");
            var screen = panel.AddComponent<NicknameEntryScreen>();

            var title = CreateTmpText(panel.transform, "Title", "NICKNAME", 48);
            SetStretchTop(title.rectTransform, 0.78f, 0.88f, 800f, 80f);

            var input = CreateTmpInputField(panel.transform, "NicknameInput", new Vector2(0.5f, 0.58f));
            var confirm = CreateMenuButton(panel.transform, "ConfirmButton", "Play", new Vector2(0.5f, 0.42f));
            var back = CreateMenuButton(panel.transform, "BackButton", "Back", new Vector2(0.5f, 0.3f));

            screen.SetRuntimeRefs(input, confirm, back);
            panel.SetActive(false);
            return screen;
        }

        private static UIScreen BuildTutorialPanel(Transform parent)
        {
            var panel = CreateOverlayPanel(parent, "TutorialScreen");
            var screen = panel.AddComponent<TutorialScreen>();

            var imgGo = new GameObject("TutorialArt", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(panel.transform, false);
            var imgRt = (RectTransform)imgGo.transform;
            imgRt.anchorMin = new Vector2(0.05f, 0.12f);
            imgRt.anchorMax = new Vector2(0.95f, 0.88f);
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;
            var image = imgGo.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = Color.white;

            var play = CreateMenuButton(panel.transform, "PlayButton", "Play", new Vector2(0.5f, 0.06f));
            screen.SetRuntimeRefs(play);
            panel.SetActive(false);
            return screen;
        }

        private static UIScreen BuildLeaderboardPanel(Transform parent)
        {
            var panel = CreateOverlayPanel(parent, "LeaderboardScreen");
            var screen = panel.AddComponent<LeaderboardScreen>();

            var title = CreateTmpText(panel.transform, "Title", "LEADERBOARD", 48);
            SetStretchTop(title.rectTransform, 0.88f, 0.96f, 900f, 72f);

            var status = CreateTmpText(panel.transform, "StatusLabel", "Loading...", 22);
            SetStretchTop(status.rectTransform, 0.82f, 0.86f, 920f, 36f);

            var back = CreateMenuButton(panel.transform, "BackButton", "Back", new Vector2(0.5f, 0.07f));

            var scrollRoot = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollRoot.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)scrollRoot.transform;
            srt.anchorMin = new Vector2(0.08f, 0.14f);
            srt.anchorMax = new Vector2(0.92f, 0.8f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            scrollRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var vpRt = (RectTransform)viewport.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(6f, 6f);
            vpRt.offsetMax = new Vector2(-6f, -6f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = (RectTransform)content.transform;
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = cRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            var rowProtoGo = new GameObject("RowPrototype", typeof(RectTransform));
            rowProtoGo.SetActive(false);
            rowProtoGo.transform.SetParent(panel.transform, false);
            var rowRt = (RectTransform)rowProtoGo.transform;
            rowRt.sizeDelta = new Vector2(100f, 34f);
            var rowTmp = rowProtoGo.AddComponent<TextMeshProUGUI>();
            rowTmp.fontSize = 26;
            rowTmp.color = Color.white;
            rowTmp.text = " ";

            screen.SetRuntimeRefs(back, status, cRt, rowTmp);
            panel.SetActive(false);
            return screen;
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var dim = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(go.transform, false);
            var dRt = (RectTransform)dim.transform;
            dRt.anchorMin = Vector2.zero;
            dRt.anchorMax = Vector2.one;
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            return go;
        }

        private static void SetStretchTop(RectTransform rt, float ymin, float ymax, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, ymin);
            rt.anchorMax = new Vector2(0.5f, ymax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static TMP_Text CreateTmpText(Transform parent, string name, string text, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        public static Button CreateMenuButton(Transform parent, string objectName, string label, Vector2 anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 80f);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var labelText = CreateTmpText(go.transform, "Label", label, 32);
            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateTmpInputField(Transform parent, string objectName, Vector2 anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 56f);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var taRt = (RectTransform)textArea.transform;
            taRt.anchorMin = new Vector2(0.03f, 0.12f);
            taRt.anchorMax = new Vector2(0.97f, 0.88f);
            taRt.offsetMin = Vector2.zero;
            taRt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 28;
            text.color = Color.white;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textArea.transform, false);
            var phRt = (RectTransform)placeholderGo.transform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10f, 4f);
            phRt.offsetMax = new Vector2(-10f, -4f);
            var ph = placeholderGo.AddComponent<TextMeshProUGUI>();
            ph.fontSize = 28;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = "Enter nickname...";

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = taRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }
    }
}
