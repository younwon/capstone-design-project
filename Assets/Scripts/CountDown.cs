using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public PlayerDeath player;

    [Header("Best Record UI")]
    public TextMeshProUGUI bestValueText; // 000 M

    public float timeRemaining = 60f;
    private bool countdownStarted = false;
    private bool countdownEnded = false;
    void Start()
    {
        // 카운트다운 시작 전이라도 베스트 기록 표시
        UpdateBestRecordUI();
        timerText.color = Color.white;
    }
    void Update()
    {
        // 플레이어 입력 감지로 카운트다운 시작
        if (!countdownStarted && PlayerStartedMoving())
        {
            countdownStarted = true;
        }

        if (!countdownStarted || countdownEnded) return;

        if (countdownEnded) return;
        timeRemaining -= Time.deltaTime;
        timerText.text = timeRemaining.ToString("F2");

        if(timeRemaining < 10)
        {
            timerText.color = Color.red;
        }

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            countdownEnded = true;

            // 플레이어 죽이기 호출
            if (player != null)
                player.Die("Countdown Ended");
        }
    }
    private bool PlayerStartedMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        return Mathf.Abs(horizontal) > 0f || Mathf.Abs(vertical) > 0f || jumpPressed;
    }
    private void UpdateBestRecordUI()
    {
        if (ScoreSystem.I != null)
        {
            var s = ScoreSystem.I;

            if (bestValueText != null)
                bestValueText.text = "MAX " + $"{s.BestEverScore:0} M";
        }
    }
}