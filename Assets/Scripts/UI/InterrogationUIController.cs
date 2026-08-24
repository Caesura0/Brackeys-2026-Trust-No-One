using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData.Interrogation;
using GameData.Models;
using System.Collections.Generic;

using System.Collections;

namespace GameData.UI
{
    /// <summary>
    /// The primary orchestrator for the Interrogation UI.
    /// This acts strictly as a "dumb" presentation layer. It does not calculate
    /// gameplay logic, count questions, or evaluate truth. It simply toggles panels
    /// and populates text fields in response to events fired by the InterrogationManager.
    /// </summary>
    public class InterrogationUIController : MonoBehaviour
    {
        private InterrogationManager _manager;

        [Header("Panels")]
        public GameObject CaseIntroPanel;
        public GameObject InterrogationPanel;
        public GameObject JudgmentPanel;
        public GameObject QuestionPanel;
        public GameObject ResultPanel;

        [Header("Case Intro")]
        public TextMeshProUGUI IntroCaseNumberText;
        public TextMeshProUGUI IntroSuspectInfoText;
        public TextMeshProUGUI IntroAccusationText;
        public TextMeshProUGUI IntroEvidenceSummaryText;
        public Button BeginButton;

        [Header("Header")]
        public TextMeshProUGUI HeaderCaseNumberText;
        public TextMeshProUGUI QuestionsRemainingText;

        [Header("Suspect")]
        public TextMeshProUGUI SuspectNameText;
        public TextMeshProUGUI SuspectAgeText;
        public TextMeshProUGUI SuspectOccupationText;
        public TextMeshProUGUI SuspectDescriptionText;

        [Header("Evidence")]
        public Transform EvidenceContainer;

        [Header("Conversation")]
        public TextMeshProUGUI TranscriptText;

        [Header("Questions")]
        public Transform QuestionButtonContainer;

        [Header("Judgment")]
        public Button GuiltyButton;
        public Button InnocentButton;

        [Header("Results")]
        public TextMeshProUGUI ResultTitleText;
        public TextMeshProUGUI ResultDescriptionText;
        public Button ContinueButton;


        private void Awake()
        {

            if (BeginButton != null) BeginButton.onClick.AddListener(OnBeginClicked);
            else Debug.LogError("[InterrogationUIController] BeginButton is not assigned in the Inspector!");

            if (GuiltyButton != null) GuiltyButton.onClick.AddListener(() => OnJudgmentClicked(Judgment.Guilty));
            else Debug.LogError("[InterrogationUIController] GuiltyButton is not assigned in the Inspector!");

            if (InnocentButton != null) InnocentButton.onClick.AddListener(() => OnJudgmentClicked(Judgment.Innocent));
            else Debug.LogError("[InterrogationUIController] InnocentButton is not assigned in the Inspector!");

            if (ContinueButton != null) ContinueButton.onClick.AddListener(OnContinueClicked);
            else Debug.LogError("[InterrogationUIController] ContinueButton is not assigned in the Inspector!");
        }



        /// <summary>
        /// Injects the runtime InterrogationManager and sets up all event subscriptions.
        /// This ensures the UI updates strictly in response to the core game logic state changes.
        /// </summary>
        public void Initialize(InterrogationManager manager)
        {
            _manager = manager;

            _manager.OnCaseStarted += HandleCaseStarted;
            _manager.OnQuestionAsked += HandleQuestionAsked;
            _manager.OnQuestionLimitReached += HandleQuestionLimitReached;
            _manager.OnJudgmentAvailable += HandleJudgmentAvailable;
            _manager.OnJudgmentSubmitted += HandleJudgmentSubmitted;

        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCaseStarted -= HandleCaseStarted;
                _manager.OnQuestionAsked -= HandleQuestionAsked;
                _manager.OnQuestionLimitReached -= HandleQuestionLimitReached;
                _manager.OnJudgmentAvailable -= HandleJudgmentAvailable;
                _manager.OnJudgmentSubmitted -= HandleJudgmentSubmitted;

            }
        }

        /// <summary>
        /// Loads case data through the read-only API of the manager to populate
        /// the Intro Panel before the interrogation actually begins.
        /// </summary>
        public void ShowIntroFromData(string caseId)
        {
            var caseData = _manager.GetCase(caseId);
            if (caseData == null)
            {
                Debug.LogError($"[InterrogationUIController] Case {caseId} not found!");
                return;
            }

            var suspectData = _manager.GetSuspect(caseData.SuspectId);
            
            var evidenceList = new List<EvidenceData>();
            foreach (var eid in caseData.EvidenceIds)
            {
                var ev = _manager.GetEvidence(eid);
                if (ev != null) evidenceList.Add(ev);
            }

            ShowIntro(caseData, suspectData, evidenceList);
        }

        /// <summary>
        /// Populates the Intro Panel fields with the retrieved data.
        /// </summary>
        private void ShowIntro(CaseData caseData, SuspectData suspectData, List<EvidenceData> evidenceList)
        {
            CaseIntroPanel.SetActive(true);
            InterrogationPanel.SetActive(false);
            ResultPanel.SetActive(false);

            IntroCaseNumberText.text = $"CASE #{caseData.Id.Replace("CASE_", "")}";
            
            string suspectName = suspectData != null ? suspectData.Name.ToUpper() : "UNKNOWN";
            string suspectAge = suspectData != null ? suspectData.Age.ToString() : "??";
            string suspectOccupation = suspectData != null ? suspectData.Occupation.ToUpper() : "UNKNOWN";
            
            IntroSuspectInfoText.text = $"{suspectName}\nAGE: {suspectAge}\nOCCUPATION: {suspectOccupation}";
            IntroAccusationText.text = caseData.Accusation;
            
            string evSummary = "";
            foreach(var ev in evidenceList)
            {
                evSummary += $"● {ev.Text}\n";
            }
            IntroEvidenceSummaryText.text = evSummary;
        }

        private void OnBeginClicked()
        {
            _manager.StartCase("CASE_001"); // Hardcoded for this testing phase
        }

        /// <summary>
        /// Triggered when the InterrogationManager formally starts the case.
        /// Flips the UI from the Intro Panel to the Active Interrogation Panel.
        /// </summary>
        private void HandleCaseStarted(CaseData caseData)
        {
            CaseIntroPanel.SetActive(false);
            InterrogationPanel.SetActive(true);
            ResultPanel.SetActive(false);
            QuestionPanel.SetActive(true);
            JudgmentPanel.SetActive(false);

            HeaderCaseNumberText.text = $"CASE #{caseData.Id.Replace("CASE_", "")}";
            UpdateQuestionsRemaining();

            var suspect = _manager.GetCurrentSuspect();
            if (suspect != null)
            {
                SuspectNameText.text = suspect.Name.ToUpper();
                SuspectAgeText.text = $"AGE: {suspect.Age}";
                SuspectOccupationText.text = $"OCCUPATION: {suspect.Occupation.ToUpper()}";
                SuspectDescriptionText.text = suspect.Description;
            }

            TranscriptText.text = "";
            PopulateEvidence(caseData);
            RefreshQuestions();

            AppendToTranscript("<b>INTERROGATION STARTED...</b>");
        }

        private void PopulateEvidence(CaseData caseData)
        {
            if (EvidenceContainer == null) return;

            // Clear previous evidence (hide existing entries)
            foreach(Transform child in EvidenceContainer)
            {
                var entry = child.GetComponent<EvidenceEntryUI>();
                if (entry != null)
                {
                    entry.gameObject.SetActive(false);
                }
            }

            int evIndex = 0;
            foreach(var eid in caseData.EvidenceIds)
            {
                var ev = _manager.GetEvidence(eid);
                if (ev != null)
                {
                    EvidenceEntryUI entry;
                    if (evIndex < EvidenceContainer.childCount)
                    {
                        var child = EvidenceContainer.GetChild(evIndex);
                        entry = child.GetComponent<EvidenceEntryUI>();
                        if (entry == null) entry = child.gameObject.AddComponent<EvidenceEntryUI>();
                    }
                    else
                    {
                        // Fallback: we shouldn't really hit this with our pre-generated UI-01 unless there are >3 evidences
                        var newObj = new GameObject($"EvidenceEntry_{evIndex:00}");
                        newObj.transform.SetParent(EvidenceContainer, false);
                        entry = newObj.AddComponent<EvidenceEntryUI>();
                    }
                    
                    entry.SetEvidence(ev);
                    evIndex++;
                }
            }
        }

        /// <summary>
        /// Retrieves the list of available (unasked) questions from the Manager
        /// and maps them to the generated QuestionButtonUI components.
        /// </summary>
        private void RefreshQuestions()
        {
            if (QuestionButtonContainer == null) return;

            var available = _manager.GetAvailableQuestions();
            
            // Re-using the pre-generated buttons from UI-01
            var buttonTransforms = new List<Transform>();
            foreach (Transform child in QuestionButtonContainer)
            {
                buttonTransforms.Add(child);
            }

            for (int i = 0; i < buttonTransforms.Count; i++)
            {
                var btnUI = buttonTransforms[i].GetComponent<QuestionButtonUI>();
                if (btnUI == null) btnUI = buttonTransforms[i].gameObject.AddComponent<QuestionButtonUI>();

                if (i < available.Count)
                {
                    var q = available[i];
                    btnUI.SetQuestion(q.Id, q.Text, OnQuestionButtonClicked);
                }
                else
                {
                    btnUI.gameObject.SetActive(false);
                }
            }
        }

        private void OnQuestionButtonClicked(string questionId)
        {
            _manager.AskQuestion(questionId);
        }

        /// <summary>
        /// Fired when the Manager successfully processes a question.
        /// Appends the Inquisitor's question and the Suspect's response to the scrolling transcript.
        /// </summary>
        private void HandleQuestionAsked(QuestionResponseResult result)
        {
            string suspectName = _manager.GetCurrentSuspect()?.Name ?? "SUSPECT";
            
            string questionPart = $"\n\n<b>INQUISITOR</b>\n{result.QuestionText}";
            string answerPart = $"\n\n<b>{suspectName.ToUpper()}</b>\n{result.AnswerText}";
            
            UpdateQuestionsRemaining();
            RefreshQuestions();

            AppendToTranscript(questionPart, answerPart);
        }

        /// <summary>
        /// Fired when the Manager dictates the question limit has been reached.
        /// Hides the question panel and reveals the judgment buttons.
        /// </summary>
        private void HandleQuestionLimitReached()
        {
            UpdateQuestionsRemaining();
            QuestionPanel.SetActive(false);
        }

        private void HandleJudgmentAvailable()
        {
            JudgmentPanel.SetActive(true);
            // We intentionally do not enable the buttons here.
            // The TypewriterRoutine will enable them when it finishes typing the final response.
        }

        private void OnJudgmentClicked(Judgment j)
        {
            // The UI does not determine if the player is correct.
            // It simply forwards their decision to the manager.
            _manager.SubmitJudgment(j);
        }

        /// <summary>
        /// Fired when the Manager finishes evaluating the player's judgment.
        /// Maps the four possible outcomes (Correct/Incorrect Guilty/Innocent) to presentation text.
        /// </summary>
        private void HandleJudgmentSubmitted(JudgmentResult result)
        {
            InterrogationPanel.SetActive(false);
            ResultPanel.SetActive(true);

            switch (result.Outcome)
            {
                case JudgmentOutcome.CorrectGuilty:
                    ResultTitleText.text = "HERETIC CONFIRMED";
                    ResultTitleText.color = new Color(0.4f, 0.13f, 0.13f); // Muted red
                    ResultDescriptionText.text = "Your judgment was correct.";
                    break;
                case JudgmentOutcome.CorrectInnocent:
                    ResultTitleText.text = "INNOCENT RELEASED";
                    ResultTitleText.color = new Color(0.13f, 0.33f, 0.2f); // Muted green
                    ResultDescriptionText.text = "Your judgment was correct.";
                    break;
                case JudgmentOutcome.InnocentExecuted:
                    ResultTitleText.text = "INNOCENT EXECUTED";
                    ResultTitleText.color = new Color(0.4f, 0.13f, 0.13f);
                    ResultDescriptionText.text = "The suspect was not a heretic.";
                    break;
                case JudgmentOutcome.HereticReleased:
                    ResultTitleText.text = "HERETIC RELEASED";
                    ResultTitleText.color = new Color(0.4f, 0.13f, 0.13f);
                    ResultDescriptionText.text = "Your judgment was incorrect.";
                    break;
            }
        }

        //private void HandleInterrogationCompleted(InterrogationSession session)
        //{
        //    // Already handled via HandleJudgmentSubmitted
        //}

        private void OnContinueClicked()
        {
            ShowIntroFromData("CASE_001");//Change this to look for all cases.
        }

        private void UpdateQuestionsRemaining()
        {
            QuestionsRemainingText.text = $"QUESTIONS: {_manager.GetQuestionsRemaining()} / 3";
        }

        private Coroutine _typewriterCoroutine;
        private float _typewriterSpeed = 0.02f; // Seconds per character

        private void AppendToTranscript(params string[] textBlocks)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutineBlocks(textBlocks));
        }

        private IEnumerator TypewriterRoutineBlocks(string[] blocks)
        {
            // Disable interaction while typing
            SetButtonsInteractable(false);
            var scroll = TranscriptText.GetComponentInParent<ScrollRect>();
            bool skipped = false;

            for (int i = 0; i < blocks.Length; i++)
            {
                // Ensure TMP has parsed current text to get an accurate character count before appending
                TranscriptText.ForceMeshUpdate();
                int startCount = TranscriptText.textInfo.characterCount;

                TranscriptText.text += blocks[i];
                
                // Force mesh update again to layout the newly added text
                TranscriptText.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                
                int endCount = TranscriptText.textInfo.characterCount;
                TranscriptText.maxVisibleCharacters = startCount;

                if (!skipped)
                {
                    while (TranscriptText.maxVisibleCharacters < endCount)
                    {
                        if (CheckSkipInput()) { skipped = true; break; }
                        
                        TranscriptText.maxVisibleCharacters += 1;
                        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
                        yield return new WaitForSeconds(_typewriterSpeed);
                    }
                }
                
                // Ensure this block's characters are fully visible (important if we skipped)
                TranscriptText.maxVisibleCharacters = endCount;
                if (scroll != null) scroll.verticalNormalizedPosition = 0f;

                // Dramatic pause between blocks (e.g. between question and answer)
                if (!skipped && i < blocks.Length - 1)
                {
                    float pauseTimer = 1.0f; // 1 second pause
                    while (pauseTimer > 0)
                    {
                        if (CheckSkipInput()) { skipped = true; break; }
                        pauseTimer -= Time.deltaTime;
                        yield return null;
                    }
                }
            }
            
            // Ensure everything is visible at the very end
            TranscriptText.maxVisibleCharacters = 99999;
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;

            // Re-enable interaction
            SetButtonsInteractable(true);
        }

        private bool CheckSkipInput()
        {
            bool skip = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) skip = true;
            if (UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)) skip = true;
#else
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) skip = true;
#endif
            return skip;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            // If we have questions remaining, toggle the question buttons
            if (_manager.GetQuestionsRemaining() > 0 && QuestionButtonContainer != null)
            {
                foreach (Transform child in QuestionButtonContainer)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null) btn.interactable = interactable;
                }
            }
            
            // If judgment is available, toggle the judgment buttons
            if (_manager.CanSubmitJudgment())
            {
                if (GuiltyButton != null) GuiltyButton.interactable = interactable;
                if (InnocentButton != null) InnocentButton.interactable = interactable;
            }
            else
            {
                // Force disable if we can't submit judgment yet
                if (GuiltyButton != null) GuiltyButton.interactable = false;
                if (InnocentButton != null) InnocentButton.interactable = false;
            }
        }
    }
}
