using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketBloom
{
    public sealed class BloomPieceInput : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public BloomGame game;
        public int slot;
        public void OnPointerDown(PointerEventData e) => game.SelectPiece(slot);
        public void OnBeginDrag(PointerEventData e) => game.BeginPieceDrag(slot, e.position);
        public void OnDrag(PointerEventData e) => game.DragPiece(slot, e.position, false);
        public void OnEndDrag(PointerEventData e) => game.DragPiece(slot, e.position, true);
    }
}
