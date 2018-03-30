using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ScreenTapHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IDropHandler, IPointerUpHandler {
	public static Transform selected = null;
	public static int selectedType = 0;
	public static ScreenTapHandler singleton = null;
	float holdTime = 0;
	MyController hitCtrl = null;
	Collider hitCol = null;
	Vector3 hitPos = Vector3.zero;
	float hitDist = 0;
	float dragDelta = 0;
	Vector2 clickPos = Vector2.zero;
	float prevDist = 0;
	public float zoom = 0;
	static float maxZoomCoef = 6.7275f;
	static float minZoomCoef = 0.14864f;

	bool twoTap = false;

	float zoomCoef {
		get {
			return Mathf.Clamp(Mathf.Pow (1.1f, -zoom), minZoomCoef, maxZoomCoef); 
		}
	}
	// Use this for initialization
	void Awake () {
		singleton = this;
		minZoomCoef = Mathf.Pow (1.1f, -20);
		maxZoomCoef = Mathf.Pow (1.1f, 20);
	}

	// Update is called once per frame
	void Update () {
		if (holdTime >= 0)
			holdTime += Time.deltaTime;
		Zoom (Input.mouseScrollDelta.y * 1.5f); 
	}

	public void OnPointerClick(PointerEventData data){
		OnPointerUp (data);
	}

	public void OnPointerDown(PointerEventData data){
		if (Input.touchCount <= 1){
			twoTap = false;
			prevDist = 0;
			hitCtrl = null;
			hitCol = null;
			hitPos = Vector3.zero;
			holdTime = 0;
			dragDelta = 0;
			clickPos = data.position;
			RaycastHit hit;
			if (CameraView.RaycastScreen (data.position, out hit)) {
				hitCol = hit.collider;
				hitCtrl = hitCol.GetComponent<MyController> ();
				hitPos = hit.point;
				hitDist = hit.distance;
				if (hitCtrl == null) {
					hitCtrl = CameraView.GetClosestControllerToPoint (hitPos,  0.06f * hitDist);
				}
				if (hitCtrl == null) {
					CameraView.ShowTapNeutral (hitPos);
				} else {
					if (hitCtrl.isLocalPlayer) {
						CameraView.ShowTapAlly (hitCtrl.transform.position);
					} else {
						CameraView.ShowTapEnemy (hitCtrl.transform.position);
					}
				}
			} else if (CameraView.SphereCastScreen (data.position, out hit)) {
				hitCol = hit.collider;
				hitCtrl = hitCol.GetComponent<MyController> ();
				hitPos = hit.point;
				hitDist = hit.distance;
				if (hitCtrl == null) {
					hitCtrl = CameraView.GetClosestControllerToPoint (hitPos,  0.06f * hitDist);
				}
				if (hitCtrl == null) {
					CameraView.ShowTapNeutral (hitPos);
				} else {
					if (hitCtrl.isLocalPlayer) {
						CameraView.ShowTapAlly (hitCtrl.transform.position);
					} else {
						CameraView.ShowTapEnemy (hitCtrl.transform.position);
					}
				}
			}
		}else{
			RaycastHit hit;
			if (CameraView.RaycastScreen (data.position, out hit)) {
				CameraView.ShowTapNeutral (hit.point);
			} else if (CameraView.SphereCastScreen (data.position, out hit)) {
					CameraView.ShowTapNeutral (hit.point);
			}
			twoTap = true;
			holdTime = -1;
			Vector2 a = Input.GetTouch (0).position;
			Vector2 b = Input.GetTouch (1).position;
			prevDist = new Vector2 ((a.x - b.x) * 12 / Screen.width, (a.y - b.y) * 12 / Screen.height).magnitude;
		}
	}

	public void OnPointerUp(PointerEventData data){
		if (!twoTap && holdTime > 0){
			Debug.Log ("Pointerup");
			if (holdTime > 0.3f) {
				if (dragDelta < 0.6f && dragDelta / holdTime < 1.2f) {
					if (hitCol != null && selectedType == 1) {
						if (hitCtrl != null) {
							if (hitCtrl.isLocalPlayer) {
								if (hitCtrl.transform == selected) {
									Debug.Log ("hold on self");
									hitCtrl.Stop ();
									holdTime = -1;
									return;
								}
							}
							Debug.Log ("follow");
							selected.GetComponent<MyController> ().MoveTo (hitCtrl.transform);
							holdTime = -1;
							return;
						} else if (selectedType == 1) {
							Debug.Log ("normal move");
							selected.GetComponent<MyController> ().MoveTo (hitPos);
						}
					} else {
						Debug.Log ("no hitcol or char unselected");
					}
				} else if (dragDelta < 0.35f) {
					Debug.Log ("dragSpeed (inch/s) " + (dragDelta / holdTime));
				} else {
					Debug.Log ("drag " + dragDelta);
				}
			} else {
				Debug.Log ("holdTime " + holdTime);
				if (hitCol != null) {
					if (hitCtrl != null) {
						if (hitCtrl.isLocalPlayer) {
							SetSelection (hitCtrl);
							holdTime = -1;
							return;
						} else if (selectedType == 1) {
							RaycastHit hit;
							if (CameraView.RaycastScreenWalk (clickPos, out hit))
								selected.GetComponent<MyController> ().MoveTo (hit.point);
							else
								selected.GetComponent<MyController> ().MoveTo (hitPos);

						}
					} else if (selectedType == 1) {
						selected.GetComponent<MyController> ().MoveTo (hitPos);
					}
				}
			}

		}
		holdTime = -1;
	}

	public void SetSelection(MyController ctrl){
		if (selectedType == 1) {
			selected.GetComponent<MyController> ().selected = false;
		}

		ctrl.selected = true;
		selected = ctrl.transform;
		selectedType = 1;
		CameraView.SetSelf (selected);
		if (ctrl.target == null) {
			if (ctrl.nma.hasPath)
				CameraView.SetTarget (ctrl.nma.pathEndPosition);
			else
				CameraView.SetTarget (null);
		} else {
			CameraView.SetTarget (ctrl.target);
		}
	}

	public void OnDrop(PointerEventData data){
		OnPointerUp (data);
	}

	public void OnDrag(PointerEventData data){
		if (Input.touchCount <= 1) {
			Vector2 drag = new Vector2 (data.delta.x * -20 / Screen.width, data.delta.y * -20 / Screen.height);
			/*if (zoom >= 0)
				CameraView.Move (drag / (1 + zoom));
			else
				CameraView.Move (drag * (1 - zoom));*/
			CameraView.Move(drag * zoomCoef);
			dragDelta += data.delta.magnitude / Screen.dpi;
		} else {
			Vector2 a = Input.GetTouch (0).position;
			Vector2 b = Input.GetTouch (1).position;
			float z = new Vector2 ((a.x - b.x) * 12 / Screen.width, (a.y - b.y) * 12 / Screen.height).magnitude;

			if (prevDist != 0) {
				Zoom((z - prevDist) * 2);
			}
			prevDist = z;
		}
	}

	void Zoom (float d){
		float prev = zoomCoef;
		zoom = zoom + d;
		CameraView.Move (d * prev);
	}


}
