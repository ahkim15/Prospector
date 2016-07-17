using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Prospector : MonoBehaviour {
	static public Prospector	S;
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

	void Awake () {
		S = this;		// set up a singleton for prospector
	}

	void Start () {
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
			print ("Game Over. You Won! :)");
		} else {
			print ("Game Over. You Lost. :(");
		}
		// reload the scene, resetting the game
		Application.LoadLevel ("__Prospector_Scene_0");
	}
	
}
