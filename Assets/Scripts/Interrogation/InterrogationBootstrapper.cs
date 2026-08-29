using UnityEngine;
using System.IO;

namespace GameData.UI
{
    using Interrogation;

    /// <summary>
    /// Acts as the entry point for the Interrogation scene.
    /// </summary>
    public class InterrogationBootstrapper : MonoBehaviour
    {
        [Tooltip("The main UI controller that handles all interrogation panel transitions.")]
        public InterrogationUIController UIController;

        [Tooltip("Configure custom case progression order. If left empty, all loaded cases will be used in alphabetical order.")]
        [GameData.Utils.CaseId]
        public System.Collections.Generic.List<string> CaseOrder = new System.Collections.Generic.List<string>();

        private void Start()
        {
            // Fallback: If the UIController isn't assigned in the inspector, find it in the scene.
            if (UIController == null)
            {
                UIController = FindAnyObjectByType<InterrogationUIController>(FindObjectsInactive.Include);
                if (UIController == null)
                {
                    Debug.LogError("[InterrogationBootstrapper] No InterrogationUIController found in scene!");
                    return;
                }
            }

            // JSON data is now loaded securely from the Resources folder by the GameDataRepository.

            // 1. Initialize the Repository which handles all JSON file reading and schema loading.
            var repository = new GameDataRepository();
            repository.LoadAllData();
            
            // 2. Initialize the InterrogationManager with the configured case order.
            var manager = new InterrogationManager(repository, CaseOrder);

            // 3. Inject the manager into the UI so the UI can act as a pure presentation layer.
            UIController.Initialize(manager);
            
            // 4. Start the initial case from the manager's progression order list.
            string firstCaseId = manager.GetCurrentCaseId();
            if (string.IsNullOrEmpty(firstCaseId))
            {
                Debug.LogError("[InterrogationBootstrapper] No cases are defined or loaded in progression!");
                return;
            }

            // Instruct the UI to display the intro panel for the first case.
            UIController.ShowIntroFromData(firstCaseId);
        }
    }
}
