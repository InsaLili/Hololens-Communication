using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TouchCtrl: MonoBehaviour {

	public float scaleFactor = 0.05f;        // The rate of change of the scale
	//public float minScale = 0.2f;
	//public float maxScale = 8f;
	public static Vector3 positionOrigin;
	public static Transform Box_Transform;

	[Serializable]
	public class GstrMessage
	{
		public string name;
		public string gstrType;
		public Vector3 posChange;
		public Quaternion rotChange;
		public float scaleChange;
	}

	private string gstrType;
	private Vector3 scaleOrigin;
	private Quaternion rotOrigin;
	private float scaleChange;
	private Vector3 positionChange;
	private Quaternion rotChange;
	private string objName;
	//private UDPController udpCtrl;
	private GstrMessage gstrMessage;

	void Start()
	{
		GameObject box = gameObject;
		if (gameObject != null)
		{
			Box_Transform = box.transform;
			//position = Box_Transform.position;
			objName = box.name;
			//positionOrigin = Box_Transform.position;
			scaleOrigin = Box_Transform.localScale;
			rotOrigin = Box_Transform.localRotation;
			Debug.Log(scaleOrigin);
		}
		//udpCtrl = gameObject.GetComponent<UDPController>();
	}

	// Update is called once per frame
	void Update()
	{

		if (Vuforia.DefaultTrackableEventHandler.findMarker && UDPController.msgReceived)//
		{
			var headPosition = Camera.main.transform.position;
			var gazeDirection = Camera.main.transform.forward;

			//RaycastHit hitInfo;

			//if (Physics.Raycast(headPosition, gazeDirection, out hitInfo))
			//{
			gstrMessage = JsonUtility.FromJson<GstrMessage>(UDPController.message);
			gstrType = gstrMessage.gstrType;

			if (gstrType == "dragging")
			{
				positionChange = gstrMessage.posChange;
				Box_Transform.position = positionOrigin + positionChange;
				Debug.Log("dragging to " + positionChange);

			}
			else if (gstrType == "rotating")
			{
				rotChange = gstrMessage.rotChange;
				Box_Transform.localRotation = rotOrigin * rotChange;
				//Box_Transform.Rotate(rotChange.x, rotChange.y, rotChange.z, Space.World);
				Debug.Log("rotation change:" + rotChange);
			}
			else if (gstrType == "pinching")
			{
				scaleChange = gstrMessage.scaleChange;
				Box_Transform.localScale = new Vector3(scaleOrigin.x*scaleChange, scaleOrigin.y * scaleChange, scaleOrigin.z * scaleChange);
				//var finalScale = Mathf.Clamp(scaleOrigin * scaleChange, minScale, maxScale);
				//Box_Transform.localScale = new Vector3(finalScale, finalScale, finalScale);
			}
			UDPController.msgReceived = false;
			//}
		}
	}
}
