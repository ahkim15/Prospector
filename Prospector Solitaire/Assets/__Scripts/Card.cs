using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {
	public string	suit;					// suit of the card (C,D, H, or S)
	public int		rank;					// rank of the card (1-14)
	public Color	color = Color.black;	// color to tint pips
	public string	colS = "Black";			// or "Red". name of the color
	// this list holds all of the Decorator GameObjects
	public List<GameObject> decoGOs = new List<GameObject> ();
	// this list holds all of the Pip GameObjects
	public List<GameObject> pipGOs = new List<GameObject> ();

	public GameObject		back;	// the GameObject of the back of the card
	public CardDefinition	def;	// parsed from DeckXML.xml

	public bool faceUp {
		get {
			return (!back.activeSelf);
		}
		set {
			back.SetActive (!value);
		}
	}

}

[System.Serializable]
public class Decorator {
	// this class stores information about each decorator or pop from DeckXML
	public string	type;			// for card pips, type = "pip"
	public Vector3	loc;			// location of Sprite on the Card
	public bool		flip = false;	// whether to flip the Sprite vertically
	public float	scale = 1f;		// scale of the Sprite
}

[System.Serializable]
public class CardDefinition {
	// this class stores information for each rank of card
	public string			face;							// sprite to use for each face card
	public int				rank;							// the rank (1-13) of this card
	public List<Decorator>	pips = new List<Decorator>();	// pips used
}
