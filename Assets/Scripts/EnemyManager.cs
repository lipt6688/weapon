using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Transform[] randTrans;
    public GameObject[] enemyWaves;

    private GameObject enemyWave;
    private bool isCompleted;

    private int randWaveNum;

    [HideInInspector] public int waveNum;

    private void Awake()
    {
        waveNum = 1;
    }

    private void Start()
    {
        int randPosNum = Random.Range(0, randTrans.Length);
        enemyWave = Instantiate(enemyWaves[0], randTrans[randPosNum].position, Quaternion.identity);
    }

    private void Update()
    {
        if(enemyWave.transform.childCount == 0)
        {
            isCompleted = true;
            randWaveNum = Random.Range(0, enemyWaves.Length);
        }

        if(isCompleted)
        {
            int randNum = Random.Range(0, randTrans.Length);

            waveNum++;
            UIManager.instance.UpdateWaveText(waveNum);

            enemyWave = Instantiate(enemyWaves[randWaveNum], randTrans[randNum].position, Quaternion.identity);
            isCompleted = false;
        }
    }
}
