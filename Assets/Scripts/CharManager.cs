using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CharManager : MonoBehaviour {
	public static CharManager singleton = null;
	public Transform charSorter = null;
	public GameObject charPanelTemplate = null;
	public static List<CharPanel> charPanels = new List<CharPanel>();
	public static HashSet<MyController> registeredChars = new HashSet<MyController> ();

	public void Awake(){
		singleton = this;
	}

	public void SpawnChar(int id, MyController ctrl){
		Debug.Log ("SPAWN");
		CharPanel panel;
		if (charSorter.childCount < id + 1) {
			panel = ((GameObject)Instantiate (charPanelTemplate)).GetComponent<CharPanel> ();
			panel.transform.SetParent (charSorter, false);
		} else {
			panel = charSorter.GetChild (id).GetComponent<CharPanel> ();
		}
			panel.ctrl = ctrl;
			ctrl.panel = panel;
		registeredChars.Add (ctrl);
	}

	public void SpawnChar(MyController ctrl){
		CharPanel panel = ((GameObject)Instantiate (charPanelTemplate)).GetComponent<CharPanel> ();
			panel.transform.SetParent (charSorter, false);
		panel.ctrl = ctrl;
		ctrl.panel = panel;
		registeredChars.Add (ctrl);
	}
}
