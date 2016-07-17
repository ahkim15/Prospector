using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ScoreEvent {
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}

public class Prospector : MonoBehaviour {
	static public Prospector	S;
	static public int			SCORE_FROM_PREV_ROUND = 0;
	static public int			HIGH_SCORE = 0;

	public float				reloadDelay = 1f;

	public Vector3				fsPosMid = new Vector3 (0.5f, 0.90f, 0);
	public Vector3				fsPosRun = new Vector3 (0.5f, 0.75f, 0);
	public Vector3				fsPosMid2 = new Vector3 (0.5f, 0.5f, 0);
	public Vector3				fsPosEnd = new Vector3 (1.0f, 0.65f, 0);

	public Deck					deck;
	public TextAsset			deckXML;
	public Layout				layout;
	public TextAsset			layoutXML;
	public Vector3				layoutCenter;
	public float				xOffset = 3;
	public float				yOffset = -2.5f;
	public Transform			layoutAnchor;

	public CardProspector		target;
	public List<CardProspector>	tableau;
	public List<CardProspector>	discardPile;
	public List<CardProspector> drawPile;

	// fields to track score info
	public int				chain = 0;		// of cards in this run
	public int				scoreRun = 0;
	public int				score = 0;
	public FloatingScore	fsRun;

	public GUIText			GTGameOver;
	public GUIText			GTRoundResult;

	void Awake () {
		S = this;		// set up a singleton for prospector
		// check for a high score in PlayerPrefs
		if (PlayerPrefs.HasKey ("ProspectorHighScore")) {
			HIGH_SCORE = PlayerPrefs.GetInt ("ProspectorHighScore");
		}
		// add the score from last round, which will be >0 if it was a win
		score += SCORE_FROM_PREV_ROUND;
		// and reset the SCORE_FROM_PREV_ROUND
		SCORE_FROM_PREV_ROUND = 0;

		// set up the GUITexts that show at the end of the round
		// get the GUIText Components
		GameObject go = GameObject.Find ("GameOver");
		if (go != null) {
			GTGameOver = go.GetComponent<GUIText>();
		}
		go = GameObject.Find ("RoundResult");
		if (go != null) {
			GTRoundResult = go.GetComponent<GUIText>();
		}
		// make them invisible
		ShowResultGTs(false);

		go = GameObject.Find ("HighScore");
		string hScore = "High Score: "+Utils.AddCommasToNumber(HIGH_SCORE);
		go.GetComponent<GUIText>().text = hScore;
	}

	void ShowResultGTs (bool show) {
		GTGameOver.gameObject.SetActive (show);
		GTRoundResult.gameObject.SetActive (show);
	}

	void Start () {
		Scoreboard.S.score = score;

		deck = GetComponent<Deck> ();	// get the deck
		deck.InitDeck (deckXML.text);	// pass deckXML to it
		//deck.Shuffle (ref deck.cards);	// this shuffles the deck
		// the ref keyword passes a reference to deck.cards, which allows deck.cards to be modified by Deck.Shuffle()

		layout = GetComponent<Layout> ();	// get the layout
		layout.ReadLayout (layoutXML.text);	// pass LayoutXML to it

		drawPile = ConvertListCardsToListCardProspectors (deck.cards);
		LayoutGame ();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD) {
		List<CardProspector> lCP = new List<CardProspector> ();
		CardProspector tCP;
		foreach (Card tCD in lCD) {
			tCP = tCD as CardProspector;
			lCP.Add (tCP);
		}
		return(lCP);
	}

	// the draw fx will pull a single card from the drawPile and return it
	CardProspector Draw() {
		CardProspector cd = drawPile [0];	// pull the 0th CardProspector
		drawPile.RemoveAt (0);				// then remove it from List<> drawPile
		return(cd);							// and return it
	}

	// convert from the layoutID int to the CardProspector with that ID
	CardProspector FindCardByLayoutID (int layoutID) {
		foreach (CardProspector tCP in tableau) {
			// search through all cards in the tableau List<>
			if (tCP.layoutID == layoutID) {
				return (tCP);
			}
		}
		return (null);
	}

	// LayoutGame() positions the initial tableau of cards, aka the "mine"
	void LayoutGame () {
		// create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject ("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		CardProspector cp;
		foreach (SlotDef tSD in layout.slotDefs) {
			cp = Draw ();
			cp.faceUp = tSD.faceUp;
			cp.transform.parent = layoutAnchor;
			cp.transform.localPosition = new Vector3 (
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID);
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.tableau;

			cp.SetSortingLayerName (tSD.layerName);		// set the sorting layers

			tableau.Add (cp);
		}

		// set which cards are hiding others
		foreach (CardProspector tCP in tableau) {
			foreach (int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutID (hid);
				tCP.hiddenBy.Add (cp);
			}
		}

		// set up the initial target card
		MoveToTarget (Draw ());

		// set up the Draw pile
		UpdateDrawPile ();
	}

	// CardClicked is called any time a card in the game is clicked
	public void CardClicked (CardProspector cd) {
		// the reaction is determined by the state of the clicked card
		switch (cd.state) {
		case CardState.target:
			break;
		case CardState.drawpile:
			MoveToDiscard (target);
			MoveToTarget (Draw ());
			UpdateDrawPile ();
			ScoreManager(ScoreEvent.draw);
			break;
		case CardState.tableau:
			// clicking a card in the tableau will check if it's a valid play
			bool validMatch = true;
			if (!cd.faceUp) {
				// if the card is face-down, it's not valid
				validMatch = false;
			}
			if (!AdjacentRank(cd, target)) {
				// if it's not an adjacent rank, it's not valid
				validMatch = false;
			}
			if (!validMatch) return;
			tableau.Remove (cd);
			MoveToTarget (cd);
			SetTableauFaces();
			ScoreManager(ScoreEvent.mine);
			break;
		}
		// check to see whether the game is over or not
		CheckForGameOver ();
	}

	// moves the current target to the discardPile
	void MoveToDiscard (CardProspector cd) {
		cd.state = CardState.discard;
		discardPile.Add (cd);
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID + 0.5f);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (-100 + discardPile.Count);
	}

	// make cd the new target card
	void MoveToTarget (CardProspector cd) {
		if (target != null)
			MoveToDiscard (target);
		target = cd;
		cd.state = CardState.target;
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (0);
	}

	// arranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile () {
		CardProspector cd;
		for (int i = 0; i < drawPile.Count; i++) {
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3 (
				layout.multiplier.x * (layout.drawPile.x + i*dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i*dpStagger.y),
				-layout.drawPile.layerID+0.1f*i);
			cd.faceUp = false;
			cd.state = CardState.drawpile;
			cd.SetSortingLayerName (layout.drawPile.layerName);
			cd.SetSortOrder (-10*i);
		}	
	}

	// return true if the two ards are adjacent in rank (A & K wrap around)
	public bool AdjacentRank (CardProspector c0, CardProspector c1) {
		// if either card is face-down, it's not adjacent
		if (!c0.faceUp || !c1.faceUp)
			return(false);
		// if they are 1 apart, they are adjacent
		if (Mathf.Abs (c0.rank - c1.rank) == 1) {
			return (true);
		}
		// if one is A and the other King, they're adjacent
		if (c0.rank == 1 && c1.rank == 13)
			return (true);
		if (c0.rank == 13 && c1.rank == 1)
			return (true);

		// otherwise, return false
		return (false);
	}

	// this turns cards in the Mine face-up or face-down
	void SetTableauFaces () {
		foreach (CardProspector cd in tableau) {
			bool fup = true;
			foreach (CardProspector cover in cd.hiddenBy) {
				if (cover.state == CardState.tableau) {
					fup = false;
				}
			}
			cd.faceUp = fup;
		}
	}

	// test whether the game is over
	void CheckForGameOver() {
		// if the tableau is empty, the game is over
		if (tableau.Count == 0) {
			// call GameOver() with a win
			GameOver (true);
			return;
		}
		// if there are still cards in the draw pile, the game's not over
		if (drawPile.Count > 0) {
			return;
		}
		// check for remaining valid plays
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank (cd, target)) {
				// if there is a valid play, the game's not over
				return;
			}
		}
		// since there are no valid plays, the game is over
		// call GameOver with a loss
		GameOver (false);
	}

	// called when the game is over
	void GameOver (bool won) {
		if (won) {
			ScoreManager(ScoreEvent.gameWin);
		} else {
			ScoreManager(ScoreEvent.gameLoss);
		}
		Invoke ("ReloadLevel", reloadDelay);
		//Application.LoadLevel ("__Prospector_Scene_0");
	}

	void ReloadLevel () {
		Application.LoadLevel ("__Prospector_Scene_0");
	}

	// ScoreManager handles all of the scoring
	void ScoreManager (ScoreEvent sEvt) {
		List<Vector3> fsPts;
		switch (sEvt) {
		// same things need to happen whether it's a draw, a win, or a loss
		case ScoreEvent.draw:	// drawing a card
		case ScoreEvent.gameWin:
		case ScoreEvent.gameLoss:
			chain = 0;
			score += scoreRun;
			scoreRun = 0;
			// add fsRun to the _Scoreboard score
			if (fsRun != null) {
				// create points for the bezier curve
				fsPts = new List<Vector3> ();
				fsPts.Add (fsPosRun);
				fsPts.Add (fsPosMid2);
				fsPts.Add (fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init (fsPts, 0, 1);
				// also adjust the fontSize
				fsRun.fontSizes = new List<float> (new float[] {28, 36, 4});
				fsRun = null;
			}
			break;
		case ScoreEvent.mine:
			chain++;
			scoreRun += chain;
			// creating a FloatingScore for this score
			FloatingScore fs;
			// move it from the mousePosition to fsPosRun
			Vector3 p0 = Input.mousePosition;
			p0.x /= Screen.width;
			p0.y /= Screen.height;
			fsPts = new List<Vector3> ();
			fsPts.Add (p0);
			fsPts.Add (fsPosMid);
			fsPts.Add (fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore (chain, fsPts);
			fs.fontSizes = new List<float> (new float[] {4, 50, 28});
			if (fsRun == null) {
				fsRun = fs;
				fsRun.reportFinishTo = null;
			} else {
				fs.reportFinishTo = fsRun.gameObject;
			}
			break;
		}

		// this second switch statement handles round wins and losses
		switch (sEvt) {
		case ScoreEvent.gameWin:
			GTGameOver.text = "Round Over";
			Prospector.SCORE_FROM_PREV_ROUND = score;
			print ("You won this round! Round score: " + score);
			GTRoundResult.text = "You won this round!\nRound Score: "+score;
			ShowResultGTs (true);
			break;
		case ScoreEvent.gameLoss:
			GTGameOver.text = "Game Over";
			if (Prospector.HIGH_SCORE <= score) {
				print ("You got the high score! High score: " + score);
				string sRR = "You got the high score!\nHigh Score: "+score;
				GTRoundResult.text = sRR;
				Prospector.HIGH_SCORE = score;
				PlayerPrefs.SetInt ("ProspectorHighScore", score);
			} else {
				print ("Your final score for the game was: " + score);
				GTRoundResult.text = "Your final score was: "+score;
			}
			ShowResultGTs(true);
			break;
		default:
			print ("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
			break;
		}
	}
	
}
