using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.VisualScripting;
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }
    public GameObject shadowPrefab;
    public GameObject copyGrid;
    
    [SerializeField] private bool networkingOn = false;
    public bool startFromScene = true;

    private GameObject shadow1;
    private GameObject shadow2;

    private GameObject w1Copy;
    private GameObject w2Copy;
    private float overlayAlpha = 0.3f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }
    void OnEnable()
    {
        //if (networkingOn)
        //{
        //    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SetUpLevel;
        //} else
        //{
        //    SceneManager.sceneLoaded += SetUpLevel;
        //}
        SceneManager.sceneLoaded += SetUpLevel;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SetUpLevel;
    }

    void Update()
    {
        // TODO: rework these for networking
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            w2Copy.gameObject.SetActive(!w2Copy.gameObject.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            w1Copy.gameObject.SetActive(!w1Copy.gameObject.activeSelf);
        }
    }


    public void SetUpLevel(Scene scene, LoadSceneMode mode)
    {
        PlayerManager.instance.SetPlayers();
        PlayerManager.instance.MakeShadows();

        CopyAndSendWorldInfo();
        SetCameras();
    }

    public void SetUpLevel(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SetUpLevel(SceneManager.GetSceneByName(sceneName), loadSceneMode);
    }

    public void CopyAndSendWorldInfo()
    {
        bool w1Open = w1Copy == null ? true : w1Copy.activeSelf;
        bool w2Open = w2Copy == null ? true : w2Copy.activeSelf;

        if (w1Copy != null)
        {
            Destroy(w1Copy.gameObject);
        }
        
        if (w2Copy != null)
        {
            Destroy(w2Copy.gameObject);
        }

        CopyHelper(1);
        CopyHelper(2);

        if (w1Copy != null && w2Copy != null)
        {
            w1Copy.SetActive(w1Open);
            w2Copy.SetActive(w2Open);
        }
    }

    private void CopyHelper(int world)
    {
        GameObject level = GameObject.FindGameObjectWithTag("World"+world+"Level");

        if (level == null)
        {
            return;
        }

        GameObject transferLevel = Instantiate(copyGrid);
        int layer = LayerMask.NameToLayer("World"+world);
        transferLevel.layer = layer;

        int direction = world == 1 ? 1 : -1;
        transferLevel.transform.position = level.transform.position + new Vector3(32 * direction, 0, 0);
        transferLevel.transform.parent = level.transform.parent;

        GameObject shadow = ChangeLayerAndOpacity(transferLevel, level, layer, world);

        if (world == 1)
        {
            w1Copy = shadow;
        } else if (world == 2)
        {
            w2Copy = shadow;
        }
    }

    private GameObject ChangeLayerAndOpacity(GameObject transferLevelGo, GameObject levelGo, int layer, int world)
    {
        if (!levelGo.activeSelf)
        {
            return null;
        }

        SpriteRenderer levelSR = levelGo.GetComponent<SpriteRenderer>();
        Tilemap levelTM = levelGo.GetComponent<Tilemap>();
        Grid grid = levelGo.GetComponent<Grid>();
        GameObject levelshadow = transferLevelGo;
        int direction = world == 1 ? 1 : -1;


        if (levelTM)
        {
            levelshadow = Instantiate(levelGo);
            Tilemap transferTM = levelshadow.GetComponent<Tilemap>();
            transferTM.color = new Color(levelTM.color.r, levelTM.color.g, levelTM.color.b, overlayAlpha);
            transferTM.GetComponent<TilemapRenderer>().sortingOrder = 1;
            levelshadow.transform.parent = transferLevelGo.transform;
        }
        else if (!grid)
        {
            levelshadow = new GameObject("LevelAssetShadow");
            levelshadow.transform.parent = transferLevelGo.transform;
            levelshadow.transform.localScale = levelGo.transform.localScale;
            levelshadow.transform.position = levelGo.transform.position + new Vector3(32 * direction, 0, 0);
            levelshadow.transform.rotation = levelGo.transform.rotation;

            if (levelSR)
            {
                SpriteRenderer transferSR = levelshadow.AddComponent<SpriteRenderer>();
                transferSR.sprite = levelSR.sprite;
                transferSR.color = new Color(transferSR.color.r, transferSR.color.g, transferSR.color.b, overlayAlpha);
                transferSR.adaptiveModeThreshold = levelSR.adaptiveModeThreshold;
                transferSR.drawMode = levelSR.drawMode;
                transferSR.size = levelSR.size;
                transferSR.tileMode = levelSR.tileMode;
                transferSR.sortingLayerName = "entities";
                transferSR.sortingOrder = 1;
            }
        }

        LevelAssetShadow shadow = levelshadow.AddComponent<LevelAssetShadow>();
        shadow.parent = levelGo;
        shadow.offset = world == 1 ? new Vector3(32, 0, 0) : new Vector3(-32, 0, 0); 

        levelshadow.layer = layer;

        for (int i = 0; i < levelGo.transform.childCount; i++)
        {
            ChangeLayerAndOpacity(levelshadow, levelGo.transform.GetChild(i).gameObject, layer, world);
        }

        return levelshadow;
    }

    private void SetCameras()
    {
        // find the cameras
        GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");

        Camera player1camera = null;
        Camera player2camera = null;

        foreach (GameObject c in cameras)
        {
            Camera camera = c.GetComponent<Camera>();
            int world1layer = LayerMask.NameToLayer("World1");
            int world2layer = LayerMask.NameToLayer("World2");

            if (c.layer == world1layer)
            {
                player1camera = camera;
            }
            else if (c.layer == world2layer)
            {
                player2camera = camera;
            }
        }

        if (player1camera == null)
        {
            return;
        }

        if (networkingOn)
        {
            if (PlayerManager.instance.currPlayer == 1)
            {
                player1camera.enabled = true;
                player2camera.enabled = false;

                //edit camera locations on display
                player1camera.rect = new Rect(0, 0, 1, 1);
            }
            else
            {
                player1camera.enabled = false;
                player2camera.enabled = true;
                player2camera.rect = new Rect(0, 0, 1, 1);
            }

        } else
        {
            player1camera.enabled = true;
            player2camera.enabled = true;

            if (PlayerManager.instance.playerOnLeft == 2)
            {
                player1camera.rect = new Rect(0.5f, 0, 0.5f, 1);
                player2camera.rect = new Rect(0, 0, 0.5f, 1);
            }
            else
            {
                player1camera.rect = new Rect(0, 0, 0.5f, 1);
                player2camera.rect = new Rect(0.5f, 0, 0.5f, 1);
            }
        }
    }

    public void SetNetworked(bool networked)
    {
        networkingOn = networked;

        if (networkingOn)
        {
            Screen.SetResolution(640, 480, false);

        } else
        {
            Screen.SetResolution(1280, 480, false);
        }
    }

    public bool IsNetworked() {
        return networkingOn;
    }

    // To deal with external functions that still call Game Manager. Should be refactored to be removed but still may be necessary for older scenes. 
    public GameObject GetPlayer(int num) { return PlayerManager.instance.GetPlayer(num); }
    public GameObject GetShadow(int num) { return PlayerManager.instance.GetShadow(num); }
    public void MakeShadows() { PlayerManager.instance.MakeShadows(); }
    public void SendMomentum(Vector2 momentum, GameObject sender) { PlayerManager.instance.SendMomentum(momentum, sender); }

    //public void SetPlayerAndShadow(GameObject player, GameObject shadow, int num) { PlayerManager.instance.SetPlayerAndShadow(player, shadow, num); }
}
