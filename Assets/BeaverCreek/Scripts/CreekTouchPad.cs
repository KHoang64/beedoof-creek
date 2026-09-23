using UnityEngine;
using UnityEngine.EventSystems;

namespace BeaverCreek
{
    // Each pad owns one pointer, so walking and looking work simultaneously.
    public sealed class CreekTouchPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public CreekFirstPerson player;
        public bool look;
        public RectTransform knob;
        int pointer = int.MinValue;
        Vector2 origin;
        RectTransform rect;
        void Awake() { rect = (RectTransform)transform; }
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out origin);
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var point);
            if (look) { player.TouchLook += point - origin; origin = point; }
            else
            {
                var offset = Vector2.ClampMagnitude(point, 64);
                player.TouchMove = offset / 64;
                if (knob) knob.anchoredPosition = offset;
            }
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) ResetPad(); }
        public void ResetPad()
        {
            pointer = int.MinValue;
            if (player) { if (look) player.TouchLook = Vector2.zero; else player.TouchMove = Vector2.zero; }
            if (knob) knob.anchoredPosition = Vector2.zero;
        }
        void OnDisable() { ResetPad(); }
        void OnApplicationFocus(bool focus) { if (!focus) ResetPad(); }
    }
}
