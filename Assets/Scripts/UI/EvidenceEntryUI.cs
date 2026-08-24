using UnityEngine;
using TMPro;
using GameData.Models;

namespace GameData.UI
{

    public class EvidenceEntryUI : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            // Locates the child "Text" element 
            Transform textTransform = transform.Find("Text");
            if (textTransform != null)
            {
                _text = textTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        public void SetEvidence(EvidenceData evidence)
        {
            if (_text != null && evidence != null)
            {
                _text.text = $"- {evidence.Text}";
            }
            gameObject.SetActive(true);
        }
    }
}
