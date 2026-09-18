using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using DG.Tweening;
using System.Linq;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using Best.HTTP.Shared;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField]
  internal GameManager gameManager;

  [SerializeField]
  private UiManager uiManager;

  internal GameData initialData = null;
  internal Root resultData = null;
  internal Player playerdata = null;
  [SerializeField]
  internal List<string> bonusdata = null;
  //WebSocket currentSocket = null;
  internal bool isResultdone = false;
  // protected string nameSpace="game"; //BackendChanges
  protected string nameSpace = "playground"; //BackendChanges
  private Socket gameSocket; //BackendChanges

  private SocketManager manager;


  protected string SocketURI = null;
  // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
  protected string TestSocketURI = "https://devrealtime.dingdinghouse.com";
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField]
  private string testToken;
  protected string gameID = "SL-WB";
  //protected string gameID = "";

  internal bool isLoaded = false;

  internal bool SetInit = false;

  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  private bool isConnected = false; //Back2 Start
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private float pongTimeout = 3f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  private Coroutine PingRoutine; //Back2 end

  private bool hasFocus = true;
  private float focusLostTime = 0f;
  private Coroutine focusCheckRoutine;
  private float maxBackgroundTime = 60f;
  private bool isExiting = false;
  private bool isBeingDestroyed = false;

  [SerializeField] private GameObject RaycastBlocker;

  private void Awake()
  {
    //Debug.unityLogger.logEnabled = false;
    isLoaded = false;
    SetInit = false;

  }

  private void OnDestroy()
  {
    isBeingDestroyed = true;
  }

  internal void HandleFocusChange(bool focus)
  {
    hasFocus = focus;

    if (!focus)
    {
      focusLostTime = Time.time;
      if (focusCheckRoutine == null && !isExiting && !isBeingDestroyed)
        focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
    }
    else
    {
      if (focusCheckRoutine != null)
      {
        StopCoroutine(focusCheckRoutine);
        focusCheckRoutine = null;
      }
    }
  }

  private IEnumerator FocusTimeoutCheck()
  {
    while (!hasFocus && !isExiting && !isBeingDestroyed)
    {
      if (Time.time - focusLostTime >= maxBackgroundTime)
      {
        Debug.LogWarning("[SOCKET] Background timeout — closing connection");
        isConnected = false;
        ResetPingRoutine();

        if (manager != null)
        {
          try { manager.Close(); }
          catch (Exception e) { Debug.LogWarning($"[SOCKET] Focus close error: {e.Message}"); }
        }

        uiManager.DisconnectionPopup();
        focusCheckRoutine = null;
        yield break;
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    focusCheckRoutine = null;
  }

  private void Start()
  {
    //OpenWebsocket();
    OpenSocket();
  }
  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
    // Proceed with connecting to the server using myAuth and socketURL
  }

  string myAuth = null;

  private void OpenSocket()
  {
    //Create and setup SocketOptions
    SocketOptions options = new SocketOptions(); //Back2 Start
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3);
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket; //BackendChanges

    //    Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.RegisterAuthTokenListener(gameObject.name); // listen for host's TokenReceived before asking
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = testToken,
        //  gameId = gameID
      };
    };
    options.Auth = authFunction;
    // Proceed with connecting to the server
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      Debug.Log("My Auth is null");
      yield return null;
    }
    while (SocketURI == null)
    {
      Debug.Log("My Socket is null");
      yield return null;
    }
    Debug.Log("My Auth is not null");
    // Once myAuth is set, configure the authFunction
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
        //gameId = gameID
      };
    };
    options.Auth = authFunction;

    Debug.Log("Auth function configured with token: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);
    yield return null;
  }

  private void SetupSocketManager(SocketOptions options)
  {
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(nameSpace))
    {  //BackendChanges Start
      gameSocket = this.manager.Socket;
    }
    else
    {
      print("nameSpace: " + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("result", OnListenEvent);
    gameSocket.On<string>("message", OnListenEvent);
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
    gameSocket.On<string>("pong", OnPongReceived);
    gameSocket.On<string>("balance:sync", OnBalanceSync);
    manager.Open();
  }

  // Connected event handler implementation
  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  } //Back2 end


  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    uiManager.DisconnectionPopup();
    ResetPingRoutine();
  } //Back2 end

  private void OnPongReceived(string data) //Back2 Start
  {
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
  } //Back2 end

  private void OnBalanceSync(string data)
  {
    BalanceSyncPayload syncPayload = JsonConvert.DeserializeObject<BalanceSyncPayload>(data);
    if (syncPayload == null) return;

    if (playerdata == null) playerdata = new Player();
    playerdata.balance = syncPayload.balance;

    gameManager.UpdateBalanceDisplay(syncPayload.balance);
  }

  private void OnError(Error err)
  {
    Debug.LogError("[ERROR] Socket error: " + err);
    if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
    {
      Debug.LogWarning("Session expired detected");
      OnDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("session_expired");
#endif
    }
    else
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("error");
#endif
    }
  }

  private void OnListenEvent(string data)
  {
    // Debug.Log("Received some_event with data: " + data);
    ParseResponse(data);
  }

  private void OnSocketState(bool state)
  {
    if (state)
    {
      Debug.Log("my state is " + state);
    }
    else
    {

    }
  }
  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }
  private void OnSocketAlert(string data)
  {
    //        Debug.Log("Received alert with data: " + data);
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
    uiManager.ADfunction();
  }

  private void SendPing() //Back2 Start
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      if (missedPongs == 0)
      {
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          uiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      SendDataWithNamespace("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end
  private void AliveRequest()
  {
    SendDataWithNamespace("YES I AM ALIVE");
  }

  private void SendDataWithNamespace(string eventName, string json = null)
  {
    // Send the message
    if (gameSocket != null && gameSocket.IsOpen) //BackendChanges
    {
      if (json != null)
      {
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        gameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }

  internal void ReactNativeCallOnFailedToConnect() //BackendChanges
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); // was "onExit" — host matches "OnExit"
#endif
  }

  internal IEnumerator CloseSocket() //Back2 Start
  {
    isExiting = true;
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  } //Back2 end
  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          gameManager.uiManager.touchDisable.SetActive(false);
          initialData = myData.gameData;
          playerdata = myData.player;
          setInitialData(initialData.bets);

#if UNITY_WEBGL && !UNITY_EDITOR
                   JSManager.SendCustomMessage("OnEnter");
#endif
          RaycastBlocker.SetActive(false);

          break;
        }
      case "ResultData":
        {
          //Debug.Log(jsonObject);
          resultData = myData;
          playerdata = myData.player;

          isResultdone = true;
          break;
        }
      case "ExitUser":
        {
          if (gameSocket != null) //BackendChanges
          {
            Debug.Log("Dispose my Socket");
            this.manager.Close();
          }
#if UNITY_WEBGL && !UNITY_EDITOR
          JSManager.SendCustomMessage("OnExit");
#endif
          break;
        }
    }
  }


  private void setInitialData(List<double> slotPop)
  {

    isLoaded = true;
    gameManager.setInitialUI();
  }



  internal void AccumulateResult(double currBet, int currentLines, int spin, int risk)
  {
    isResultdone = false;
    MessageData message = new MessageData();
    //  message.data = new BetData();
    message.payload = new Data();
    // message.data.currentBet = currBet;
    // message.data.spins = 1;
    // message.data.currentLines = currentLines;
    // message.data.selectedRisk = risk;
    // message.id = "SPIN";
    message.type = "BLOCK_SPIN";
    message.payload.betIndex = currBet;
    message.payload.spins = spin;
    message.payload.selectedRisk = risk;

    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }


  private List<string> RemoveQuotes(List<string> stringList)
  {
    for (int i = 0; i < stringList.Count; i++)
    {
      stringList[i] = stringList[i].Replace("\"", ""); // Remove inverted commas
    }
    return stringList;
  }

  internal List<int> ConvertListListIntToListint(List<List<int>> listOfLists)
  {
    List<int> resultList = new List<int>();

    foreach (List<int> innerList in listOfLists)
    {
      // Convert each integer in the inner list to string
      List<int> intList = new List<int>();
      foreach (int number in innerList)
      {

        resultList.Add(number);
      }


    }

    return resultList;
  }

  public List<int> ConvertStringsToIntegers(List<List<string>> inputList)
  {
    List<int> outputList = new List<int>();

    foreach (var row in inputList)
    {
      foreach (var item in row)
      {
        if (int.TryParse(item, out int number))
        {
          outputList.Add(number);
        }
      }
    }

    return outputList;
  }

  private List<string> TransformAndRemoveRecurring(List<List<string>> originalList)
  {
    // Flattened list
    List<string> flattenedList = new List<string>();
    foreach (List<string> sublist in originalList)
    {
      flattenedList.AddRange(sublist);
    }

    // Remove recurring elements
    HashSet<string> uniqueElements = new HashSet<string>(flattenedList);

    // Transformed list
    List<string> transformedList = new List<string>();
    foreach (string element in uniqueElements)
    {
      transformedList.Add(element.Replace(",", ""));
    }

    return transformedList;
  }
}

[Serializable]
public class BetData
{
  public double currentBet;
  public int currentLines;
  public int spins;
  public double selectedRisk;
}

[Serializable]
public class MessageData
{
  public BetData data;
  public string id;
  public string type;
  public Data payload;
}

[Serializable]
public class Data
{
  public double betIndex;
  public double spins;
  public double selectedRisk;
}

public class GameData
{
  public List<double> bets { get; set; }
  public List<string> risks { get; set; }
  public List<List<int>> resultSymbolMatrix { get; set; }
  public List<List<double>> multipliers { get; set; }
}

public class Message
{
  public GameData GameData { get; set; }
  public Player PlayerData { get; set; }
}

public class Player
{
  public double balance { get; set; }
  public double currentWining;
}

[Serializable]
public class BalanceSyncPayload
{
  public double balance;
}

public class Root
{
  public string id { get; set; }
  public Message message { get; set; }
  public string username { get; set; }

  public GameData gameData { get; set; }
  public Player player { get; set; }

  public bool success { get; set; }

  public List<List<string>> matrix { get; set; }
  public Payload payload { get; set; }
}

[Serializable]
public class Payload
{
  public double winAmount { get; set; }
  public List<int> winingColors { get; set; }
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace; //BackendChanges
}




