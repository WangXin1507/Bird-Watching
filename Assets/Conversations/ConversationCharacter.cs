using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdWatching.Conversation
{
    [RequireComponent(typeof(UIElement))]
    public class ConversationCharacter : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] TextMeshProUGUI text;
        UIElement element;

        public void SetCharacterSpeech(Sprite portrait, string text, float duration = 0)
        {
            image.sprite = portrait;
            this.text.text = text;

            element.Show(duration);
        }

        public void HideCharacterSpeech()
        {
            image.sprite = null;
            text.text = "";

            element.Hide();
        }

        void Awake()
        {
            element = GetComponent<UIElement>();
        }
    }
}