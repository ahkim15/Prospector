using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SlotDef {
	public float		x;
	public float		y;
	public bool			faceUp=false;
	public string		layerName="Default";
	public int			layerID = 0;
	public int			id;
	public List<int>	hiddenBy = new List<int>();
	public string		type="slot";
	public Vector2		stagger;
}

public class Layout : MonoBehaviour {
	public PT_XMLReader		xmlr;		// has a PT_XMLReader
	public PT_XMLHashtable	xml;		// easier xml access
	public Vector2			multiplier;	// sets spacing of the tablaeu
	// SlotDef references
	public List<SlotDef>	slotDefs;	// all the SlotDefs for Row0-Row3
	public SlotDef			drawPile;
	public SlotDef			discardPile;
	public string[]			sortingLayerNames = new string[] { "Row0", "Row1", "Row2", "Row3", "Discard", "Draw" };

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

		for (int i = 0; i < slotsX.Count; i++) {
			tSD = new SlotDef ();
			if (slotsX [i].HasAtt ("type")) {
				tSD.type = slotsX [i].att ("type");
			} else {
				tSD.type = "slot";
			}
			tSD.x = float.Parse (slotsX [i].att ("x"));
			tSD.y = float.Parse (slotsX [i].att ("y"));
			tSD.layerID = int.Parse (slotsX [i].att ("layer"));
			tSD.layerName = sortingLayerNames [tSD.layerID];

			switch (tSD.type) {
			case "slot":
				tSD.faceUp = (slotsX [i].att ("faceup") == "1");
				tSD.id = int.Parse (slotsX [i].att ("id"));
				if (slotsX [i].HasAtt ("hiddenby")) {
					string[] hiding = slotsX [i].att ("hiddenby").Split (',');
					foreach (string s in hiding) {
						tSD.hiddenBy.Add (int.Parse (s));
					}
				}
				slotDefs.Add (tSD);
				break;

			case "drawpile":
				tSD.stagger.x = float.Parse (slotsX [i].att ("xstagger"));
				drawPile = tSD;
				break;
			case "discardpile":
				discardPile = tSD;
				break;
			}
		}
	}
			
}
