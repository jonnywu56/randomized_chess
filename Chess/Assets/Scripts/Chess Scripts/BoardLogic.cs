using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using TMPro;

// Handles game logic and updating GameUI text
public class BoardLogic : MonoBehaviour
{
	// Used for tiles of chess board
    [SerializeField] private GameObject tile;
	// Selection displayed when promoting pieces
    [SerializeField] private GameObject wPromote;
    [SerializeField] private GameObject bPromote;
	// Includes all buttons & text not part of board when playing
	[SerializeField] private GameObject gameUI;

	// Sets colors of board and highlighted squares
    private Color oddColor = new Color32(118,150,86,255);
    private Color evenColor = new Color32(238,238,210,255);
    private Color selectColor = new Color32(226,184,63,255);
    private Color potentialColor = new Color32(226,100,63,255);
    private Color checkColor = new Color32(255,20,20,255);

	// Contains TileLogic scripts of all tiles
    private TileLogic [,] boardTiles = new TileLogic[8,8];
	// Contains list of all possible moves for pieces of current turn
	private List<(int row, int col)>[,] boardTilesMoves = new List<(int row, int col)> [8,8];
	// Contains list of past moves made
	private List<Moves> boardMoves = new List<Moves>();

	// Tracks pos of currently selected tile, value is (-10,-10) if none selected
    private (int row, int col) selectPos = (-10,-10);
	// Tracks king pos if in check, value is (-10,-10) if king not in check
    private (int row, int col) checkPos = (-10, -10);

	// Different game states for turns
    private enum GameState{whiteMove, blackMove, whitePromotion, blackPromotion, whiteWin, blackWin, stalemate, oppLeft}
	// Starts off as blackMove b/c NextTurn() called at start of every game
    private GameState curState  = GameState.blackMove;

	// Used to track randomizers, values are randomized before each match
	private int[] mods = {0,0,0};
	// Used to randomize values of mods
	private System.Random rand = new System.Random();

	// Multiplayer variables
	// Tracks number of connected players
	private int playerCount = -1;
	// Tracks color of current player, 0 for white, 1 for black
	public int curTeam = -1;
	// Tracks whether current game is local
	public bool localGame = true;
	// Tracks player inputs for rematches, takebacks, and quits
	private int [] rematchArray = {-1,-1};
	// Used to set up board at start of each match (needed so both players have same setup for random setup modifiers)
	private string [,] boardSetup = new string[8,8];


	// Called when object created, registers events to listen for
	private void Awake(){
		RegisterEvents();
    }

	// Creates tiles on board, sets mods if local game
	public void BoardStart(){
		// Resets position of board for white
		this.transform.position = new Vector3(0,2,0);
		this.transform.eulerAngles = new Vector3(0,0,0);
		this.transform.localScale = new Vector3(1,1,1);

		// Create board with 8x8 tiles
    	for (int row = 0; row < 8; row++){
        	for (int col = 0; col < 8; col++){
				// Creates tiles with appropriate position
        		Vector3 pos = new Vector3((float)(-3.5)+col,(float)(-3.5)+row,0);
        		GameObject newSquare = Instantiate(tile,pos,Quaternion.identity,this.transform);
        		newSquare.name = "Tile: Row "+row.ToString()+" Col: "+col.ToString();

				// Updates tile row and col for tileScript calls to SelectedTile
        		TileLogic tileScript = newSquare.GetComponent<TileLogic>();
        		tileScript.tilePos.row=row;
        		tileScript.tilePos.col=col;
        		newSquare.GetComponent<SpriteRenderer>().color = calcColor(tileScript.tilePos);
	        	
				// Adds tileScript to boardTiles
	        	boardTiles[row, col] = tileScript;
       		}
        }

        // Set up pieces and randomizers if local game
		if(localGame){
			curTeam = 1;
			// Sets randomizers
			mods[0]= rand.Next(11);
			mods[1]= rand.Next(11);
			mods[2]= rand.Next(5);
			// Finds appropriate setup from BoardSetup object
			BoardSetup bSetup = new BoardSetup();
			foreach (var piece in bSetup.PieceLayout(mods[0])){
				boardTiles[piece.row,piece.col].SetImage(piece.name);
			}
			// Resets position of board for white
			this.transform.position = new Vector3(0,2,0);
			this.transform.eulerAngles = new Vector3(0,0,0);
			this.transform.localScale = new Vector3(1,1,1);
		}
		// Multiplayer: Uses pieceLayout to set up pieces (so both players have same setup)
		else {
			// Sets up player pieces with pieceLayout
			for(int row=0;row<8;row++){
				for(int col=0;col<8;col++){
					boardTiles[row,col].SetImage(boardSetup[row,col]);
				}
			}
			// Sets position of board for black
			if(curTeam==1){
				this.transform.position = new Vector3(0,-2,0);
				this.transform.eulerAngles = new Vector3(180,0,0);
				this.transform.localScale = new Vector3(-1,1,1);
			}
		}

		// Set up game UI
		gameUI.SetActive(true);
		WriteMods();
	
		// Moves to next turn in case of check
		NextTurn(false);
	}

    // Calculates default color of square based on position
    private Color calcColor((int row, int col) tilePos){
    	if ((tilePos.row+tilePos.col)%2==0) { 
			return oddColor;
		} else { 
			return evenColor;
		}
    }

    // Returns tile status, can be "none", "w", "b", "oob"
    private string TileStatus((int row, int col) tilePos){
    	if (tilePos.row>=0 && tilePos.row <=7 && tilePos.col >= 0 && tilePos.col<= 7) {
    		string pieceName = boardTiles[tilePos.row,tilePos.col].piece;
    		if(string.Equals(pieceName,"none")){
	    		return "none";
	    	}
	    	return pieceName.Substring(0,1);
	    	}
    	return "oob";
    }

	// After selecting tile, changes colors and handles selection logic
    public void SelectedTile((int row, int col) tilePos){
 
		// Restore color of all tiles
    	for (int rowInc=0; rowInc < 8; rowInc++){
    		for (int colInc=0; colInc < 8; colInc++){
    			boardTiles[rowInc,colInc].ChangeColor(calcColor((rowInc,colInc)));
    		}
    	}
    
    	// Highlights check
		if(checkPos!=(-10,-10)){
			boardTiles[checkPos.row,checkPos.col].ChangeColor(checkColor);
		}

		// Checks if correct player is selecting tile
		if((curState == GameState.whiteMove && curTeam!=0) || (curState == GameState.blackMove && curTeam!=1)){
			return;
		} 
    	
    	// Checks if actual move being made (selected tile on potentialPos list)
    	if ((selectPos != (-10,-10)) && boardTilesMoves[selectPos.row,selectPos.col].Contains(tilePos)){
    		MovePiece(selectPos,tilePos);
    		return;
    	}

    	// Checks if selected tile is right color
    	string color = boardTiles[tilePos.row,tilePos.col].piece.Substring(0,1);
    	if (!(curState==GameState.whiteMove && string.Equals(color,"w")) &&
    		 !(curState==GameState.blackMove && string.Equals(color,"b"))) {
    		selectPos = (-10,-10);
    		return;
    	}

    	// Selects new tile of correct color
    	if(tilePos!=selectPos){
    		boardTiles[tilePos.row,tilePos.col].ChangeColor(selectColor);
    		
    		foreach (var pos in boardTilesMoves[tilePos.row,tilePos.col]){
    			boardTiles[pos.row,pos.col].ChangeColor(potentialColor);
    		}
    		selectPos = tilePos;

    	// Selects same tile (should deselect tile)
    	} else {
    		selectPos = (-10,-10);
    	}
    }

    // Given pos, returns list all squares that piece can move to 
    private List<(int row, int col)> PotentialTiles((int row, int col) tilePos) {
		// Creates list of tiles that can be moved to
    	List<(int row, int col)> potentialPos = new List<(int row, int col)>();

		// Extracts piece and color of piece from tilePos
    	string pieceName = boardTiles[tilePos.row,tilePos.col].piece;
		// Modifier 1.9: Tiles trap pieces after being moved from/on 15 times (Note: Trapped pieces can still check king)
    	if(string.Equals(pieceName,"none") || (mods[1]==9 && boardTiles[tilePos.row,tilePos.col].changeCounter>=15)){
    		return potentialPos;
    	}
		// Modifier 1.10: Pieces can't be moved over consecutive turns (Note: Frozen pieces can still check king)
		if(mods[1]==10 && boardMoves.Count>=2){
			Moves pastMove = boardMoves[boardMoves.Count-2];
			if (pastMove.GetToPos() == tilePos){
				return potentialPos;
			}
		}
    	string color = pieceName.Substring(0,1);
    	string piece = pieceName.Substring(1);
    	
		// Calculates opposite color
    	string oppColor;
    	if (string.Equals(color,"w")){
    		oppColor = "b";
    	} else {
    		oppColor = "w";
    	}
		
    	// Pawn logic 
    	if(string.Equals(piece,"Pawn")){
			// Creates list of pos where en passant takes are possible
			List<(int row, int col)> enPassPos = new List<(int row, int col)>();
			if (boardMoves.Count>0){
				Moves lastMove = boardMoves[boardMoves.Count-1];
				if (string.Equals(lastMove.GetPieceMoved(),"Pawn")){
					int loRow = Math.Min(lastMove.GetFromPos().row, lastMove.GetToPos().row);
					int hiRow = Math.Max(lastMove.GetFromPos().row, lastMove.GetToPos().row);
					for (int i=loRow+1;i<hiRow;i++){
						enPassPos.Add((i,lastMove.GetToPos().col));
					}
				}
			}

			int changeCounter = boardTiles[tilePos.row,tilePos.col].changeCounter;
  			int pawnMult = (string.Equals(color,"w")) ? 1 : -1;
				// Pawn - Move up one
				if (mods[1]!=5){
					(int row, int col) oneUp = (tilePos.row+pawnMult*1,tilePos.col);
					if(string.Equals(TileStatus(oneUp),"none")) {
						potentialPos.Add(oneUp);
						// Pawn - Move up two
						(int row, int col) twoUp = (tilePos.row+pawnMult*2,tilePos.col);
						if((changeCounter == 0) && string.Equals(TileStatus(twoUp),"none")) {
							potentialPos.Add(twoUp);
						}
					}
				} else{
					// Modifier 1.5: Pawns can move forward any number of squares
					int inc = 1;
					(int row, int col) nextPos =  (tilePos.row+pawnMult*inc,tilePos.col);
					while(string.Equals(TileStatus(nextPos),"none")) {
						potentialPos.Add(nextPos);
						inc++;
						nextPos = (tilePos.row+pawnMult*inc,tilePos.col);
					}
				}
				// Pawn - Take left and right
				(int row, int col) leftUp = (tilePos.row+pawnMult*1,tilePos.col-1);
				(int row, int col) rightUp = (tilePos.row+pawnMult*1, tilePos.col+1);

				if(string.Equals(TileStatus(leftUp),oppColor) || enPassPos.Contains(leftUp)){
					// Modifier 1.6: Only queens can take queens
					bool queenTake = (mods[1]==6) && string.Equals(boardTiles[leftUp.row,leftUp.col].piece.Substring(1),"Queen");
					// Modifier 1.8: Pawns can only take other pawns
					bool royalTake = (mods[1]==8) && !string.Equals(boardTiles[leftUp.row,leftUp.col].piece.Substring(1),"Pawn");
					if(!(queenTake || royalTake)){
						potentialPos.Add(leftUp);
					}
				} 
				if(string.Equals(TileStatus(rightUp),oppColor) || enPassPos.Contains(rightUp)){
					bool queenTake = (mods[1]==6) && string.Equals(boardTiles[rightUp.row,rightUp.col].piece.Substring(1),"Queen");
					// Modifier 1.8: Pawns can only take other pawns
					bool royalTake = (mods[1]==8) && !string.Equals(boardTiles[rightUp.row,rightUp.col].piece.Substring(1),"Pawn");
					if(!(queenTake || royalTake)){
						potentialPos.Add(rightUp);
					}
				} 
		}	

    	// Knight logic
    	if(string.Equals(piece,"Knight")){
    		(int row, int col) nextPos;
    		List<(int rowMult, int colMult)> mults = new List<(int row, int col)>
    			{(2,1),(-2,1),(2,-1),(-2,-1),(1,2),(-1,2),(1,-2),(-1,-2)};
			// Modifier 1.1: Greatly increase horse mobility
			if(mods[1]==1){
				mults.AddRange(new List<(int row, int col)>
    			{(2,2),(-2,2),(2,-2),(-2,-2),(2,0),(0,2),(-2,0),(0,-2)});
			}

			// Looks through possible moves to add to potential positions
    		foreach (var mult in mults){
    			nextPos = (tilePos.row+mult.rowMult,tilePos.col+mult.colMult);
    			if (string.Equals(TileStatus(nextPos),oppColor) || string.Equals(TileStatus(nextPos),"none")){
    				// Modifier 1.6: Only queens can take queens
					if(mods[1]!=6 || !string.Equals(boardTiles[nextPos.row,nextPos.col].piece.Substring(1),"Queen")){
						potentialPos.Add(nextPos);
					}
    			}
    		}
    	}

    	// Rook & queen logic
		// Modifier 1.7: King can move same as queen
    	if(string.Equals(piece,"Rook") || string.Equals(piece,"Queen") || (mods[1]==7 && string.Equals(piece,"King"))){
    		int inc;
    		(int row, int col) nextPos;
			List<(int rowMult, int colMult)> mults = new List<(int row, int col)>{(-1,0),(1,0),(0,-1),(0,1)};

			// Looks through possible moves to add to potential positions
	    	foreach (var mult in mults){
	    		inc = 1;
	    		nextPos = (tilePos.row+mult.rowMult*inc,tilePos.col-mult.colMult*inc);
	    		
				// Keeps looking at new tiles along mults while path not blocked
	    		while(string.Equals(TileStatus(nextPos),"none")) {
	    			potentialPos.Add(nextPos);
	    			inc++;
					// Modifier 1.2: Bishops/Rooks/Queens can only travel 3 squares at a time
					if(mods[1]==2 && inc>3){
						break;
					}
	    			nextPos = (tilePos.row+mult.rowMult*inc,tilePos.col-mult.colMult*inc);
	    		}

				// Adds tile if piece on tile can be taken
	    		if(string.Equals(TileStatus(nextPos),oppColor)) {
					// Modifier 1.6: Only queens can take queens
					if(mods[1]!=6 || string.Equals(piece,"Queen") || !string.Equals(boardTiles[nextPos.row,nextPos.col].piece.Substring(1),"Queen")){
						potentialPos.Add(nextPos);
					}
				}
			}
    	}

    	// Bishop & queen logic
		// Modifier 1.7: King can move same as queen
    	if(string.Equals(piece,"Bishop") || string.Equals(piece,"Queen") || (mods[1]==7 && string.Equals(piece,"King"))) {
    		int inc;
    		(int row, int col) nextPos;
			List<(int rowMult, int colMult)> mults = new List<(int row, int col)>{(1,1),(1,-1),(-1,1),(-1,-1)};

	    	// Looks through possible moves to add to potential positions
			foreach (var mult in mults){
	    		inc = 1;
	    		nextPos = (tilePos.row+mult.rowMult*inc,tilePos.col-mult.colMult*inc);
	    		
				// Keeps looking at new tiles along mults while path not blocked
	    		while(string.Equals(TileStatus(nextPos),"none")) {
	    			potentialPos.Add(nextPos);
	    			inc++;
					// Modifier 1.2: Bishops/Rooks/Queens can only travel 3 squares at a time
					if(mods[1]==2 && inc>3){
						break;
					}
	    			nextPos = (tilePos.row+mult.rowMult*inc,tilePos.col-mult.colMult*inc);
	    		}

				// Adds tile if piece on tile can be taken
	    		if(string.Equals(TileStatus(nextPos),oppColor)) {
					// Modifier 1.6: Only queens can take queens
					if(mods[1]!=6 || string.Equals(piece,"Queen") || !string.Equals(boardTiles[nextPos.row,nextPos.col].piece.Substring(1),"Queen")){
						potentialPos.Add(nextPos);
					}
				}
			}
    	}

    	// King logic
		// Modifier 1.7: King can move same as queen
    	if(string.Equals(piece,"King") && mods[1]!=7){
    		(int row, int col) nextPos;
    		List<(int rowMult, int colMult)> mults = new List<(int rowMult, int colMult)>
    			{(0,1),(0,-1),(1,0),(-1,0),(1,1),(-1,1),(1,-1),(-1,-1)};

			// Looks through possible moves to add to potential positions
    		foreach (var mult in mults){
    			nextPos = (tilePos.row+mult.rowMult,tilePos.col+mult.colMult);
    			if (string.Equals(TileStatus(nextPos),oppColor) || string.Equals(TileStatus(nextPos),"none")){
    				// Modifier 1.6: Only queens can take queens
					if(mods[1]!=6 || !string.Equals(boardTiles[nextPos.row,nextPos.col].piece.Substring(1),"Queen")){
						potentialPos.Add(nextPos);
					}
    			}
    		}

			// Checks if castling is possible
            foreach (var pos in CastleLogic(color)){
                potentialPos.Add(pos);
            }
    	}

		// Modifier 2.1/2.4: For elimination game modes, don't check if moves can put player in check
		if (mods[2]==1 || mods[2]==4) {
			return potentialPos;
		}

		// Modifier 2.0: Standard checkmate
		// Eliminating moves that put yourself in check
    	List<(int row, int col)> filterPos = new List<(int row, int col)>();
    	foreach (var toPos in potentialPos){
    		if (!MoveCheck(color, tilePos, toPos)){
    			filterPos.Add(toPos);
    		}
    	}
    	
    	return filterPos;
		
    }

    // Castle logic, returns potential positions for king moves for castling 
    private List<(int row, int col)> CastleLogic(string color){
        // Booleans for checking whether castling is possible
        List<(int row, int col)> castlePos = new List<(int row, int col)>();
		bool underCheck = (checkPos != (-10, -10));

        if (string.Equals(color,"w")){
			(bool kingMoved, bool lRookMoved, bool rRookMoved) wCastle;
			wCastle.kingMoved = !((TilePiece((0,4),"wKing") && boardTiles[0,4].changeCounter==0));
			wCastle.lRookMoved = !((TilePiece((0,0),"wRook") && boardTiles[0,0].changeCounter==0));
			wCastle.rRookMoved = !((TilePiece((0,7),"wRook") && boardTiles[0,7].changeCounter==0));

            // White Left Castle
            if(!wCastle.kingMoved && !wCastle.lRookMoved){
                bool oneStep = MoveCheck("w",(0,4),(0,3));
                bool twoStep = MoveCheck("w",(0,4),(0,2));
                bool oneClear = (TileStatus((0,3)) == "none");
                bool twoClear = (TileStatus((0,2)) == "none");
                if(!underCheck && !oneStep && !twoStep && oneClear && twoClear){
                    castlePos.Add((0,2));
                }
            }

            // White Right Castle
            if(!wCastle.kingMoved && !wCastle.rRookMoved){
                bool oneStep = MoveCheck("w",(0,4),(0,5));
                bool twoStep = MoveCheck("w",(0,4),(0,6));
                bool oneClear = (TileStatus((0,5)) == "none");
                if(!underCheck && !oneStep && !twoStep && oneClear){
                    castlePos.Add((0,6));
                }
            }
        } else {
			(bool kingMoved, bool lRookMoved, bool rRookMoved) bCastle;
			bCastle.kingMoved = !((TilePiece((7,4),"bKing") && boardTiles[7,4].changeCounter==0));
			bCastle.lRookMoved = !((TilePiece((7,0),"bRook") && boardTiles[7,0].changeCounter==0));
			bCastle.rRookMoved = !((TilePiece((7,7),"bRook") && boardTiles[7,7].changeCounter==0));

            // Black Left Castle
            if(!bCastle.kingMoved && !bCastle.lRookMoved){
                bool oneStep = MoveCheck("b",(7,4),(7,3));
                bool twoStep = MoveCheck("b",(7,4),(7,2));
                bool oneClear = (TileStatus((7,3)) == "none");
                bool twoClear = (TileStatus((7,2)) == "none");
                if(!underCheck && !oneStep && !twoStep && oneClear && twoClear){
                    castlePos.Add((7,2));
                }
            }

            // Black Right Castle
            if(!bCastle.kingMoved && !bCastle.rRookMoved){
                bool oneStep = MoveCheck("b",(7,4),(7,5));
                bool twoStep = MoveCheck("b",(7,4),(7,6));
                bool oneClear = (TileStatus((7,5)) == "none");
                if(!underCheck && !oneStep && !twoStep && oneClear){
                    castlePos.Add((7,6));
                }
            }
        }
        return castlePos;
    }

    // Moves piece from fromPos to toPos
    private void MovePiece((int row, int col) fromPos, (int row, int col) toPos){
		// Variables about fromPos. toPos
		TileLogic fromScript = boardTiles[fromPos.row, fromPos.col];
    	TileLogic toScript = boardTiles[toPos.row, toPos.col];
        string color = fromScript.piece.Substring(0,1);
        string piece = fromScript.piece.Substring(1);
		string takenPiece="none";
		if(!string.Equals(toScript.piece,"none")){
			takenPiece=toScript.piece.Substring(1);
		}
		
		// Finds other pieces that can move to square
		List<(int row, int col, string piece)> pieces = new List<(int row, int col, string piece)> ();
		pieces.Add((fromPos.row,fromPos.col,piece));
		for (int row=0;row<8;row++){
			for(int col=0;col<8;col++){
				if(boardTilesMoves[row,col].Contains(toPos)){
					string pieceName = boardTiles[row,col].piece.Substring(1);
					pieces.Add((row,col,pieceName));
				}
			}
		}

		// Adds move to boardMoves
		Moves curMove = new Moves(fromPos,toPos,color,piece,takenPiece,pieces);
		boardMoves.Add(curMove);

		// Adds past board state to curMove
		List<(int row, int col, string piece, int changeCounter)> pastState = new List<(int row, int col, string piece, int changeCounter)>();
		for (int row=0;row<8;row++){
			for(int col=0;col<8;col++){
				pastState.Add((row,col,boardTiles[row,col].piece, boardTiles[row,col].changeCounter));
			}
		}
		curMove.SetPastState(pastState);

        // Checks if king, rook moved from spot for castle
        if (string.Equals(piece,"King")){
            // White left castle
            if (fromPos==(0,4) && toPos==(0,2)){
                TileLogic newRookScript = boardTiles[0,3];
                TileLogic oldRookScript = boardTiles[0,0];
                newRookScript.ChangeImage("wRook");
                oldRookScript.ChangeImage("none");
				curMove.Castle("O-O-O");
            } else if (fromPos==(0,4) && toPos==(0,6)){
                TileLogic newRookScript = boardTiles[0,5];
                TileLogic oldRookScript = boardTiles[0,7];
                newRookScript.ChangeImage("wRook");
                oldRookScript.ChangeImage("none");
				curMove.Castle("O-O");
            } else if (fromPos==(7,4) && toPos==(7,2)){
                TileLogic newRookScript = boardTiles[7,3];
                TileLogic oldRookScript = boardTiles[7,0];
                newRookScript.ChangeImage("bRook");
                oldRookScript.ChangeImage("none");
				curMove.Castle("O-O-O");
            } else if (fromPos==(7,4) && toPos==(7,6)){
                TileLogic newRookScript = boardTiles[7,5];
                TileLogic oldRookScript = boardTiles[7,7];
                newRookScript.ChangeImage("bRook");
                oldRookScript.ChangeImage("none");
				curMove.Castle("O-O");
            }
        }

        // Checks if pawn is performing en passant take
        if(string.Equals(piece,"Pawn") && string.Equals(toScript.piece,"none") && (fromPos.col!=toPos.col)){
			(int row, int col) enPos;
			TileLogic enScript;
			if(string.Equals(color,"w")){
				enPos=(toPos.row-1,toPos.col);
				enScript = boardTiles[enPos.row,enPos.col];
				while(!string.Equals(enScript.piece,"bPawn")){
					enPos=(enPos.row-1,enPos.col);
					enScript = boardTiles[enPos.row,enPos.col];
				}
			} else {
				enPos=(toPos.row+1,toPos.col);
				enScript = boardTiles[enPos.row,enPos.col];
				while(!string.Equals(enScript.piece,"wPawn")){
					enPos=(enPos.row+1,enPos.col);
					enScript = boardTiles[enPos.row,enPos.col];
				}
			}
            curMove.EnPas(enPos);
            enScript.ChangeImage("none");
			takenPiece="Pawn";
        }

		// Takes piece by change toScript image to fromScript image, fromScript image to none
		// Modifier 1.3: Piece that takes also dies (except king)
		if(mods[1]==3 && !string.Equals(takenPiece,"none") && !string.Equals(piece,"King")){
			toScript.ChangeImage("none");
		// Modifier 1.4: Piece that takes upgrades
		} else if (mods[1]==4 && !string.Equals(takenPiece,"none")) {
			if(string.Equals(piece,"Pawn")){
				toScript.ChangeImage(color+"Knight");
			} else if(string.Equals(piece,"Knight")){
				toScript.ChangeImage(color+"Bishop");
			} else if(string.Equals(piece,"Bishop")){
				toScript.ChangeImage(color+"Rook");
			} else if(string.Equals(piece,"Rook")){
				toScript.ChangeImage(color+"Queen");
			} else {
				toScript.ChangeImage(fromScript.piece);
			}
		} 
		else {
			toScript.ChangeImage(fromScript.piece);
		}
    	fromScript.ChangeImage("none");

        // Handles promotion logic
        if (string.Equals(piece,"Pawn")){
            if (string.Equals(color,"w") && toPos.row == 7){
                curState = GameState.whitePromotion;
				if (curTeam==0){
                	GameObject promotion = Instantiate(wPromote,new Vector3(toPos.col-3.5f,4.5f,0),Quaternion.identity,this.transform);
					promotion.name = "promotion";
				}
                return;
            }
            if (string.Equals(color,"b") && toPos.row == 0){
                curState = GameState.blackPromotion;
				if (curTeam==1){
                	GameObject promotion = Instantiate(bPromote,new Vector3(toPos.col-3.5f,-4.5f,0),Quaternion.identity,this.transform);
					promotion.name = "promotion";
				}
				return;
            }
        }

		// Moves game to next turn
    	NextTurn(true);
    }

	// Handles promotion logic, called by tiles in wPromotion/bPromotion
    public void PromotePiece(float x){
		// Piece promotion depends on position of tile calling PromotePiece
        string[] promotions = new string[] {"Knight","Bishop","Rook","Queen"};
        string promotedPiece=promotions[(int) (x + 1.5f)];
        
		// Finds position of promoted piece
		(int row, int col) promotePos = boardMoves[boardMoves.Count-1].GetToPos();

		// Finds color of promoted piece
		string color;
		if(promotePos.row==7){
            color="w";
        } else {
            color="b";
        }
        
		// Changes tiles to match promotion logic
        boardTiles[promotePos.row,promotePos.col].ChangeImage(color+promotedPiece);
		boardMoves[boardMoves.Count-1].Promote(promotedPiece);

		// Moves game to next turn
        NextTurn(true);
    }

	// Checks for stalemate & oppColor loss
	private void WinCheck(string oppColor){
		// Modifier 2.0/2.2/2.3: Standard checkmate
		if(mods[2]==0 || mods[2]==2 || mods[2]==3){
			// Checks whether king is in check
			checkPos = KingCheck(oppColor);
			// Highlights check square
			if(checkPos!=(-10,-10)){
				boardTiles[checkPos.row,checkPos.col].ChangeColor(checkColor);
				boardMoves[boardMoves.Count-1].Check();
			}
			// Checks whether opponent is in stalemate, sets board state accordingly 
			if (StaleCheck(oppColor)){
				if (checkPos!=(-10,-10)){
					curState = (string.Equals(oppColor,"w")) ? GameState.blackWin : GameState.whiteWin;
					boardMoves[boardMoves.Count-1].Checkmate();

				} else {
					curState=GameState.stalemate;
					boardMoves[boardMoves.Count-1].Stalemate();
				}
			}
			// Modifier 2.2: Promoting pawn wins game
			if (mods[2]==2 && boardMoves.Count>0) {
				Moves lastMove = boardMoves[boardMoves.Count-1];
				if(!string.Equals(lastMove.GetPiecePromotion(),"none")){
					curState = (string.Equals(oppColor,"w")) ? GameState.blackWin : GameState.whiteWin;
					boardMoves[boardMoves.Count-1].Checkmate();
				}
			}
			// Modifier 2.3: King moves to opponent's half of board
			if (mods[2]==3 && boardMoves.Count>0) {
				Moves lastMove = boardMoves[boardMoves.Count-1];
				bool cross = false;
				if(string.Equals(oppColor,"w") && string.Equals(lastMove.GetPieceMoved(),"King") && lastMove.GetToPos().row<4){
					cross = true;
				}
				if(string.Equals(oppColor,"b") && string.Equals(lastMove.GetPieceMoved(),"King") && lastMove.GetToPos().row>3){
					cross = true;
				}
				if(cross){
					curState = (string.Equals(oppColor,"w")) ? GameState.blackWin : GameState.whiteWin;
					boardMoves[boardMoves.Count-1].Checkmate();
				}
			}
		}
		// Modifier 2.1/2.4: Eliminate all enemies
		else if (mods[2]==1 || mods[2]==4) {
			// Checks if any oppColor pieces left
			bool gameOver = true;
			for (int row=0;row<8;row++){
				for (int col=0;col<8;col++){
					if(string.Equals(TileStatus((row,col)),oppColor)){
						gameOver = false;
						break;
					}
				}
			}
			// Checks if game is over
			if(gameOver){
				curState = (string.Equals(oppColor,"w")) ? GameState.blackWin : GameState.whiteWin;
				boardMoves[boardMoves.Count-1].Checkmate();
				return;
			}
			// Checks if opponnet has any moves
			if(StaleCheck(oppColor)){
				curState=GameState.stalemate;
				boardMoves[boardMoves.Count-1].Stalemate();
			}

			// Modifier 2.4: Most suriving pieces after 30 turns
			if(mods[2]==4 && boardMoves.Count>=60){
				int wCount = 0;
				int bCount = 0;

				// Counts number of pieces for each color
				for (int row=0;row<8;row++){
					for (int col=0;col<8;col++){
						if(string.Equals(TileStatus((row,col)),"w")){
							wCount++;
						} else if (string.Equals(TileStatus((row,col)),"b")){
							bCount++;
						}
					}
				}

				// Sets winner based off remaining pieces
				if(wCount>bCount) curState = GameState.whiteWin;
				else if (wCount==bCount) curState = GameState.stalemate;
				else curState = GameState.blackWin;
			}	
		}
	}

    // Called after move or promotion ahs been made, changes game state and caculates variables for next turn & takebacks
    private void NextTurn(bool recalc){	
		// Multiplayer: Case where opponent left
		if(curState==GameState.oppLeft){
			return;
		}

    	// Restores color of all tiles
    	for (int rowInc=0; rowInc < 8; rowInc++){
    		for (int colInc=0; colInc < 8; colInc++){
    			boardTiles[rowInc,colInc].ChangeColor(calcColor((rowInc,colInc)));
    		}
    	}

		// Resets selectPos, initializes variables for turn logic
    	selectPos=(-10,-10);
		string color = (curState==GameState.whiteMove || curState==GameState.whitePromotion) ? "w" : "b";
		string oppColor = (string.Equals(color,"w")) ? "b" : "w";
		rematchArray = new int[] {0,0};
		if(!localGame){
			RecolorGameUI();
		}

		// Updates potential tiles for opposite color, used for StaleCheck
		for (int row=0;row<8;row++){
			for (int col=0;col<8;col++){
				if (string.Equals(boardTiles[row,col].piece.Substring(0,1),oppColor)){
					boardTilesMoves[row,col]=PotentialTiles((row,col));
				} else {
					boardTilesMoves[row,col]=new List<(int row, int col)>();
				}
			}
		}
   
		// Checks win conditions based on mods
		WinCheck(oppColor);

		// GameUI: Updates move list
		if (recalc){
			if (boardMoves.Count>0){
				boardMoves[boardMoves.Count-1].ConvertString();
				string moveName = boardMoves[boardMoves.Count-1].GetString();
				GameObject.Find("/Canvas/GameUI/Scroll/MoveText").GetComponent<MoveUI>().AddMove(moveName,color);
			}
		}

		// GameUI: Sets current game state text
		TextMeshProUGUI gameText = gameUI.transform.Find("GameState/Text").GetComponent<TextMeshProUGUI>();
		switch (curState){
			case GameState.whiteMove: gameText.text = "Black Move"; break; 
			case GameState.whitePromotion: gameText.text = "Black Move"; break; 
			case GameState.blackMove: gameText.text = "White Move"; break; 
			case GameState.blackPromotion: gameText.text = "White Move"; break; 
			case GameState.whiteWin: gameText.text = "White Win"; break; 
			case GameState.blackWin: gameText.text = "Black Win"; break; 
			case GameState.stalemate: gameText.text = "Stalemate"; break;
			default:  gameText.text = "Unknown"; break;
		}
		

		// Multiplayer: Broadcasts move to all clients
		if(recalc && !localGame && (boardMoves.Count>0) && ((string.Equals(color,"w") && curTeam==0) || (string.Equals(color,"b") && curTeam==1))){
			Moves lastMove = boardMoves[boardMoves.Count-1];
			NetMakeMove move = new NetMakeMove();
			move.fromPosRow = lastMove.GetFromPos().row; 
			move.fromPosCol = lastMove.GetFromPos().col;
			move.toPosRow = lastMove.GetToPos().row;
			move.toPosCol = lastMove.GetToPos().col;
			switch (lastMove.GetPiecePromotion()){
				case "Knight": move.promotedPiece = 0; break;
				case "Bishop": move.promotedPiece = 1; break;
				case "Rook": move.promotedPiece = 2; break;
				case "Queen": move.promotedPiece = 3; break;
				default: move.promotedPiece = 4; break;
			}
			move.teamId = curTeam;
			Client.Instance.SendToServer(move);
		}

		// Returns early if game has already ended
		if(curState == GameState.whiteWin || curState == GameState.blackWin){
			return;
		}

		// Changes game state based on past turn
		curState = (string.Equals(color,"w")) ? GameState.blackMove : GameState.whiteMove;

		// Switches player for local game
		if(localGame){
			curTeam = (curTeam==0) ? 1 : 0;
		}
			
    }

	// Returns whether tile at pos contains piece
	private bool TilePiece((int row, int col) pos, string piece){
		if (pos.row>=0 && pos.row <=7 && pos.col >= 0 && pos.col<= 7){
			return string.Equals(boardTiles[pos.row,pos.col].piece,piece);
		}
		return false;
	}

    // Given color, returns kingPos if in check
    private (int row, int col) KingCheck(string color) {
		// Sets up basic variables
    	(int row, int col) kingPos = (-1,-1);
		string oppColor = string.Equals(color,"w") ? "b" : "w";

		// Finds pos of king
    	for (int row=0;row<8;row++){
    		for (int col=0;col<8;col++){
    			if (string.Equals(boardTiles[row,col].piece,color+"King")){
    				kingPos=(row,col);
    			}
    		}
    	}

		// Checks pawns for check
		if(string.Equals(color,"w")){
			if(TilePiece((kingPos.row+1,kingPos.col-1),"bPawn") || TilePiece((kingPos.row+1,kingPos.col+1),"bPawn")){
				return kingPos;
			}
		} else {
			if(TilePiece((kingPos.row-1,kingPos.col-1),"wPawn") || TilePiece((kingPos.row-1,kingPos.col+1),"wPawn")){
				return kingPos;
			}
		}

		(int row, int col) nextPos;
		// Checks knights for check
		List<(int rowMove, int colMove)> knightMoves = new List<(int row, int col)>
    			{(2,1),(-2,1),(2,-1),(-2,-1),(1,2),(-1,2),(1,-2),(-1,-2)};
		// Modifier 1.1: Greatly increase horse mobility
		if(mods[1]==1){
			knightMoves.AddRange(new List<(int row, int col)>
			{(2,2),(-2,2),(2,-2),(-2,-2),(2,0),(0,2),(-2,0),(0,-2)});
		}
		// Looks through possible moves to add to potential positions
    	foreach (var move in knightMoves){
			nextPos = (kingPos.row+move.rowMove,kingPos.col+move.colMove);
			if(TilePiece(nextPos,oppColor+"Knight")){
				return kingPos;
			}
    	}

		// Checks rook and queens for check
		List<(int rowMove, int colMove)> rookMoves = new List<(int row, int col)>{(-1,0),(1,0),(0,-1),(0,1)};
		// Looks through possible moves to add to potential positions
		foreach (var move in rookMoves){
			int inc = 1;
			nextPos = (kingPos.row+move.rowMove*inc,kingPos.col+move.colMove*inc);

			// Keeps looking at new tiles along mults while path not blocked
			while(string.Equals(TileStatus(nextPos),"none")) {
				inc++;
				// Modifier 1.2: Bishops/Rooks/Queens can only travel 3 squares at a time
				if(mods[1]==2 && inc>3){
					break;
				}
				nextPos = (kingPos.row+move.rowMove*inc,kingPos.col+move.colMove*inc);
			}

			if(TilePiece(nextPos,oppColor+"Rook") || TilePiece(nextPos,oppColor+"Queen") ) {
				return kingPos;
			}
		}

    	// Bishop & queen logic
		List<(int rowMove, int colMove)> bishopMoves = new List<(int row, int col)>{(-1,-1),(1,-1),(-1,1),(1,1)};
		foreach (var move in bishopMoves){
			int inc = 1;
			nextPos = (kingPos.row+move.rowMove*inc,kingPos.col+move.colMove*inc);

			// Keeps looking at new tiles along mults while path not blocked
			while(string.Equals(TileStatus(nextPos),"none")) {
				inc++;
				// Modifier 1.2: Bishops/Rooks/Queens can only travel 3 squares at a time
				if(mods[1]==2 && inc>3){
					break;
				}
				nextPos = (kingPos.row+move.rowMove*inc,kingPos.col+move.colMove*inc);
			}

			if(TilePiece(nextPos,oppColor+"Bishop") || TilePiece(nextPos,oppColor+"Queen") ) {
				return kingPos;
			}
		}
    	
    	// King logic
    	List<(int rowMove, int colMove)> kingMoves = new List<(int rowMult, int colMult)>
    		{(0,1),(0,-1),(1,0),(-1,0),(1,1),(-1,1),(1,-1),(-1,-1)};
		// Looks through possible king moves
    	foreach (var move in kingMoves){
    		nextPos = (kingPos.row+move.rowMove,kingPos.col+move.colMove);
    		if (TilePiece(nextPos,oppColor+"King")){
    			return kingPos;
    		}
    	}
           
		return (-10,-10);   	
    }

    // Given proposed move, returns whether color's king is in check
    private bool MoveCheck(string color, (int row, int col) fromPos, (int row, int col) toPos){
        // Variables for proposed move
    	TileLogic fromScript = boardTiles[fromPos.row, fromPos.col];
    	TileLogic toScript = boardTiles[toPos.row, toPos.col];
    	string fromPiece = fromScript.piece;
    	string toPiece = toScript.piece;

    	// Modifier 1.3: Piece that takes also dies
		if(mods[1]==3 && !string.Equals(toPiece,"none") && !string.Equals(fromPiece.Substring(1),"King")){
			toScript.piece="none";
		} else {
			toScript.piece=fromScript.piece;
		}
		// Piece on fromPos is moved
    	fromScript.piece="none";

		// Check whether king is put in check
        bool checkBool = (KingCheck(color) != (-10,-10));

		// Swaps pieces back
    	fromScript.piece=fromPiece;
    	toScript.piece=toPiece;

    	return checkBool;
    }

    // Given color, returns whether color has any possible moves
    private bool StaleCheck(string color){
        for (int row=0;row<8;row++) {
            for (int col=0;col<8;col++){
                if (string.Equals(boardTiles[row,col].piece.Substring(0,1),color)){
                    int posMoves = boardTilesMoves[row,col].Count;
                    if (posMoves>0){
                        return false;
                    }
                }
            }
        }
        return true;
    }


	// Restarts game, creates new board instantly if instant is true, doesn't call ResetMulti()
	private void RestartGame(bool instant){
		// Clears tiles
		for(int row=0;row<8;row++){
			for(int col=0;col<8;col++){
				boardTiles[row,col].ChangeImage("none");
				Destroy(boardTiles[row,col].gameObject);
			}
		}
		// Clears promotion if exists
		if(curState==GameState.whitePromotion || curState==GameState.blackPromotion){
			Destroy(GameObject.Find("/Board/promotion"));
		}

		// Initializes variables
		boardTiles = new TileLogic[8,8];
		boardTilesMoves = new List<(int row, int col)> [8,8];
		boardMoves = new List<Moves>();
		GameObject.Find("/Canvas/GameUI/Scroll/MoveText").GetComponent<MoveUI>().DestroyMoves();
		curState  = GameState.blackMove;

		// Multiplayer: Resets curTeam if local, resets rematchArray if multi
		if(localGame){
			curTeam = 1;
		} else {
			rematchArray = new int[] {-1,-1};
		}

		// Resets board if instant, resets team logic if not instant
		if (instant){
			BoardStart();
		} else {
			curTeam = -1;
			playerCount = -1;
		}

	}

	// Wrapper for RestartGame for Invoke()
	private void RestartGameWrapper(){
		RestartGame(true);
	}

	// Called when pressing restart button
	public void OnRestartButton(){
		// Multiplayer: Restarts game if local, sends restart message to server if multi
		if(localGame){
			RestartGame(true);
		} else {
			NetRematch rematch = new NetRematch();
			rematch.teamId = curTeam;
			rematch.wantRematch = 1;
			Client.Instance.SendToServer(rematch);
		}
	}

	// Handles takeback logic
	private void TakebackMove(){
		// Destroys promotion UI if in promotion state
		if((curState == GameState.whitePromotion && curTeam==0) || (curState == GameState.blackPromotion && curTeam==1)){
			Destroy(GameObject.Find("/Board/promotion"));
		}

		// Removes move from move display
		if(curState!=GameState.whitePromotion && curState!=GameState.blackPromotion){
			GameObject.Find("/Canvas/GameUI/Scroll/MoveText").GetComponent<MoveUI>().RemoveMove();
		}
		
		// Changes game state
		if(curState==GameState.whiteWin || curState == GameState.whitePromotion) {
			curState = GameState.blackMove;
			if(localGame) curTeam = 1;
		}
		if(curState==GameState.blackWin || curState == GameState.blackPromotion){
			curState = GameState.whiteMove;
			if(localGame) curTeam = 0;
		} 
		
		// Changes board to match previous state
		Moves lastMove = boardMoves[boardMoves.Count-1];
		boardMoves.RemoveAt(boardMoves.Count-1);
		foreach (var entry in lastMove.GetPastState()){
			boardTiles[entry.row,entry.col].SetImage(entry.piece);
			boardTiles[entry.row,entry.col].changeCounter=entry.changeCounter;
		}

		// Moves to next turn
		NextTurn(false);

	}
	
	// Called when pressing takeback button
	public void OnTakebackButton(){
		// Can't takeback on white's first move or black's first move
		if((boardMoves.Count==0) || (boardMoves.Count==1 && curTeam==1 && !localGame && rematchArray[0]!=2)){
			return;
		}
		// Multiplayer: Takeback move if local, send message to takeback move if multi
		if(localGame){
			TakebackMove();
		} else {
			NetRematch rematch = new NetRematch();
			rematch.teamId = curTeam;
			rematch.wantRematch = 2;
			Client.Instance.SendToServer(rematch);
		}
	}

	// Called when pressing quit button
	public void OnQuitButton(){
		// Multiplayer: Resets game for local, sends message for quit for multi
		if(localGame){
			// Reset key variables
			RestartGame(false);

			// Reset multiplayer variables
			MultiReset();

			// Disable game UI
			gameUI.SetActive(false);
		} else {
			// Sends quit message
			if(rematchArray[1-curTeam]!=3){
				NetRematch rematch = new NetRematch();
				rematch.teamId = curTeam;
				rematch.wantRematch = 3;
				Client.Instance.SendToServer(rematch);
			}
			
			// Resets key variables
			RestartGame(false);

			// Disables game UI
			gameUI.SetActive(false);


			// Resets multiplayer variables and shutdown
			Invoke("MultiShutdown",0.2f);
		}
	}

	// Changes text for game UI buttons based on rematchArray
	private void RecolorGameUI(){
		// Variables for game UI buttons
		int myResponse = rematchArray[curTeam];
		int oppResponse = rematchArray[1-curTeam];
		TextMeshProUGUI restartButton = gameUI.transform.Find("RestartButton/Text").GetComponent<TextMeshProUGUI>();
		TextMeshProUGUI takebackButton = gameUI.transform.Find("TakebackButton/Text").GetComponent<TextMeshProUGUI>();

		// Resets game UI text
		restartButton.text = "Restart";
		takebackButton.text = "Takeback";

		// Changes game UI text based on player response
		if(myResponse == 1) restartButton.text = "Requested";
		if(myResponse == 2) takebackButton.text = "Requested";

		// Changes game UI text based on opponent response
		if(oppResponse == 1) restartButton.text = "Give Restart?";
		if(oppResponse == 2) takebackButton.text = "Give Takeback?";
	}

	// Writes text for mods display based on mods array
	private void WriteMods(){
		ModsDict modsDict = new ModsDict();
		// Looks up each text entry to change text value
		for (int modNum=0;modNum<3;modNum++){
			TextMeshProUGUI modText = gameUI.transform.Find("Mod"+(modNum+1).ToString()+"/Text").GetComponent<TextMeshProUGUI>();
			(string modName, string modDesc) modName = modsDict.Print(modNum,mods[modNum]);
			modText.text = "<b><size=20>"+modName.modName+"</size></b>\n"+modName.modDesc;

		}
	}

	// Multiplayer: Flips board if player color is black
	private void OnStartGameClient(NetMessage obj) {
		if (!localGame){
			GameObject menu;
			if(curTeam==0){
				menu = GameObject.Find("/Canvas/HostMenu");
			} else {
				menu = GameObject.Find("/Canvas/JoinMenu");
			}
			menu.SetActive(false);
			BoardStart();
		}
	}

	// Multiplayer: Registers events to listen for
	private void RegisterEvents()
	{
		NetUtility.S_WELCOME += OnWelcomeServer;
		NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
		NetUtility.S_REMATCH += OnRematchServer;
		NetUtility.S_SETUP += OnSetupServer;

		NetUtility.C_WELCOME += OnWelcomeClient;
		NetUtility.C_START_GAME += OnStartGameClient;
		NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
		NetUtility.C_REMATCH += OnRematchClient;
		NetUtility.C_SETUP += OnSetupClient;
	}

	// Multiplayer: Unregisters events to listen for
	private void UnRegisterEvents(){
		NetUtility.S_WELCOME -= OnWelcomeServer;
		NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
		NetUtility.S_REMATCH -= OnRematchServer;
		NetUtility.S_SETUP -= OnSetupServer;

		NetUtility.C_WELCOME -= OnWelcomeClient;
		NetUtility.C_START_GAME -= OnStartGameClient;
		NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
		NetUtility.C_REMATCH -= OnRematchClient;
		NetUtility.C_SETUP -= OnSetupClient;
	}

	// Multiplayer: Sets localGame variable
	public void SetLocalGame(bool localGame){
		this.localGame=localGame;
	}

	// Multiplayer: Creates NetSetup message for board setup
	private NetSetup CreateNetSetup(){
			NetSetup startMsg = new NetSetup();
			startMsg.mod1 = mods[0];
			startMsg.mod2 = mods[1];
			startMsg.mod3 = mods[2];

			BoardSetup bSetup = new BoardSetup();
			foreach (var piece in bSetup.PieceLayout(mods[0])){
				switch(piece.name){
					case "wPawn": startMsg.boardSetup[piece.row,piece.col]=1; break;
					case "wRook": startMsg.boardSetup[piece.row,piece.col]=2; break;
					case "wKnight": startMsg.boardSetup[piece.row,piece.col]=3; break;
					case "wBishop": startMsg.boardSetup[piece.row,piece.col]=4; break;
					case "wQueen": startMsg.boardSetup[piece.row,piece.col]=5; break;
					case "wKing": startMsg.boardSetup[piece.row,piece.col]=6; break;
					case "bPawn": startMsg.boardSetup[piece.row,piece.col]=7; break;
					case "bRook": startMsg.boardSetup[piece.row,piece.col]=8; break;
					case "bKnight": startMsg.boardSetup[piece.row,piece.col]=9; break;
					case "bBishop": startMsg.boardSetup[piece.row,piece.col]=10; break;
					case "bQueen": startMsg.boardSetup[piece.row,piece.col]=11; break;
					case "bKing": startMsg.boardSetup[piece.row,piece.col]=12; break;
					default: Debug.LogError("Cant match"+piece.name); break;
				}
			}

			return startMsg;
	}

	// Multiplayer Server: Receives NetWelcome message and broadcasts assigned team to connected player
	private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn){
		// Client has connected, assign a team and return the message back
		NetWelcome nw = msg as NetWelcome;

		// Assign a team
		nw.AssignedTeam = ++playerCount;

		// Return back to client
		Server.Instance.SendToClient(cnn, nw);

		if (playerCount==1){	
			Server.Instance.Broadcast(CreateNetSetup());
			Server.Instance.Broadcast(new NetStartGame());
		}
	}

	// Multiplayer Server: Recieves NetMove message and broadcasts to players
	private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn){
		NetMakeMove msgChecked = msg as NetMakeMove;
		Server.Instance.Broadcast(msgChecked);
	}
	
	// Multiplayer Server: Recieves NetRematch message and broadcasts to players
	private void OnRematchServer(NetMessage msg, NetworkConnection cnn){
		NetRematch msgChecked = msg as NetRematch;
		Server.Instance.Broadcast(msgChecked);
	}

	// Multiplayer Server: Recieves NetSetup message and broadcasts to players
	private void OnSetupServer(NetMessage msg, NetworkConnection cnn){
		NetSetup msgChecked = msg as NetSetup;
		Server.Instance.Broadcast(msgChecked);
	}

	
	// Multiplayer Client: Recieves NetWelcome message to find assigned team
	private void OnWelcomeClient(NetMessage msg){
		// Receive the connection message
		NetWelcome nw = msg as NetWelcome;

		// Assign the team
		curTeam = nw.AssignedTeam;
	}

	// Multiplayer Client: Recieves NetMove message to find opponent move
	private void OnMakeMoveClient(NetMessage msg){
		NetMakeMove msgParsed = msg as NetMakeMove;
		if(msgParsed.teamId != curTeam){
			MovePiece((msgParsed.fromPosRow,msgParsed.fromPosCol),(msgParsed.toPosRow,msgParsed.toPosCol));
			if(msgParsed.promotedPiece != 4){
				PromotePiece(msgParsed.promotedPiece-1.5f);
			}
		}
	}

	// Multiplayer Client: Recieves NetSetup message to set up game board
	private void OnSetupClient(NetMessage msg){
		Debug.Log("GOT CLIENT MESSAGE");
		NetSetup msgParsed = msg as NetSetup;
		mods[0]=msgParsed.mod1;
		mods[1]=msgParsed.mod2;
		mods[2]=msgParsed.mod3;
		for (int row=0;row<8;row++){
			for (int col=0;col<8;col++){
				switch(msgParsed.boardSetup[row,col]){
					case 0: boardSetup[row,col]="none"; break;
					case 1: boardSetup[row,col]="wPawn"; break;
					case 2: boardSetup[row,col]="wRook"; break;
					case 3: boardSetup[row,col]="wKnight"; break;
					case 4: boardSetup[row,col]="wBishop"; break;
					case 5: boardSetup[row,col]="wQueen"; break;
					case 6: boardSetup[row,col]="wKing"; break;
					case 7: boardSetup[row,col]="bPawn"; break;
					case 8: boardSetup[row,col]="bRook"; break;
					case 9: boardSetup[row,col]="bKnight"; break;
					case 10: boardSetup[row,col]="bBishop"; break;
					case 11: boardSetup[row,col]="bQueen"; break;
					case 12: boardSetup[row,col]="bKing"; break;
					default: Debug.LogError("NO MATCH"); break;
				}
			}
		}
	}
	
	// Multiplayer Client: Recieves NetRematch message to handle rematch & takeback logic
	private void OnRematchClient(NetMessage msg){
		// For msgParsed.wantRematch: 0 is default, 1 is want rematch, 2 is takeback, 3 is exit
		NetRematch msgParsed = msg as NetRematch;
		rematchArray[msgParsed.teamId]=msgParsed.wantRematch;

		int oppTeam = 1 - curTeam;
		// Case where opponent has left
		if(rematchArray[oppTeam]==3){
			gameUI.transform.Find("GameState/Text").GetComponent<TextMeshProUGUI>().text="Opp Left";
			return;
		}
		// Case where both players want rematch
		else if(rematchArray[curTeam]==1 && rematchArray[oppTeam]==1){
			mods[0]= rand.Next(11);
			mods[1]= rand.Next(11);
			mods[2]= rand.Next(5);
			Client.Instance.SendToServer(CreateNetSetup());
			Invoke("RestartGameWrapper",0.2f);
			rematchArray = new int[] {0,0};
		}
		// Case where both players want takeback
		else if(rematchArray[curTeam]==2 && rematchArray[oppTeam]==2){
			if((curState==GameState.whitePromotion && curTeam==0) || (curState==GameState.blackPromotion && curTeam ==1)){
				TakebackMove();
			}
			TakebackMove();
			rematchArray = new int[] {0,0};
		}
		// Update game UI accordingly
		RecolorGameUI();
	}

	// Multiplayer: Reset multiplayer variables
	public void MultiReset(){
		playerCount = -1;
		curTeam = -1;
		localGame = true; 
		rematchArray = new int[] {-1,-1};
	}

	// Multiplayer: Close server/client connections if they exist
	public void MultiShutdown(){
		MultiReset();
		Debug.Log("Shutting down");
		 if (Client.Instance!=null){
            Client.Instance.Shutdown();
        }
        if (Server.Instance!=null){
            Server.Instance.Shutdown();
        }

	}
}
