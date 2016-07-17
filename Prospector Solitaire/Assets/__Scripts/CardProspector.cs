using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CardState {
	drawpile,
	tableau,
	target,
	discard
}

public class CardProspector : Card {
	// this is how you use the enum CardState
	public CardState				state = CardState.drawpile;
	public List<CardProspector>		hiddenBy = new List<CardProspector> ();
	public int						layoutID;
	public SlotDef					slotDef;

	// this allows the card to react to being clicked
	override public void OnMouseUpAsButton () {
		// call the CardClicked method on the Prospector singleton
		Prospector.S.CardClicked (this);
		// also call the base class (Card.cs) version of this method
		base.OnMouseUpAsButton ();
	}
}
