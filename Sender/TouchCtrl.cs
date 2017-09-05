using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TouchCtrl : MonoBehaviour
{
	public float rotSpeed = 10;
	public float dragSpeed = 0.0015f;
	public float scaleFactor = 0.02f;        // The rate of change of the scale
	public float minScale = 0.3f;
	public float maxScale = 8f;

	[Serializable]
	public class GstrMessage
	{
		public string name;
		public string gstrType;
		public Vector3 posChange;
		public Quaternion rotChange;
		public float scaleChange;
	}

	private Transform objTransform;
	private string touchType = "";
	private bool firstTouch = true;
	private string objName;
	private Vector3 scaleOrigin;
	private Quaternion rotOrigin;
	private Vector3 posOrigin;
	private float posDiff;
	private float pixelToCM = 0.00016f;
	private float SmoothTime;
	private float inertiaTime = 0.0f;
	private bool underInertia = false;
	private Touch preTouch1;
	private Touch preTouch2;
	private Touch preTouch3;

	private UDPController UDPCtrl;
	private Text txtTouchEvent;
	private Text txtPosition;
	private Text txtRotation;
	private Text txtScale;

	private void Awake()
	{
		UDPCtrl = gameObject.GetComponent<UDPController>();
		var txtTouch = GameObject.FindGameObjectWithTag("touchEvent");
		var txtPos = GameObject.FindGameObjectWithTag("coordinate");
		var txtRot = GameObject.FindGameObjectWithTag("rotation");
		var txtScl = GameObject.FindGameObjectWithTag("scale");

		txtTouchEvent = txtTouch.GetComponent<Text>();
		txtPosition = txtPos.GetComponent<Text>();
		txtRotation = txtRot.GetComponent<Text>();
		txtScale = txtScl.GetComponent<Text>();
	}

	// Use this for initialization
	void Start()
	{
		if (gameObject != null)
		{
			objTransform = gameObject.transform;
			objName = gameObject.name;
			scaleOrigin = objTransform.localScale;
			rotOrigin = objTransform.localRotation;
			posOrigin = objTransform.localPosition;
		}
	}

//#if !UNITY_EDITOR
	// Update is called once per frame
	void Update()
	{
		//---- Rotating on X and Y axes
		if (Input.touchCount == 1)
		{
			if (Input.GetTouch(0).phase == TouchPhase.Moved)
			{
				// Get movement of the finger since last frame
				Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
				//Debug.Log(touchDeltaPosition);
				RotateXY(touchDeltaPosition.x, touchDeltaPosition.y);
			}
		}
		else if (Input.touchCount == 2)     //---- Pinching & dragging
		{
			//-----begin touch
			if (Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(1).phase == TouchPhase.Began)
			{
				preTouch1 = Input.GetTouch(0);
				preTouch2 = Input.GetTouch(1);
				//posDiff = (touch1.position - touch2.position).magnitude;
				//Vector3 pos = touch.position;
				//var dist = transform.position.z - Camera.main.transform.position.z;
				//Vector3 v3 = new Vector3(pos.x, pos.y, dist);
				//v3 = Camera.main.ScreenToWorldPoint(v3);
				//offsetPos = transform.position - v3;
			}
			else if (Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
			{
				Touch touch1 = Input.GetTouch(0);
				Touch touch2 = Input.GetTouch(1);

				Vector2 deltaTouch1 = touch1.position - preTouch1.position;
				Vector2 deltaTouch2 = touch2.position - preTouch2.position;

				//float angleBetweenTouch = Vector2.Angle(touch1.deltaPosition, touch2.deltaPosition);
				float angleBetweenTouch = Vector2.Angle(deltaTouch1, deltaTouch2);

				//---------judge touch type in the first time; 
				if (firstTouch)
				{
					//if (Mathf.Abs(deltaMagnitudeDiff) < 0.8)
					if (angleBetweenTouch < 90)
					{
						//----------dragging on X and Y axes
						touchType = "dragXY";
					}
					else// if (Mathf.Abs(deltaMagnitudeDiff) > 2)
					{
						//----------pinching
						touchType = "pinch";
					}
					firstTouch = false;
				}
				//-------- lock to one type of interaction during the touch
				else
				{
					if (touchType == "dragXY")
						DragXY(touch1);
					else if (touchType == "pinch")
					{
						//--------calculate distance difference between to points delta
						Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
						Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
						float prevTouchDeltaMag = (touch1PrevPos - touch2PrevPos).magnitude;
						float touchDeltaMag = (touch1.position - touch2.position).magnitude;
						float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;
						deltaMagnitudeDiff = Mathf.Clamp(deltaMagnitudeDiff, -4.0f, 4.0f);

						Pinch(deltaMagnitudeDiff);
					}
				}
			}
			//------- stop touch
			else if (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled || Input.GetTouch(1).phase == TouchPhase.Ended || Input.GetTouch(1).phase == TouchPhase.Canceled)
			{
				underInertia = true;
				firstTouch = true;
				touchType = "";
			}
		}
		else if (Input.touchCount == 3)
		{
			if (Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(1).phase == TouchPhase.Began && Input.GetTouch(2).phase == TouchPhase.Began)
			{
				preTouch1 = Input.GetTouch(0);
				preTouch2 = Input.GetTouch(1);
				preTouch3 = Input.GetTouch(2);
			}
			else if (Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved && Input.GetTouch(2).phase == TouchPhase.Moved)
			{
				Touch touch1 = Input.GetTouch(0);
				Touch touch2 = Input.GetTouch(1);
				Touch touch3 = Input.GetTouch(2);

				Vector2 deltaTouch1 = touch1.position - preTouch1.position;
				Vector2 deltaTouch2 = touch2.position - preTouch2.position;
				Vector2 deltaTouch3 = touch3.position - preTouch3.position;

				Vector2 deltaTouch = deltaTouch1 + deltaTouch2 + deltaTouch3;

				if (firstTouch)
				{
					//---- rotate around Z axis when fingers move horizontalliy
					if (Mathf.Abs(deltaTouch.x) > Mathf.Abs(deltaTouch.y))
					{
						touchType = "rotateZ";
					}
					//----- drag along z axix when fingers move vertically
					else
					{
						touchType = "dragZ";
					}
					firstTouch = false;
				}
				//----- lock the interaction type
				if (!firstTouch)
				{
					Vector2 rotDelta = (touch1.deltaPosition + touch2.deltaPosition + touch3.deltaPosition)/3;

					if (touchType == "rotateZ")
					{
						RotateZ(rotDelta);
					}
					else if (touchType == "dragZ")
					{
						DragZ(rotDelta);
					}
				}
			}
			else if (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(1).phase == TouchPhase.Ended || Input.GetTouch(2).phase == TouchPhase.Ended)
			{
				firstTouch = true;
				touchType = "";
			}
		}

		//----- add inertia motion
		if (underInertia == true)
		{
			if (inertiaTime <= SmoothTime)
			{
				inertiaTime += Time.smoothDeltaTime;
				if (touchType == "dragging")
				{

				}

			}
			else
			{
				underInertia = false;
				inertiaTime = 0.0f;
				touchType = "";
			}
		}

	}


	void DragXY(Touch touch)
	{
		//var dist = transform.position.z - Camera.main.transform.position.z;
		//Vector3 v3 = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dist);
		//v3 = Camera.main.ScreenToWorldPoint(v3);
		//transform.position = v3 + offsetPos;

		var zOffset = transform.localPosition.z;

		Vector2 touchDeltaPosition = touch.deltaPosition;
		Vector2 touchPre = touch.position - touch.deltaPosition;
		Vector3 touchPreWorld = new Vector3(touchPre.x, touchPre.y, zOffset);
		Vector3 touchWorld = new Vector3(touch.position.x, touch.position.y, zOffset);
		Vector3 touchDeltaWorld = Camera.main.ScreenToWorldPoint(touchWorld) - Camera.main.ScreenToWorldPoint(touchPreWorld);
		//touchDeltaPosition;

		var xOffset = transform.localPosition.x + touchDeltaWorld.x;
		var yOffset = transform.localPosition.y + touchDeltaWorld.y;
		transform.localPosition = new Vector3(xOffset, yOffset, zOffset);

		string touchEvent ="drag x & y";
		updateTxt(touchEvent);

		//message
		Vector3 touchOffsetPixel = Camera.main.WorldToScreenPoint(transform.localPosition) - Camera.main.WorldToScreenPoint(posOrigin);
		Vector3 touchOffsetCM = touchOffsetPixel * pixelToCM;
		//var xOffsetHolo = touchDeltaPosition.x * pixelToCM;
		//var yOffsetHolo = touchDeltaPosition.y * pixelToCM;
		string gstrType = "dragging";
		Vector3 positionChange = new Vector3(touchOffsetCM.x, touchOffsetCM.y, touchOffsetPixel.z);
		Quaternion rotationChange = new Quaternion(0,0,0,0);
		float scaleChange = 0;
		sendMessage(gstrType, positionChange, rotationChange, scaleChange);
	}

	void DragZ(Vector2 touchDeltaPosition)
	{
		var zOffset = transform.localPosition.z + touchDeltaPosition.y * dragSpeed;
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, zOffset);

		string touchEvent = "drag z";
		updateTxt(touchEvent);
		
		//message
		Vector3 touchOffsetPixel = Camera.main.WorldToScreenPoint(transform.localPosition) - Camera.main.WorldToScreenPoint(posOrigin);
		Vector3 touchOffsetCM = touchOffsetPixel * pixelToCM;
		Debug.Log("touch offset pixel:" + touchOffsetPixel);
		string gstrType = "dragging";
		Vector3 positionChange = new Vector3(touchOffsetCM.x, touchOffsetCM.y, touchOffsetPixel.z);
		Quaternion rotationChange = new Quaternion(0,0, 0, 0);
		float scaleChange = 0;
		sendMessage(gstrType, positionChange, rotationChange, scaleChange);
	}

	void RotateXY(float x, float y)
	{
		float rotX = x * rotSpeed * Mathf.Deg2Rad;
		float rotY = y * rotSpeed * Mathf.Deg2Rad;

		transform.Rotate(0, -rotX, 0, Space.World);
		transform.Rotate(rotY, 0, 0, Space.World);

		Quaternion rotCurrent = transform.localRotation;

		string touchEvent = "rotate x & y";
		updateTxt(touchEvent);
		
		//message
		string gstrType = "rotating";
		Vector3 positionChange = new Vector3(0, 0, 0);
		Quaternion rotationChange = Quaternion.Inverse(rotOrigin) * rotCurrent;
		//Vector3 rotationChange = new Vector3(rotY, -rotX, 0);
		float scaleChange = 0;
		sendMessage(gstrType, positionChange, rotationChange, scaleChange);
	}

	void RotateZ(Vector2 touchDeltaPosition)
	{
		float rotZ = touchDeltaPosition.x * rotSpeed * Mathf.Deg2Rad;
		transform.Rotate(0, 0, rotZ, Space.World);
		Quaternion rotCurrent = transform.localRotation;

		string touchEvent = "rotate z";
		updateTxt(touchEvent);

		//message
		string gstrType = "rotating";
		Vector3 positionChange = new Vector3(0, 0, 0);
		Quaternion rotationChange = Quaternion.Inverse(rotOrigin) * rotCurrent;
		float scaleChange = 0;
		sendMessage(gstrType, positionChange, rotationChange, scaleChange);
	}

	void Pinch(float deltaMagnitudeDiff)
	{
		var changeScale = deltaMagnitudeDiff * scaleFactor;
		transform.localScale += new Vector3(changeScale, changeScale, changeScale); ;
		transform.localScale = new Vector3(Mathf.Clamp(transform.localScale.x, minScale, maxScale), Mathf.Clamp(transform.localScale.y, minScale, maxScale), Mathf.Clamp(transform.localScale.z, minScale, maxScale));

		string touchEvent = "pinch";
		updateTxt(touchEvent);

		//message
		string gstrType = "pinching";
		Vector3 positionChange = new Vector3(0, 0, 0);
		Quaternion rotationChange = new Quaternion(0, 0, 0, 0);
		float scaleChange = transform.localScale.x / scaleOrigin.x;
		sendMessage(gstrType, positionChange, rotationChange, scaleChange);
	}

	//async 
		void sendMessage(string gstrType, Vector3 positionChange, Quaternion rotationChange, float scaleChange)
	{
		GstrMessage gstr = new GstrMessage();
		gstr.name = objName;
		gstr.gstrType = gstrType;
		gstr.posChange = positionChange;
		gstr.rotChange = rotationChange;
		gstr.scaleChange = scaleChange;
		//transfer to holo axis
		//gstr.pos = new Vector3(gstr.pos.x, gstr.pos.z, gstr.pos.y);
		string message = JsonUtility.ToJson(gstr);
		//await UDPCtrl.SendMessage(message);
	}

	void updateTxt(string touchEvent)
	{
		txtPosition.text = "("+transform.localPosition.x+", "+ transform.localPosition.y + ", " + transform.localPosition.z + ")";
		txtRotation.text = "(" + transform.localRotation.x + ", " + transform.rotation.y + ", " + transform.rotation.z + ")";
		txtTouchEvent.text = touchEvent;
		txtScale.text = transform.localScale.x.ToString();
	}

	IEnumerator MyTimer()
	{
		Debug.Log("about to yield return WaitForSeconds(1)");
		yield return new WaitForSeconds(1);
		Debug.Log("Just waited 1 second");
		yield break;
	}
//#endif
}

//--------------- lock the interaction type
//	if (firstTouch)
//	{
//		if (Math.Abs(touchDeltaPosition.x) - Math.Abs(touchDeltaPosition.y) > 1)
//		{
//			rotateX = true;
//			firstTouch = false;

//		}
//		else if (Math.Abs(touchDeltaPosition.y) - Math.Abs(touchDeltaPosition.x) > 1)
//		{
//			firstTouch = false;
//		}
//	}
//	else
//	{
//		// Start rotate
//		if (rotateX == true)
//		{
//			RotateXY(touchDeltaPosition.x,0);
//		}
//		else
//		{
//			RotateXY(0, touchDeltaPosition.y);
//		}
//	}
//}
//else if (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)
//{
//	rotateX = false;
//	firstTouch = true;
//}