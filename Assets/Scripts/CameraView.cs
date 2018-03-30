using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CameraView : MonoBehaviour {
	public static CameraView singleton = null;
	public Transform myTrans = null;
	public Camera cam = null;
	public Transform camTrans = null;
	public CharacterController cc = null;
	public LayerMask ctrlMask;
	public LayerMask clickMask;
	public LayerMask walkMask;
	public TapSphere tapSphere = null;
	public LineRenderer pathRenderer = null;
	public Transform targetPoint = null;
	public Transform selfPoint = null;

	void Awake () {
		singleton = this;
	}

	void Start(){
		if (cc == null)
			cc = GetComponent<CharacterController> ();
		if (cam == null)
			cam = Camera.main;
		if (camTrans == null)
			camTrans = Camera.main.transform;
		myTrans = transform;
	}

	void LateUpdate(){
		camTrans.position = Vector3.Lerp (camTrans.position, myTrans.position, 12 * Time.deltaTime);
	}

	public static void Move(float movement){
		singleton.cc.Move (singleton.myTrans.forward * movement);
	}

	public static void Move(Vector2 movement){
		singleton.cc.Move(new Vector3(movement.x, 0, movement.y));
	}
	public static void Move(Vector3 movement){
		singleton.cc.Move(movement);
	}

	public static bool RaycastScreen (Vector2 screenPoint, out RaycastHit hit){
		return Physics.Raycast (singleton.cam.ScreenPointToRay (new Vector3 (screenPoint.x, screenPoint.y, 0)), out hit, Mathf.Infinity, singleton.clickMask);
	}
	public static bool RaycastScreenWalk (Vector2 screenPoint, out RaycastHit hit){
		return Physics.Raycast (singleton.cam.ScreenPointToRay (new Vector3 (screenPoint.x, screenPoint.y, 0)), out hit, Mathf.Infinity, singleton.walkMask);
	}
	public static bool SphereCastScreen (Vector2 screenPoint, out RaycastHit hit){
		return Physics.SphereCast (singleton.cam.ScreenPointToRay (new Vector3 (screenPoint.x, screenPoint.y, 0)), 1, out hit, Mathf.Infinity, singleton.clickMask);
	}

	public static MyController GetClosestControllerToPoint (Vector3 point, float radius, bool allyOnly = false){

		Collider[] cols = Physics.OverlapSphere (point, radius, singleton.ctrlMask);
		MyController retCtrl = null;
		if (cols.Length > 0) {
			float minSqDist = Mathf.Infinity;
			foreach (Collider c in cols) {
				float trySqDist = (c.transform.position - point).sqrMagnitude;
				if (trySqDist < minSqDist) {
					if (allyOnly) {
						MyController tryCtrl = c.GetComponent<MyController> ();
						if (tryCtrl != null && tryCtrl.isLocalPlayer) {
							minSqDist = trySqDist;
							retCtrl = tryCtrl;
						}
					} else {
						minSqDist = trySqDist;
						retCtrl = c.GetComponent<MyController> ();
					}
				}
			}
		} 
		return retCtrl;
	}

	public static void ShowTapNeutral(Vector3 worldPos){
		singleton.tapSphere.ShowTap (worldPos, 
			Color.white);
	}
	public static void ShowTapEnemy(Vector3 worldPos){
		singleton.tapSphere.ShowTap (worldPos, 
			Color.red);
	}
	public static void ShowTapAlly(Vector3 worldPos){
		singleton.tapSphere.ShowTap (worldPos, 
			Color.green);
	}

	public static void SetSelf (Transform self){
		if (self == null) {
			singleton.selfPoint.gameObject.SetActive (false);
			singleton.targetPoint.gameObject.SetActive (false);
		}else{
			singleton.selfPoint.gameObject.SetActive (true);
			singleton.selfPoint.parent = self;
			singleton.selfPoint.localPosition = new Vector3 (0, -0.99f, 0);
		}
	}

	public static void SetTarget (Transform target){
		if (target == null) {
			singleton.targetPoint.parent = null;
			singleton.targetPoint.gameObject.SetActive (false);
		}else {
			singleton.targetPoint.gameObject.SetActive (true);
			singleton.targetPoint.parent = target;
			singleton.targetPoint.localPosition = new Vector3 (0, -0.99f, 0);
			//RefreshTargetRotation ();
			singleton.targetPoint.rotation = Quaternion.LookRotation(Vector3.down);
		}
	}
	public static void SetTarget (Vector3 target){
		singleton.targetPoint.gameObject.SetActive (true);
		singleton.targetPoint.parent = null;
		singleton.targetPoint.position = target + new Vector3(0, 0.01f, 0);
		singleton.targetPoint.rotation = Quaternion.LookRotation(Vector3.down);
	}
	public static void RefreshTargetRotation(){
		singleton.targetPoint.LookAt (singleton.myTrans);
		singleton.targetPoint.localPosition = singleton.targetPoint.forward * 1.2f;
		singleton.targetPoint.rotation = singleton.myTrans.rotation;
	}
	public static void RefreshPath(Vector3[] path, Vector3 nextPos){
		if (ScreenTapHandler.selectedType != 1 || path == null || path.Length < 2) {
			singleton.pathRenderer.enabled = false;
		} else {
			singleton.pathRenderer.enabled = true;
			List<Vector3> pos = new List<Vector3> ();
			//pos.Add (ScreenTapHandler.selected.position);
			int index = Math.Max (Array.IndexOf<Vector3> (path, nextPos), 0);
			for (int a = index; a < path.Length; a++) {
				pos.Add (path [a]);
			}
			singleton.pathRenderer.SetVertexCount (pos.Count);
			singleton.pathRenderer.SetPositions (pos.ToArray ());
		}
	}
}
