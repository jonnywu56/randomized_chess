using System;
using System.Collections;
using System.Collections.Generic;

// Used to determine piece locations for different board set-ups
public class BoardSetup
{
    // Stores position of pieces on board
    private List<(int row, int col, string piece)> layoutList = new List<(int row, int col, string name)>{};
    // Used to generate random locations & orderings
    private System.Random rand = new System.Random();

    // Sets row1 and row2 to pieceName, excluding col 4 if kingRow is true
    private void SetRow(int row1, int row2, string pieceName, bool kingRow){
        for (int col=0;col<8;col++){
            if (!(kingRow && col==4)){
                layoutList.Add((row1,col,"w"+pieceName));
                layoutList.Add((row2,col,"b"+pieceName));
            }
        }
    }

    // Adds random royal piece (excluding king) to (row1, col) and (row2, col)
    private void AddRandomRoyal(int row1, int row2, int col){
        switch(rand.Next(4)){
            case 0: layoutList.Add((row1,col,"wRook")); layoutList.Add((row2,col,"bRook")); break;
            case 1: layoutList.Add((row1,col,"wBishop")); layoutList.Add((row2,col,"bBishop")); break;
            case 2: layoutList.Add((row1,col,"wKnight")); layoutList.Add((row2,col,"bKnight")); break;
            case 3: layoutList.Add((row1,col,"wQueen")); layoutList.Add((row2,col,"bQueen")); break;
            default: break;
        }
    }

    // Adds random piece (excluding king) to (row1, col) and (row2, col)
    private void AddRandom(int row1, int row2, int col){
        switch(rand.Next(5)){
            case 0: layoutList.Add((row1,col,"wRook")); layoutList.Add((row2,col,"bRook")); break;
            case 1: layoutList.Add((row1,col,"wBishop")); layoutList.Add((row2,col,"bBishop")); break;
            case 2: layoutList.Add((row1,col,"wKnight")); layoutList.Add((row2,col,"bKnight")); break;
            case 3: layoutList.Add((row1,col,"wQueen")); layoutList.Add((row2,col,"bQueen")); break;
            case 4: layoutList.Add((row1,col,"wPawn")); layoutList.Add((row2,col,"bPawn")); break;
            default: break;
        }
    }

    // Shuffles list
    public List<string> ShuffleList(List<string> arr){
        int len = arr.Count;  
        while (len > 1) {  
            len--;  
            int pos = rand.Next(len + 1);  
            string temp = arr[pos];  
            arr[pos] = arr[len];  
            arr[len] = temp;  
        }
        return arr;
    }

    // Creates list with piece locations based on moddnum
    public List<(int row, int col, string name)> PieceLayout(int modNum){
        switch(modNum){
            // Normal setup
            case 0:
                SetRow(1,6,"Pawn",false);
                List <(int row, string color)> rcList = new List <(int row, string color)> {(0,"w"),(7,"b")};
                foreach (var rc in rcList){
                    layoutList.Add((rc.row,0,rc.color+"Rook"));
                    layoutList.Add((rc.row,1,rc.color+"Knight"));
                    layoutList.Add((rc.row,2,rc.color+"Bishop"));
                    layoutList.Add((rc.row,3,rc.color+"Queen"));
                    layoutList.Add((rc.row,4,rc.color+"King"));
                    layoutList.Add((rc.row,5,rc.color+"Bishop"));
                    layoutList.Add((rc.row,6,rc.color+"Knight"));
                    layoutList.Add((rc.row,7,rc.color+"Rook"));
                }
                break;

            // Shuffled: Back line is scrambled
            case 1: 
                SetRow(1,6,"Pawn",false);
                List<string> royals = new List<string> {"Rook","Rook","Knight","Knight","Bishop","Bishop","Queen","King"};
                royals = ShuffleList(royals);
                for(int col = 0; col<8;col++){
                    layoutList.Add((0,col,"w"+royals[col]));
                    layoutList.Add((7,col,"b"+royals[col]));
                }
                break;

            // Shuffled II: All pieces scrambled
            case 2:
                List<string> pieces = new List<string> {"Pawn","Pawn","Pawn","Pawn","Pawn","Pawn","Pawn","Pawn","Rook","Rook","Knight","Knight","Bishop","Bishop","Queen","King"};
                pieces = ShuffleList(pieces);
                for(int col = 0; col<8;col++){
                    // Make sure king isn't on front row to avoid instant check
                    if(string.Equals(pieces[2*col+1],"King")){
                        layoutList.Add((0,col,"w"+pieces[2*col+1]));
                        layoutList.Add((7,col,"b"+pieces[2*col+1]));
                        layoutList.Add((1,col,"w"+pieces[2*col]));
                        layoutList.Add((6,col,"b"+pieces[2*col]));

                    } else {
                        layoutList.Add((0,col,"w"+pieces[2*col]));
                        layoutList.Add((7,col,"b"+pieces[2*col]));
                        layoutList.Add((1,col,"w"+pieces[2*col+1]));
                        layoutList.Add((6,col,"b"+pieces[2*col+1]));
                    }
                }
                break;

            // Random: All backline pieces random
            case 3:
                int kingCol = rand.Next(8);
                SetRow(1,6,"Pawn",false);
                for (int col=0;col<8;col++){
                    if (col==kingCol){
                        layoutList.Add((0,col,"wKing"));
                        layoutList.Add((7,col,"bKing"));
                    } else {
                        AddRandomRoyal(0,7,col);
                    }
                }
                break;
            
            // Random II: All pieces (except King) random
            case 4:
                int kingCol2 = rand.Next(8);
                for (int col=0;col<8;col++){
                    if (col==kingCol2){
                        layoutList.Add((0,col,"wKing"));
                        layoutList.Add((7,col,"bKing"));
                    } else {
                        AddRandom(0,7,col);
                    }
                    AddRandom(1,6,col);
                }
                break;

            // Army: Backline replaced with pawns
            case 5:
                SetRow(1,6,"Pawn",false);
                SetRow(0,7,"Pawn",true);
                layoutList.Add((0,4,"wKing"));
                layoutList.Add((7,4,"bKing"));
                break;

            // Army II: Backline replaced with rooks
            case 6:
                SetRow(1,6,"Pawn",false);
                SetRow(0,7,"Rook",true);
                layoutList.Add((0,4,"wKing"));
                layoutList.Add((7,4,"bKing"));
                break;

            // Army III: Backline replaced with knights
            case 7:
                SetRow(1,6,"Pawn",false);
                SetRow(0,7,"Knight",true);
                layoutList.Add((0,4,"wKing"));
                layoutList.Add((7,4,"bKing"));
                break;

            // Army IV: Backline replaced with bishops
            case 8:
                SetRow(1,6,"Pawn",false);
                SetRow(0,7,"Bishop",true);
                layoutList.Add((0,4,"wKing"));
                layoutList.Add((7,4,"bKing"));
                break;

            // Army V: Backline replaced with queens
            case 9:
                SetRow(1,6,"Pawn",false);
                SetRow(0,7,"Queen",true);
                layoutList.Add((0,4,"wKing"));
                layoutList.Add((7,4,"bKing"));
                break;

            // Reversed: Pawns and backline are swapped - Can't possibly start off in check
            case 10:
                SetRow(0,7,"Pawn",false);
                List <(int row, string color)> rcListRev = new List <(int row, string color)> {(1,"w"),(6,"b")};
                foreach (var rc in rcListRev){
                    layoutList.Add((rc.row,0,rc.color+"Rook"));
                    layoutList.Add((rc.row,1,rc.color+"Knight"));
                    layoutList.Add((rc.row,2,rc.color+"Bishop"));
                    layoutList.Add((rc.row,3,rc.color+"Queen"));
                    layoutList.Add((rc.row,4,rc.color+"King"));
                    layoutList.Add((rc.row,5,rc.color+"Bishop"));
                    layoutList.Add((rc.row,6,rc.color+"Knight"));
                    layoutList.Add((rc.row,7,rc.color+"Rook"));
                }
                break;

            default:
                break; 

        }
        return layoutList;

    }
}
