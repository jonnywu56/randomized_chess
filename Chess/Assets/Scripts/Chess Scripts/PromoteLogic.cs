using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles promotions for wPromote & bPromote, communicates when tile is pressed
public class PromoteLogic : MonoBehaviour
{
    // Used to communicate to boardLogic
    private BoardLogic parentScript;

    // Finds parentScript upon creation
    void Awake()
    {
        parentScript = this.transform.parent.parent.GetComponent<BoardLogic>();
    }

    // Sends clicked position to BoardLogic by calling BoardLogic.PromotePiece
    void OnMouseDown(){
    	parentScript.PromotePiece(this.transform.localPosition.x);
        Destroy(this.transform.parent.gameObject);
    }
}
