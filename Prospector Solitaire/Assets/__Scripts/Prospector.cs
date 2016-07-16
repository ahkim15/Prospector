using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Prospector : MonoBehaviour {
	static public Prospector	S;
	public Deck					deck;
	public TextAsset			deckXML;

	void Awake () {
		S = this;		// set up a singleton for prospector
	}

	void Start () {
		deck = GetComponent<Deck> ();	// get the deck
		deck.InitDeck (deckXML.text);	// pass deckXML to it
		Deck.Shuffle (ref deck.cards);	// this shuffles the deck
		// the ref keyword passes a reference to deck.cards, which allows deck.cards to be modified by Deck.Shuffle()
	}

}
