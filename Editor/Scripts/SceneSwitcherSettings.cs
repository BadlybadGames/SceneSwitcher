using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BBG.SceneSwitcher.Editor
{
    [FilePath("BBG/SceneSwitcher/SceneSwitcherSettings.ini", FilePathAttribute.Location.ProjectFolder)]
    public class SceneSwitcherSettings : ScriptableSingleton<SceneSwitcherSettings>
    {
        private List<string> favoriteSceneGUIDS = new List<string>();

        public static event Action changed;

        public string[] FavoriteSceneGuids
        {
            get { return favoriteSceneGUIDS.ToArray(); }
            private set { favoriteSceneGUIDS = value.ToList(); }
        }

        public void AddEntry(string entry)
        {
            favoriteSceneGUIDS.Add(entry);    
            changed?.Invoke();
        }
        
        public void RemoveEntry(string entry)
        {
            favoriteSceneGUIDS.Remove(entry);
            changed?.Invoke();
        }

        public static void Save()
        {
            instance.Save(false);
        }

        public static void ClearDataAndSave()
        {
            instance.favoriteSceneGUIDS.Clear();
            Save();
            changed?.Invoke();
        }
        
    }
}