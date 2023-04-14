using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BBG.SceneSwitcher.Editor
{
    [InitializeOnLoad]
    public class SceneSwitcher
    {
        private static ScriptableObject _toolbar;

        static SceneSwitcher()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;

                EditorSceneManager.activeSceneChangedInEditMode -= ActiveSceneChanged;
                EditorSceneManager.activeSceneChangedInEditMode += ActiveSceneChanged;

            };
        }

        private static void ActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Debug.Log($"Old scene: {oldScene}");
            Debug.Log($"New scene: {newScene}");
        }

        private static void Update()
        {
            if (_toolbar == null)
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;

                UnityEngine.Object[] toolbars =
                    UnityEngine.Resources.FindObjectsOfTypeAll(editorAssembly.GetType("UnityEditor.Toolbar"));
                _toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (_toolbar != null)
                {
                    AddVisualTreeElement();
                }

                SceneSwitcherSettings.changed -= UpdateDropdownChoices;
                SceneSwitcherSettings.changed += UpdateDropdownChoices;
            }
        }

        private static DropdownField _dropdown;
        private static Dictionary<string, SceneSwitcherSettings.Entry> _choiceMap = new Dictionary<string, SceneSwitcherSettings.Entry>();

        private static void UpdateDropdownChoices()
        {
            if (_dropdown == null)
            {
                return;
            }
            
            List<SceneAsset> scenes = new List<SceneAsset>();
            var props = new List<string>();
            foreach (var entry in SceneSwitcherSettings.instance.Entries)
            {
                var sceneNames = entry.SceneNames();

                string prop;
                if (sceneNames.Length == 1)
                {
                    prop = sceneNames[0];
                }
                else
                {
                    prop = string.Join(", ", sceneNames);
                }

                _choiceMap[prop] = entry;
            }


            _dropdown.choices = props;
        }

        private static void AddVisualTreeElement()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.badlybadgames.sceneswitcher/Editor/tree.uxml");
            var ui = asset.Instantiate();

            var root = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var rawRoot = root.GetValue(_toolbar);
            var mRoot = rawRoot as VisualElement;

        
            _dropdown = ui.Q<DropdownField>();

            _dropdown.RegisterValueChangedCallback(handler);


            var r = mRoot.Q<VisualElement>("ToolbarContainerContent");
            r.Insert(1, ui);
        
            UpdateDropdownChoices();
        
        }


        private static void handler(ChangeEvent<string> evt)
        {
            if (evt.newValue == evt.previousValue)
            {
                return;
            }

            var entry = _choiceMap[evt.newValue];

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var guids = entry.ScenesGUIDS();
                var firstScene = GetScenePathFromGUID(guids[0]);
                EditorSceneManager.OpenScene(firstScene, OpenSceneMode.Single);
                if (guids.Length > 1)
                {
                    foreach (var guid in guids[1..])
                    {
                        var scenePath = GetScenePathFromGUID(guid);
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }
                }
            }
        }

        private static string GetScenePathFromGUID(string sceneGUID)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            
            return assetPath;
        }

        private static void ToggleFavoriteSceneStatus(SceneAsset asset)
        {
            bool success =
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);

            if (!success)
            {
                Debug.LogError("Failed to get guid for object: " + asset, asset);
            }
            
            Debug.Log($"Guid for {asset.name} is: {guid}");
        
            var saved = SceneSwitcherSettings.IsFavorited(asset.name);

            if (saved)
            {
                SceneSwitcherSettings.instance.AddFavoriteScenes(asset.name);
            }
            else
            {
                throw new NotImplementedException();
                Debug.Log("Remove guid: " + guid);
                SceneSwitcherSettings.instance.R(guid);
            }
        }

        [MenuItem("Assets/Toggle Favorite Scene", false, 111)]
        private static void ToggleSceneFavoriteMenuOption()
        {
            foreach (var obj in Selection.objects)
            {
                Debug.Log("toggle for: "  +(obj as SceneAsset).name);
                ToggleFavoriteSceneStatus(obj as SceneAsset);
            }
            SceneSwitcherSettings.Save();
        }

        // Note that we pass the same path, and also pass "true" to the second argument.
        [MenuItem("Assets/Toggle Favorite Scene", true, 111)]
        private static bool CheckToggleSceneFavoriteMenuOption()
        {
            return Selection.objects.All(o => o is SceneAsset);
        }
    }
}