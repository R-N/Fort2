using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[NetworkSettings(sendInterval = 0.04f)]
public class MyController : NetworkBehaviour {
	public class Buff
	{
		public int id = 0;
		public float duration = 0;
		public float curStack = 1;
		public float level = 0;
		public Buff(){
		}

		public static float[][] valueByIdAndLevel = new float[][]{ };
		public static float[][] durationByIdAndLevel = new float[][]{ };
		public static int[][] maxStackByIdAndLevel = new int[][]{ };
	}

	public UnityEngine.AI.NavMeshAgent nma = null;
	public Rigidbody rb = null;
	public NetworkTransform netTrans = null;
	public CharPanel panel = null;

	public int playerId = 0;
	public int localId = 0;
	public int charSkillLevel = 1;

	public Transform target = null;
	public Vector3 targetPos = Vector3.zero;

	int pathStatus = 0;

	bool _selected = false;
	public bool selected {
		get {
			return _selected;
		}
		set {
			if (panel != null) {
				panel.selected = value;
			}
			_selected = value;
		}
	}

	bool _isPrisoner = false;

	public bool canMove = true;
	public bool canDoSkill = true;

	public bool isPrisoner{
		get{
			return _isPrisoner;
		}
		set{
			if (value) {
				canMove = false;
				canDoSkill = false;
			}
			_isPrisoner = value;
		}
	}

	public float curHP = 0;
	public float maxHP = 100;
	public float baseMaxHP = 100;

	public float baseMvSpd = 5;
	public float mvSpd = 5;

	public Dictionary<int, Buff> buffs = new Dictionary<int, Buff>();

	public class Impulse{
		public Vector3 impLeft;
		public float impDeacc;
		public Vector3 accMove = Vector3.zero;

		public bool Tick(UnityEngine.AI.NavMeshAgent nma, float dt){
			if (impLeft == Vector3.zero)
				return false;
			Vector3 prevPos = nma.transform.position;
			Vector3 prevImp = impLeft;
			impLeft = Vector3.MoveTowards (impLeft, Vector3.zero, impDeacc * dt);
			nma.Move (0.5f * (prevImp + impLeft) * dt);
			Vector3 delta = nma.transform.position - prevPos;
			accMove += delta;
			if (impLeft != Vector3.zero && impLeft == prevImp)
				Debug.Log ("deltaImp " + delta + " impLeft " + impLeft);
			return true;
		}
	}

	public Dictionary<int, Impulse> impulses = new Dictionary<int, Impulse> ();

	int nextImp = int.MinValue;

	public Vector3 impulseCorrection = Vector3.zero;

	[SyncVar]
	public Vector3 moveDir = Vector3.zero;

	Coroutine destCor = null;

	Vector3 prevPos = Vector3.zero;

	void Awake (){
		if (nma == null)
			nma = GetComponent<UnityEngine.AI.NavMeshAgent> ();
	}

	void Start(){
		prevPos = transform.position;
	}

	public override void OnStartServer(){
		base.OnStartServer ();
		nma.enabled = true;
	}

	public override void OnStartClient(){
		base.OnStartClient ();
	}

	public override void OnStartLocalPlayer(){
		base.OnStartLocalPlayer ();
		CharManager.singleton.SpawnChar (this);
	}

	public void Dash(){
		if (isServer) {
			if (moveDir == Vector3.zero)
				ActualDash (transform.forward);
			else
				ActualDash (moveDir.normalized);
		} else {
			if (moveDir == Vector3.zero)
				CmdDash (transform.forward);
			else
				CmdDash (moveDir.normalized);
		}
	}

	[Command]
	public void CmdDash(Vector3 dir){
		ActualDash (dir);
	}

	public void ActualDash(Vector3 dir){
		AddImpulse (dir * mvSpd * 3, mvSpd * 4.5f);
	}

	public void AddImpulse(Vector3 imp, float deaccel){
		if (isServer)
			ActualAddImpulse (nextImp, imp, deaccel);
		else
			CmdAddImpulse (imp, deaccel);
	}

	[Command]
	public void CmdAddImpulse(Vector3 imp, float deaccel){
		ActualAddImpulse (nextImp, imp, deaccel);
	}

	public void ActualAddImpulse(int key, Vector3 imp, float deaccel){
		impulses.Add (key, new Impulse (){ impLeft = imp, impDeacc = deaccel });
		if (nextImp == int.MaxValue)
			nextImp = int.MinValue;
		else
			nextImp = key + 1;
	}

	public void ImpulseUpdate(){
		foreach (int u in impulses.Keys) {
			if (!impulses [u].Tick (nma, Time.deltaTime) && isServer) {
				RpcRemoveImpulse (u, impulses [u].accMove);
			}
		}
		if (impulseCorrection != Vector3.zero) {
			Vector3 pos = transform.position;
			Vector3 cor = Vector3.Lerp (impulseCorrection, Vector3.zero, 12 * Time.deltaTime);
			nma.Move (cor);
			impulseCorrection -= pos - transform.position;
		}
	}

	[ClientRpc]
	public void RpcRemoveImpulse(int u, Vector3 accMove){
		if (impulses.ContainsKey (u)) {
			if (impulses [u].impLeft == Vector3.zero) {
				impulseCorrection += accMove - impulses [u].accMove;
				impulses.Remove (u);
			} else {
				StartCoroutine (WaitForImpulse (u, accMove));
			}
		} else {
			impulseCorrection += accMove;
		}
	}

	public IEnumerator WaitForImpulse(int u, Vector3 accMove){
		Impulse imp = impulses [u];
		while (imp.impLeft != Vector3.zero) {
			if (impulses.ContainsKey (u))
				yield return new WaitForEndOfFrame ();
			else
				break;
		}
		if (impulses.ContainsKey (u)) {
			impulseCorrection += accMove - impulses [u].accMove;
			impulses.Remove (u);
		}
	}

	public void ApplyBuff(int leId, int leLevel){
		if (isServer) {
			if (buffs.ContainsKey (leId))
				RpcApplyBuff (leId, leLevel, (int)Mathf.Clamp (buffs [leId].curStack + 1, 0, Buff.maxStackByIdAndLevel [leId] [leLevel]));
			else
				RpcApplyBuff (leId, leLevel, 1);
		}
	}

	[ClientRpc]
	public void RpcApplyBuff(int leId, int leLevel, int stack){
		if (buffs.ContainsKey (leId)) {
			buffs [leId].curStack = Mathf.Clamp (stack, 0, Buff.maxStackByIdAndLevel [leId] [leLevel]);
			buffs [leId].level = leLevel;
			buffs [leId].duration = Buff.durationByIdAndLevel [leId] [leLevel];
		} else {
			buffs.Add (leId, new Buff (){ level = leLevel, id = leId, duration = Buff.durationByIdAndLevel [leId] [leLevel], curStack = stack });
		}
	}

	void BuffUpdate(){
		maxHP = baseMaxHP;
		mvSpd = baseMvSpd;
		foreach (Buff b in buffs.Values) {
			b.duration -= Time.fixedDeltaTime;
			if (b.duration > 0) {
				switch (b.id) {
				}
				
			} else {
				switch (b.id) {
				default:
					{
						RpcRemoveBuff (b.id);
						break;
					}
				}
			}
		}
		nma.speed = mvSpd;
		nma.acceleration = mvSpd * 2;
	}

	[ClientRpc]
	public void RpcRemoveBuff(int id){
		buffs.Remove (id);
	}

	void Update(){
		if (isServer) {
			if (rb.velocity != Vector3.zero)
				Debug.Log ("vel " + rb.velocity);
			ImpulseUpdate ();
			if (nma.hasPath) {
				moveDir = nma.path.corners[1] - nma.path.corners[0];

				if (nma.path.corners.Length == 2){
					Vector3 prevTargetDir = targetPos - prevPos;
					Vector3 deltaPos = transform.position - prevPos;
					Vector3 proj = Vector3.Project (prevTargetDir, deltaPos);

					if (Vector3.Dot(proj, deltaPos) > 0 && proj.sqrMagnitude <= deltaPos.sqrMagnitude) {
						float sqrMag = (prevTargetDir - proj).sqrMagnitude;
						if (sqrMag <= 4)
						ActualStop ();
						else
							Debug.Log ("sqrMag " + sqrMag);
					}
				}

			}
			if (target != null) {
				if ((targetPos - target.position).sqrMagnitude < 9) {
					SetDestination (target.position);
				} else {
					MoveTo (targetPos);
					RpcSetTargetV3 (targetPos);
				}
			} else if (pathStatus == 1) {
				if (!nma.pathPending)
					pathStatus = 2;
				RpcSetTargetV3 (nma.pathEndPosition);
			} else if (pathStatus == 2 && !nma.hasPath) {
				Stop ();
			} 

		}
		prevPos = transform.position;
	}

	void FixedUpdate(){
		if (isServer) {
			BuffUpdate ();
			rb.velocity = Vector3.zero;
		}
	}

	void OnCollisionEnter(Collision col){
		MyController ctrl = col.collider.GetComponent<MyController> ();
		if (ctrl != null && curHP < ctrl.curHP + 1) {
			
		}
	}

	void LateUpdate(){
		/*if (target != null && selected)
			CameraView.RefreshTargetRotation ();*/
	}

	public void MoveTo(Transform trans){
		NetworkIdentity netId = trans.GetComponent<NetworkIdentity> ();
		if (netId != null) {
			if (isServer) {
				Debug.Log ("follow by server");
				ActualMoveToTrans (netId.netId.Value);
			} else {
				Debug.Log ("follow by client");
				CmdMoveToTrans (netId.netId.Value);
			}
		} else {
			Debug.Log ("No net id " + trans.name);
		}
		if (selected)
			CameraView.SetTarget (trans);
	}
	[ClientRpc]
	public void RpcSetTargetTrans (uint id){
		Debug.Log ("settarget");
		NetworkInstanceId netId = new NetworkInstanceId (id);
		GameObject trans = ClientScene.FindLocalObject (netId);
		if (trans != null) {
			target = trans.transform;
			targetPos = target.position;
			if (selected) {
				CameraView.SetTarget (trans.transform);
			}
		} else {
			Debug.Log ("settarget trans is null");
		}
	}

	[ClientRpc]
	public void RpcSetTargetV3 (Vector3 pos){
		targetPos = pos;
		if (selected)
			CameraView.SetTarget (pos);
	}

	[ClientRpc]
	public void RpcSetTargetNull(){
		target = null;
		targetPos = transform.position;
		if (selected) {
			Debug.Log ("settargetnull");
			CameraView.SetTarget (null);
		}
	}

	[Command]
	public void CmdMoveToTrans (uint transId){

		Debug.Log ("cmdmoveto");
		ActualMoveToTrans (transId);
	}

	public void ActualMoveToTrans(uint transId){
		Debug.Log ("actualmoveto");
		NetworkInstanceId netId = new NetworkInstanceId (transId);
		GameObject trans = ClientScene.FindLocalObject (netId);
		if (trans != null) {
			target = trans.transform;
			pathStatus = 0;
			SetDestination (target.position);
			RpcSetTargetTrans (transId);
		} else {
			Debug.Log ("trans is null");
		}
	}

	public void MoveTo(Vector3 dest){
			UnityEngine.AI.NavMeshHit hit;
		if (UnityEngine.AI.NavMesh.SamplePosition (dest, out hit, Mathf.Infinity, UnityEngine.AI.NavMesh.AllAreas)) {
			if (selected)
				CameraView.SetTarget (hit.position);
			if (isServer)
				ActualMoveToV3 (hit.position);
			else
				CmdMoveToV3 (hit.position);
		}
		
	}

	[Command]
	public void CmdMoveToV3 (Vector3 dest){
		ActualMoveToV3 (dest);
	}

	public void ActualMoveToV3(Vector3 dest){
		target = null;
		SetDestination (dest);
		pathStatus = 1;
	}

	void SetDestination (Vector3 dest){
		if (destCor != null)
			StopCoroutine (destCor);
		destCor = StartCoroutine (SetDestCor (dest));
	}

	IEnumerator SetDestCor (Vector3 dest){
		while (!nma.enabled || !nma.isOnNavMesh) {
			yield return new WaitForEndOfFrame ();
		}
		targetPos = dest;
		nma.SetDestination (dest);
		nma.Resume ();
		destCor = null;
	}

	public void Stop(){
		if (isServer)
			ActualStop ();
		else
			CmdStop ();
		if (selected)
			CameraView.SetTarget (null);
	}

	[Command]
	public void CmdStop(){
		ActualStop ();
	}

	public void ActualStop(){
		if (nma.isActiveAndEnabled && nma.isOnNavMesh) {
			nma.Stop ();
			nma.ResetPath ();
		}
		pathStatus = 3;
		target = null;
		targetPos = transform.position;
		if (destCor != null) {
			StopCoroutine (destCor);
			destCor = null;
		}
		RpcSetTargetNull ();
	}

	public void DoSkill(){
		if (isLocalPlayer) {
			if (isServer)
				ActualDoSkill ();
			else
				CmdDoSkill ();
		}
	}

	[Command]
	public void CmdDoSkill(){
		ActualDoSkill ();
	}

	public void ActualDoSkill(){
		bool done = false;
		if (done)
			RpcDoSkill ();
	}

	[ClientRpc]
	public void RpcDoSkill(){
	}

	public void Bail(){
		if (isLocalPlayer) {
			if (isServer)
				ActualBail ();
			else
				CmdBail ();
		}
	}

	[Command]
	public void CmdBail(){
		ActualBail ();
	}

	public void ActualBail(){
		if (GameManager.myPlayerInfo.points >= 25) {
			GameManager.myPlayerInfo.points -= 25;
			 Respawn ();
		}
	}

	public void Respawn(){
		ActualStop ();
		Vector3 pos = Vector3.zero;
		transform.position = pos;
		nma.Warp (pos);
		if (destCor != null) {
			StopCoroutine (destCor);
			destCor = null;
		}
		RpcRespawn (pos);
	}

	public void SetPosition(Vector3 pos){
		if (isServer) {
			transform.position = pos;
			nma.Warp (pos);
			ActualStop ();
			RpcSetPosition (pos);
		}
	}

	[ClientRpc]
	public void RpcSetPosition (Vector3 pos){
		transform.position = pos;
	}

	[ClientRpc]
	public void RpcRespawn(Vector3 pos){
		panel.SetPrisoner (false);
		curHP = maxHP;
		isPrisoner = false;
		canMove = true;
		transform.position = pos;
	}
}
