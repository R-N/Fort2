using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : NetworkManager {
	public static Dictionary<int, int> playerIdByConnectionId = new Dictionary<int, int>();
	public class PlayerInfo{
		public int points = 0;
		public List<int> ctrlIds = new List<int>();
	}
	public static Dictionary<int, PlayerInfo> playerInfoByPlayerId = new Dictionary<int, PlayerInfo>();
	public static Dictionary<int, Transform> fortByPlayerId = new Dictionary<int, Transform>();
	public static Dictionary<int, MyController> ctrlByCtrlId = new Dictionary<int, MyController>();
	public static int myPlayerId = 0;
	public static PlayerInfo myPlayerInfo{
		get{
			return playerInfoByPlayerId [myPlayerId];
		}
	}

	public GameObject player = null;

	public InputField ipField = null;

	public GameObject charBt = null;

	public void Host(){
		StartHost ();
		StartCoroutine (WaitForClient ());
	}

	public void Join(){
		networkAddress = ipField.text;
		StartClient ();
		StartCoroutine (WaitForClient ());
	}

	public IEnumerator WaitForClient(){
		while (!NetworkManager.singleton.client.isConnected) {
			yield return new WaitForEndOfFrame ();
		}
		charBt.SetActive (true);
	}

	public void SpawnChar(){
		if (ClientScene.localPlayers.Count < 8)
		ClientScene.AddPlayer (NetworkManager.singleton.client.connection, (short)ClientScene.localPlayers.Count);
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short localId){
		GameObject ply = (GameObject)Instantiate (player, new Vector3 (Random.Range (-10, 10), 1, Random.Range (-10, 10)), Quaternion.identity);
		NetworkServer.AddPlayerForConnection (conn, ply, localId);
	}
}
