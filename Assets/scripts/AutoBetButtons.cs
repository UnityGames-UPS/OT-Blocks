using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoBetButtons : MonoBehaviour
{
    [SerializeField]
    int autobetCount;
    [SerializeField]
    public 
    Button button;
    [SerializeField]
    GameManager gameManager;

    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Sprite GraySprite;
    [SerializeField] private Sprite GreenSprite;


    private void Start()
    {
        button.onClick.AddListener(onButtonClick);
    }
    void OnEnable()
    {
        button.GetComponent<Image>().sprite = GraySprite;
    }

    void onButtonClick()
    {
        Debug.Log(autobetCount);
        audioManager.PlayWLAudio("toggle");
        gameManager.autoBetTotalCount = autobetCount;
        button.GetComponent<Image>().sprite = GreenSprite;
        for (int i = 0; i < gameManager.autoButtons.Count; i++)
        {
            gameManager.autoButtons[i].button.interactable = true;
            gameManager.autoButtons[i].button.GetComponent<Image>().sprite = GraySprite;
        }
        button.interactable = false;
        button.GetComponent<Image>().sprite = GreenSprite;

        gameManager.sendBet_Button.interactable = true;
    }
}
