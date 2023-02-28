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
            
            };
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
        private static Dictionary<string, string> _choiceMap = new Dictionary<string, string>();

        private static void UpdateDropdownChoices()
        {
            if (_dropdown == null)
            {
                return;
            }
            
            List<SceneAsset> scenes = new List<SceneAsset>();
            Debug.Log("Checking favorites");
            foreach (var guid in SceneSwitcherSettings.instance.FavoriteSceneGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath);
                scenes.Add(scene);

                _choiceMap[scene.name] = guid;
            }

        
            _dropdown.choices = scenes.Select(s => s.name).ToList();
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

            var guid = _choiceMap[evt.newValue];

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var sceneAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Single);
            }
        }

        private static void ToggleFavoriteSceneStatus(SceneAsset asset)
        {
            bool success =
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Selection.activeObject, out string guid, out long localId);

            if (!success)
            {
                Debug.LogError("Failed to get guid for object: " + asset, asset);
            }
        
            var saved = SceneSwitcherSettings.instance.FavoriteSceneGuids;

            if (saved.Contains(guid))
            {
                SceneSwitcherSettings.instance.RemoveEntry(guid);
            
            }
            else
            {
                SceneSwitcherSettings.instance.AddEntry(guid);
            }
        }

        [MenuItem("Assets/Toggle Favorite Scene", false, 111)]
        private static void ToggleSceneFavoriteMenuOption()
        {
            foreach (var obj in Selection.objects)
            {
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