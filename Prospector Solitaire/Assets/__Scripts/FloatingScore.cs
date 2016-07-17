using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FSState {
	idle,
	pre,
	active,
	post
}

// FloatingScore can move itself on screen following a Bezier curve
public class FloatingScore : MonoBehaviour {
	public FSState		state = FSState.idle;
	[SerializeField]
	private int			_score = 0;
	public string		scoreString;

	// the score property also sets scoreString when set
	public int score {
		get {
			return(_score);
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber(_score);
			GetComponent<GUIText>().text = scoreString;
		}
	}

	public List<Vector3>	bezierPts;		// bezier points for movement
	public List<float>		fontSizes;		// bezier points for font scaling
	public float			timeStart = -1f;
	public float			timeDuration = 1f;
	public string			easingCurve = Easing.InOut;

	// the gameObject that will receive the SendMessage when this is done moving
	public GameObject		reportFinishTo = null;

	// set up the FloatingScore and movement
	// note the use of parameter defaults for eTimeS & eTimeD
	public void Init (List<Vector3> ePts, float eTimeS = 0, float eTimeD = 1) {
		bezierPts = new List<Vector3>(ePts);

		if (ePts.Count == 1) {
			transform.position = ePts[0];
			return;
		}

		// if eTimes is the deafault, just start at the current time
		if (eTimeS == 0) eTimeS = Time.time;
		timeStart = eTimeS;
		timeDuration = eTimeD;

		state = FSState.pre;
	}

	public void FSCallback (FloatingScore fs) {
		score += fs.score;
	}

	void Update () {
		if (state == FSState.idle) return;

		float u = (Time.time - timeStart)/timeDuration;
		float uC = Easing.Ease (u, easingCurve);
		if (u < 0) {
			state = FSState.pre;
			transform.position = bezierPts[0];
		} else {
			if (u >= 1) {
				uC = 1;
				state = FSState.post;
				if (reportFinishTo != null) {
					reportFinishTo.SendMessage("FSCallback", this);
					Destroy (gameObject);
				} else {
					state = FSState.idle;
				}
			} else {
				state = FSState.active;
			}
			// use bezier curve to move this to the right point
			Vector3 pos = Utils.Bezier(uC, bezierPts);
			transform.position = pos;
			if (fontSizes != null && fontSizes.Count > 0) {
				int size = Mathf.RoundToInt (Utils.Bezier(uC, fontSizes));
				GetComponent<GUIText>().fontSize = size;
			}
		}
	}
}
