using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//CBState includes both states for the game and to ___ states for movement
public enum CBState {
	drawpile,
	toHand,
	hand,
	toTarget,
	target,
	discard,
	to,
	idle
}

//CardBartok extends Card just as CardProspector did
public class CardBartok : Card {
	// these static fields are used to set values that will be the same for all instances of CardBartok
	static public float		MOVE_DURATION = 0.5f;
	static public string	MOVE_EASING = Easing.InOut;
	static public float		CARD_HEIGHT = 3.5f;
	static public float		CARD_WIDTH = 2f;

	public CBState			state = CBState.drawpile;

	// fields to store info the card will use to move and rotate
	public List<Vector3>	bezierPts;
	public List<Quaternion>	bezierRots;
	public float			timeStart, timeDuration;	// declares 2 fields

	// when the card is done moving, it will call reportFinishTo.SendMessage()
	public GameObject		reportFinishTo = null;

	// MoveTo tells the card to interpolate to a new position and rotation
	public void MoveTo (Vector3 ePos, Quaternion eRot) {
		// make new interpolation lists for the card
		// position & rotation will each have only 2 points
		bezierPts = new List<Vector3> ();
		bezierPts.Add (transform.localPosition);	// current position
		bezierPts.Add (ePos);	// new position
		bezierRots = new List<Quaternion> ();
		bezierRots.Add (transform.rotation);	// current rotation
		bezierRots.Add (eRot);		// new rotation

		if (timeStart == 0) {
			timeStart = Time.time;
		}
		timeDuration = MOVE_DURATION;

		state = CBState.to;
	}
	public void MoveTo (Vector3 ePos) {
		MoveTo (ePos, Quaternion.identity);
	}

	void Update () {
		switch (state) {
		case CBState.toHand:
		case CBState.toTarget:
		case CBState.to:
			float u = (Time.time - timeStart)/timeDuration;
			float uC = Easing.Ease (u, MOVE_EASING);

			if (u < 0) {
				transform.localPosition = bezierPts[0];
				transform.rotation = bezierRots[0];
				return;
			} else if ( u >= 1) {
				uC = 1;
				if (state == CBState.toHand)	state = CBState.hand;
				if (state == CBState.toTarget)	state = CBState.toTarget;
				if (state == CBState.to)		state = CBState.idle;
				// move to the final position
				transform.localPosition = bezierPts[bezierPts.Count-1];
				transform.rotation = bezierRots[bezierPts.Count-1];
				timeStart = 0;

				if (reportFinishTo != null) {
					reportFinishTo.SendMessage ("CBCallback", this);
					reportFinishTo = null;
				} else {
					// do nothing
				}
			} else {
				Vector3 pos = Utils.Bezier (uC, bezierPts);
				transform.localPosition = pos;
				Quaternion rotQ = Utils.Bezier (uC, bezierRots);
				transform.rotation = rotQ;

			}
			break;
		}
	}
	
}
