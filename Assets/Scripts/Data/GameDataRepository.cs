using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using GameData.Models;

namespace GameData
{
    public class GameDataRepository
    {
        private Dictionary<string, CaseData> _cases = new Dictionary<string, CaseData>();
        private Dictionary<string, SuspectData> _suspects = new Dictionary<string, SuspectData>();
        private Dictionary<string, QuestionData> _questions = new Dictionary<string, QuestionData>();
        private Dictionary<string, EvidenceData> _evidence = new Dictionary<string, EvidenceData>();
        private Dictionary<string, EndingData> _endings = new Dictionary<string, EndingData>();

        public void LoadAllData()
        {

            LoadCategory("Cases", _cases);
            LoadCategory("Suspects", _suspects);
            LoadCategory("Questions", _questions);
            LoadCategory("Evidence", _evidence);
            LoadCategory("Endings", _endings);
            
            Debug.Log($"Loaded {_cases.Count} cases, {_suspects.Count} suspects, {_questions.Count} questions, {_evidence.Count} evidence, {_endings.Count} endings.");
        }

        private void LoadCategory<T>(string folderName, Dictionary<string, T> dictionary)
        {
            TextAsset[] textAssets = Resources.LoadAll<TextAsset>($"GameData/{folderName}");
            if (textAssets == null || textAssets.Length == 0)
            {
                Debug.LogWarning($"[GameDataRepository] No TextAssets found in Resources/GameData/{folderName}");
                return;
            }

            foreach (var textAsset in textAssets)
            {
                try
                {
                    string json = textAsset.text;
                    T data = JsonConvert.DeserializeObject<T>(json);
                    
                    // Use reflection to get the 'Id' property and use it as key
                    var idProp = typeof(T).GetProperty("Id");
                    if (idProp != null)
                    {
                        string id = idProp.GetValue(data) as string;
                        if (!string.IsNullOrEmpty(id))
                        {
                            if (!dictionary.ContainsKey(id))
                            {
                                dictionary[id] = data;
                            }
                            else
                            {
                                Debug.LogError($"Duplicate ID found: {id} in {folderName}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading {textAsset.name}: {e.Message}");
                }
            }
        }

        public CaseData GetCase(string id) => _cases.TryGetValue(id, out var data) ? data : null;
        public SuspectData GetSuspect(string id) => _suspects.TryGetValue(id, out var data) ? data : null;
        public QuestionData GetQuestion(string id) => _questions.TryGetValue(id, out var data) ? data : null;
        public EvidenceData GetEvidence(string id) => _evidence.TryGetValue(id, out var data) ? data : null;
        public EndingData GetEnding(string id) => _endings.TryGetValue(id, out var data) ? data : null;
        
        public IEnumerable<CaseData> GetAllCases() => _cases.Values;
        public IEnumerable<SuspectData> GetAllSuspects() => _suspects.Values;
        public IEnumerable<QuestionData> GetAllQuestions() => _questions.Values;
        public IEnumerable<EvidenceData> GetAllEvidence() => _evidence.Values;
        public IEnumerable<EndingData> GetAllEndings() => _endings.Values;
    }
}
