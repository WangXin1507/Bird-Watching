using System.Linq;
using Extensions.Ids;
using Extensions.Logic;
using QuestSystem;
using UnityEngine;

namespace BirdWatching.Quests
{
    /**
     * <summary>
     * Starts the quest service before the first scene loads and binds it to the quest runtime, so quest triggers and
     * reactions in any scene can reach it. Replaces the old QuestManager singleton.
     * </summary>
     * <remarks>
     * Lives at Resources/QuestSetup. Every QuestDefinition in the asset catalog is loaded, so a new quest only needs
     * to be created; the editor adds it to the catalog on import.
     * </remarks>
     */
    [CreateAssetMenu(fileName = "QuestSetup", menuName = "Quests/Quest Setup")]
    public class QuestSetup : ScriptableObject
    {
        private const string ResourcePath = "QuestSetup";

        [Tooltip("The runtime asset that quest triggers and reactions in scenes reference.")]
        public QuestRuntime runtime;

        [Tooltip("Every asset with an asset id. The editor keeps it up to date.")]
        public AssetCatalog catalog;

        public static QuestService Service { get; private set; }
        public static FactRegistry Facts { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartQuests()
        {
            var setup = Resources.Load<QuestSetup>(ResourcePath);
            if (setup == null || setup.runtime == null || setup.catalog == null)
            {
                Debug.LogError("[QuestSetup] Resources/QuestSetup is missing, or has no runtime or catalog. Quests won't run.");
                return;
            }

            Service?.Dispose();

            var quests = setup.catalog.Assets.OfType<QuestDefinition>().ToList();
            Facts = new FactRegistry();
            Service = new QuestService(Facts, quests, setup.catalog);
            Service.NewGame(); // quests aren't saved yet, so every run starts fresh
            setup.runtime.Bind(Service);

            Application.quitting += () =>
            {
                setup.runtime.Unbind(Service);
                Service?.Dispose();
                Service = null;
            };
        }
    }
}
