using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class GameManager : MonoBehaviour {

	Stopwatch sw = new Stopwatch();
	public GameObject TilePrefab;
	public GameObject whiteToken;
	public GameObject blackToken;
	public static Tile blueTile;
	bool error = false;
	bool playerMax;
	string message;
	bool illegalMessage;
	bool displayWinMessage = false;
	bool whiteVictory = false;
	bool blackVictory = false;
	bool legal = true;
	bool played;
	bool turn; // when its true, it's black token's turn, otherwise false, it's white token's turn
	public static int mapSize = 8;
	List <Tile> token = new List <Tile>();
	List <List<Tile>> field = new List <List<Tile>>();
	List <Tile> searchList = new List <Tile>();
	int depth = 2; // depth 3 exceeds 3 seconds; //110; //32;
	int recursions = -1;
	byte average;
	int indexX;
	int indexY;
	int valueOfUsed;	
	bool AIPLAYSFIRST;



	// Use this for initialization
	void Start () {
		//Creates the field of play (changing mapSize to 6 will give you a 6x6 map)
		generateField();

		// to let AI play 
		AIPLAYSFIRST = false; // false is like it used to be.
		if(AIPLAYSFIRST) {
			sw.Start();
			Vector2 position2D;
			//Depth 9
			// create a copy of the board, that will be used by minimax, to recurse on:
			//Tile tile = recursion(depth, true, Int32.MinValue, Int32.MaxValue);//(AIPlayer1st(), depth, true, Int32.MinValue, Int32.MaxValue);
			bool isAIsturn = true;
			Tile tile = AIPlayerMax(isAIsturn);
			tile.decidedToUse = true;
			tile.used = true; // added trying fix bug
			tile.usedForEvaluation = true; // added trying fix bug
			print ("score: " + tile.score);
			resetEvaluatedTiles();
			determineTileTheList(tile);
			if(tile.black || tile.white){
				checkIlligalMessage(1);
				isTurn();
				played = false;
				print ("Did i switch");
			}
			else if(isLegal(indexX,indexY)){
				position2D = tile.gridPosition;
				print (position2D);
				Vector3 position = new Vector3(position2D.x, 0, position2D.y);
				placeToken(position);
				played = false;
			}
			else{
				// NB this else should never happen (now that AI playing illegal moves is fixed), 
				// else we're disqualified, 
				// i.e. our AI lost automatically from providing an illegal position --> exit gracefully:
				isTurn ();
				played = false;
			}
			sw.Stop();
			print(sw.Elapsed + " ms");
			sw.Reset();

		}
	}

	// Update is called once per frame
	void Update () {
		//Detect mouse input
		if(Input.GetMouseButtonDown(0)){
			resetEvaluatedTiles();
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if(Physics.Raycast( ray, out hit, 100 ) ){

				// ADD Nicolas: to fix black playing right after white clicks first row (which is an invalid move).
				//Retrieve selected position
				Vector2 tilePosition = new Vector2 (blueTile.transform.position.x, blueTile.transform.position.z);
				for(int i = 0; i < mapSize; i++){
					for(int j = 0; j < mapSize; j++){
						//checks which field in the list is the position we are looking for
						if(field[i][j].gridPosition == tilePosition){
							//Check if move is legal
							if(isLegal(i,j)){

								// NB original code was outside of this double for loop.
								if(blueTile.black || blueTile.white){
									checkIlligalMessage(1);
								}
								else{
									played = true;
									placeToken (blueTile.transform.position);
									if(illegalMessage){ // why change the turn if the white human player played an invalid move?
										isTurn ();
									}
								}

							}
						}
					}
				}


			}
		}
		//Checks if somebody won the game
		if(checkEndGame()){
			Time.timeScale = 0;
			displayWinMessage = true;
		}

		if(played && !blackVictory){
			sw.Start();
			Vector2 position2D;
			//Depth 9
			// create a copy of the board, that will be used by minimax, to recurse on:
			Tile tile = recursion(depth, true, Int32.MinValue, Int32.MaxValue);//(AIPlayer1st(), depth, true, Int32.MinValue, Int32.MaxValue);
			tile.decidedToUse = true;
			tile.used = true; // added trying fix bug
			tile.usedForEvaluation = true; // added trying fix bug
			print ("score: " + tile.score);
			resetEvaluatedTiles();
			determineTileTheList(tile);
			if(tile.black || tile.white){
				checkIlligalMessage(1);
				isTurn();
				played = false;
				print ("Did i switch");
			}
			else if(isLegal(indexX,indexY)){
				position2D = tile.gridPosition;
				print (position2D);
				Vector3 position = new Vector3(position2D.x, 0, position2D.y);
				placeToken(position);
				played = false;
			}
			else{
				// NB this else should never happen (now that AI playing illegal moves is fixed), 
				// else we're disqualified, 
				// i.e. our AI lost automatically from providing an illegal position --> exit gracefully:
				isTurn ();
				played = false;
			}
			sw.Stop();
			print(sw.Elapsed + " ms");
			sw.Reset();
		}
		//Checks if somebody won the game
		if(checkEndGame()){
			Time.timeScale = 0;
			displayWinMessage = true;
		}
	}

	void resetScores(){
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				field[i][j].score = 0;
			}
		}
	}

	void resetEvaluatedTiles(){
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				field[i][j].scoreDetermined = false;
				field[i][j].score = 0;
				if(field[i][j].used){
					if(field[i][j].min){
						field[i][j].min = false;
						field[i][j].used = false;
						field[i][j].usedForEvaluation = false;
					}
					else if(field[i][j].max){
						field[i][j].max = false;
						field[i][j].used = false;
						field[i][j].usedForEvaluation = false;
					}
				}
			}
		}
	}






	// returns the score for a tree branch:
	Tile recursion(int downTheTree, bool isAIsturn, int alpha, int beta){ //Dictionary<string, Object> 
		bool youCanGo = false;

		// ADD Nicolas, trying to fix black playing illegal moves
		// this is so that we don't consider illegal moves in the new if statement below.
		// just ignoring used tile isn't sufficient, we must also ignore illegal moves.
//		if(tile.max == false) {
//			turn = true;
//		} else {
//			turn = false;
//		}

		//debug:
		print ("downTheTree: "+downTheTree);


//		int maxScoreForThisTreeLevel = -10000;
//		int minScoreForThisTreeLevel = 10000;
		int score = -1;
		Tile bestTileToPlayAtThisDepth = new Tile (); 

		if(downTheTree <= 0) {
			if(isAIsturn) {
				bestTileToPlayAtThisDepth = AIPlayerMax(isAIsturn);
				// debug:
				if(bestTileToPlayAtThisDepth == null) {
					print ("bestTileToPlayAtThisDepth is NULL. There are no more possible moves at this level");
					return null;
				}
				if(isLegalMINIMAX(bestTileToPlayAtThisDepth.i, bestTileToPlayAtThisDepth.j, isAIsturn)) {
					return bestTileToPlayAtThisDepth;
				} else {
					print ("BREAKPOINT, BEST MOVE IS ILLEGAL");
					return bestTileToPlayAtThisDepth;
				}
			} else {
				bestTileToPlayAtThisDepth = AIPlayerMin(isAIsturn);
				// debug:
				if(bestTileToPlayAtThisDepth == null) {
					print ("bestTileToPlayAtThisDepth is NULL. There are no more possible moves at this level");
					return null;
				}
				if(isLegalMINIMAX(bestTileToPlayAtThisDepth.i, bestTileToPlayAtThisDepth.j, isAIsturn)) {
					return bestTileToPlayAtThisDepth;
				} else {
					print ("BREAKPOINT, BEST MOVE IS ILLEGAL: "+ "i: " +bestTileToPlayAtThisDepth.i + ", j: " + bestTileToPlayAtThisDepth.j);
					return bestTileToPlayAtThisDepth;
				}

			}
		}

		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
//				if(field[i][j].used == true){
				if(!isLegalMINIMAX(i, j, isAIsturn)) { // ADD Nicolas, to fix black playing illegal moves!
					continue;
				}
				else{
					youCanGo = true;

					// "play that tile" and call recursion again:

					// -----------------------------------------------------------
					// try this move for the current "player"
					field[i][j].usedForEvaluation = true;//cells[move[0]][move[1]].content = player;
					//field[i][j].used = true;
					if (!isAIsturn) {  // mySeed (computer) is maximizing player
						Tile miniMaxsPick = recursion(downTheTree - 1, !isAIsturn, alpha, beta);//[0];
						if(miniMaxsPick == null) {
							// do nothing
						}
						else if (miniMaxsPick.score > alpha) {
							alpha = miniMaxsPick.score;
							bestTileToPlayAtThisDepth = field[i][j];
						}
					} else {  // oppSeed is minimizing player
						Tile miniMaxsPick = recursion(downTheTree - 1, !isAIsturn, alpha, beta);
						if(miniMaxsPick == null) {
							// do nothing
						}
						else if (miniMaxsPick.score < beta) {
							beta = miniMaxsPick.score;
							bestTileToPlayAtThisDepth = field[i][j];
						}
					}
					// undo move
					field[i][j].usedForEvaluation = false;//cells[move[0]][move[1]].content = Seed.EMPTY;
					//field[i][j].used = false;
					// cut-off
					if (alpha >= beta) {
						print ("ALPHA BETA PRUNED.");
						goto endOfLoop;//break; from the 2 loops, not just one.
					}



				}
			}
		}
		endOfLoop:
		

		// the return the max score and corresponding tile to play:

		// ADD Nicolas to fix black playing illegal moves:
		// reset turn to AI's turn, (since we've been playing with it to use the isLegal() function during minimax, so the turn was artificially changed, temporarily for the sake of the minimax algorithm)
		//turn = true;

		if(youCanGo == false){
			print("All have been evaluated -- there are no possible moves for branches under this minimax tree node.");
			return null;
		}

		//return tile;
		return bestTileToPlayAtThisDepth; //new int[] {(player == mySeed) ? alpha : beta, bestRow, bestCol};


		// =========== TODO: 

		// pick the first legal move, any of them.
		//		is legal must take a board, pass a board as argument., no just use tile.usedForEvaluation
		//		when I pick a move, modify the board and pass that.
		// then call recursion on it. decrement the depth.
		//		recursion also must take a board are param.
		// 		.... recursion then picks a legal move and calls itself again.
		//			.... UNTIL you reach a leaf node. (depth == depth)
		//			.... or until you 
		//				then you pick the max or min for that depth.
		//				// alpha-b pruning: check that score trumps alpha or beta depending on turn.
		//		recursion must stop at the desired depth.
		// 		recursion must simply give a score to each node.


	}

	void generateField(){
		field = new List <List<Tile>>();
		for(int i = 0; i < mapSize; i++){
			List <Tile> row = new List<Tile>();
			for(int j = 0; j < mapSize; j++){
				//Creates the tiles and set the boolean values to false, also indicates the gridposition of each tile
				Tile tile = ((GameObject)Instantiate(TilePrefab,new Vector3(i,0, j),new Quaternion())).GetComponent<Tile>();
				tile.used = false;
				tile.black = false;
				tile.white = false;
				tile.gridPosition = new Vector2(i,j);
				row.Add (tile);
			}
			//adds each row to the 2D list
			field.Add (row);
		}

	}

	void placeToken(Vector3 position){
		//Retrieve selected position
		Vector2 tilePosition = new Vector2 (position.x, position.z);
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				//checks which field in the list is the position we are looking for
				if(field[i][j].gridPosition == tilePosition){
					//Check if move is legal
					if(isLegal(i,j)){
						//Checks whose turn it is, if true, it's black Tokens, if false, it's white tokens
						if(isTurn ()){
							//Checks for index overflow
							if(!(i+1 >= mapSize)){
								//Checks the availability of the 2 spots required for the Black token
								if((i+1) <= mapSize && field[i][j].used != true && field[i+1][j].used != true){
									//Checks no index is overflowing
									if(!(i < 0 || j < 0 || i >= mapSize || j >= mapSize)){
										//Create the black tiles and set their variables to true
										Instantiate(blackToken,new Vector3 (field[i][j].gridPosition.x, 1, field[i][j].gridPosition.y), new Quaternion());
										Instantiate (blackToken,new Vector3 (field[i+1][j].gridPosition.x, 1, field[i+1][j].gridPosition.y), new Quaternion());
										field[i][j].used = true; //indicates that it's used
										field[i+1][j].used = true;
										field[i][j].black = true; //indicates that it's a black token
										field[i+1][j].black = true;
										field[i][j].min = false; //indicates that it's a not min token
										field[i+1][j].min = false;
										field[i][j].max = false; //indicates that it's a not max token
										field[i+1][j].max = false;
										token.Add (field[i][j]);
										token.Add (field[i+1][j]);
									}
								}
							}
						}
						//Checks for index overflow
						else if(!(j-1 < 0)) {
							//Checks if positions are taken
							if(field[i][j].used != true && field[i][j-1].used != true && turn) {
								//Checks for index overflow
								if(!(i < 0 || (j-1) < 0 || i >= mapSize || j >= mapSize)){
									//Creates the white tiles and set their variables to true
									Instantiate(whiteToken, new Vector3 (field[i][j].gridPosition.x, 1, field[i][j].gridPosition.y), new Quaternion());
									Instantiate (whiteToken, new Vector3 (field[i][j-1].gridPosition.x, 1, field[i][j-1].gridPosition.y), new Quaternion());
									field[i][j].used = true; //indicates that it's used
									field[i][j-1].used = true;
									field[i][j].white = true;//indicates that it's a white token
									field[i][j-1].white = true;
									field[i][j].min = false; //indicates that it's a black token
									field[i][j-1].min = false;
									field[i][j].max = false; //indicates that it's a black token
									field[i][j-1].max = false;
									token.Add (field[i][j]);
									token.Add (field[i][j-1]);
								}
							}
						}
					}
				}
			}
		}
	}

	//Checks whose turn it is
	bool isTurn(){
		if(turn){
			turn = false;
			return true;
		}
		else{
			turn = true;
			return false;
		}
	}

	//Displays messages on the screen (default method in unity), acts as a loop
	void OnGUI(){
		//Displays whose turn it is
		if(turn){
			GUI.Box(new Rect(10,10, 200, 25), "Black Team Turn");
		}
		else{
			GUI.Box(new Rect(10,10, 200, 25), "White Team Turn");
		}
		//Displays victory message
		if(displayWinMessage){
			if(whiteVictory && blackVictory){
				GUI.Box (new Rect(10, 35, 200, 25), "This is an epic Tie");
			}
			else if(whiteVictory){
				GUI.Box (new Rect(10, 35, 200, 25), "White Victory");
			}
			else if(blackVictory){
				GUI.Box (new Rect(10, 35, 200, 25), "Black Victory");
			}
		}
		//Display any Illegal Moves
		if(illegalMessage){
			GUI.Box(new Rect(10,60, 200, 25), message);
		}
	}

	//Determines what kind of error occurred and sends an appropriate message to the OnGUI method
	void checkIlligalMessage(int type){
		if(type == 0){
			illegalMessage = false;
			message = "";
		}
		else if(type == 1){
			illegalMessage = true;
			message = "Tile already in use";
		}
		else if(type == 2){
			illegalMessage = true;
			message = "Out of bounds for white tiles";
		}
		else if(type == 3){
			illegalMessage = true;
			message = "Out of bounds for black tiles";
		}
	}

	//Checks if a move is legal
	bool isLegal(int i, int j){
		// print ("i -> "+i+", j -> "+j);
		// j is rows, (0 is bottom of board).
		// i is columns, (0 is left of board).
		// black needs "tile to its right" to be free.
		// white needs "tile below itself" to be free.
		// turn == true --> AI's turn to play (black).
		// !turn == true --> human's turn to play (white).
		if(field[i][j].used){
			checkIlligalMessage(1);
			return false;
		}
		else if((j - 1 < 0) && !turn){
			checkIlligalMessage(2);
			return false;
		}
		else if((i + 1 >= mapSize) && turn){
			checkIlligalMessage(3);
			return false;
		}
		else if((i + 1 < mapSize) && turn){
			if(field[i+1][j].used){
				checkIlligalMessage(1);
				return false;
			}
		}
		else if((j - 1 >= 0) && !turn){ 
			if(field[i][j-1].used){
				checkIlligalMessage(1);
				return false;
			}
		}
		checkIlligalMessage(0);
		return true;
	}

	void printField() {
		string rowString = "";
		for(int i = 0; i < mapSize; i++){
			rowString = "";
			for(int j = 0; j < mapSize; j++){
				rowString += field[i][j].used+"-"+field[i][j].usedForEvaluation + "/";
			}
			print ("row"+i+" --> "+ rowString);
		}
	}

	//Checks if a move is legal
	bool isLegalMINIMAX(int i, int j, bool isAIsturn){

		//debug:
		//printField ();

		// print ("i -> "+i+", j -> "+j);
		// j is rows, (0 is bottom of board).
		// i is columns, (0 is left of board).
		// black needs "tile to its right" to be free.
		// white needs "tile below itself" to be free.
		// isAIsturn or turn == true --> AI's turn to play (black).
		// !turn == true --> human's turn to play (white).
		bool f = field[i][j].usedForEvaluation;
		if(field[i][j].usedForEvaluation || field[i][j].used){
			checkIlligalMessage(1);
			return false;
		}
		else if((j - 1 < 0) && !isAIsturn){
			checkIlligalMessage(2);
			return false;
		}
		else if((i + 1 >= mapSize) && isAIsturn){
			checkIlligalMessage(3);
			return false;
		}
		else if((i + 1 < mapSize) && isAIsturn){
			bool x = field[i+1][j].usedForEvaluation;
			bool y = field[i+1][j].used;
			if(field[i+1][j].usedForEvaluation || field[i+1][j].used){
				checkIlligalMessage(1);
				return false;
			}
		}
		else if((j - 1 >= 0) && !isAIsturn){ 
			if(field[i][j-1].usedForEvaluation || field[i][j-1].used){
				checkIlligalMessage(1);
				return false;
			}
		}
		checkIlligalMessage(0);
		return true;
	}

	
	Tile findANotUsedTile(bool isAIsturn){

		// ADD Nicolas, trying to fix black playing illegal moves
		// this is so that we don't consider illegal moves in the new if statement below.
		// just ignoring used tile isn't sufficient, we must also ignore illegal moves.
		//turn = true;
		
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				//				if(field[i][j].used == true){

				field[i][j].i = i;
				field[i][j].j = j;

				if(isLegalMINIMAX(i, j, isAIsturn)) { // ADD Nicolas, trying to fix black playing illegal moves!
					print("Found One Empty Tile for this tree node: "+"i = "+i+", j = "+j);
					field[i][j].i = i;
					field[i][j].j = j;
					return field[i][j];			
				}
			}
		}
		
		// ADD Nicolas to fix black playing illegal moves:
		// reset turn to AI's turn, (since we've been playing with it to use the isLegal() function during minimax, so the turn was artificially changed, temporarily for the sake of the minimax algorithm)
		//turn = true;

//		for(int i = 0; i < mapSize; i++){
//			for(int j = 0; j < mapSize; j++){
//				if(field[i][j].used == false){
//					print("Found One Empty Tile for this tree node");
//					return field[i][j];
//				}
//			}
//		}
		//return field[0][0];
		return null; // should almost never reach here! except toward the end of the game.
	}

	void resetUsedFOREVALUATION() {

	}

	Tile AIPlayerMin(bool isAIsturn) { //(Tile tile){

		Tile tile =  findANotUsedTile(isAIsturn);//new Tile();
		//debug:
		if(tile == null) {
			print ("TILE RETURNED BY findANotUsedTile IS NULL !!!!!!!!!!!!!!!!!!!!!!!!!!!!! ");
			return null;
		}
		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			//return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
		}

		Tile scd = findANotUsedTile(isAIsturn);
		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			//return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
		}

		for(int i = 0; i < mapSize - 1; i++){
			for(int j = 0; j < mapSize; j++){

				field[i][j].i = i;//debug
				field[i][j].j = j;

				if(field[i][j].used == false && field[i][j].usedForEvaluation == false &&
				   field[i+1][j].used == false && field[i+1][j].usedForEvaluation == false){ // isn't this if statement redundant with isLegal() ?

					//
					//turn = false;
					isAIsturn = false;
					if(isLegalMINIMAX (i,j, isAIsturn)){
						determineTileScore(i,j);
						if(field[i][j].score < tile.score && !token.Contains(tile)){
							tile = field[i][j];
							scd = field[i+1][j];
						}
					}
					//
					//turn = true;
				}
			}
		}
		tile.min = true;
		//tile.used = true;
		searchList.Add (tile);
		scd.min = true;
		//scd.used = true;
		searchList.Add (scd);
		if(tile.usedForEvaluation){
			print ("Min already used");
		}
		else{
			//tile.usedForEvaluation = true;
			//scd.usedForEvaluation = true;
		}

		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
			return tile;
		}	
	}

	//For Deliverable 2 and 3, Min player options, selects the highest score
	Tile AIPlayerMax(bool isAIsturn) { //(Tile tile){

		Tile tile =  findANotUsedTile(isAIsturn);//new Tile();
		//debug:
		if(tile == null) {
			print ("TILE RETURNED BY findANotUsedTile IS NULL !!!!!!!!!!!!!!!!!!!!!!!!!!!!! ");
			return null;
		}
		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			//return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
		}
		
		Tile scd = findANotUsedTile(isAIsturn);
		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			//return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
		}

		for(int i = 0; i < mapSize; i++){
			for(int j = 1; j < mapSize; j++){

				field[i][j].i = i;//debug
				field[i][j].j = j;

				if(field[i][j].used == false && field[i][j].usedForEvaluation == false &&
				   field[i][j-1].used == false && field[i][j-1].usedForEvaluation == false){

					//
					//turn = true;
					isAIsturn = true;
					if(isLegalMINIMAX (i,j, isAIsturn)){
						determineTileScore(i,j);
						if(field[i][j].score > tile.score && !token.Contains(tile)){
							tile = field[i][j];
							scd = field[i][j-1];
						}
					}
					//
					//turn = true;
				}
			}
		}
		tile.min = true;
		//tile.used = true;
		searchList.Add (tile);
		scd.min = true;
		//scd.used = true;
		searchList.Add (scd);
		if(tile.usedForEvaluation){
			print ("BREAKPOINT Max already used !!!!!!!! ");
		}
		else{
			//tile.usedForEvaluation = true;
			//scd.usedForEvaluation = true;
		}

		if(isLegalMINIMAX(tile.i, tile.j, isAIsturn)) {
			return tile;
		} else {
			print ("BREAKPOINT: AI returns an illegal move!");
			return tile;
		}

	}

	void determineTileTheList(Tile tile){
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				if(field[i][j].gridPosition == tile.gridPosition){
					indexX = i;
					indexY = j;
				}
			}
		}
	}

	//Determine the score (heuristic) of each Tile, checks the surrounding tile (used or not) and gives a value
	void determineTileScore(int i, int j){
		for(int a = 0; a < mapSize-1; a++){
			if(!(GameManager.mapSize <= a || a < 0) && !field[i][j].scoreDetermined ){
				if(field[a][j].used == true && field[a+1][j].used){
					field[i][j].score++;
				}
				else if(field[a][j].used == false && !field[i][j].usedForEvaluation && a!=i ){
					field[i][j].score--;
				}
			}
		}

		for(int a = 0; a < mapSize; a++){
			if(!(GameManager.mapSize <= a || a < 0) && !field[i][j].scoreDetermined ){
				if(field[a][j].used == true){
					field[i][j].score++;
				}
				else if(field[i][a].used == false && !field[i][j].usedForEvaluation && a!=i ){
					field[i][j].score--;
				}
			}
		}

		field[i][j].scoreDetermined = true;
	}

	//Checks if the game reached it's ending condition
	bool checkEndGame(){
		int black = 0;
		int white = 0;
		for(int i = 0; i < mapSize; i++){
			for(int j = 0; j < mapSize; j++){
				if(field[i][j].used == false){
					if(i+1 < mapSize){
						//Checks if the black Team lost, if no-one makes 
						if(field[i+1][j].used == false){
							whiteVictory = false;
							black++;
						}
					}
					if(j+1 < mapSize){
						//Checks if the white team lost
						if(field[i][j+1].used == false){
							blackVictory  = false;
							white++;
						}
					}
				}
			}
		}
		if(black == 0 && white == 0){
			blackVictory = true;
			whiteVictory = true;
		}
		//if no-one has a move, then team black lost
		if(black == 0){
			whiteVictory = true;
			return true;
		}
		//if no-one has a move, then team black lost
		if(white == 0){
			blackVictory = true;
			return true;
		}
		return false;

	}
}
