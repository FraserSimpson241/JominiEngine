using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using ProtoMessageClient;
using System;

public class MapManager : Controller
{
    [SerializeField] private Tilemap map;

    [SerializeField] private TileBase whiteTile;

    [SerializeField] private GameObject lblTest;
    [SerializeField] private GameObject canvasWorldSpace;

    [SerializeField] private Sprite whiteSprite;

    [SerializeField] private GameObject hexagonPrefab;

    [SerializeField] private Text lblPageTitle;
    [SerializeField] private Text lblMessageForUser;

    /// <summary>
    /// 2D array containing Fief IDs where the indices are their positions on the grid.
    /// </summary>
    private string[,] mapLocationsFiefIDs;

    //private GameObject cam;
    //[SerializeField]
    //private GameObject cam;

    //private CameraDragController camDragController;

    //public struct RGB {
    //    public RGB(int r, int g, int b) {
    //        R = r;
    //        G = g;
    //        B = b;
    //    }
    //    public int R { get; }
    //    public int G { get; }
    //    public int B { get; }
    //}

    ////private List<RGB> colours;
    

    void Awake() {
        //lblMessageForUser = GameObject.Find("lblMessageForUser").GetComponent<Text>();
    }

    // Start is called before the first frame update
    new void Start()
    {
        //GameObject lblPageTitle = GameObject.Find("lblPageTitle");
        //lblPageTitle.GetComponent<Text>().text = "World Map";
        lblPageTitle.text = "World Map";
        lblMessageForUser.text = "";
        int i;

        //Color myColor = new Color32(255,0,0,255);

        var worldMap = (ProtoWorldMap)GetWorldMap(tclient);
                
        fiefNames.Clear();
        fiefOwners.Clear();
        for(i = 0; i < worldMap.fiefIDNames.Length-2; i+=3) {
            fiefNames.Add(worldMap.fiefIDNames[i], worldMap.fiefIDNames[i+1]);
            fiefOwners.Add(worldMap.fiefIDNames[i], worldMap.fiefIDNames[i+2]);
        }

        int dimensionY = worldMap.dimensionY;
        int dimensionX = worldMap.dimensionX;
        mapLocationsFiefIDs = new string[dimensionY, dimensionX];

        i = 0;
        for(int y = 0; y < worldMap.dimensionY; y++) {
            for(int x = 0; x < worldMap.dimensionX; x++) {
                // Rebuild 2D array grid from 1D array.
                string fiefID = worldMap.gameMapLayout[i];
                mapLocationsFiefIDs[y,x] = fiefID;

                if(string.IsNullOrWhiteSpace(fiefID)) {
                    // Water hexes are no longer rendered as a background image has taken their place.
                }
                else {
                    // fief hex
                    Vector3Int gridPosition = new Vector3Int(x,-y,0);
                    GameObject newHexagon = Instantiate(hexagonPrefab);
                    SpriteRenderer hexagonColor = newHexagon.GetComponent<SpriteRenderer>();

                    newHexagon.transform.position = map.CellToWorld(gridPosition);

                    if(ownerColours.TryGetValue(fiefOwners[fiefID], out Color colour)) {
                        hexagonColor.color = colour;
                    }
                    else {
                        // Pick a random colour
                        int index = UnityEngine.Random.Range(0, colours.Count);
                        colour = colours[index];

                        // Assign to char
                        ownerColours.Add(fiefOwners[fiefID], colour);
                        // Remove colour from availability
                        colours.RemoveAt(index);

                        // Set hex colour
                        hexagonColor.color = colour;
                    }

                    //if(fiefOwners[fiefID].Equals("Char_158")) {
                    //    hexagonColor.color = myColor;
                    //}
                    //else {
                    //    hexagonColor.color = Color.green;
                    //}
                    
                    // Fief label.
                    GameObject myText = (GameObject)Instantiate(lblTest);
                    myText.transform.SetParent(canvasWorldSpace.transform);
                    myText.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    myText.transform.position = map.CellToWorld(gridPosition);
                    //myText.GetComponent<Text>().text = mapLocationsFiefIDs[y,x];
                    myText.GetComponent<Text>().text = fiefNames[fiefID];
                }

                i++;
            }
        }

        // Set camera's start point to the centre of the map.
        Vector3Int centreHex = new Vector3Int(dimensionX/2, -dimensionY/2, -10);
        Camera.main.transform.position = centreHex;
        //Vector3 centrePoint = map.CellToWorld(centreHex);
        //Camera.main.transform.position = centrePoint;

        //camDragController = cam.GetComponent<CameraDragController>();
        //camDragController.resetCamera = centrePoint;


        GameObject testCam = GameObject.Find("Main Camera");
        //testCam.GetComponent<CameraDragController>().resetCamera = centrePoint;
        testCam.GetComponent<CameraDragController>().resetCamera = centreHex;

        //Debug.Log("Map Manager's Start has ended.");
    }

    // Update is called once per frame
    void Update()
    {
        //if(Input.GetKey(KeyCode.G)) {
        //    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    Vector3Int gridPosition = map.WorldToCell(mousePosition);

        //    //TileBase clickedTile = map.GetTile(gridPosition);
        //    ////print("At position " + gridPosition + " there is a " + clickedTile);
        //    //print(gridPosition.x + " + " + gridPosition.y + " = " + (gridPosition.x + gridPosition.y));

        //    map.SetTile(gridPosition, whiteTile);
        //    GameObject myText = (GameObject)Instantiate(lblTest);
        //    myText.transform.SetParent(canvas.transform);
        //    //myText.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        //    myText.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        //    myText.transform.position = map.CellToWorld(gridPosition);

        //    myText.GetComponent<Text>().text = gridPosition.ToString();
        //}

        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPosition = map.WorldToCell(mousePosition);
            try{
                string selectedFiefID = mapLocationsFiefIDs[-gridPosition.y, gridPosition.x];
            
                print(gridPosition.x + " " + gridPosition.y + " " + selectedFiefID);

                // Clicked on water.
                if(string.IsNullOrWhiteSpace(selectedFiefID)) {
                    return;
                }

                fiefToViewID = selectedFiefID;
                GoToScene(SceneName.ViewFief);

                //var fief = GetFiefDetails(selectedFiefID, tclient);
                //if(fief.ResponseType == DisplayMessages.ErrorGenericTooFarFromFief) {
                //    lblMessageForUser.text = "You have no information on fief " + selectedFiefID + ".";
                //}
                //else {
                //    currentlyViewedFief = (ProtoFief)GetFiefDetails(selectedFiefID, tclient);
                //    GoToScene(SceneName.ViewFief);
                //}
            }
            catch(IndexOutOfRangeException) {
                // IndexOutOfRangeException
            }
        }
    }
}
