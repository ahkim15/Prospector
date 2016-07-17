using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The Scoreboard class manages showing the score to the player
public class Scoreboard : MonoBehaviour {
	public static Scoreboard S;

	public GameObject prefabFloatingScore;

	public bool ___________________;
	[SerializeField]
	private int	_score = 0;
	public string _scoreString;

	// the score property also sets the scoreString
	public int score {
		get {
			return(_score);
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber(_score);
		}
	}

	// the scoreString property also sets the GUIText.text
	public string scoreString {
		get {
			return(_scoreString);
		}
		set {
			_scoreString = value;
			GetComponent<GUIText>().text = _scoreString;
		}
	}

	void Awake () {
		S = this;
	}

	// when called by SendMessage, this adds the fs.score to this.score
	public void FSCallback (FloatingScore fs) {
		score += fs.score;
	}

	public FloatingScore CreateFloatingScore (int amt, List<Vector3> pts) {
		GameObject go = Instantiate (prefabFloatingScore) as GameObject;
		FloatingScore fs = go.GetComponent<FloatingScore> ();
		fs.score = amt;
		fs.reportFinishTo = this.gameObject;
		fs.Init (pts);
		return(fs);
	}

}
