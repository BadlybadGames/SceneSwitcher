using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BBG.SceneSwitcher.Editor
{
    [FilePath("BBG/SceneSwitcher/SceneSwitcherSettings.ini", FilePathAttribute.Location.ProjectFolder)]
    public class SceneSwitcherSettings : ScriptableSingleton<SceneSwitcherSettings>
    {
        [Serializable]
        public class Entry
        {
            public List<string> sceneGUIDs;

            public Entry(string scene)
            {
                sceneGUIDs = new List<string>();
                sceneGUIDs.Add(scene);
            }

            public Entry(string[] scenes)
            {
                sceneGUIDs = new List<string>(scenes);
            }

            public string[] ScenesGUIDS()
            {
                return sceneGUIDs.ToArray();
            }

            public string[] SceneNames()
            {
                var ret = new string[sceneGUIDs.Count];
                for (var index = 0; index < sceneGUIDs.Count; index++)
                {
                    var guid = sceneGUIDs[index];
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath);
                    ret[index] = scene.name;
                }

                return ret;
            }

            public string Scene()
            {
                if (Count() != 1)
                {
                    return "";
                }

                return sceneGUIDs[0];
            }

            public int Count()
            {
                return sceneGUIDs.Count;
            }

            public bool Equals(string[] other)
            {
                return sceneGUIDs.Equals(other);
            }
        }
        
        [FormerlySerializedAs("favoriteSceneGUIDS")] [SerializeField]
        private List<Entry> entries = new List<Entry>();

        public static event Action changed;

        private void Awake()
        {
            if (!File.Exists(SceneSwitcherSettings.GetFilePath()))
            {
                Debug.LogError("Could not find SceneSwitcher Settings");
                instance.entries = new List<Entry>();
                instance.Save(false);
            }
        }


        public Entry[] Entries
        {
            get
            {
                return SceneSwitcherSettings.instance.entries.ToArray();
            }
        }
        /*public string[] FavoriteSceneGuids
        {
            get { return SceneSwitcherSettings.instance.entries.Select(e => e.sceneGUID).ToArray(); }
            private set { SceneSwitcherSettings.instance.entries = value.Select(s => new Entry(){sceneGUID = s}).ToList(); }
        }*/

        public void AddFavoriteScenes(string[] scenes)
        {
            foreach (var s in scenes)
            {
                AddEntry(s);
                
            }
        }
        public void AddFavoriteScenes(string entry)
        {
            AddFavoriteScenes(new string[]{entry});
        }
        

        private void AddEntry(string entry)
        {
            instance.entries.Add(new Entry(entry));    
            changed?.Invoke();
        }
        
        private void RemoveEntry(string entry)
        {
            //SceneSwitcherSettings.instance.entries.Remove(instance.entries.FirstOrDefault(e => e.sceneGUID==entry));
            changed?.Invoke();
        }

        public static void Save()
        {
            instance.Save(false);
        }

        public static void ClearDataAndSave()
        {
            instance.entries.Clear();
            Save();
            changed?.Invoke();
        }

        public static bool IsFavorited(string sceneName)
        {
            return instance.entries.Any(e => e.Count() == 1 && e.Scene() == sceneName);
        }
        
        public static bool IsFavorited(string[] sceneName)
        {
            return instance.entries.Any(e => e.Equals(sceneName));
        }
        
    }
}