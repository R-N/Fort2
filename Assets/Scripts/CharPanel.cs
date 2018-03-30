using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharPanel : MonoBehaviour {
	public MyController ctrl = null;
	public GameObject skillBt = null;
	public GameObject bailBt = null;
	public GameObject selectionFrame = null;
	public Image skillCdMask = null;
	public GameObject dangerEffect = null;

	bool _selected = false;

	public bool selected {
		get {
			return _selected;
		}
		set {
			selectionFrame.SetActive (value);
			_selected = value;
		}
	}

	public void SetPrisoner(bool isPrisoner){
		skillBt.SetActive (!isPrisoner);
		bailBt.SetActive (isPrisoner);
	}

	public void DoSkill(){
		ctrl.DoSkill ();
	}

	public void Bail(){
		if (GameManager.myPlayerInfo.points >= 25)
			ctrl.Bail ();
	}

	public void SetSelection (bool focus = false){
		if (focus && ScreenTapHandler.selected == ctrl.transform) {
			CameraView.singleton.myTrans.position = ctrl.transform.position + new Vector3 (0, 9f, -2.6794f);
		} else {
			ScreenTapHandler.singleton.SetSelection (ctrl);
		}

	}

	public void SetCooldownProgress(float cd){
		skillCdMask.fillAmount = 1 - cd;
	}
}
