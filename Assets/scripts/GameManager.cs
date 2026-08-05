using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Best.SocketIO;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    int marblesToSpawn = 2;
    [Header("Buttons")]
    [SerializeField]
    private Button TBetPlus_Button;
    [SerializeField]
    private Button TBetMinus_Button;
    [SerializeField]
    public Button sendBet_Button;
    [SerializeField]
    private Button highRisk_Button;
    [SerializeField]
    private Button mediumRisk_Button;
    [SerializeField]
    private Button lowRisk_Button;
    [SerializeField]
    private Button manualBetButton;
    [SerializeField]
    private Button autoBet_Button;
    [SerializeField]
    private Button autoBet_Stop;

    [SerializeField] private Button Turbo_Button;

    private double currentTotalBet = 0;
    private double currentBalance;
    internal int BetCounter;
    internal int BallCounter;
    [SerializeField]
    List<TextMeshProUGUI> riskContainers = new List<TextMeshProUGUI>();

    [SerializeField] private List<GameObject> RiskContainersWinObject = new List<GameObject>();
    private string CurrentRiskType = "";
    private string LastSelectedRiskType = "";
    [SerializeField]
    internal List<AutoBetButtons> autoButtons = new List<AutoBetButtons>();
    [SerializeField]
    Color highlightButtonCol;
    [SerializeField]
    Color hideButtonCol;
    [SerializeField]
    List<Vector3> cubeRotaionPoints = new List<Vector3>();
    [SerializeField]
    List<cubeRotation> cubes = new List<cubeRotation>();

    [SerializeField]
    List<cubeRotation> Maincubes = new List<cubeRotation>();
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text balance_text;
    [SerializeField]
    private TMP_Text win_text;
    [SerializeField]
    private TMP_Text autoBetCount_text;
    [SerializeField]
    TMP_InputField autoBetField;
    internal int autoBetTotalCount, autoBetCurrentCount;
    [SerializeField]
    internal float autoBetFrequency = 0.5f;
    bool isAutoBetPlaying;
    bool autoBetInstanceDone;
    internal int totalMarbleInAction;
    Coroutine autoBetCoroutine;
    [SerializeField]
    AudioManager audioManager;
    [SerializeField]
    SocketIOManager socketIoManager;
    [SerializeField]
    internal UiManager uiManager;
    internal gameType _gameType = gameType.TWELVE;
    int currentLines = 20;
    int riskfactor = 0;
    bool isAutoBet, isAutobetInstanceDone, autobetRunning;
    [SerializeField] GameObject autoBetpanel;
    [SerializeField] GameObject touchDisable;

    public bool IsTurboOn = false;

    [SerializeField]
    Color highlightTextsCol;
    [SerializeField]
    Color hideTextsCol;

    [SerializeField]
    private TMP_Text Manual_text;
    [SerializeField]
    private TMP_Text Auto_text;
    [SerializeField]
    private TMP_Text Low_text;
    [SerializeField]
    private TMP_Text Medium_text;
    [SerializeField]
    private TMP_Text High_text;

    [SerializeField] private Image BetFilled_image;



    public enum marbleType
    {
        RED,
        YELLOW,
        GREEN
    }

    public enum gameType
    {
        TWELVE,
        SIXTEEN
    }

    [SerializeField] private List<Sprite> TurboSprites = new List<Sprite>();
    private List<int> cubeSides = new List<int>();

    private void Start()
    {
        IsTurboOn = false;
        if (TBetPlus_Button) TBetPlus_Button.onClick.RemoveAllListeners();
        if (TBetPlus_Button) TBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); audioManager.PlayWLAudio("plusminus"); });

        if (TBetMinus_Button) TBetMinus_Button.onClick.RemoveAllListeners();
        if (TBetMinus_Button) TBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); audioManager.PlayWLAudio("plusminus"); });

        if (sendBet_Button) sendBet_Button.onClick.RemoveAllListeners();
        if (sendBet_Button) sendBet_Button.onClick.AddListener(delegate { sendBetData(); audioManager.PlayBetButtonAudio(); });

        if (autoBet_Button) autoBet_Button.onClick.RemoveAllListeners();
        if (autoBet_Button) autoBet_Button.onClick.AddListener(delegate { gameMode(true); audioManager.PlayWLAudio("toggle"); });

        if (manualBetButton) manualBetButton.onClick.RemoveAllListeners();
        if (manualBetButton) manualBetButton.onClick.AddListener(delegate { gameMode(false); audioManager.PlayWLAudio("toggle"); });

        if (autoBet_Stop) autoBet_Stop.onClick.RemoveAllListeners();
        if (autoBet_Stop) autoBet_Stop.onClick.AddListener(delegate { stopAutoBet(); audioManager.PlayBetButtonAudio(); });

        if (lowRisk_Button) lowRisk_Button.onClick.RemoveAllListeners();
        if (lowRisk_Button) lowRisk_Button.onClick.AddListener(delegate { changeRiskFactor("low"); audioManager.PlayWLAudio("toggle"); LastSelectedRiskType = "low"; });

        if (mediumRisk_Button) mediumRisk_Button.onClick.RemoveAllListeners();
        if (mediumRisk_Button) mediumRisk_Button.onClick.AddListener(delegate { changeRiskFactor("medium"); audioManager.PlayWLAudio("toggle"); LastSelectedRiskType = "medium"; });

        if (highRisk_Button) highRisk_Button.onClick.RemoveAllListeners();
        if (highRisk_Button) highRisk_Button.onClick.AddListener(delegate { changeRiskFactor("high"); audioManager.PlayWLAudio("toggle"); LastSelectedRiskType = "high"; });

        if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if (Turbo_Button) Turbo_Button.onClick.AddListener(delegate { TurboToggle(); audioManager.PlayButtonAudio(); });
        Manual_text.color = highlightTextsCol;
        ResetMultiplierWinObject();
        LastSelectedRiskType = "low";


    }




    internal void setInitialUI()
    {
        currentBalance = socketIoManager.playerdata.balance;
        balance_text.text = socketIoManager.playerdata.balance.ToString("f2");
        currentTotalBet = socketIoManager.initialData.bets[0];
        if (TotalBet_text) TotalBet_text.text = (socketIoManager.initialData.bets[BetCounter]).ToString("f2");
        currentTotalBet = socketIoManager.initialData.bets[BetCounter];
        BetFilled_image.fillAmount = (float)(BetCounter + 1) / (float)socketIoManager.initialData.bets.Count;
        changeRiskFactor("low");
        CurrentRiskType = "low";


    }





    private void sendBetData()
    {
        toggleUI(false);
        if (!isAutoBet)
        {
            StartCoroutine(accumulateResult());

        }
        else
        {
            isAutoBetPlaying = true;
            autoBetCurrentCount = autoBetTotalCount;
            autoBetCount_text.text = "Stop Auto Bet " + (autoBetTotalCount).ToString();
            autoBetCoroutine = StartCoroutine(startAutoBet());
        }

    }


    IEnumerator accumulateResult()
    {
        ResetMultiplierWinObject();
        currentBalance = socketIoManager.playerdata.balance;
        if (currentBalance < currentTotalBet)
        {
            lowBalance();
            yield break;
        }
        win_text.text = "";
        ResetAnimCubeObject();
        cubeSides.Clear();
        ToggleButtons(false);
        balance_text.text = socketIoManager.playerdata.balance.ToString("f2");
        // else
        // {
        touchDisable.SetActive(true);
        updateBalance(currentTotalBet, false);
        //socketIoManager.AccumulateResult(socketIoManager.initialData.Bets[BetCounter], rowDropDown.value, riskDropDown.value);
        socketIoManager.AccumulateResult(BetCounter, currentLines, 1, riskfactor);
        yield return new WaitUntil(() => socketIoManager.isResultdone);
        cubeSides = socketIoManager.ConvertStringsToIntegers(socketIoManager.resultData.matrix);
        float WaitTimer = 2.5f;
        if (IsTurboOn) WaitTimer = 1f;

        for (int i = 0; i < Maincubes.Count; i++)
        {
            Maincubes[i].StartRotation(cubeRotaionPoints[cubeSides[i]], WaitTimer);
        }

        yield return new WaitForSeconds(1f);
        isAutobetInstanceDone = true;
        if(IsTurboOn)  ToggleButtons(true);
        Debug.Log($"########### auto bet playing " + isAutoBetPlaying);
        //CheckWin();
        yield return StartCoroutine(CheckWin());
        win_text.text = socketIoManager.resultData.payload.winAmount.ToString("f2");
        balance_text.text = socketIoManager.playerdata.balance.ToString("f2");
        if (!isAutoBetPlaying)
        {
            touchDisable.SetActive(false);
        }
        ToggleButtons(true);
        if (isAutoBetPlaying) yield return new WaitForSeconds(2f);


        // }

    }
    void ResetAnimCubeObject()
    {
        for (int i = 0; i < Maincubes.Count; i++)
        {
            Maincubes[i].ResetAnimObject();
        }
    }



    void lowBalance()
    {
        toggleUI(true);
        if (isAutoBetPlaying)
        {
            StopCoroutine(autoBetCoroutine);
            autobetRunning = false;
            autoBet_Stop.gameObject.SetActive(false);
            autoBetCount_text.text = "Stop Auto Bet ";
        }
        uiManager.LowBalPopup();
    }

    internal void updateBalance(double amount, bool add)
    {
        if (add)
        {

            currentBalance += amount;
            balance_text.text = currentBalance.ToString("f2");
        }
        else
        {
            currentBalance -= amount;
            balance_text.text = currentBalance.ToString("f2");

        }
    }

    internal void checkForFallingMarbles()
    {
        if (totalMarbleInAction == 0)
        {
            toggleUI(true);
        }
    }


    void gameMode(bool autoBet)
    {
        isAutoBet = autoBet;
        if (isAutoBet)
        {
            autoBetpanel.SetActive(true);

            sendBet_Button.interactable = false;
            manualBetButton.image.color = hideButtonCol;
            autoBet_Button.image.color = highlightButtonCol;
            Manual_text.color = hideTextsCol;
            Auto_text.color = highlightTextsCol;
            ResetAutospinsbuttons();
        }
        else
        {
            autoBetpanel.SetActive(false);
            sendBet_Button.interactable = true;
            //  sendBet_Button.gameObject.SetActive(true);
            manualBetButton.image.color = highlightButtonCol;
            autoBet_Button.image.color = hideButtonCol;
            Auto_text.color = hideTextsCol;
            Manual_text.color = highlightTextsCol;
        }
    }


    private void ChangeBet(bool IncDec)
    {
        Debug.Log("changeBetRan");
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter >= socketIoManager.initialData.bets.Count)
            {
                BetCounter = 0;
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = socketIoManager.initialData.bets.Count - 1;
            }
        }
        if (TotalBet_text) TotalBet_text.text = (socketIoManager.initialData.bets[BetCounter]).ToString("f2");
        Debug.Log($"bet counter : " + BetCounter + "  count : " + socketIoManager.initialData.bets.Count);
        // BetFilled_image.fillAmount = BetCounter / socketIoManager.initialData.bets.Count;
        BetFilled_image.fillAmount = (float)(BetCounter + 1) / (float)socketIoManager.initialData.bets.Count;

        currentTotalBet = socketIoManager.initialData.bets[BetCounter];
        changeRiskFactor(LastSelectedRiskType);

    }




    private void changeRiskFactor(string risk)
    {
        CurrentRiskType = risk;
        ResetMultiplierWinObject();
        Debug.Log(risk);
        for (int i = 0; i < riskContainers.Count; i++)
        {
            riskContainers[i].transform.parent.gameObject.SetActive(false);
        }
        switch (risk)
        {
            case "low":
                {
                    riskfactor = 0;
                    int containerMultiplier = 3;
                    for (int i = 0; i < socketIoManager.initialData.multipliers[0].Count; i++)
                    {
                        riskContainers[i].text = containerMultiplier + "\n" + (socketIoManager.initialData.multipliers[0][i]*socketIoManager.initialData.bets[BetCounter]).ToString() ;
                        riskContainers[i].transform.parent.gameObject.SetActive(true);
                        containerMultiplier++;
                    }
                    mediumRisk_Button.image.color = hideButtonCol;
                    highRisk_Button.image.color = hideButtonCol;
                    lowRisk_Button.image.color = highlightButtonCol;
                    Low_text.color = highlightTextsCol;
                    Medium_text.color = hideTextsCol;
                    High_text.color = hideTextsCol;

                    break;

                }
            case "medium":
                {
                    riskfactor = 1;
                    int containerMultiplier = 4;
                    for (int i = 0; i < socketIoManager.initialData.multipliers[1].Count; i++)
                    {
                        riskContainers[i].text = containerMultiplier + "\n" + (socketIoManager.initialData.multipliers[1][i]*socketIoManager.initialData.bets[BetCounter]).ToString() ;
                        riskContainers[i].transform.parent.gameObject.SetActive(true);
                        containerMultiplier++;
                    }
                    mediumRisk_Button.image.color = highlightButtonCol;
                    highRisk_Button.image.color = hideButtonCol;
                    lowRisk_Button.image.color = hideButtonCol;
                    Low_text.color = hideTextsCol;
                    Medium_text.color = highlightTextsCol;
                    High_text.color = hideTextsCol;
                    break;
                }
            case "high":
                {
                    riskfactor = 2;
                    int containerMultiplier = 5;
                    for (int i = 0; i < socketIoManager.initialData.multipliers[2].Count; i++)
                    {
                        riskContainers[i].text = containerMultiplier + "\n" + (socketIoManager.initialData.multipliers[2][i]*socketIoManager.initialData.bets[BetCounter]).ToString() ;
                        riskContainers[i].transform.parent.gameObject.SetActive(true);
                        containerMultiplier++;
                    }
                    mediumRisk_Button.image.color = hideButtonCol;
                    highRisk_Button.image.color = highlightButtonCol;
                    lowRisk_Button.image.color = hideButtonCol;
                    Low_text.color = hideTextsCol;
                    Medium_text.color = hideTextsCol;
                    High_text.color = highlightTextsCol;
                    break;
                }
        }
    }



    IEnumerator startAutoBet()
    {
        currentBalance = socketIoManager.playerdata.balance;
        if (currentBalance < currentTotalBet)
        {
            lowBalance();
            yield break;
        }
        autoBet_Stop.gameObject.SetActive(true);
        sendBet_Button.gameObject.SetActive(false);
        Debug.Log(autoBetCurrentCount);
        autobetRunning = true;
        //  autoBetCount_text.text = "Stop Auto Bet " + (autoBetTotalCount ).ToString();
        for (int i = 0; i < autoBetTotalCount; i++)
        {
            autoBetInstanceDone = false;
            StartCoroutine(accumulateResult());
            yield return new WaitUntil(() => isAutobetInstanceDone);
            autoBetCount_text.text = "Stop Auto Bet " + (autoBetTotalCount - i - 1).ToString();

            yield return new WaitForSeconds(2f);



        }
        autobetRunning = false;
        touchDisable.SetActive(false);
        isAutoBetPlaying = false;
        autoBet_Stop.gameObject.SetActive(false);
        sendBet_Button.gameObject.SetActive(true);
        sendBet_Button.interactable = true;




    }

    void stopAutoBet()
    {


        autoBet_Stop.gameObject.SetActive(false);
        sendBet_Button.gameObject.SetActive(true);
        sendBet_Button.interactable = true;
        StopCoroutine(autoBetCoroutine);
        isAutoBetPlaying = false;
        touchDisable.SetActive(false);

    }


    private void toggleUI(bool toggle)
    {

        TBetPlus_Button.interactable = toggle;
        TBetMinus_Button.interactable = toggle;


    }

    private void TurboToggle()
    {
        if (IsTurboOn)
        {
            IsTurboOn = false;
            Turbo_Button.image.sprite = TurboSprites[0];
        }
        else
        {
            IsTurboOn = true;
            Turbo_Button.image.sprite = TurboSprites[1];
        }
    }

    private void ToggleButtons(bool toggle)
    {
        TBetPlus_Button.interactable = toggle;
        TBetMinus_Button.interactable = toggle;
        sendBet_Button.interactable = toggle;
        lowRisk_Button.interactable = toggle;
        mediumRisk_Button.interactable = toggle;
        highRisk_Button.interactable = toggle;
        manualBetButton.interactable = toggle;
        autoBet_Button.interactable = toggle;
        autoBet_Stop.interactable = toggle;

        // TBetPlus_Button.interactable=toggle;

    }

    // private void CheckWin()
    // {
    //     if (socketIoManager.resultData.payload.winingColors.Count > 0)
    //     {
    //         Debug.Log($"Win animation enable -1");

    //         for (int i = 0; i < cubeSides.Count; i++)
    //         {
    //             if (socketIoManager.resultData.payload.winingColors.Contains(cubeSides[i]))
    //             {
    //                 Debug.Log($"Win animation enable 0 " + cubeSides[i]);

    //                 Dummycubes[i].EnableAnimObjects();
    //             }
    //         }
    //     }


    // }

    private IEnumerator CheckWin()
    {
        if (socketIoManager.resultData.payload.winingColors.Count > 0)
        {
            //audioManager.PlayWLAudio("win");
            Debug.Log($"Win animation enable -1");

            for (int i = 0; i < cubeSides.Count; i++)
            {
                if (socketIoManager.resultData.payload.winingColors.Contains(cubeSides[i]))
                {
                    Debug.Log($"Win animation enable 0 " + cubeSides[i]);
                    Maincubes[i].EnableAnimObjects();
                }
            }
            win_text.text = socketIoManager.resultData.payload.winAmount.ToString("f2");
            audioManager.PlayWLAudio("win");
            CheckForWinningMultiplier();
            yield return new WaitForSeconds(0.5f);

            yield break; // exits here immediately

        }
        else
        {
            audioManager.PlayWLAudio("lose");
            yield break;
        }
    }

    //     public void CheckForWinningMultiplier()
    // {
    //     foreach (var winningColor in socketIoManager.resultData.payload.winingColors)
    //     {
    //         int multiplierCount = cubeSides.Count(side => side == winningColor);
    //         EnableMultiplierWin(multiplierCount);
    //     }
    // }

    // void EnableMultiplierWin(int winIndex)
    // {
    //     int riskIndex = CurrentRiskType switch
    //     {
    //         "low"    => 0,
    //         "medium" => 1,
    //         "high"   => 2,
    //         _        => -1
    //     };

    //     if (riskIndex == -1) return;

    //     var multipliers = socketIoManager.initialData.multipliers[riskIndex];
    //     for (int i = 0; i < multipliers.Count; i++)
    //     {
    //         if (multipliers[i] == winIndex)
    //         {
    //             RiskContainersWinObject[i].gameObject.SetActive(true);
    //             break;
    //         }
    //     }
    // }
    public void CheckForWinningMultiplier()
    {
        if (socketIoManager.resultData.payload.winingColors.Count > 0)
        {
            for (int i = 0; i < socketIoManager.resultData.payload.winingColors.Count; i++)
            {
                int MultiplierCount = 0;
                for (int j = 0; j < cubeSides.Count; j++)
                {
                    if (socketIoManager.resultData.payload.winingColors[i] == cubeSides[j])
                    {
                        MultiplierCount++;
                    }
                }
                EnableMultiplierWin(MultiplierCount);
            }
        }
    }

    void EnableMultiplierWin(int winIndex)
    {
        if (CurrentRiskType == "low")
        {
            int containerMultiplier = 3;
            int multiplierindex = winIndex - containerMultiplier;
            RiskContainersWinObject[multiplierindex].gameObject.SetActive(true);
        }

        if (CurrentRiskType == "medium")
        {
            int containerMultiplier = 4;
            int multiplierindex = winIndex - containerMultiplier;
            RiskContainersWinObject[multiplierindex].gameObject.SetActive(true);
        }

        if (CurrentRiskType == "high")
        {
            int containerMultiplier = 5;
            int multiplierindex = winIndex - containerMultiplier;
            RiskContainersWinObject[multiplierindex].gameObject.SetActive(true);
        }

    }

    void ResetMultiplierWinObject()
    {
        for (int i = 0; i < RiskContainersWinObject.Count; i++)
        {
            RiskContainersWinObject[i].SetActive(false);
        }

    }


    void ResetAutospinsbuttons()
    {
        for (int i = 0; i < autoButtons.Count; i++)
        {
            autoButtons[i].button.interactable = true;
          //  autoButtons[i].button.GetComponent<Image>().sprite = GraySprite;
        }
    }





}
