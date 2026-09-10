using BepInEx;
using BepInEx.Bootstrap;
using GorillaTag;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Utilla.Attributes;
using Utilla.Tools;

namespace Utilla.Behaviours
{
    internal class ConductBoardManager : MonoBehaviour
    {
        private int PageCount => boardContent.Count;

        private readonly List<Section> boardContent = new List<Section>();

        private ModeSelectButton buttonTemplate;

        private GameObject stumpRootObject;

        private Transform conductTransform;

        private TextMeshPro baseHeaderText, baseBodyText, headerText, bodyText, footerText;

        private int currentPage = 0;

        public void Start()
        {
            buttonTemplate = FindFirstObjectByType<GameModeSelectorButtonLayout>().pf_button;

            stumpRootObject = Array.Find(ZoneManagement.instance.allObjects, gameObject => gameObject.name == "TreeRoom");
            conductTransform = stumpRootObject.transform.FindChildRecursive("code of conduct");

            // Get Top Header and Body Text
            baseHeaderText = stumpRootObject.transform.FindChildRecursive("WelcomeToGorilllaTagHeadingText")?.GetComponent<TextMeshPro>();
            if (baseHeaderText == null)
            {
                Logging.Warning("Code of Conduct Header Text is Missing");
                return;
            }

            baseBodyText = stumpRootObject.transform.FindChildRecursive("HeadOutside")?.GetComponent<TextMeshPro>();
            if (baseBodyText == null)
            {
                Logging.Warning("Code of Conduct Body Text is Missing");
                return;
            }

            // Get Code of Conduct Header and Body Text and Transfer it to the Top Header and Body
            TextMeshPro conductHeaderText = stumpRootObject.transform.FindChildRecursive("CodeOfConductHeadingText")?.GetComponent<TextMeshPro>();
            if (conductHeaderText != null)
            {
                baseHeaderText.gameObject.name = conductHeaderText.gameObject.name;
                baseHeaderText.text = conductHeaderText.text;

                conductHeaderText.gameObject.Destroy();
            }

            TextMeshPro conductBodyText = stumpRootObject.transform.FindChildRecursive("COCBodyText_TitleData")?.GetComponent<TextMeshPro>();
            if (conductBodyText != null)
            {
                baseBodyText.gameObject.name = conductBodyText.gameObject.name;
                baseBodyText.text = conductBodyText.text;

                conductBodyText.gameObject.Destroy();
            }

            // Apply Text Format Settings
            GameObject headingTextObject = Instantiate(baseHeaderText.gameObject);
            headingTextObject.transform.position = baseHeaderText.transform.position;
            headingTextObject.transform.rotation = baseHeaderText.transform.rotation;
            headingTextObject.transform.localScale = baseHeaderText.transform.localScale;
            SanitizeTextObject(headingTextObject);

            headerText = headingTextObject.GetComponent<TextMeshPro>();
            headerText.fontSizeMax = baseHeaderText.fontSize;
            headerText.enableAutoSizing = true;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;

            GameObject bodyTextObject = Instantiate(baseBodyText.gameObject);
            bodyTextObject.transform.position = baseBodyText.transform.position;
            bodyTextObject.transform.rotation = baseBodyText.transform.rotation;
            bodyTextObject.transform.localScale = baseBodyText.transform.localScale;
            SanitizeTextObject(bodyTextObject);

            bodyText = bodyTextObject.GetComponent<TextMeshPro>();
            bodyText.fontSizeMax = baseBodyText.fontSize;
            bodyText.fontSizeMin = 0f;
            bodyText.enableAutoSizing = true;
            bodyText.margin = new Vector4(0f, 0f, 0f, 36f);
            bodyText.richText = true;

            GameObject footerTextObject = Instantiate(baseHeaderText.gameObject);
            footerTextObject.transform.position = baseHeaderText.transform.position;
            footerTextObject.transform.rotation = baseHeaderText.transform.rotation;
            footerTextObject.transform.localScale = baseHeaderText.transform.localScale;
            SanitizeTextObject(footerTextObject);

            footerText = footerTextObject.GetComponent<TextMeshPro>();
            footerText.text = $"{Constants.Name} {Constants.Version}".ToUpper();
            footerText.enableAutoSizing = false;
            footerText.fontSize = 45;
            footerText.margin = new Vector4(0f, 110f, 0f, 0f);
            footerText.characterSpacing = -11.5f;
            footerText.enabled = true;
            footerText.renderer.enabled = true;

            boardContent.Insert(0, new Section(baseHeaderText.text, baseBodyText.text));

            CreateConductButton(-1f, "-->", NextPage);
            CreateConductButton(1f, "<--", PrevPage);

            ShowPage();
            CreateEntries();
        }

        private void NextPage()
        {
            currentPage = (currentPage + 1) % PageCount;
            ShowPage();
        }

        private void PrevPage()
        {
            currentPage = (currentPage <= 0) ? PageCount - 1 : currentPage - 1;
            ShowPage();
        }

        private void ShowPage()
        {
            if (baseHeaderText == null || baseBodyText == null) return;

            Section content = boardContent.ElementAtOrDefault(Mathf.Max(0, Mathf.Min(currentPage, boardContent.Count - 1)));

            if (content.UseBaseText)
            {
                if (baseHeaderText.renderer != null)
                    baseHeaderText.renderer.forceRenderingOff = false;

                headerText.enabled = false;

                if (baseBodyText.renderer != null)
                    baseBodyText.renderer.forceRenderingOff = false;

                bodyText.enabled = false;
            }
            else
            {
                if (baseHeaderText.renderer != null)
                    baseHeaderText.renderer.forceRenderingOff = true;

                headerText.enabled = true;
                headerText.text = content.Title;

                if (baseBodyText.renderer != null)
                    baseBodyText.renderer.forceRenderingOff = true;

                bodyText.enabled = true;
                bodyText.text = content.Body;
            }
        }

        private void CreateConductButton(float horizontalPosition, string text, Action onButtonPressed = null)
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonObject.name = $"UtillaButton_{text}";
            buttonObject.transform.parent = conductTransform;
            buttonObject.transform.localPosition = new Vector3(horizontalPosition, 0.52f, 0.13f);
            buttonObject.transform.localRotation = Quaternion.Euler(353.5f, 0f, 0f);
            buttonObject.transform.localScale = new Vector3(0.1427168f, 0.1427168f, 0.1f);
            buttonObject.GetComponent<Renderer>().material = buttonTemplate.unpressedMaterial;
            buttonObject.GetComponent<Collider>().isTrigger = true;
            buttonObject.SetLayer(UnityLayer.GorillaInteractable);

            GameObject textObject = new GameObject();
            textObject.transform.parent = buttonObject.transform;
            textObject.transform.localPosition = Vector3.forward * 0.525f;
            textObject.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
            textObject.transform.localScale = Vector3.one;

            TextMeshPro textMeshPro = textObject.AddComponent<TextMeshPro>();
            textMeshPro.font = buttonTemplate?.GetComponentInChildren<TMP_Text>()?.font ?? stumpRootObject.GetComponentInChildren<GorillaComputerTerminal>()?.myScreenText?.font;
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.characterSpacing = -10f;
            textMeshPro.overflowMode = TextOverflowModes.Overflow;
            textMeshPro.fontSize = 3f;
            textMeshPro.color = new Color(0.1960784f, 0.1960784f, 0.1960784f);
            textMeshPro.text = text;

            GorillaPressableButton pressableButton = buttonObject.AddComponent<GorillaPressableButton>();
            pressableButton.buttonRenderer = buttonObject.GetComponent<MeshRenderer>();
            pressableButton.unpressedMaterial = buttonTemplate.unpressedMaterial;
            pressableButton.pressedMaterial = buttonTemplate.pressedMaterial;

            UnityEvent onPressEvent = new UnityEvent();
            onPressEvent.AddListener(new UnityAction(() =>
            {
                pressableButton.StartCoroutine(ButtonColourUpdate(pressableButton));
            }));
            onPressEvent.AddListener(new UnityAction(onButtonPressed));
            pressableButton.onPressButton = onPressEvent;
        }

        private void CreateEntries()
        {
            boardContent.Add(new Section(
                "CHEATING",
                "CHEATING IS PROHIBITED BY THE DEVELOPERS OF GORILLA TAG AND MAY GO AGAINST THE GORILLA TAG INTELLECTUAL PROPERTY POLICY.\n\n" +
                "https://gorillatagvr.com/gorillatagvr-mod-policy\n\n" +
                "CHEATING CAN INCLUDE, BUT IS NOT LIMITED TO:\n" +
                "-USING MODS OUTSIDE OF MODDED ROOMS THAT CAN MODIFY GAMEPLAY\n" +
                "-USING EXPLOITS OR MODS THAT NEGATIVELY IMPACT GAMEPLAY IN ANY ROOM\n\n" +
                "YOU CAN POTENTIALLY RECEIVE A BAN FROM CHEATING IN A FEW WAYS:\n" +
                "-BEING REPORTED BY OTHER PLAYERS\n" +
                "-BEING DETECTED BY THE ANTI-CHEAT SYSTEM\n\n" +
                "TO REDUCE THE RISK OF A BAN AND MOD MORE SAFELY:\n" +
                "-USE MODS FROM TRUSTED SOURCES ONLY\n" +
                "-BE CAUTIOUS OF MODS THAT INCLUDE \"DETECTED\" FEATURES\n\n" +
                "ULTIMATELY, IT IS UP TO YOU, THE USER, TO DECIDE IF AND HOW YOU CHOOSE TO MOD AND WHAT RISKS YOU ARE WILLING TO TAKE."
            ));

            foreach (var info in Chainloader.PluginInfos)
            {
                if (info.Value is null) continue;

                BaseUnityPlugin plugin = info.Value.Instance;
                if (plugin is null) continue;

                Type type = plugin.GetType();
                IEnumerable<ModdedBoardTextAttribute> attributes = type.GetCustomAttributes<ModdedBoardTextAttribute>();

                if (!(attributes is null))
                {
                    var assembly = type.Assembly;
                    var names = assembly.GetManifestResourceNames();

                    foreach (ModdedBoardTextAttribute attribute in attributes)
                    {
                        if (string.IsNullOrEmpty(attribute.Title) || string.IsNullOrWhiteSpace(attribute.Title) || string.IsNullOrEmpty(attribute.Text) || string.IsNullOrWhiteSpace(attribute.Text)) continue;

                        if (names.SingleOrDefault(resourceName => resourceName == attribute.Text) is string resourceName)
                        {
                            using Stream stream = assembly.GetManifestResourceStream(resourceName);
                            using StreamReader reader = new StreamReader(stream);
                            string resourceText = reader.ReadToEnd();
                            boardContent.Add(new Section(attribute.Title, resourceText));
                            continue;
                        }

                        boardContent.Add(new Section(attribute.Title, attribute.Text));
                    }
                }
            }
        }

        private IEnumerator ButtonColourUpdate(GorillaPressableButton pressableButton)
        {
            pressableButton.isOn = true;
            pressableButton.UpdateColor();

            yield return new WaitForSeconds(pressableButton.debounceTime);
            if ((pressableButton.touchTime + pressableButton.debounceTime) < Time.time)
            {
                pressableButton.isOn = false;
                pressableButton.UpdateColor();
            }

            yield break;
        }

        private void SanitizeTextObject(GameObject gameObject)
        {
            Type[] typesToRemove = new Type[] { typeof(PlayFabTitleDataTextDisplay), typeof(LocalizedText), typeof(StaticLodGroup) };
            Component[] components = gameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                if (typesToRemove.Contains(type)) Destroy(components[i]);
            }
        }

        private struct Section
        {
            public bool UseBaseText;

            [TextArea(1, 1)]
            public string Title;

            [TextArea(12, 32)]
            public string Body;

            public Section(bool useBaseText)
            {
                UseBaseText = useBaseText;
                Title = null;
                Body = null;
            }

            public Section(string title, string body)
            {
                UseBaseText = false;
                Title = title;
                Body = body;
            }
        }
    }
}