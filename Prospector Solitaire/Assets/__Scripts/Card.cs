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
	public SpriteRenderer[]	spriteRenderers;

	void Start () {
		SetSortOrder (0);
	}

	public bool faceUp {
		get {
			return (!back.activeSelf);
		}
		set {
			back.SetActive (!value);
		}
	}

	// if spriteRenderers is not yet defined, this function defines it
	public void PopulateSpriteRenderers () {
		// if spriteRenderers is null or empty
		if (spriteRenderers == null || spriteRenderers.Length == 0) {
			// get SpriteRenderer Components of this GameObject and its children
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		}
	}

	// sets the sortingLayerName on all SpriteRenderer Components
	public void SetSortingLayerName (string tSLN) {
		PopulateSpriteRenderers ();

		foreach (SpriteRenderer tSR in spriteRenderers) {
			tSR.sortingLayerName = tSLN;
		}
	}

	// sets the sortingOrder of all SpriteRenderer Components
	public void SetSortOrder (int sOrd) {
		PopulateSpriteRenderers ();

		// the white background of the card is on bottom (sOrd)
		// on top of that are all the pips, decorators, face, etc (sOrd + 1)
		// the back is on top so that when visible, it covers the rest (sOrd + 2)

		// iterate through all the spriteRenderers as tSR
		foreach (SpriteRenderer tSR in spriteRenderers) {
			if (tSR.gameObject == this.gameObject) {
				// if the gameObject is this.gameObject, it's the background
				tSR.sortingOrder = sOrd;
				continue;
			}
			// each of the children of this GameObject are named
			// switch based on the names
			switch (tSR.gameObject.name) {
			case "back":
				tSR.sortingOrder = sOrd + 2;
				break;
			case "face":
			default:
				tSR.sortingOrder = sOrd + 1;
				break;
			}
		}
	}

	// virtual methods can be overridden by subclass methods with the same name
	virtual public void OnMouseUpAsButton () {
		print (name);	// when clicked, this outputs the card name
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
