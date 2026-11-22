using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPRPG
{
    public class ListEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Button button;
        public Text label;
        public Text detail;

        public Action onClick;
        public Action onHighlight;
        public Action onUnhighlight;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Setup(string text, string detailText, bool enabled)
        {
            if (label != null) label.text = text;
            if (detail != null) detail.text = detailText;
            if (button != null) button.interactable = enabled;
        }

        public void OnPointerEnter(PointerEventData e) => onHighlight?.Invoke();
        public void OnPointerExit(PointerEventData e) => onUnhighlight?.Invoke();
        public void OnSelect(BaseEventData e) => onHighlight?.Invoke();
        public void OnDeselect(BaseEventData e) => onUnhighlight?.Invoke();
    }
}
