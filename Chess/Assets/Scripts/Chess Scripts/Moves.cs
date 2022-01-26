using System.Collections.Generic;
using System;

public class Moves
{
    // Basic variables for all types of moves
    private (int row, int col) fromPos;
    private (int row, int col) toPos;
    private string turn;
    private string pieceMoved = "none";
    private string pieceTaken = "none";
    private List<(int row, int col, string piece)> options;
    
    // Specialized variables for specific types of moves
    private (int row, int col) enPos = (-10,-10);
    private bool castled = false;
    private string promotedPiece = "none";
    private bool check = false;
    private bool checkmate = false;
    private bool stalemate = false;

    // Tracks moveName to display in UI, pastState for takeback moves
    private string moveName = "";
    private List<(int row, int col, string piece, int changeCounter)> pastState;

    // Sets basic variables for move
    public Moves((int row, int col) fromPos,(int row, int col) toPos,string turn, string pieceMoved, string pieceTaken,
    List<(int row, int col, string piece)> options){
        this.fromPos=fromPos;
        this.toPos=toPos;
        this.turn=turn;
        this.pieceMoved=pieceMoved;
        this.pieceTaken=pieceTaken;
        this.options=options;
    }

    // Called for en passant takes, sets enPos value to track which square contained taken pawn
    public void EnPas((int row, int col) enPos){
        this.enPos=enPos;
    }

    // Called for castles, sets moveName to castle type, sets castled to true
    public void Castle(string side){
        castled = true;
        moveName = side;
    }

    // Called for promotions, sets promotedPiece to promoted piece
    public void Promote(string promotedPiece){
        this.promotedPiece=promotedPiece;
    }

    // Called when move puts opponent's king in check, sets check to true
    public void Check(){
        check=true;
    }
    
    // Called when move puts opponent's king in checkmate, sets check to false, checkmate to true
    public void Checkmate(){
        checkmate=true;
        check = false; 
    }

    // Called when move puts opponent in stalemate, sets stalemate to true
    public void Stalemate(){
        stalemate=true;
    }

    // Changes (row, col) to chess tile notation
    private string GridToChess((int row, int col) pos){
        return Convert.ToChar(pos.col+97)+(pos.row+1).ToString();
    }

    // Returns abbreivation for pieces used in algebraic notation
    private string PieceToAbbrev(string piece){
        if (String.Equals("Pawn",piece)){
            return "";
        } else if (String.Equals("Knight",piece)){
            return "N";
        }
        return piece.Substring(0,1);
    }

    // Creates move name in algebraic notation
    public void ConvertString(){
        // Castling moves already set moveName, so returns if castled is true
        if (castled){
            return;
        }

        // Tracks whether row and/or col of moving piece is needed
        bool includeRow=false;
        bool includeCol=false;
        (int row, int col, string piece) curEntry = options[0];

        // Logic for determining whether to include row and/or col
        foreach (var option in options){
            if(string.Equals(option.piece,curEntry.piece)){
                if (option.row==curEntry.row && option.col!=curEntry.col){
                    includeCol=true;
                } else if (option.col==curEntry.col && option.row!=curEntry.row) {
                    includeRow=true;
                }
            }
        } 
        
        // Creates moveName based on given variables
        moveName = moveName + PieceToAbbrev(curEntry.piece);

        if(includeCol && !string.Equals(curEntry.piece,"Pawn")){
            moveName = moveName + Convert.ToChar(curEntry.col+97);
        }
        if(includeRow){
            moveName = moveName + (curEntry.row+1).ToString();
        }

        string toName=GridToChess(toPos);

        if (!string.Equals(pieceTaken,"none") || enPos!=(-10,-10)){
            if(string.Equals(curEntry.piece,"Pawn")){
                moveName = moveName + Convert.ToChar(curEntry.col+97);
            }
            moveName = moveName + "x";
        }

        moveName = moveName + toName;

        if(!string.Equals(promotedPiece,"none")){
            moveName = moveName + PieceToAbbrev(promotedPiece);
        }

        if(check){
            moveName = moveName + "+";
        }

        if(checkmate){
            moveName = moveName + "#";
        }

        if (enPos!=(-10,-10)){
            moveName = moveName + " e.p.";
        }

        if (stalemate){
            moveName = "1/2 - 1/2";
        }

    }

    // Following functions are used to extract private values of moves

    public (int row, int col) GetFromPos(){
        return this.fromPos;
    }
    public (int row, int col) GetToPos(){
        return this.toPos;
    }
    public string GetColor(){
        return this.turn;
    }
    public string GetPieceTaken(){
        return this.pieceTaken;
    }
    public string GetPieceMoved(){
        return this.pieceMoved;
    }
    public string GetPiecePromotion(){
        return this.promotedPiece;
    }
    public string GetString(){
        return this.moveName;
    }
    public List<(int row, int col, string piece, int changeCounter)> GetPastState(){
        return this.pastState;
    }
    public void SetPastState(List<(int row, int col, string piece, int changeCounter)> pastState){
        this.pastState=pastState;
    }

}
