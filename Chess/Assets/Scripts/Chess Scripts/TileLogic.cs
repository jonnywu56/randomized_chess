using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles logic for each tile on board, communicates when tile is pressed
public class TileLogic : MonoBehaviour
{
    // Tracks position of tile, current piece, and number of changes
    public (int row, int col) tilePos = (-1,-1);
    public string piece = "none";
    public int changeCounter = 0;

    // Prefab for displaying piece
    [SerializeField] private GameObject holder;

    // Tracks BoardLogic script and tile's SpriteRenderer
    private BoardLogic parentScript;
    private SpriteRenderer pieceDisplayed;

    // Sets parentScript, sets GameObject to hold image of piece
    void Awake()
    {
        parentScript = this.transform.parent.GetComponent<BoardLogic>();
        pieceDisplayed = Instantiate(holder,this.transform, false).GetComponent<SpriteRenderer>();
        if(!parentScript.localGame && parentScript.curTeam ==1){
            pieceDisplayed.transform.eulerAngles = new Vector3(180,0,0);
            pieceDisplayed.transform.localScale = new Vector3(-1,1,1);
        }
    }

    // Sends clicked position on click by calling BoardLogic.SelectedTile
    void OnMouseDown(){
    	parentScript.SelectedTile(tilePos);
    }

    // Sets color of tile
    public void ChangeColor(Color color){
    	this.GetComponent<SpriteRenderer>().color = color;
    }

    // Sets image of SpriteRenderer without changing changeCounter
    public void SetImage(string piece){
        SwapImage(piece,false);
    }

    // Sets image of SpriteRenderer and increases changeCounter
    public void ChangeImage(string piece){
    	SwapImage(piece,true);
    }

    // Sets image of SpriteRenderer and increases chageCounter if addChange is true
    private void SwapImage(string piece, bool addChange){
        if (string.Equals(piece,"none")){
    		pieceDisplayed.sprite = null;
    	} else {
            Sprite pieceSprite = Resources.Load<Sprite>("ChessPieces/"+piece);
            pieceDisplayed.sprite = pieceSprite;
    	}
        if(addChange){
            changeCounter++;
        }
    	this.piece = piece;
    }
}
