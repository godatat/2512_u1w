using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI targetWordText;
    public TextMeshProUGUI typedText;
    public TextMeshProUGUI nameText;
    public bool autoStart = true;
    public AudioSource sePlayer;
    public AudioClip correctSE;
    public AudioClip incorrectSE;
    public AudioClip clearSE;
    public AudioClip moneySE;
    public OneMoreCall oneMoreCall;
    public float slideDuration = 0.3f;
    public Zunda zunda;
    public Metan metan;
    public ExplainDialog explainDialog;
    public Osake osake;
    public OsakePriceNum osakePriceNum;
    public AgeGauge ageGauge;
    public TextMeshProUGUI countdownText;
    public CanvasGroup _coreCanvasGroup;
    public CanvasGroup _countdownCanvasGroup;
    public CanvasGroup _questionCanvasGroup;
    public CanvasGroup _messageCanvasGroup;
    public CanvasGroup _gaugeCanvasGroup;

    private List<string> japaneseWords = new List<string>();
    private List<string> romajiWords = new List<string>();
    private List<int> wordLineNumbers = new List<int>();
    private ExplainData[] explainDataList;
    private string currentWord = "";
    private string currentRomaji = "";
    private int currentIndex = 0;
    private int currentLineNumber = -1;
    private bool isPlaying = false;
    private string typedBuffer = "";
    private float originalTextX = 0f;
    private float questionStartTime = 0f;
    private int totalTypedChars = 0;
    private int missCount = 0;
    private List<float> accuracyHistory = new List<float>();
    private int lastCompletedLineNumber = -1;
    private float excitementLevel = 20f; // 初期値20%
    public float missPenalty = 2f;
    private float totalInputTime = 0f;
    private float lastQuestionTime = 0f; // 最後の問題にかかった時間
    private float lastQuestionAccuracy = 0f; // 最後の問題の正答率
    private float lastQuestionTargetTime = 20f; // 最後の問題の目標秒数（デフォルト20秒）
    private int totalEarnedMoney = 0; // 累積したお金

    // アゲアゲ度（0-100の値）
    public float AgeAgeDo
    {
        get { return excitementLevel; }
        private set { excitementLevel = Mathf.Clamp(value, 0f, 100f); }
    }

    // 残りの問題数を取得
    public int RemainingQuestions
    {
        get
        {
            if (explainDataList == null || lastCompletedLineNumber < 0)
            {
                return explainDataList != null ? explainDataList.Length : 0;
            }
            int totalQuestions = explainDataList.Length;
            int currentQuestion = lastCompletedLineNumber + 1;
            return Mathf.Max(0, totalQuestions - currentQuestion);
        }
    }

    [Serializable]
    private class ExplainData
    {
        public string call;
        public string explain;
        public float targetTime; // 目標秒数
    }

    void Start()
    {
        LoadWords();
        LoadExplains();
        
        // 初期状態: カウントダウンを非表示、ゲームUIを非表示
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.alpha = 0f;
            _countdownCanvasGroup.gameObject.SetActive(true);
        }
        if (_coreCanvasGroup != null)
        {
            _coreCanvasGroup.alpha = 1f;
            _coreCanvasGroup.gameObject.SetActive(true);
        }
        
        if (autoStart)
        {
            StartCoroutine(OpeningConversationCoroutine());
        }
    }

    private void LoadExplains()
    {
        var ta = Resources.Load<TextAsset>("explains");
        if (ta != null)
        {
            string json = "{\"items\":" + ta.text + "}";
            explainDataList = JsonUtility.FromJson<ExplainDataArray>(json).items;
        }
    }

    [Serializable]
    private class ExplainDataArray
    {
        public ExplainData[] items;
    }

    private void LoadWords()
    {
        var ta = Resources.Load<TextAsset>("words");
        var text = ta.text;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int lineNumber = 0;
        foreach (var l in lines)
        {
            var line = l.Trim();
            var two = line.Split(new[] { ',' }, 2);
            var japSegment = two[0].Trim();
            var romSegment = two[1].Trim();
            var jItems = japSegment.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var rItems = romSegment.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var max = System.Math.Max(jItems.Length, rItems.Length);
            for (int i = 0; i < max; i++)
            {
                var j = jItems[i].Trim();
                var r = i < rItems.Length ? rItems[i].Trim() : "";
                japaneseWords.Add(j);
                romajiWords.Add(r);
                wordLineNumbers.Add(lineNumber);
            }
            lineNumber++;
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        var input = Input.inputString;
        foreach (var c in input)
        {
            if (c == '\b')
            {
                typedBuffer = typedBuffer.Substring(0, Math.Max(0, typedBuffer.Length - 1));
            }
            else if (c == '\n' || c == '\r')
            {
                if (string.Equals(NormalizeForCompare(typedBuffer), NormalizeForCompare(currentRomaji), StringComparison.OrdinalIgnoreCase)) OnCorrectWord();
            }
            else
            {
                totalTypedChars++;
                var newBuffer = typedBuffer + c;
                if (NormalizeForCompare(currentRomaji).StartsWith(NormalizeForCompare(newBuffer), StringComparison.OrdinalIgnoreCase))
                {
                    typedBuffer = newBuffer;
                }
                else
                {
                    missCount++;
                    Debug.Log($"missCount: {missCount}, excitementLevel: {excitementLevel}");
                    // ミス時にアゲアゲ度を下げる
                    excitementLevel = Mathf.Max(0f, excitementLevel - missPenalty);
                    if (ageGauge != null)
                    {
                        ageGauge.SetValue(excitementLevel / 100f);
                    }
                    
                    // アゲアゲ度が0になったらゲームオーバー
                    if (excitementLevel <= 0f)
                    {
                        GameOver();
                        return;
                    }
                    
                    PlayIncorrectSE();
                    if (zunda != null)
                    {
                        zunda.ChangeFacial(Zunda.Facial.Miss);
                        //zunda.Shake();
                    }
                    if (metan != null)
                    {
                        metan.ChangeFacial(Metan.Facial.Miss);
                    }
                    Invoke("ResetCharacters", 0.5f);
                }
            }
        }

        UpdateTypedUI();

        if (string.Equals(NormalizeForCompare(typedBuffer), NormalizeForCompare(currentRomaji), StringComparison.OrdinalIgnoreCase)) OnCorrectWord();
    }

    IEnumerator OpeningConversationCoroutine()
    {
        // ゲームUI要素を非表示（message、zunda、metan以外）
        if (targetWordText != null)
        {
            targetWordText.gameObject.SetActive(false);
        }
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.alpha = 0f;
            _questionCanvasGroup.gameObject.SetActive(false);
        }
        if (_gaugeCanvasGroup != null)
        {
            _gaugeCanvasGroup.alpha = 0f;
            _gaugeCanvasGroup.gameObject.SetActive(false);
        }
        if (osake != null)
        {
            osake.gameObject.SetActive(false);
        }
        if (osakePriceNum != null)
        {
            osakePriceNum.gameObject.SetActive(false);
        }
        
        // zundaとmetanを表示
        if (zunda != null)
        {
            zunda.gameObject.SetActive(true);
        }
        if (metan != null)
        {
            metan.gameObject.SetActive(true);
        }
        
        // メッセージUIを非表示
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.alpha = 0f;
        }
        
        // countdownTextで「とあるホストクラブにて」を表示
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.alpha = 0f;
            _countdownCanvasGroup.gameObject.SetActive(true);
        }
        if (countdownText != null)
        {
            countdownText.text = "とあるホストクラブにて";
            countdownText.gameObject.SetActive(true);
        }
        
        // フェードイン
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.DOFade(1f, 0.3f);
        }
        yield return new WaitForSeconds(0.3f);
        
        // 2秒表示
        yield return new WaitForSeconds(2f);
        
        // フェードアウト
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.DOFade(0f, 0.3f);
        }
        yield return new WaitForSeconds(0.3f);
        
        // countdownTextを非表示
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        /*
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.gameObject.SetActive(false);
        }
        */
        
        // メッセージUIを表示
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.alpha = 0f;
            _messageCanvasGroup.gameObject.SetActive(true);
            _messageCanvasGroup.DOFade(1f, 0.3f);
        }
        
        yield return new WaitForSeconds(0.3f);

        /*
        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "いえええい！！！俺の";
        }
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Happy);
        }
        yield return new WaitForSeconds(3f);
        */

        // めたん「ずんだもんまた来ちゃった」
        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        if (typedText != null)
        {
            typedText.text = "ずんだもんまた来ちゃった";
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }
        yield return new WaitForSeconds(2f);
        
        // ずんだもん「めたん！いつもご指名ありがとうなのだ！今日も全力でコールするから楽しんでいってほしいのだ」
        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "めたん！いつもご指名ありがとうなのだ！\n今日も全力でコールするから楽しんでいってほしいのだ";
        }
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Happy);
        }
        yield return new WaitForSeconds(3f);
        
        // めたん「ありがとう。上手にコールしてくれたらいいお酒を入れるわ」
        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        if (typedText != null)
        {
            typedText.text = "ありがとう。上手にコールしてくれたらいいお酒を入れるわ";
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }
        yield return new WaitForSeconds(3f);
        
        // ずんだもん「ほんとなのだ！？がんばるのだ！！！」
        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "ほんとなのだ！？がんばるのだ！！！";
        }
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Happy);
        }
        yield return new WaitForSeconds(3f);

        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        if (typedText != null)
        {
            typedText.text = "逆に手を抜いたら水しか頼まないから覚悟してね？";
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.OrderAngry);
        }
        yield return new WaitForSeconds(3f);

        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "も、もちろんそんなことは絶対ないのだ！";
        }
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Miss);
        }
        yield return new WaitForSeconds(3f);

        //表情を戻す
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Normal);
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }

        // メッセージUIを非表示
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.DOFade(0f, 0.3f);
        }
        yield return new WaitForSeconds(0.3f);
        
        // ゲームUI要素を再度表示
        if (targetWordText != null)
        {
            targetWordText.gameObject.SetActive(true);
        }
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.gameObject.SetActive(true);
        }
        if (_gaugeCanvasGroup != null)
        {
            _gaugeCanvasGroup.gameObject.SetActive(true);
        }
        if (osake != null)
        {
            osake.gameObject.SetActive(true);
        }
        if (osakePriceNum != null)
        {
            osakePriceNum.gameObject.SetActive(true);
        }
        
        // カウントダウンを開始
        StartCountdown();
    }
    
    void StartCountdown()
    {
        // カウントダウンUIをフェードイン
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.alpha = 0f;
            _countdownCanvasGroup.DOFade(1f, 0.3f);
        }
        
        // ゲームUIを非表示
        if (_coreCanvasGroup != null)
        {
            _coreCanvasGroup.alpha = 0f;
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }
            yield return new WaitForSeconds(1f);
        }
        
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            yield return new WaitForSeconds(0.5f);
        }
        
        // カウントダウンUIをフェードアウト
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.DOFade(0f, 0.3f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        StartGame();
    }

    void StartGame()
    {
        // ゲームUIをフェードイン
        if (_coreCanvasGroup != null)
        {
            _coreCanvasGroup.alpha = 0f;
            _coreCanvasGroup.DOFade(1f, 0.3f);
        }
        // gauge, message, questionを表示
        if (_gaugeCanvasGroup != null)
        {
            _gaugeCanvasGroup.alpha = 1f;
            _gaugeCanvasGroup.gameObject.SetActive(true);
        }
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.alpha = 1f;
            _messageCanvasGroup.gameObject.SetActive(true);
        }
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.alpha = 1f;
            _questionCanvasGroup.gameObject.SetActive(true);
        }
        currentIndex = 0;
        accuracyHistory.Clear();
        excitementLevel = 20f; // 初期値20%
        totalEarnedMoney = 0;
        totalInputTime = 0f;
        questionStartTime = Time.time;
        GameResultData.Reset();
        
        // アゲアゲ度ゲージを初期値20%に設定
        if (ageGauge != null)
        {
            ageGauge.SetValue(0.2f); // 20%
        }
        
        PickNextWord();
        isPlaying = true;
        typedBuffer = "";
        UpdateTypedUI();
    }

    void UpdateExcitementLevel()
    {
        if (accuracyHistory.Count == 0)
        {
            excitementLevel = 0f;
            if (ageGauge != null)
            {
                ageGauge.SetValue(0f);
            }
            return;
        }

        float sum = 0f;
        foreach (float acc in accuracyHistory)
        {
            sum += acc;
        }
        float averageAccuracy = sum / accuracyHistory.Count;
        float accuracyRate = averageAccuracy / 100f;

        int totalQuestions = explainDataList != null ? explainDataList.Length : 0;
        if (totalQuestions == 0)
        {
            totalQuestions = 1;
        }

        float maxValue = totalQuestions * accuracyRate;
        float currentValue = accuracyHistory.Count * accuracyRate;
        
        excitementLevel = maxValue > 0 ? (currentValue / maxValue * 100f) : 0f;
        excitementLevel = Mathf.Clamp(excitementLevel, 0f, 100f);
    }

    void GameOver()
    {
        isPlaying = false;
        
        // ゲームオーバー処理
        Debug.Log("Game Over: アゲアゲ度が0になりました");
        
        // ゲームを停止
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Miss);
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.OrderAngry);
        }
        
        // タイトルシーンに戻る
        StartCoroutine(GameOverCoroutine());
    }
    
    IEnumerator GameOverCoroutine()
    {
        // メッセージUIを表示
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.alpha = 0f;
            _messageCanvasGroup.gameObject.SetActive(true);
            _messageCanvasGroup.DOFade(1f, 0.3f);
        }
        
        // ゲームUIを非表示
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.DOFade(0f, 0.3f);
        }
        if (_gaugeCanvasGroup != null)
        {
            _gaugeCanvasGroup.DOFade(0f, 0.3f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // ゲームオーバーメッセージを表示
        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        if (typedText != null)
        {
            typedText.text = "ずんだもん。今日のコール全然気持ちよくないわ";
        }
        // めたんの表情を怒りに変える
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.OrderAngry);
        }
        yield return new WaitForSeconds(3f);

        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "そ、そんなぁ...";
        }
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Miss);
        }
        yield return new WaitForSeconds(3f);

        nameText.text = "";
        if (typedText != null)
        {
            typedText.text = "GameOver";
        }
        yield return new WaitForSeconds(3f);

        // リザルト画面に遷移
        SceneTransition.LoadScene("ResultScene");
    }

    void PickNextWord()
    {
        if (currentIndex >= japaneseWords.Count) currentIndex = 0;
        string newWord = japaneseWords[currentIndex];
        string newRomaji = romajiWords[currentIndex];
        currentIndex++;

        bool hasCurrentWord = !string.IsNullOrEmpty(currentWord);
        
        if (hasCurrentWord)
        {
            // 既存の文字をスライドアウト
            RectTransform rectTransform = targetWordText.rectTransform;
            Vector2 currentPos = rectTransform.anchoredPosition;
            rectTransform.DOAnchorPosX(1000f, slideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => { ShowNewWord(newWord, newRomaji); });
        }
        else
        {
            // 最初の文字はスライドインのみ
            ShowNewWord(newWord, newRomaji);
        }
    }

    void ShowNewWord(string word, string romaji)
    {
        currentWord = word;
        currentRomaji = romaji;

        RectTransform rectTransform = targetWordText.rectTransform;
        Vector2 currentPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = new Vector2(-1000f, currentPos.y);
        
        // Zundaがコールするときは「ずんだもん」を表示
        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        
        // Zundaの表情をNormalに戻す
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Normal);
        }
        
        // Metanの表情をNormalに戻す
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }
        
        targetWordText.text = word;
        typedText.text = EscapeRichText(romaji);
        typedBuffer = "";
        UpdateTypedUI();

        // スライドイン
        rectTransform.DOAnchorPosX(0, slideDuration)
            .SetEase(Ease.OutQuad);
    }

    void OnCorrectWord()
    {
        PlayCorrectSE();
        
        bool isLineComplete = false;
        int completedLineNumber = -1;
        
        if (currentIndex > 0)
        {
            int currentWordLine = wordLineNumbers[currentIndex - 1];
            
            // 次の単語がある場合
            if (currentIndex < wordLineNumbers.Count)
            {
                int nextWordLine = wordLineNumbers[currentIndex];
                
                if (currentWordLine != nextWordLine)
                {
                    PlayClearSE();
                    isLineComplete = true;
                    completedLineNumber = currentWordLine;
                }
            }
            // 最後の単語を正解した場合（次の単語がない）
            else if (currentIndex == wordLineNumbers.Count)
            {
                PlayClearSE();
                isLineComplete = true;
                completedLineNumber = currentWordLine;
            }
        }
        
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Happy);
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Happy);
        }
        
        // 正解時にアゲアゲ度を上げる（1単語正解ごとに）
        excitementLevel = Mathf.Min(100f, excitementLevel + 2f); // 1単語正解で5%増加
        if (ageGauge != null)
        {
            ageGauge.SetValue(excitementLevel / 100f);
        }
        
        Invoke("ResetCharacters", 0.5f);
        typedBuffer = "";
        UpdateTypedUI();
        
        if (isLineComplete && explainDialog != null)
        {
            isPlaying = false;
            string callText = "";
            string explainText = "";
            
            if (explainDataList != null && completedLineNumber >= 0 && completedLineNumber < explainDataList.Length)
            {
                callText = explainDataList[completedLineNumber].call;
                explainText = explainDataList[completedLineNumber].explain;
            }
            
            // 完了した行番号を保存
            lastCompletedLineNumber = completedLineNumber;
            
            float elapsedTime = Time.time - questionStartTime;
            totalInputTime += elapsedTime; // 各問題の入力時間を累積
            float accuracy = totalTypedChars > 0 ? ((float)(totalTypedChars - missCount) / totalTypedChars * 100f) : 100f;
            
            // 最後の問題の時間と正答率、目標秒数を保存
            lastQuestionTime = elapsedTime;
            lastQuestionAccuracy = accuracy;
            if (explainDataList != null && completedLineNumber >= 0 && completedLineNumber < explainDataList.Length)
            {
                lastQuestionTargetTime = explainDataList[completedLineNumber].targetTime;
            }

            Debug.Log($"totalTypedChars: {totalTypedChars}, missCount: {missCount}, accuracy: {accuracy}");

            /*
            // 1行完了時の追加ボーナス（正答率に応じて）
            float bonus = accuracy / 10f; // 正答率の10%をボーナスとして追加
            excitementLevel = Mathf.Min(100f, excitementLevel + bonus);
            if (ageGauge != null)
            {
                ageGauge.SetValue(excitementLevel / 100f);
            }
            */
            
            accuracyHistory.Add(accuracy);
            explainDialog.Show(callText, explainText, elapsedTime, totalTypedChars, accuracy, CloseExplainDialogAndContinue);
        }
        else
        {
            PickNextWord();
        }
    }

    void ResetCharacters()
    {
        if (zunda != null)
        {
            zunda.ChangeFacial(Zunda.Facial.Normal);
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }
    }

    void CloseExplainDialogAndContinue()
    {
        if (explainDialog != null)
        {
            explainDialog.Hide();
        }
        
        // Metanにしゃべらせる（最後の問題でもメタンの評価を聞く）
        StartCoroutine(MetanTalkCoroutine());
    }

    IEnumerator MetanTalkCoroutine()
    {
        // Metanがしゃべるときは「めたん」を表示
        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        
        // _messageCanvasGroupを非表示
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.DOFade(0f, 0.3f);
        }
        
        // typedTextに「ありがとう」を表示
        if (typedText != null)
        {
            typedText.text = "うーん。そうねぇ...";
        }
        
        // 数秒待つ
        yield return new WaitForSeconds(2f);
        
        // お酒を決定してメッセージに表示
        string drinkName = DetermineDrink(lastQuestionTime, lastQuestionAccuracy, lastQuestionTargetTime);
        if (typedText != null)
        {
            typedText.text = $"{drinkName}を頼もうかしら";
        }
        
        // お酒の種類に応じてMetanの表情を変更
        if (metan != null)
        {
            Metan.Facial orderFacial = GetOrderFacial(drinkName);
            metan.ChangeFacial(orderFacial);
        }
        
        // お酒を表示
        if (osake != null)
        {
            Osake.OsakeType osakeType = GetOsakeType(drinkName);
            osake.Show(osakeType);
        }
        
        // お酒をお金に換算して累積
        int drinkPrice = GetDrinkPrice(drinkName);
        totalEarnedMoney += drinkPrice;
        
        // 価格を表示
        if (osakePriceNum != null)
        {
            osakePriceNum.Show(drinkPrice);
        }
        
        // お金のSEを再生
        PlayMoneySE();
        
        // 数秒待つ
        yield return new WaitForSeconds(2f);
        
        // Zundaに感想を言わせる
        yield return StartCoroutine(ZundaCommentCoroutine(drinkName));
        
        // 最後の問題かどうかを判定
        bool isLastLine = false;
        if (explainDataList != null && lastCompletedLineNumber >= 0)
        {
            isLastLine = (lastCompletedLineNumber == explainDataList.Length - 1);
        }
        
        // 最後の問題だったらメタンと会話してからリザルトに遷移
        if (isLastLine)
        {
            yield return StartCoroutine(FinalMetanConversationCoroutine());
            ReturnToTitle();
            yield break;
        }
        
        // しゃべり終わったら_messageCanvasGroupを戻す
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.DOFade(1f, 0.3f);
        }
        
        // 設問の間に「one more call！」を表示
        ShowQuestionInterval();
    }
    
    IEnumerator FinalMetanConversationCoroutine()
    {
        // メタンがしゃべるときは「めたん」を表示
        if (nameText != null)
        {
            nameText.text = "めたん";
        }
        // questionCanvasGroupを非表示
        if (_questionCanvasGroup != null)
        {
            _questionCanvasGroup.DOFade(0f, 0.3f);
        }
        // ゲージを非表示
        if (_gaugeCanvasGroup != null)
        {
            _gaugeCanvasGroup.DOFade(0f, 0.3f);
        }
        // メッセージUIを表示
        if (_messageCanvasGroup != null)
        {
            _messageCanvasGroup.gameObject.SetActive(true);
            _messageCanvasGroup.DOFade(1f, 0.3f);
        }
        // お酒を非表示
        if (osake != null)
        {
            osake.Hide();
        }
        // 価格を非表示
        if (osakePriceNum != null)
        {
            osakePriceNum.Hide();
        }

        yield return new WaitForSeconds(0.3f);
        if (typedText != null)
        {
            typedText.text = "夜はまだまだこれからよ？";
        }
        if (metan != null)
        {
            metan.ChangeFacial(Metan.Facial.Normal);
        }
        yield return new WaitForSeconds(3f);

        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        if (typedText != null)
        {
            typedText.text = "もちろんなのだ！！！";
        }
        yield return new WaitForSeconds(3f);

        nameText.text = "";
        if (typedText != null)
        {
            typedText.text = "そうして2人の楽しい夜は続いていく...";
        }
        yield return new WaitForSeconds(3f);

        nameText.text = "";
        if (typedText != null)
        {
            typedText.text = "Fin ~";
        }
        yield return new WaitForSeconds(3f);
    }
    
    IEnumerator ZundaCommentCoroutine(string drinkName)
    {
        // Zundaがしゃべるときは「ずんだもん」を表示
        if (nameText != null)
        {
            nameText.text = "ずんだもん";
        }
        
        // お酒の種類に応じてZundaの表情を変更
        if (zunda != null)
        {
            Zunda.Facial commentFacial = GetZundaFacial(drinkName);
            zunda.ChangeFacial(commentFacial);
        }
        
        // Zundaの感想を取得
        string comment = GetZundaComment(drinkName);
        if (typedText != null)
        {
            typedText.text = comment;
        }
        
        // 数秒待つ
        yield return new WaitForSeconds(2f);
    }
    
    Zunda.Facial GetZundaFacial(string drinkName)
    {
        switch (drinkName)
        {
            case "ノンアル":
            case "安シャン":
                return Zunda.Facial.Miss;
            case "シャンディガフ":
                return Zunda.Facial.Normal;
            case "シャンパン":
            case "シャンパンタワー":
                return Zunda.Facial.Happy;
            default:
                return Zunda.Facial.Normal;
        }
    }
    
    Osake.OsakeType GetOsakeType(string drinkName)
    {
        switch (drinkName)
        {
            case "ノンアル":
                return Osake.OsakeType._ノンアル;
            case "安シャン":
                return Osake.OsakeType._安シャン;
            case "シャンディガフ":
                return Osake.OsakeType._シャンディガフ;
            case "シャンパン":
                return Osake.OsakeType._シャンパン;
            case "シャンパンタワー":
                return Osake.OsakeType._シャンパンタワー;
            default:
                return Osake.OsakeType._ノンアル;
        }
    }
    
    string GetZundaComment(string drinkName)
    {
        switch (drinkName)
        {
            case "ノンアル":
                return "だいぶ雰囲気やばいのだ..";
            case "安シャン":
                return "ちょっと雰囲気やばいのだ..";
            case "シャンディガフ":
                return "悪くないのだ！";
            case "シャンパン":
                return "やったのだ！シャンパンいただきましたぁ！！！";
            case "シャンパンタワー":
                return "シャンパンタワー入りま～～す！！！！ああ～～！極上なのだ！！！";
            default:
                return "いいね！";
        }
    }
    
    int GetDrinkPrice(string drinkName)
    {
        switch (drinkName)
        {
            case "ノンアル":
                return 0;
            case "安シャン":
                return 10000;
            case "シャンディガフ":
                return 30000;
            case "シャンパン":
                return 100000;
            case "シャンパンタワー":
                return 1000000;
            default:
                return 0;
        }
    }
    
    string DetermineDrink(float time, float accuracy, float targetTime)
    {
        // スコア計算: 時間が短いほど高スコア、正答率が高いほど高スコア
        // 時間の基準: 目標秒数を基準として、それより短いとボーナス、長いとペナルティ
        float timeScore = Mathf.Max(0f, 1f - (time / targetTime)); // 目標秒数で0、0秒で1
        
        // 正答率スコア: 70%を0点、100%を100点（1.0）にする
        float accuracyScore = 0f;
        if (accuracy >= 70f)
        {
            // 70%から100%の間を0から1.0に線形補間
            accuracyScore = (accuracy - 70f) / 30f; // (accuracy - 70) / (100 - 70)
        }
        // 70%未満の場合は0点のまま

        // 総合スコア（時間と正答率の平均、ただし正答率を重視）
        float totalScore = (timeScore * 0.5f + accuracyScore * 0.5f);
        
        // スコアに応じてお酒を決定（安い順）
        if (totalScore <= 0.3f)
        {
            return "ノンアル";
        }
        else if (totalScore <= 0.5f)
        {
            return "安シャン";
        }
        else if (totalScore <= 0.65f)
        {
            return "シャンディガフ";
        }
        else if (totalScore <= 0.8f)
        {
            return "シャンパン";
        }
        else
        {
            return "シャンパンタワー";
        }
    }
    
    Metan.Facial GetOrderFacial(string drinkName)
    {
        switch (drinkName)
        {
            case "ノンアル":
            case "安シャン":
                return Metan.Facial.OrderAngry;
            case "シャンディガフ":
                return Metan.Facial.OrderNormal;
            case "シャンパン":
            case "シャンパンタワー":
                return Metan.Facial.OrderHappy;
            default:
                return Metan.Facial.OrderNormal;
        }
    }

    void ShowQuestionInterval()
    {
        // お酒を非表示
        if (osake != null)
        {
            osake.Hide();
        }
        
        // カウントダウンUIをフェードイン
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.alpha = 0f;
            _countdownCanvasGroup.DOFade(1f, 0.3f);
        }
        
        // ゲームUIを非表示
        if (_coreCanvasGroup != null)
        {
            _coreCanvasGroup.alpha = 0f;
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            
            // 最後の問題かどうかを判定
            bool isLastQuestion = false;
            if (explainDataList != null && lastCompletedLineNumber >= 0)
            {
                int currentQuestion = lastCompletedLineNumber + 1; // 現在の問題番号（1ベース）
                int totalQuestions = explainDataList.Length;
                isLastQuestion = currentQuestion + 1 == 10;
                
                if (isLastQuestion)
                {
                    // 最後の問題の場合は赤文字で「last call」を表示
                    countdownText.text = "<color=#ff0000>final buster call！！！(10/10)</color>";
                    oneMoreCall.PlayRandomSE();
                }
                else
                {
                    // 残りの問題数を計算
                    int remainingQuestions = currentQuestion + 1;
                    string questionCountText = $" {remainingQuestions}/{totalQuestions}";
                    countdownText.text = $"one more call！ ({questionCountText})";
                    oneMoreCall.PlayRandomSE();
                }
            }
            else
            {
                countdownText.text = "one more call！";
                oneMoreCall.PlayRandomSE();
            }
        }
        
        StartCoroutine(QuestionIntervalCoroutine());
    }

    IEnumerator QuestionIntervalCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        
        // カウントダウンUIをフェードアウト
        if (_countdownCanvasGroup != null)
        {
            _countdownCanvasGroup.DOFade(0f, 0.3f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 7問目以降の場合、キャラクターの分身を表示
        if (RemainingQuestions <= 7)
        {
            if (metan != null)
            {
                metan.ShowClones();
            }
        }
        
        missCount = 0;
        totalTypedChars = 0;
        questionStartTime = Time.time;
        
        if (ageGauge != null)
        {
            ageGauge.SetValue(excitementLevel / 100f);
        }
        
        // ゲームUIをフェードイン
        if (_coreCanvasGroup != null)
        {
            _coreCanvasGroup.alpha = 0f;
            _coreCanvasGroup.DOFade(1f, 0.3f);
        }
        
        isPlaying = true;
        PickNextWord();
    }

    void ReturnToTitle()
    {
        // 正確性の平均を計算
        float averageAccuracy = 0f;
        if (accuracyHistory.Count > 0)
        {
            float sum = 0f;
            foreach (float acc in accuracyHistory)
            {
                sum += acc;
            }
            averageAccuracy = sum / accuracyHistory.Count;
        }
        
        // 総時間（各問題の入力時間の合計）と総スコア、正確性を記録
        float totalScore = excitementLevel; // アゲアゲ度をスコアとして使用
        GameResultData.SetResult(totalInputTime, totalScore, averageAccuracy, totalEarnedMoney);
        
        // 最後の問題の解説が終わったらリザルトシーンに遷移
        SceneTransition.LoadScene("ResultScene");
    }

    void PlayCorrectSE()
    {
        if (sePlayer != null && correctSE != null)
        {
            sePlayer.PlayOneShot(correctSE);
        }
    }

    void PlayIncorrectSE()
    {
        if (sePlayer != null && incorrectSE != null)
        {
            sePlayer.PlayOneShot(incorrectSE);
        }
    }

    void PlayClearSE()
    {
        if (sePlayer != null && clearSE != null)
        {
            sePlayer.PlayOneShot(clearSE);
        }
    }
    
    void PlayMoneySE()
    {
        if (sePlayer != null && moneySE != null)
        {
            sePlayer.PlayOneShot(moneySE);
        }
    }

    void UpdateTypedUI()
    {
        var expected = currentRomaji;
        var expectedNorm = NormalizeForCompare(expected);
        var typedNorm = NormalizeForCompare(typedBuffer);

        int correctCount = 0;
        for (int i = 0; i < typedNorm.Length && i < expectedNorm.Length; i++)
        {
            if (typedNorm[i] == expectedNorm[i]) correctCount++;
            else break;
        }

        // Map correctCount (count of matching non-space chars) back to the original expected string
        int charsToInclude = 0;
        int seen = 0;
        while (charsToInclude < expected.Length && seen < correctCount)
        {
            if (expected[charsToInclude] != ' ') seen++;
            charsToInclude++;
        }

        var correctPart = EscapeRichText(expected.Substring(0, charsToInclude));
        var restPart = EscapeRichText(expected.Substring(charsToInclude));

        if (correctCount > 0)
            typedText.text = $"<color=#ff4444>{correctPart}</color>{restPart}";
        else
            typedText.text = restPart;
    }

    private string EscapeRichText(string s)
    {
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private string NormalizeForCompare(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace(" ", "").ToLowerInvariant();
    }

    public void Restart()
    {
        StartGame();
    }
}
