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

            // Define where our JSON data is stored at runtime.
            string dataPath = Path.Combine(Application.streamingAssetsPath, "GameData");
            if (!Directory.Exists(dataPath))
            {
                Debug.LogError($"[InterrogationBootstrapper] GameData directory not found at: {dataPath}");
                return;
            }

            // 1. Initialize the Repository which handles all JSON file reading and schema loading.
            var repository = new GameDataRepository();
            repository.LoadAllData();
            
            // 2. Initialize the InterrogationManager which handles all gameplay logic, question limits, and truth evaluation.
            var manager = new InterrogationManager(repository);

            // 3. Inject the manager into the UI so the UI can act as a pure presentation layer.
            UIController.Initialize(manager);

            // 4. Start the initial test case. Verify it exists to prevent silent failures if the JSON is missing or misnamed.
            if (repository.GetCase("CASE_001") == null)
            {
                Debug.LogError("[InterrogationBootstrapper] CASE_001 does not exist in the repository!");
                return;
            }

            // Instruct the UI to display the intro panel for first Case using the data from the repository.
            UIController.ShowIntroFromData("CASE_001");
        }
    }
}
