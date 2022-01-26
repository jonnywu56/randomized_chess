using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Controls move display for game UI
public class MoveUI : MonoBehaviour
{
    // Prefab for text of each turn's moves
    [SerializeField] private GameObject moveTemplate;
    // Tracks current game's moves
    private List<GameObject> moves = new List<GameObject>();
    
    // Adds move to move display
    public void AddMove(string move, string color){
        // White: Create new GameObject, extend RectTransform
        if (string.Equals(color,"w")){
            GameObject newMove = Instantiate(moveTemplate,Vector3.zero,Quaternion.identity,this.transform);
            newMove.name = "Move "+(moves.Count+1).ToString();
            newMove.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (moves.Count+1).ToString();
            newMove.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = move;

            moves.Add(newMove);
            this.GetComponent<RectTransform>().sizeDelta = new Vector2(200,30*(moves.Count)); 
        // Black: Adds moveName to most recent GameObject
        } else {
            GameObject lastMove = moves[moves.Count-1];
            lastMove.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = move;
        }
        // Scroll down to bottom of move list
        GameObject.Find("/Canvas/GameUI/Scroll").GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
    }

    // Removes move from move display
    public void RemoveMove(){
        GameObject lastMove = moves[moves.Count-1];
        TextMeshProUGUI blackTMP = lastMove.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        // White: Destroys last created GameObject
        if(string.Equals(blackTMP.text,"")){
            GameObject blackMove = moves[moves.Count-1];
            moves.RemoveAt(moves.Count-1);
            Destroy(blackMove);
            this.GetComponent<RectTransform>().sizeDelta = new Vector2(200,30*(moves.Count)); 
        // Black: Set black move text to empty
        } else {
            blackTMP.text="";
        }
    }
    
    // Removes all moves from move display
    public void DestroyMoves(){
        // Removes move from move display, destroys relevant GameObject
        while (moves.Count>0){
            GameObject move = moves[moves.Count-1];
            moves.RemoveAt(moves.Count-1);
            Destroy(move);
        }
        // Resets RectTransform
        this.GetComponent<RectTransform>().sizeDelta = new Vector2(0,0);
    }
}
