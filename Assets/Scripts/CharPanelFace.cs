using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CharPanelFace : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
	public CharPanel panel = null;
	float timeSinceLastClick = 0;
	float holdTime = 0;
	Vector2 dragDelta = Vector3.zero;
	bool enter = false;
	
	// Update is called once per frame
	void Update () {
		if (timeSinceLastClick < 0.3f)
			timeSinceLastClick += Time.deltaTime;
		if (holdTime >= 0)
			holdTime += Time.deltaTime;
	}

	public void OnPointerDown(PointerEventData data){
		holdTime = 0;
		dragDelta = Vector2.zero;
		enter = true;
	}
	public void OnPointerUp(PointerEventData data){
		if (dragDelta.y / Screen.height > dragDelta.x * 2 / Screen.width && !enter) {
			panel.ctrl.Dash ();
		} else {
			if (holdTime > 0.3f) {
				if (new Vector2 (dragDelta.x / Screen.width * 2, dragDelta.y * 2 / Screen.height).sqrMagnitude <= 0.00001f) {
					panel.ctrl.Stop ();
				} 
			} else {
				CameraView.ShowTapAlly (panel.ctrl.transform.position);
				panel.SetSelection (timeSinceLastClick < 0.3f);
			}
		}
		timeSinceLastClick = 0;
		holdTime = -1;
		enter = false;
	}
	public void OnPointerClick(PointerEventData data){
	}
	public void OnPointerEnter(PointerEventData data){
		enter = true;
	}
	public void OnPointerExit(PointerEventData data){
		enter = false;
	}
	public void OnDrag(PointerEventData data){
			dragDelta += data.delta;
	}
}
