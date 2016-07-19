using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;	// enables LINQ queries, which will be explained soon

// the player can either be human or an ai
public enum PlayerType {
	human,
	ai
}

// the individual player of the game
// NOte: player does NOT extend Monobehavior (or any other class)

[System.Serializable] // make the player class visible in the Inspector pane
public class Player {
	public PlayerType		type = PlayerType.ai;
	public int				playerNum;

	public List<CardBartok>	hand;	// the cards in this player's hand

	public SlotDef			handSlotDef;

	// add a card to the hand
	public CardBartok AddCard (CardBartok eCB) {
		if (hand == null)
			hand = new List<CardBartok> ();

		// add the card to the hand
		hand.Add (eCB);

		// sort the cards by rank using LINQ if this is a human
		if (type == PlayerType.human) {
			CardBartok[] cards = hand.ToArray ();	// copy hand to a new array
			cards = cards.OrderBy (cd => cd.rank).ToArray ();
			hand = new List<CardBartok> (cards);
		}
		FanHand ();
		return (eCB);
	}

	public void FanHand () {
		// startRot is the rotation about z of the first card
		float startRot = 0;
		startRot = handSlotDef.rot;
		if (hand.Count > 1) {
			startRot += Bartok.S.handFanDegrees * (hand.Count - 1) / 2;
		}
		// then each card is rotated handFanDegrees from that to fan the cards

		// move all the cards to their new positions
		Vector3 pos;
		float rot;
		Quaternion rotQ;
		for (int i=0; i < hand.Count; i++) {
			rot = startRot - Bartok.S.handFanDegrees * i;
			rotQ = Quaternion.Euler (0, 0, rot);

			pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
			pos = rotQ * pos;
			pos += handSlotDef.pos;
			pos.z = -0.5f * i;

			hand [i].transform.localPosition = pos;
			hand [i].transform.rotation = rotQ;
			hand [i].state = CBState.hand;
			hand [i].faceUp = (type == PlayerType.human);
			hand [i].SetSortOrder (i * 4);
		}
	}

	// remove a card from the hand
	public CardBartok RemoveCard (CardBartok cb) {
		hand.Remove (cb);
		return (cb);
	}

}
