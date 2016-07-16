﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deck : MonoBehaviour {
	// suits
	public Sprite	suitClub;
	public Sprite	suitDiamond;
	public Sprite	suitHeart;
	public Sprite	suitSpade;

	public Sprite[]	faceSprites;
	public Sprite[]	rankSprites;

	public Sprite	cardBack;
	public Sprite	cardBackGold;
	public Sprite	cardFront;
	public Sprite	cardFrontGold;

	// prefabs
	public GameObject	prefabSprite;
	public GameObject	prefabCard;

	public bool _____________________;

	public PT_XMLReader					xmlr;
	public List<string>					cardNames;
	public List<Card>					cards;
	public List<Decorator>				decorators;
	public List<CardDefinition>			cardDefs;
	public Transform					deckAnchor;
	public Dictionary<string,Sprite>	dictSuits;

	// InitDeck is called by Prospector when it is ready
	public void InitDeck (string deckXMLText) {
		// this creates an anchor for all the Card GameObjects in the Hierarchy
		if (GameObject.Find ("_Deck") == null) {
			GameObject anchorGO = new GameObject ("_Deck");
			deckAnchor = anchorGO.transform;
		}

		// initialize the dictionary of SuitSprites with necessary Sprites
		dictSuits = new Dictionary<string, Sprite> () {
			{"C", suitClub},
			{"D", suitDiamond},
			{"H", suitHeart},
			{"S", suitSpade}
		};
		ReadDeck (deckXMLText);
		MakeCards ();
	}

	// ReadDeck parses w XML file passed to it into CardDefinitions
	public void ReadDeck (string deckXMLText) {
		xmlr = new PT_XMLReader ();		// create a new PT_XMLReader
		xmlr.Parse (deckXMLText);		// use that PT_XMLReader to parse DeckXML

		// this prints a test line to show you how xmlr can be used
		string s = "xml[0] decorator[0] ";
		s += "type=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("type");
		s += " x=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("x");
		s += " y=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("y");
		s += " scale=" + xmlr.xml ["xml"] [0] ["decorator"] [0].att ("scale");
		//print (s);

		// read decorators for all Cards
		decorators = new List<Decorator> ();	// Init the List of Decorators
		// grab a PT_XMLHashList of all <decorator>s in the XML file
		PT_XMLHashList xDecos = xmlr.xml["xml"] [0] ["decorator"];
		Decorator deco;
		for (int i = 0; i < xDecos.Count; i++) {
			deco = new Decorator ();
			deco.type = xDecos [i].att ("type");
			deco.flip = (xDecos [i].att ("flip") == "1");
			deco.scale = float.Parse (xDecos [i].att ("scale"));
			deco.loc.x = float.Parse (xDecos [i].att ("x"));
			deco.loc.y = float.Parse (xDecos [i].att ("y"));
			deco.loc.z = float.Parse (xDecos [i].att ("z"));
			decorators.Add (deco);
		}

		// read pip locations for each card number
		cardDefs = new List<CardDefinition> ();
		PT_XMLHashList xCardDefs = xmlr.xml ["xml"] [0] ["card"];
		for (int i = 0; i < xCardDefs.Count; i++) {
			CardDefinition cDef = new CardDefinition ();
			cDef.rank = int.Parse (xCardDefs [i].att ("rank"));
			PT_XMLHashList xPips = xCardDefs [i] ["pip"];
			if (xPips != null) {
				for (int j=0; j < xPips.Count; j++) {
					deco = new Decorator ();
					deco.type = "pip";
					deco.flip = (xPips [j].att ("flips") == "1");
					deco.loc.x = float.Parse (xPips [j].att ("x"));
					deco.loc.y = float.Parse (xPips [j].att ("y"));
					deco.loc.z = float.Parse (xPips [j].att ("z"));
					if (xPips [j].HasAtt ("scale")) {
						deco.scale = float.Parse (xPips [j].att ("scale"));
					}
					cDef.pips.Add (deco);
				}
			}
			if (xCardDefs [i].HasAtt ("face")) {
				cDef.face = xCardDefs [i].att ("face");
			}
			cardDefs.Add (cDef);
		}

	}

	// get the proper CardDefinition based on Rank (1 to 14 is Ace to King)
	public CardDefinition GetCardDefinitionByRank (int rnk) {
		// search through all of the CardDefinitions
		foreach (CardDefinition cd in cardDefs) {
			// if the rank is correct, return this definition
			if (cd.rank == rnk) {
				return (cd);
			}
		}
		return (null);
	}

	// make the Card GameObjects
	public void MakeCards () {
		// cardNames will be the names of cards to build. each suit goes from 1-13
		cardNames = new List<string> ();
		string[] letters = new string[] {"C", "D", "H", "S"};
		foreach (string s in letters) {
			for (int i = 0; i < 13; i++) {
				cardNames.Add (s+(i+1));
			}
		}

		// make a list to hold all the cards
		cards = new List<Card> ();
		// several variables that will be reused several times
		Sprite tS = null;
		GameObject tGO = null;
		SpriteRenderer tSR = null;

		// iterate through all of the card names that were just made
		for (int i = 0; i < cardNames.Count; i++) {
			// create a new Card GameObject
			GameObject cgo = Instantiate (prefabCard) as GameObject;
			// set the transform.parent of the new card to the anchor
			cgo.transform.parent = deckAnchor;
			Card card = cgo.GetComponent<Card>();	// get the Card Component

			// this just stacks the cards so that they're all in nice rows
			cgo.transform.localPosition = new Vector3((i%13)*3, i/13*4, 0);

			// assign basic values to the Card
			card.name = cardNames[i];
			card.suit = card.name[0].ToString ();
			card.rank = int.Parse (card.name.Substring(1));
			if (card.suit == "D" || card.suit == "H") {
				card.colS = "Red";
				card.color = Color.red;
			}
			// pull the CardDefinition for this card
			card.def = GetCardDefinitionByRank(card.rank);

			// add decorators
			foreach(Decorator deco in decorators) {
				if (deco.type == "suit") {
					// Instantiate a Sprite GameObject
					tGO = Instantiate (prefabSprite) as GameObject;
					// get the SpriteRenderer Component
					tSR = tGO.GetComponent<SpriteRenderer>();
					// set the Sprite to the proper suit
					tSR.sprite = dictSuits[card.suit];
				} else {
					tGO = Instantiate (prefabSprite) as GameObject;
					tSR = tGO.GetComponent <SpriteRenderer>();
					// get the proper Sprite to show this rank
					tS = rankSprites[card.rank];
					// assign this rank Sprite to the SpriteRenderer
					tSR.sprite = tS;
					// set the color of the rank to match the suit
					tSR.color = card.color;
				}
				// make the deco Sprites render above the Card
				tSR.sortingOrder = 1;
				// make the decorator Sprite a child of the Card
				tGO.transform.parent = cgo.transform;
				// set the localPosition based on the location from DeckXML
				tGO.transform.localPosition = deco.loc;
				// flip the doecorator if needed
				if (deco.flip) {
					// an euler rotation of 180 around the z-axis will flip it
					tGO.transform.rotation = Quaternion.Euler (0,0,180);
				}
				// set the scale to keep decos from being too big
				if (deco.scale != 1) {
					tGO.transform.localScale = Vector3.one * deco.scale;
				}
				// name this GameObject so it's easy to find
				tGO.name = deco.type;
				// add this deco GameObject to the List card.decoGOs
				card.decoGOs.Add(tGO);
			}

			// add Pips
			foreach (Decorator pip in card.def.pips) {
				// instantiate a Sprite GameObject
				tGO = Instantiate (prefabSprite) as GameObject;
				// set the parent to be the card GameObject
				tGO.transform.parent = cgo.transform;
				// set the position to that specified in the XML
				tGO.transform.localPosition = pip.loc;
				// flip it if necessary
				if (pip.flip) {
					tGO.transform.rotation = Quaternion.Euler (0,0,180);
				}
				// scale it if necessary (only for the Ace)
				if (pip.scale != 1) {
					tGO.transform.localScale = Vector3.one * pip.scale;
				}
				// give this GameObject a name
				tGO.name = "pip";
				// get the SpriteRenderer Component
				tSR = tGO.GetComponent<SpriteRenderer>();
				// set the Sprite to the proper suit
				tSR.sprite = dictSuits[card.suit];
				// set sortingOrder so the pip is rendered above the Card_Front
				tSR.sortingOrder = 1;
				// add this to the Card's list of pips
				card.pipGOs.Add (tGO);
			}

			// handle face cards
			if (card.def.face != " ") {
				tGO = Instantiate (prefabSprite) as GameObject;
				tSR = tGO.GetComponent <SpriteRenderer> ();
				// generate the right name and pass it to GetFace ()
				tS = GetFace (card.def.face+card.suit);
				tSR.sprite = tS;
				tSR.sortingOrder = 1;
				tGO.transform.parent = card.transform;
				tGO.transform.localPosition = Vector3.zero;
				tGO.name = "face";
			}

			// add card_back which will be able to cover everything esle on the Card
			tGO = Instantiate (prefabSprite) as GameObject;
			tSR = tGO.GetComponent<SpriteRenderer>();
			tSR.sprite = cardBack;
			tGO.transform.parent = card.transform;
			tGO.transform.localPosition = Vector3.zero;
			// this is a higher sortingOrder than anything else
			tSR.sortingOrder = 2;
			tGO.name = "back";
			card.back = tGO;

			// default to face-up
			card.faceUp = false;	// use the property faceUp of Card

			// add the card to the deck
			cards.Add (card);
		}
	}

	// find the proper face card Sprite
	public Sprite GetFace (string faceS) {
		foreach (Sprite tS in faceSprites) {
			if  (tS.name == faceS) {
				return(tS);
			}
		}
		// if nothing can be found, return null
		return (null);
	}

	// shuffle the Cards in Deck.cards
	static public void Shuffle (ref List<Card> oCards) {
		// create a temporary List to hold the new shuffle order
		List<Card> tCards = new List<Card> ();

		int ndx;
		tCards = new List<Card> ();
		while (oCards.Count > 0) {
			// pick the index of a random card
			ndx = Random.Range (0, oCards.Count);
			// add that card to the temporary List
			tCards.Add (oCards [ndx]);
			// and remove that card from the original list
			oCards.RemoveAt (ndx);
		}
		// replace the original List with the temporary List
		oCards = tCards;
	}

}
