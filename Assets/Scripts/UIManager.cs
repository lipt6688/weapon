using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text waveText;
    public static UIManager instance;

    public Image[] hpImages;
    public Animator gameOverAnim;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            if(instance != this)
            {
                Destroy(gameObject);
            }
        }
        DontDestroyOnLoad(gameObject);
    }

    public void UpdateWaveText(int _num)
    {
        if (_num <= 9)
            waveText.text = "WAVE 0" + _num.ToString();
        else
            waveText.text = "WAVE " + _num.ToString();      
    }

    public void UpdateHp(int _hp)
    {
        for(int i = 0; i < hpImages.Length; i++)
        {
            if (i < _hp)
                hpImages[i].gameObject.SetActive(true);
            else
                hpImages[i].gameObject.SetActive(false);
        }
    }

    public void GameOverAnimation()
    {
        Time.timeScale = 0;
        gameOverAnim.SetTrigger("GameOver");
    }

}
