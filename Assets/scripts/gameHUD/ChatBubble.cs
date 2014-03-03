﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChatBubble : MonoBehaviour {

    public static ChatBubble Instance;

    private GameObject m_myChatBubblePrefab;
    private Dictionary<PlayerInfo, GameObject> m_chatBubbles;

    private networkVariables m_nvs;
    private PlayerInfo m_myPlayerInfo;
    private GameObject m_myCart;
    private Camera m_myCamera;

    private bool m_initialized = false;
    private int m_numPlayersExpected;

    //tweak this for offest of chat bubble over cart
    private const float k_heightOverCart = 4.2f;


    void Start()
    {
        AttemptInitialize();
        if (Instance == null) {
            Instance = this;
        }
    }

    void Update()
    {
        //never do anything if network variables weren't found
        if (!m_initialized) {
            AttemptInitialize();
            return;
        }

        //first, make sure the players we expect are there, or clean up
        //appropriate containing structures
        CheckPlayerListValidity();
        //then, update marker positions
        UpdatePositions();

    }

    void CheckPlayerListValidity()
    {
        // -1 is to account for player not being in enemy marker list
        if (m_numPlayersExpected < m_nvs.players.Count) {
            RegisterNewPlayers();
        } else if (m_numPlayersExpected > m_nvs.players.Count) {
            CleanupPlayerList();
        }
    }

    void CleanupPlayerList()
    {
        for (int i = 0; i < m_chatBubbles.Keys.Count; i++) {
            PlayerInfo[] keys = new PlayerInfo[m_chatBubbles.Keys.Count];
            m_chatBubbles.Keys.CopyTo(keys, 0);
            PlayerInfo player = keys[i];
            if (player != null) {
                if (!m_nvs.players.Contains(player)) {
                    Destroy(m_chatBubbles[player]);
                    m_chatBubbles.Remove(player);
                    m_numPlayersExpected--;
                }
            }
        }
    }

    void RegisterNewPlayers()
    {
        for (int i = 0; i < m_nvs.players.Count; i++) {
            PlayerInfo player = (PlayerInfo)m_nvs.players[i];
            if (player != null) {
                if (!m_chatBubbles.ContainsKey(player)) {
                    GameObject playerCart = player.cartGameObject;
                    Vector3 thisChatBubblePos = playerCart.transform.position;
                    thisChatBubblePos.y += k_heightOverCart;
                    GameObject thisChatBubble = GameObject.Instantiate(Resources.Load("chatBubblePrefab")) as GameObject;
                    thisChatBubble.transform.position = thisChatBubblePos;
                    Renderer objRenderer = thisChatBubble.GetComponentInChildren<Renderer>();

                    //if renderer is not obtained, bail out
                    if (objRenderer != null) {
                        Color objColor = objRenderer.material.GetColor("_Color");

                        objColor.a = 0.0f;

                        objRenderer.material.SetColor("_Color", objColor);
                    }

                    m_chatBubbles.Add(player, thisChatBubble);
                    m_numPlayersExpected++;
                }
            }
        }
    }

    void UpdatePositions()
    {
        for (int i = 0; i < m_nvs.players.Count; i++) {
            PlayerInfo player = (PlayerInfo)m_nvs.players[i];
            if (player != null) {
                if (m_chatBubbles.ContainsKey(player)) {
                    GameObject playerChatBubble = m_chatBubbles[player];
                    Vector3 thisChatBubblePos = player.cartGameObject.transform.position;
                    thisChatBubblePos.y += k_heightOverCart;
                    playerChatBubble.transform.position = thisChatBubblePos;

                    playerChatBubble.transform.rotation = m_myCamera.transform.rotation; //billboard ball marker towards the camera
                }
            }
        }
    }

    void AttemptInitialize()
    {
        m_nvs = FindObjectOfType<networkVariables>() as networkVariables;

        //confirm ability to get network variables, else return here without setting initialization flag
        if (m_nvs == null) {
            //Debug.Log("Unable to find network variables!");
            return;
        }

        Initialize();
    }

    void Initialize()
    {
        m_myPlayerInfo = m_nvs.myInfo;

        //can't do anything else if we don't have PlayerInfo resources loaded!
        if (m_myPlayerInfo.cartGameObject == null) return;

        //m_myCamera = m_myPlayerInfo.cartGameObject.transform.FindChild("multi_buggy_cam").gameObject.camera;
        if (m_myCamera == null) m_myCamera = Camera.main;

        m_myCart = m_myPlayerInfo.cartGameObject;

        //need own cart and camera to be existent to initialize
        if (m_myCart == null || m_myCamera == null) {
            return;
        }
        
        //initialize enemy ball markers
        m_chatBubbles = new Dictionary<PlayerInfo, GameObject>();
        for (int i = 0; i < m_nvs.players.Count; i++) {
            PlayerInfo player = (PlayerInfo)m_nvs.players[i];
            if (player != null) {
                if (!m_chatBubbles.ContainsKey(player)) {
                    GameObject playerCart = player.cartGameObject;
                    Vector3 thisChatBubblePos = playerCart.transform.position;
                    thisChatBubblePos.y += k_heightOverCart;
                    GameObject thisChatBubble = GameObject.Instantiate(Resources.Load("chatBubblePrefab")) as GameObject;
                    Renderer objRenderer = thisChatBubble.GetComponentInChildren<Renderer>();

                    //if renderer is not obtained, bail out
                    if (objRenderer != null) {
                        Color objColor = objRenderer.material.GetColor("_Color");

                        objColor.a = 0.0f;

                        objRenderer.material.SetColor("_Color", objColor);
                    }
                    thisChatBubble.transform.position = thisChatBubblePos;

                    m_chatBubbles.Add(player, thisChatBubble);
                    m_numPlayersExpected++;
                }
            }
        }

        m_initialized = true;
    }
    
    //called in from wherever a netChat message is received, giving network ID of player to display bubble over
    public static void DisplayChat(NetworkViewID ID)
    {
        //Debug.Log("Passed ID: " + ID);
        for (int i = 0; i < Instance.m_nvs.players.Count; i++) {
            PlayerInfo player = (PlayerInfo)Instance.m_nvs.players[i];
            if (player != null) {
                //Debug.Log("My ID: " + player.ballViewID);
                if (player.ballViewID == ID) {
                    Instance.StartCoroutine(Instance.Display(Instance.m_chatBubbles[player], 1.0f));
                    break;
                }
            }
        }
    }

    //display chat bubble over a players head, for time "overTime"
    IEnumerator Display(GameObject chatBubble, float overTime)
    {
        float startTime = Time.time;
        Renderer objRenderer = chatBubble.GetComponentInChildren<Renderer>();

        //if renderer is not obtained, bail out
        if (objRenderer != null) {
            Color objColor = objRenderer.material.GetColor("_Color");

            objColor.a = 1.0f;

            objRenderer.material.SetColor("_Color", objColor);

            while (Time.time < startTime + overTime) {
                yield return null;
            }

            if (objRenderer != null) {
                objColor.a = 0.0f;
                objRenderer.material.SetColor("_Color", objColor);
            }
        }
    }
}
