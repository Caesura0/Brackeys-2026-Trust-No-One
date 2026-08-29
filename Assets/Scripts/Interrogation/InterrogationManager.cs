using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameData.Models;

namespace GameData.Interrogation
{
    /// <summary>
    /// The core runtime engine for the interrogation gameplay loop.
    /// It manages state (e.g. tracking questions remaining), enforces limits,
    /// retrieves answers from the GameDataRepository, and evaluates final judgments.
    /// It is intentionally NOT a MonoBehaviour or Singleton, making it easily testable.
    /// </summary>
    public class InterrogationManager
    {
        private GameDataRepository _repository;
        private InterrogationSession _session;

        /// <summary>
        /// The list of case IDs defining the order of progression.
        /// </summary>
        public List<string> CaseOrder { get; private set; }

        /// <summary>
        /// The current position in the CaseOrder progression.
        /// </summary>
        public int CurrentCaseIndex { get; private set; }

        // Playthrough Score Tracking
        public int TotalCasesCompleted { get; private set; }
        public int TotalCorrectJudgments { get; private set; }

        // Events
        public event Action<CaseData> OnCaseStarted;
        public event Action<QuestionResponseResult> OnQuestionAsked;
        public event Action OnQuestionLimitReached;
        public event Action OnJudgmentAvailable;
        public event Action<JudgmentResult> OnJudgmentSubmitted;
        public event Action<InterrogationSession> OnInterrogationCompleted;
        public event Action<EndingData, int, int> OnPlaythroughCompleted;

        /// <summary>
        /// Constructs a new InterrogationManager, injecting its dependency on the GameDataRepository
        /// and optionally a custom case progression order.
        /// </summary>
        public InterrogationManager(GameDataRepository repository, List<string> caseOrder = null)
        {
            _repository = repository;

            if (caseOrder != null && caseOrder.Count > 0)
            {
                CaseOrder = new List<string>(caseOrder);
            }
            else
            {
                // Fallback: Populate case order dynamically from the repository, sorted alphabetically
                CaseOrder = _repository.GetAllCases()
                    .Select(c => c.Id)
                    .OrderBy(id => id)
                    .ToList();
            }
            CurrentCaseIndex = 0;
        }

        /// <summary>
        /// Gets the Case ID of the current case in the progression chain.
        /// </summary>
        public string GetCurrentCaseId()
        {
            if (CaseOrder == null || CaseOrder.Count == 0) return null;
            if (CurrentCaseIndex < 0 || CurrentCaseIndex >= CaseOrder.Count) return null;
            return CaseOrder[CurrentCaseIndex];
        }

        /// <summary>
        /// Starts the current case in the progression chain.
        /// </summary>
        public void StartCurrentCase()
        {
            string caseId = GetCurrentCaseId();
            if (string.IsNullOrEmpty(caseId))
            {
                Debug.LogError("[InterrogationManager] Cannot start current case: case order is empty or index is out of range.");
                return;
            }
            StartCase(caseId);
        }

        /// <summary>
        /// Moves progression to the next case. Returns true if there is a next case,
        /// or false if we have reached the end of the order list.
        /// </summary>
        public bool MoveToNextCase()
        {
            if (CaseOrder == null || CaseOrder.Count == 0) return false;
            
            if (CurrentCaseIndex + 1 < CaseOrder.Count)
            {
                CurrentCaseIndex++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the case progression index and playthrough score back to the beginning.
        /// </summary>
        public void ResetProgression()
        {
            CurrentCaseIndex = 0;
            TotalCasesCompleted = 0;
            TotalCorrectJudgments = 0;
        }

        /// <summary>
        /// Initializes a new interrogation session for the specified Case ID.
        /// Resets the question counter and fires the OnCaseStarted event.
        /// </summary>
        public void StartCase(string caseId)
        {
            var caseData = _repository.GetCase(caseId);
            if (caseData == null)
            {
                Debug.LogError($"[InterrogationManager] Cannot start case. Invalid Case ID: {caseId}");
                return;
            }

            _session = new InterrogationSession
            {
                CaseId = caseId,
                QuestionsRemaining = 3,
                State = InterrogationState.InProgress
            };

            OnCaseStarted?.Invoke(caseData);
        }

        public CaseData GetCurrentCase()
        {
            return _session != null ? _repository.GetCase(_session.CaseId) : null;
        }

        public CaseData GetCase(string caseId)
        {
            return _repository.GetCase(caseId);
        }

        public SuspectData GetCurrentSuspect()
        {
            var currentCase = GetCurrentCase();
            return currentCase != null ? _repository.GetSuspect(currentCase.SuspectId) : null;
        }

        public SuspectData GetSuspect(string suspectId)
        {
            return _repository.GetSuspect(suspectId);
        }

        public EvidenceData GetEvidence(string evidenceId)
        {
            return _repository.GetEvidence(evidenceId);
        }

        public List<QuestionData> GetAvailableQuestions()
        {
            if (_session == null || _session.State != InterrogationState.InProgress) return new List<QuestionData>();

            var currentCase = GetCurrentCase();
            if (currentCase == null) return new List<QuestionData>();

            return currentCase.QuestionIds
                .Where(qId => !_session.QuestionsAsked.Contains(qId))
                .Select(qId => _repository.GetQuestion(qId))
                .Where(q => q != null)
                .ToList();
        }

        public int GetQuestionsRemaining()
        {
            return _session?.QuestionsRemaining ?? 0;
        }

        public bool CanAskQuestion(string questionId)
        {
            if (_session == null || _session.State != InterrogationState.InProgress) return false;
            if (_session.QuestionsRemaining <= 0) return false;
            if (_session.QuestionsAsked.Contains(questionId)) return false;

            var currentCase = GetCurrentCase();
            if (currentCase == null || !currentCase.QuestionIds.Contains(questionId)) return false;

            return true;
        }

        /// <summary>
        /// Processes a player's question. Decrements the remaining question count,
        /// determines the correct response based on the suspect's conditional truth logic,
        /// and fires events to notify the UI to update.
        /// </summary>
        public QuestionResponseResult AskQuestion(string questionId)
        {
            if (!CanAskQuestion(questionId))
            {
                Debug.LogWarning($"[InterrogationManager] Request to ask question {questionId} rejected.");
                return null;
            }

            var questionData = _repository.GetQuestion(questionId);
            if (questionData == null)
            {
                Debug.LogError($"[InterrogationManager] Question {questionId} not found in repository.");
                return null;
            }

            _session.QuestionsAsked.Add(questionId);
            _session.QuestionsRemaining--;

            var responseResult = EvaluateResponses(questionData);

            OnQuestionAsked?.Invoke(responseResult);

            if (_session.QuestionsRemaining == 0)
            {
                _session.State = InterrogationState.AwaitingJudgment;
                OnQuestionLimitReached?.Invoke();
                OnJudgmentAvailable?.Invoke();
            }

            return responseResult;
        }

        private QuestionResponseResult EvaluateResponses(QuestionData questionData)
        {
            var currentCase = GetCurrentCase();
            foreach (var response in questionData.Responses)
            {
                if (EvaluateCondition(response.Condition, currentCase.Truth))
                {
                    return new QuestionResponseResult
                    {
                        QuestionId = questionData.Id,
                        QuestionText = questionData.Text,
                        AnswerText = response.Answer,
                        MatchedCondition = response.Condition
                    };
                }
            }

            // Default safe response if no condition matched
            Debug.LogWarning($"[InterrogationManager] No condition matched for question {questionData.Id}. Returning default safe response.");
            return new QuestionResponseResult
            {
                QuestionId = questionData.Id,
                QuestionText = questionData.Text,
                AnswerText = "...",
                MatchedCondition = "NONE"
            };
        }

        private bool EvaluateCondition(string conditionStr, Dictionary<string, object> truth)
        {
            if (string.IsNullOrWhiteSpace(conditionStr)) return true;

            // Simple parser: split by " == " or " != "
            bool isEquals = conditionStr.Contains("==");
            bool isNotEquals = conditionStr.Contains("!=");
            
            if (!isEquals && !isNotEquals)
            {
                Debug.LogWarning($"[InterrogationManager] Invalid condition format: {conditionStr}. Only == and != are supported.");
                return false;
            }

            string[] parts = isEquals 
                ? conditionStr.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries)
                : conditionStr.Split(new[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                Debug.LogWarning($"[InterrogationManager] Unparsable condition: {conditionStr}");
                return false;
            }

            string factKey = parts[0].Trim();
            string expectedValueStr = parts[1].Trim().Trim('"', '\''); // Handle string quotes
            
            if (!truth.TryGetValue(factKey, out object actualValueObj))
            {
                if (expectedValueStr.ToLower() == "null")
                {
                    return isEquals;
                }
                return isNotEquals;
            }

            if (actualValueObj == null)
            {
                return (expectedValueStr.ToLower() == "null") == isEquals;
            }

            string actualValueStr = actualValueObj.ToString();

            // Compare as strings case-insensitively to simplify boolean checking (true/false)
            bool valuesMatch = string.Equals(actualValueStr, expectedValueStr, StringComparison.OrdinalIgnoreCase);

            return isEquals ? valuesMatch : !valuesMatch;
        }

        public bool CanSubmitJudgment()
        {
            return _session != null && _session.State == InterrogationState.AwaitingJudgment;
        }

        /// <summary>
        /// Submits the final judgment (Guilty/Innocent) and evaluates it against
        /// the actual truth stored in the Suspect's JSON data. 
        /// Fires OnJudgmentSubmitted and OnInterrogationCompleted.
        /// </summary>
        public JudgmentResult SubmitJudgment(Judgment judgment)
        {
            if (!CanSubmitJudgment())
            {
                Debug.LogWarning("[InterrogationManager] Cannot submit judgment at this time.");
                return null;
            }

            var currentCase = GetCurrentCase();
            bool actualIsHeretic = false;
            
            if (currentCase.Truth.TryGetValue("isHeretic", out object isHereticObj))
            {
                if (isHereticObj is bool b) actualIsHeretic = b;
                else if (isHereticObj is string s && bool.TryParse(s, out bool sb)) actualIsHeretic = sb;
            }
            else
            {
                Debug.LogError($"[InterrogationManager] Case {currentCase.Id} truth is missing 'isHeretic' value!");
            }

            Judgment actualJudgment = actualIsHeretic ? Judgment.Guilty : Judgment.Innocent;
            bool wasCorrect = judgment == actualJudgment;

            JudgmentOutcome outcome;
            if (wasCorrect)
            {
                outcome = judgment == Judgment.Guilty ? JudgmentOutcome.CorrectGuilty : JudgmentOutcome.CorrectInnocent;
                TotalCorrectJudgments++;
            }
            else
            {
                outcome = judgment == Judgment.Guilty ? JudgmentOutcome.InnocentExecuted : JudgmentOutcome.HereticReleased;
            }
            
            TotalCasesCompleted++;

            var result = new JudgmentResult
            {
                PlayerJudgment = judgment,
                ActualJudgment = actualJudgment,
                WasCorrect = wasCorrect,
                Outcome = outcome,
                SuspectId = currentCase.SuspectId,
                CaseId = currentCase.Id
            };

            _session.PlayerJudgment = judgment;
            _session.Result = result;
            _session.State = InterrogationState.Completed;

            OnJudgmentSubmitted?.Invoke(result);
            OnInterrogationCompleted?.Invoke(_session);

            return result;
        }

        public InterrogationState GetState()
        {
            return _session?.State ?? InterrogationState.NotStarted;
        }

        /// <summary>
        /// Evaluates the playthrough ending based on the total correct judgments and fires OnPlaythroughCompleted.
        /// </summary>
        public void CompletePlaythrough()
        {
            EndingData earnedEnding = null;

            foreach (var ending in _repository.GetAllEndings())
            {
                int minCorrect = 0;
                int maxCorrect = 999;
                
                if (ending.Conditions != null)
                {
                    if (ending.Conditions.TryGetValue("minCorrect", out int min)) minCorrect = min;
                    if (ending.Conditions.TryGetValue("maxCorrect", out int max)) maxCorrect = max;
                }

                if (TotalCorrectJudgments >= minCorrect && TotalCorrectJudgments <= maxCorrect)
                {
                    earnedEnding = ending;
                    break;
                }
            }

            if (earnedEnding == null)
            {
                // Fallback ending if none match
                earnedEnding = new EndingData
                {
                    Id = "ENDING_UNKNOWN",
                    Name = "UNKNOWN OUTCOME",
                    Description = "The Inquisition's records are incomplete."
                };
            }

            OnPlaythroughCompleted?.Invoke(earnedEnding, TotalCorrectJudgments, TotalCasesCompleted);
        }
    }
}
