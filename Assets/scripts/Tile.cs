using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {

	// let the tile be aware of its position on the grid: field[i][j]
	public int i;
	public int j;

	public Vector2 gridPosition = new Vector2 (2,2);
	public Vector3 position;
	public GameObject textMesh;
	public bool used;
	public bool usedForEvaluation;
	public bool decidedToUse;
	public bool max;
	public bool min;
	public bool black;
	public bool white;
	public bool highlighted;
	public bool scoreDetermined;
	public int score;
	public byte score2; //secondary heuristic that costs less

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		//Visual update to show which tiles are used
		if(used){
			if(white){
				transform.renderer.material.color = Color.green;
			}
			else{
				transform.renderer.material.color = Color.red;
			}
		}
		if(usedForEvaluation){
			if(max){
				transform.renderer.material.color = Color.cyan;
			}
			else{
				transform.renderer.material.color = Color.gray;
			}
		}
		else{
			if(!highlighted){
				transform.renderer.material.color = Color.white;
			}
			if(used){
				if(white){
					transform.renderer.material.color = Color.green;
				}
				else{
					transform.renderer.material.color = Color.red;
				}
			}
		}
	}

	//Returns tile position
	Vector3 givePosition(){
		return position;
	}

	//Checks which tile we are hovering on
	void OnMouseEnter(){
		transform.renderer.material.color = Color.blue;
		GameManager.blueTile = this;
		highlighted = true;
	}

	//Reverts the highlight on the Tile when the mouse leaves it
	void OnMouseExit(){
		transform.renderer.material.color = Color.white;
		highlighted = false;
	}
}
