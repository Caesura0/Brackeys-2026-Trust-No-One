#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameData.Utils;

namespace GameData.Editor
{
    [CustomPropertyDrawer(typeof(CaseIdAttribute))]
    public class CaseIdDrawer : PropertyDrawer
    {
        private string[] _caseIds;
        private bool _isLoaded = false;

        private void LoadCaseIds()
        {
            if (_isLoaded) return;
            
            var ids = new List<string>();
            string folder = Path.Combine(Application.dataPath, "Resources", "GameData", "Cases");
            
            if (Directory.Exists(folder))
            {
                foreach(var file in Directory.GetFiles(folder, "*.json"))
                {
                    // Fast regex parse to find the "id" value without full deserialization
                    string content = File.ReadAllText(file);
                    var match = Regex.Match(content, @"""id""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        ids.Add(match.Groups[1].Value);
                    }
                    else
                    {
                        // Fallback to filename if "id" property is missing
                        ids.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            }

            if (ids.Count == 0) ids.Add("NO CASES FOUND");
            
            ids.Sort();
            _caseIds = ids.ToArray();
            _isLoaded = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[CaseId] can only be used on string properties.", MessageType.Error);
                return;
            }

            LoadCaseIds();

            // Find current index
            int currentIndex = Mathf.Max(0, System.Array.IndexOf(_caseIds, property.stringValue));
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _caseIds);
            
            if (EditorGUI.EndChangeCheck() || string.IsNullOrEmpty(property.stringValue))
            {
                if (_caseIds.Length > 0 && _caseIds[0] != "NO CASES FOUND")
                {
                    property.stringValue = _caseIds[newIndex];
                }
            }
        }
    }
}
#endif
