using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace QuestSystem.EditorTools
{
    /**
     * <summary>Edits several selected quests at once: kind, giver and place, each one Undo step. A field shows the shared
     * value, or a mixed value when the quests differ.</summary>
     */
    public sealed class BulkEditPanel : VisualElement
    {
        #region Private Fields

        /** <summary>A picker's entry for quests that differ, or have none; picking it changes nothing.</summary> */
        private const string Mixed = "—";

        private List<QuestDefinition> _quests = new List<QuestDefinition>();

        #endregion

        #region Properties

        /** <summary>Raised after an edit, with the quests changed.</summary> */
        public event Action<IReadOnlyList<QuestDefinition>> OnEdited = delegate { };

        #endregion

        #region Public API

        /** <summary>Makes a hidden panel.</summary> */
        public BulkEditPanel()
        {
            AddToClassList("quest-graph__bulk");
            style.display = DisplayStyle.None;
        }

        /** <summary>Shows the panel for two or more quests, choosing among the kinds given; hides it otherwise.</summary> */
        public void Show(IReadOnlyCollection<QuestDefinition> quests, IReadOnlyList<QuestKind> kinds)
        {
            Clear();
            _quests = quests.Where(quest => quest != null).ToList();
            style.display = _quests.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_quests.Count < 2) return;
            Add(new Label($"{_quests.Count} quests"));
            Add(KindPicker(kinds));
            Add(AssetPicker("Giver", typeof(IQuestGiverAsset), quest => quest.GiverAsset, QuestEdits.SetGiver));
            Add(AssetPicker("Place", typeof(IQuestPlaceAsset), quest => quest.PlaceAsset, QuestEdits.SetPlace));
        }

        #endregion

        #region Helpers

        /** <summary>A popup of every kind by position, so kinds sharing a name stay apart.</summary> */
        private PopupField<int> KindPicker(IReadOnlyList<QuestKind> kinds)
        {
            var choices = Enumerable.Range(-1, kinds.Count + 1).ToList();
            var shared = _quests.Select(quest => quest.Kind).Distinct().ToList();
            var current = shared.Count == 1 && shared[0] != null ? IndexOf(kinds, shared[0]) : -1;
            var field = new PopupField<int>("Kind", choices, current + 1, index => index < 0 ? Mixed : QuestStates.NameOf(kinds[index]), index => index < 0 ? Mixed : QuestStates.NameOf(kinds[index]));
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 0) return;
                QuestEdits.SetKind(_quests, kinds[evt.newValue]);
                OnEdited(_quests);
            });
            return field;
        }

        /** <summary>A popup of the assets that are <paramref name="type" />, None first.</summary> */
        private PopupField<int> AssetPicker(string label, Type type, Func<QuestDefinition, Object> read, Func<IEnumerable<QuestDefinition>, Object, bool> write)
        {
            var choices = AssetChoices.Of(type);
            var shared = _quests.Select(quest => AssetChoices.Real(read(quest))).Distinct().ToList();
            var current = shared.Count == 1 ? choices.IndexOf(shared[0]) : -1;
            string Name(int index) => index < 0 ? Mixed : choices.Labels[index].text;
            var field = new PopupField<int>(label, Enumerable.Range(-1, choices.Assets.Count + 1).ToList(), current + 1, Name, Name);
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 0 || !write(_quests, choices.Assets[evt.newValue])) return;
                OnEdited(_quests);
            });
            return field;
        }

        private static int IndexOf(IReadOnlyList<QuestKind> kinds, QuestKind kind)
        {
            for (var i = 0; i < kinds.Count; i++)
                if (kinds[i] == kind) return i;
            return -1;
        }

        #endregion
    }
}
