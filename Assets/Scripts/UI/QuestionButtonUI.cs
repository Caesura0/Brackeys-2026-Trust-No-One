using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace GameData.UI
{
    /// <summary>
    /// A simple, dumb presentation component for a Question Button.
    /// It is responsible only for displaying question text and forwarding clicks to a callback.
    /// It knows nothing about the InterrogationManager or game state.
    /// </summary>
    public class QuestionButtonUI : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _text;
        private string _questionId;
        private Action<string> _onClickCallback;

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            // Assume the text is a child named "Text" based on the static UI generation
            Transform textTransform = transform.Find("Text");
            if (textTransform != null)
            {
                _text = textTransform.GetComponent<TextMeshProUGUI>();
            }

            if (_button != null)
            {
                // To prevent lambda subscription bugs when the question list refreshes,
                // we clear all old listeners before adding the primary HandleClick listener.
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }
            else
            {
                Debug.LogError($"[QuestionButtonUI] No Button component found on {gameObject.name}. Is this script attached to the wrong object?");
            }
        }

        public void SetQuestion(string questionId, string text, Action<string> onClickCallback)
        {
            _questionId = questionId;
            
            if (_text != null)
            {
                _text.text = text;
            }

            _onClickCallback = onClickCallback;
            gameObject.SetActive(true);
        }


        private void HandleClick()
        {
            if (string.IsNullOrEmpty(_questionId)) return;
            
            // Fire the callback, passing the stored Question ID up to the UI Controller.
            _onClickCallback?.Invoke(_questionId);
        }
    }
}
