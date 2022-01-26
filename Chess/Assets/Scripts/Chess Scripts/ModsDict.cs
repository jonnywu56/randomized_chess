using System.Collections.Generic;

// Used to track name & description of mods
public class ModsDict
{
    // Type 0 Mods: Determines initial setup of the board
    private List<(string modName, string modDesc)> mod0List = new List<(string modName, string modDesc)> () {
        ("Normal Setup", "No setup modifier"),
        ("Scrambled", "Back row has random placements"),
        ("Scrambled II", "All pieces have random placements"),
        ("Random", "Back row is random"),
        ("Random II", "All pieces are random"),
        ("Army of Pawns", "Back row replaced with pawns"),
        ("Army of Rooks", "Back row replaced with rooks"),
        ("Army of Knights", "Back row replaced with knights"),
        ("Army of Bishops", "Back row replaced with bishops"),
        ("Army of Queens", "Back row replaced with queens"),
        ("Reversed","Back row and front row are swapped")
    };

    // Type 1 Mods: Impacts gameplay and piece movement
    private List<(string modName, string modDesc)> mod1List = new List<(string modName, string modDesc)> () {
        ("Normal Gameplay", "No gameplay modifier"),
        ("Horsepower", "Knights have improved moveset"),
        ("Shortsighted", "Pieces can only travel up to 3 squares per move"),
        ("Duel", "Pieces except king die when capturing other pieces"),
        ("Piece Game", "Pieces upgrade upon capturing another piece"),
        ("Scouts", "Pawns can move forward any number of squares"),
        ("Untouchable", "Queens can only be taken by other queens"),
        ("Divine Birthright", "Kings can move like queens"),
        ("Royal Power", "Pawns can only take other pawns"),
        ("Medusa", "Tiles trap pieces after being moved to/from 15 times"),
        ("Cooldown", "Pieced cannot be moved two turns in a row")
    };

    // Type 2 Mods: Determines how games can be won
    private List<(string modName, string modDesc)> mod2List = new List<(string modName, string modDesc)> () {
        ("Normal Win", "No win condition modifiers"),
        ("Elimination", "Take all enemy pieces to win"),
        ("Ascension", "Pawn promotion also wins game"),
        ("Escort", "King crossing center row also wins game"),
        ("Survival", "Most surviving pieces after 30 turns wins")
 
    };

    // Returns modName and modDesc given modType and modNum
    public (string modName, string modDesc) Print(int modType, int modNum){
        switch(modType){
            case 0: return mod0List[modNum];
            case 1: return mod1List[modNum];
            case 2: return mod2List[modNum];
            default: return ("Error", "ModNum not found"); 
        }
    }

    // Returns number of mods that are modType
    public int DictLength(int dictNum){
        switch (dictNum){
            case 0: return mod0List.Count;
            case 1: return mod1List.Count;
            case 2: return mod2List.Count;
            default: return 0;
        }
    }
}
