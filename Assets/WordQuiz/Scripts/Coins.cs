using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Coins : MonoBehaviour
{
    public static Coins inst;

    public Text coinText;

    public static int coin = 0;

    private void Awake()
    {
        inst = this;
    }

    void Start()
    {

        coinText.text = coin.ToString();

    }

    public void AddCoin()
    {

        coin += 50;
        coinText.text = coin.ToString();

    }

    public void SpendCoin()
    {

        coin -= 100;
        coinText.text = coin.ToString();

    }

   
}
