using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// SlotDef class is not based on MonoBehavior, so it doesn't need its own file
[System.Serializable]
public class SlotDef {
	public float		x;
	public float		y;
	public bool			faceUp=false;
	public string		layerName="Default";
	public int			layerID=0;
	public int			id;
	public List<int>	hiddenBy = new List<int>();
	public float		rot;
	public string		type = "slot";
	public Vector2		stagger;
	public int			player;
	public Vector3		pos;
}

public class BartokLayout : MonoBehaviour {
	public PT_XMLReader		xmlr;			
	public PT_XMLHashtable	xml;			// variable is for faster xml access
	public Vector2			multiplier;		// sets the spacing of the tableau
	// SlotDef references
	public List<SlotDef>	slotDefs;		// SlotDefs hands
	public SlotDef			drawPile;
	public SlotDef			discardPile;
	public SlotDef			target;

	// this function is called to read in the LayoutXML.xml file
	public void ReadLayout (string xmlText) {
		xmlr = new PT_XMLReader ();
		xmlr.Parse (xmlText);
		xml = xmlr.xml ["xml"] [0];

		// read in the multiplier, which sets card spacing
		multiplier.x = float.Parse (xml ["multiplier"] [0].att ("x"));
		multiplier.y = float.Parse (xml ["multiplier"] [0].att ("y"));

		// read in the slots
		SlotDef tSD;
		// slotsX is used as a shortcut to all the <slot>s
		PT_XMLHashList slotsX = xml ["slot"];

		for (int i =0; i < slotsX.Count; i++) {
			tSD = new SlotDef ();
			if (slotsX [i].HasAtt ("type")) {
				// if this <slot> has a type attribute parse it
				tSD.type = slotsX [i].att ("type");
			} else {
				// if not, set its type to "slot"
				tSD.type = "slot";
			}

			// various attributes are parsed into numerical values
			tSD.x = float.Parse (slotsX [i].att ("x"));
			tSD.y = float.Parse (slotsX [i].att ("y"));
			tSD.pos = new Vector3 (tSD.x * multiplier.x, tSD.y * multiplier.y, 0);

			// sorting layers
			tSD.layerID = int.Parse (slotsX [i].att ("layer"));
			// in this game, the sorting layers are named 1,2,3 .. through 10
			// this converts the number of the layerID into a text layerName
			tSD.layerName = tSD.layerID.ToString ();
			// the layers are used to make sure that the correct cards are on tyop of the others.
			// pull additional attributes based on the type of each <slot>
			switch (tSD.type) {
			case "slot":
				break;

			case "drawpile":
				tSD.stagger.x = float.Parse (slotsX [i].att ("xstagger"));
				drawPile = tSD;
				break;

			case "discardpile":
				discardPile = tSD;
				break;

			case "target":
				target = tSD;
				break;

			case "hand":
				tSD.player = int.Parse (slotsX [i].att ("player"));
				tSD.rot = float.Parse (slotsX [i].att ("rot"));
				slotDefs.Add (tSD);
				break;
			}
		}
	}
	
}
