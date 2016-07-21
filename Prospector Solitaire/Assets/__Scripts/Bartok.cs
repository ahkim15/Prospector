using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// this enum contains the different phases of a game turn
public enum TurnPhase {
	idle,
	pre,
	waiting,
	post,
	gameOver
}

public class Bartok : MonoBehaviour {
	static public Bartok S;
	static public Player CURRENT_PLAYER;

	public TextAsset		deckXML;
	public TextAsset		layoutXML;
	public Vector3			layoutCenter = Vector3.zero;

	// the number of degrees to fan each card in a hand
	public float			handFanDegrees = 10f;
	public int				numStartingCards = 7;
	public float			drawTimeStagger = 0.1f;

	public bool _____________;

	public Deck				deck;
	public List<CardBartok>	drawPile;
	public List<CardBartok>	discardPile;

	public BartokLayout		layout;
	public Transform		layoutAnchor;
	public List<Player>		players;
	public CardBartok		targetCard;
	public TurnPhase		phase = TurnPhase.idle;
	public GameObject		turnLight;

	public GameObject		GTGameOver;
	public GameObject		GTRoundResult;

	void Awake () {
		S = this;

		// find the TurnLight by name
		turnLight = GameObject.Find ("TurnLight");
		GTGameOver = GameObject.Find ("GTGameOver");
		GTRoundResult = GameObject.Find ("GTRoundResult");
		GTGameOver.SetActive (false);
		GTRoundResult.SetActive (false);
	}

	void Start () {
		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle (ref deck.cards);

		layout = GetComponent<BartokLayout> ();
		layout.ReadLayout (layoutXML.text);

		drawPile = UpgradeCardsList (deck.cards);
		LayoutGame ();
	}

	// UpgradeCardsList casts the Cards in lCD to be CardBartoks
	List<CardBartok> UpgradeCardsList (List<Card> lCD) {
		List<CardBartok> lCB = new List<CardBartok>();
		foreach (Card tCD in lCD) {
			lCB.Add (tCD as CardBartok);
		}
		return (lCB);
	}

	// position all the cards in the drawPile properly
	public void ArrangeDrawPile() {
		CardBartok tCB;

		for (int i=0; i < drawPile.Count; i++) {
			tCB = drawPile [i];
			tCB.transform.parent = layoutAnchor;
			tCB.transform.localPosition = layout.drawPile.pos;
			// rotation should start at 0
			tCB.faceUp = false;
			tCB.SetSortingLayerName (layout.drawPile.layerName);
			tCB.SetSortOrder (-i * 4);	// order them front to back
			tCB.state = CBState.drawpile;
		}
	}

	// perform the initial game layout
	void LayoutGame () {
		// create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null) {
			GameObject tGo = new GameObject ("_LayoutAnchor");
			// ^create an empty GameObject named _LayoutAnchor in the Hierarchy
			layoutAnchor = tGo.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		// position the drawPile cards
		ArrangeDrawPile ();

		// set up the players
		Player pl;
		players = new List<Player> ();
		foreach (SlotDef tSD in layout.slotDefs) {
			pl = new Player ();
			pl.handSlotDef = tSD;
			players.Add (pl);
			pl.playerNum = players.Count;
		}
		players [0].type = PlayerType.human;		// make the 0th player human

		CardBartok tCB;
		// deal 7 cards to each player
		for (int i = 0; i < numStartingCards; i++) {
			for (int j = 0; j <4; j++) {
				tCB = Draw ();
				tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);
				players [(j + 1) % 4].AddCard (tCB);
			}
		}

		// call Bartok.DrawFirstTarget() when the hand cards have been drawn
		Invoke ("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
	}

	public void DrawFirstTarget () {
		CardBartok tCB = MoveToTarget (Draw ());
		tCB.reportFinishTo = this.gameObject;
	}

	public void CBCallback (CardBartok cb) {
		Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.CBCallback()", cb.name);

		StartGame ();
	}

	public void StartGame () {
		PassTurn (1);
	}

	public void PassTurn (int num=-1) {
		if (num == -1) {
			int ndx = players.IndexOf (CURRENT_PLAYER);
			num = (ndx + 1) % 4;
		}
		int lastPlayerNum = -1;
		if (CURRENT_PLAYER != null) {
			lastPlayerNum = CURRENT_PLAYER.playerNum;
			// check for Game Over and need to reshuffle discards
			if (CheckGameOver()) {
				return;
			}
		}
		CURRENT_PLAYER = players [num];
		phase = TurnPhase.pre;

		CURRENT_PLAYER.TakeTurn ();

		Vector3 lPos = CURRENT_PLAYER.handSlotDef.pos + Vector3.back * 5;
		turnLight.transform.position = lPos;

		Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.PassTurn()",
		          "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
	}

	public bool ValidPlay (CardBartok cb) {
		if (cb.rank == targetCard.rank)
			return(true);
		if (cb.suit == targetCard.suit) {
			return (true);
		}
		return (false);
	}

	// this makes a new card the target
	public CardBartok MoveToTarget (CardBartok tCB) {
		tCB.timeStart = 0;
		tCB.MoveTo (layout.discardPile.pos + Vector3.back);
		tCB.state = CBState.toTarget;
		tCB.faceUp = true;
		tCB.SetSortingLayerName ("10");		// layout.target.layerName
		tCB.eventualSortLayer = layout.target.layerName;
		if (targetCard != null) {
			MoveToDiscard (targetCard);
		}

		targetCard = tCB;

		return (tCB);
	}

	public CardBartok MoveToDiscard (CardBartok tCB) {
		tCB.state = CBState.discard;
		discardPile.Add (tCB);
		tCB.SetSortingLayerName (layout.discardPile.layerName);
		tCB.SetSortOrder (discardPile.Count * 4);
		tCB.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

		return (tCB);
	}

	// the draw function will pull a single card from the drawpile and return it
	public CardBartok Draw () {
		CardBartok cd = drawPile [0];
		drawPile.RemoveAt (0);
		return (cd);
	}

/*	// this update method is used to test adding cards to players' hands
	void Update () {
		if (Input.GetKeyDown (KeyCode.Alpha1)) {
			players[0].AddCard (Draw());
		}
		if (Input.GetKeyDown (KeyCode.Alpha2)) {
			players[1].AddCard (Draw());
		}
		if (Input.GetKeyDown (KeyCode.Alpha3)) {
			players[2].AddCard (Draw());
		}
		if (Input.GetKeyDown (KeyCode.Alpha4)) {
			players[3].AddCard (Draw());
		}
	}
*/
	public void CardClicked (CardBartok tCB) {
		// if it's not the human's turn, don't respond
		if (CURRENT_PLAYER.type != PlayerType.human)
			return;
		// if the game is waiting on a card to move, don't respond
		if (phase == TurnPhase.waiting)
			return;

		// act differently based on whether it was a card in hand
		// or on the drawPile that was clicked
		switch (tCB.state) {
		case CBState.drawpile:
			// draw the top card, not necessarily the one clicked
			CardBartok cb = CURRENT_PLAYER.AddCard (Draw());
			cb.callbackPlayer = CURRENT_PLAYER;
			Utils.tr (Utils.RoundToPlaces (Time.time),
			          "Bartok.CardClicked()","Draw",cb.name);
			phase = TurnPhase.waiting;
			break;
		case CBState.hand:
			// check to see whether the card is valid
			if (ValidPlay(tCB)) {
				CURRENT_PLAYER.RemoveCard (tCB);
				MoveToTarget(tCB);
				tCB.callbackPlayer = CURRENT_PLAYER;
				Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.CardClicked()","Play",tCB.name,
				          targetCard.name+" is target");
				phase = TurnPhase.waiting;
			} else {
				// just ignore it
				Utils.tr (Utils.RoundToPlaces (Time.time), "Bartok.CardClicked()","Attempted to Play",
				          tCB.name,targetCard.name+" is target");
			}
			break;
		}
	}

	public bool CheckGameOver () {
		// see if we need to reshuffle the discard pile into the draw pile
		if (drawPile.Count == 0) {
			List<Card> cards = new List<Card>();
			foreach (CardBartok cb in discardPile) {
				cards.Add (cb);
			}
			discardPile.Clear ();
			Deck.Shuffle (ref cards);
			drawPile = UpgradeCardsList(cards);
			ArrangeDrawPile();
		}

		// check to see if the current player has won
		if (CURRENT_PLAYER.hand.Count == 0) {
			// the current player has won!
			if (CURRENT_PLAYER.type == PlayerType.human) {
				GTGameOver.guiText.text = "You Won!";
				GTRoundResult.guiText.text = "";
			} else {
				GTGameOver.guiText.text = "Game Over";
				GTRoundResult.guiText.text = "Player "+CURRENT_PLAYER.playerNum+ " won";
			}
			GTGameOver.SetActive (true);
			GTRoundResult.SetActive (true);
			phase = TurnPhase.gameOver;
			Invoke("RestartGame", 1);
			return(true);
		}

		return (false);
	}

	public void RestartGame () {
		CURRENT_PLAYER = null;
		Application.LoadLevel ("__Bartok_Scene_0");
	}
	
}
