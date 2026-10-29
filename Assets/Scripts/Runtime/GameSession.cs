using UnityEngine;
using TMPro;

public sealed class GameSession : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private CoinPickup[] coins;
    [SerializeField] private AudioSource collectAudio;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text hud;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private TMP_Text bestText;

    private const string BestKey = "PlatformCoinLab.Best";

    private AudioClip generatedCollectClip;
    private CoinRound round;
    private Vector3 startPosition;
    private float roundSeconds = 30f;
    private bool playing;
    private bool showingResult;
    private bool busy;
    private bool paused;
    private int best;

    public int Score => round.Score;
    public bool IsPlaying => playing;
    public bool CanMove => playing && !paused;
    public bool IsLoading => busy;
    public bool IsReady => coins != null && coins.Length > 0 && !busy;

    private void Awake()
    {
        // 저장한 최고 점수가 없으면 기본값 0으로 시작합니다.
        best = PlayerPrefs.GetInt(BestKey, 0);

        round = new CoinRound(roundSeconds);
        startPosition = player.position;

        // 외부 AudioClip이 연결되어 있지 않을 때만 짧은 수집음을 직접 만듭니다.
        if (collectAudio != null && collectAudio.clip == null)
        {
            float[] samples = new float[2205];

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] =
                    Mathf.Sin(2f * Mathf.PI * 880f * i / 22050f)
                    * (1f - (float)i / samples.Length)
                    * 0.25f;
            }

            generatedCollectClip =
                AudioClip.Create("CollectTone", samples.Length, 1, 22050, false);

            generatedCollectClip.SetData(samples, 0);
            collectAudio.clip = generatedCollectClip;
        }
    }

    private void OnDestroy()
    {
        if (generatedCollectClip != null)
        {
            Destroy(generatedCollectClip);
        }
    }

    private void Start()
    {
        SetStage(coins);
    }

    private void Update()
    {
        if (playing && !paused)
        {
            round.Tick(Time.deltaTime);

            if (round.IsFinished)
            {
                FinishRound();
            }
        }

        RefreshUi();
    }

    public void BeginRound()
    {
        if (!IsReady || playing)
        {
            return;
        }

        round.Reset(roundSeconds);
        player.position = startPosition;

        foreach (CoinPickup coin in coins)
        {
            coin.gameObject.SetActive(true);
        }

        playing = true;
        showingResult = false;
        paused = false;

        RefreshUi();
    }

    public bool TryCollect(int id)
    {
        if (!CanMove || !round.TryCollect(id))
        {
            return false;
        }

        if (collectAudio != null)
        {
            collectAudio.Play();
        }

        return true;
    }

    public void ReturnToMenu()
    {
        playing = false;
        showingResult = false;
        paused = false;

        RefreshUi();
    }

    public void SetStage(CoinPickup[] stageCoins)
    {
        coins = stageCoins ?? new CoinPickup[0];

        foreach (CoinPickup coin in coins)
        {
            coin.Initialize(this, player);
        }

        RefreshUi();
    }

    public void SetBusy(bool value)
    {
        busy = value;
        RefreshUi();
    }

    public void SetStatus(string message)
    {
        statusText.text = message;
    }

    public void SetRoundSeconds(int seconds)
    {
        if (playing)
        {
            return;
        }

        roundSeconds = Mathf.Clamp(seconds, 10, 120);
    }

    private void FinishRound()
    {
        playing = false;
        showingResult = true;
        SaveBest();
    }

    public void SaveBest()
    {
        // 기존 최고 점수와 이번 점수 중 큰 값만 남겨 기록이 낮아지지 않게 합니다.
        best = Mathf.Max(best, Score);
        PlayerPrefs.SetInt(BestKey, best);

        // 메모리에서 바꾼 저장 값을 실제 저장소에 반영합니다.
        PlayerPrefs.Save();

        RefreshUi();
    }

    private void OnApplicationPause(bool value)
    {
        // 중단이면 true, 복귀이면 false입니다. 같은 값을 이동·시간 처리의 잠금에도 사용합니다.
        paused = value;

        if (value)
        {
            SaveBest();
        }
    }

    private void RefreshUi()
    {
        menuPanel.SetActive(!playing && !showingResult);
        hudPanel.SetActive(playing);
        resultPanel.SetActive(showingResult);
        startButton.interactable = IsReady;

        hud.text = $"Score: {Score}  Time: {Mathf.CeilToInt(round.Remaining)}";
        resultText.text = $"Score: {Score}";

        if (bestText != null)
        {
            bestText.text = $"Best: {best}";
        }
    }
}
