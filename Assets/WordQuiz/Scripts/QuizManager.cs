using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    public static QuizManager instance;

    [SerializeField]
    private QuizDataScriptable questionData;
    [SerializeField]
    private Image questionImage;
    [SerializeField]
    private Image questionImage01;
    [SerializeField]
    private Image questionImage02;
    [SerializeField]
    private Image questionImage03;
    [SerializeField]
    private WordData[] answerWordArray;
    [SerializeField]
    private WordData[] optionWordArray;
    [SerializeField]
    private GameObject endGameCanvas;
    [SerializeField]
    private Text levelText;
    [SerializeField]
    private Text coinsText; 
    [SerializeField]
    private GameObject notEnoughCoinsUI;
    [SerializeField]
    private GameObject confirmationUI;

    private char[] charArray = new char[12];
    private int currentAnswerIndex = 0;
    private bool correctAnswer = true;
    private List<int> selectedWordIndex;
    private int currentQuestionIndex = -1; // Start with an invalid index
    private GameStatus gameStatus = GameStatus.Playing;
    private string answerWord;
    private List<int> askedQuestionIndices;
    
    

    // Add a list to track which letters have been revealed
    private HashSet<int> revealedLetterIndices;

    private int currentLevel = 1;
    private int coins = 0; // Variable to keep track of coins

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        selectedWordIndex = new List<int>();
        askedQuestionIndices = new List<int>();
        revealedLetterIndices = new HashSet<int>();
    }

    private void Start()
    {
        if (AudioManager.isMusicPlaying){
            FindObjectOfType<AudioManager>().Play("MusicBG");
        }
        LoadGameData();
        SetQuestion();
        UpdateUI();
    }

    private void SetQuestion()
    {
        if (askedQuestionIndices.Count >= questionData.questions.Count)
        {
            Debug.Log("No more questions available. Quiz is finished.");
            endGameCanvas.SetActive(true);
            gameStatus = GameStatus.Next;
            return;
        }

        currentAnswerIndex = 0;
        selectedWordIndex.Clear();
        revealedLetterIndices.Clear(); // Reset revealed letters for new question

        // If the current question index is valid and has not been answered
        if (currentQuestionIndex < 0 || askedQuestionIndices.Contains(currentQuestionIndex))
        {
            do
            {
                currentQuestionIndex = Random.Range(0, questionData.questions.Count);
            }
            while (askedQuestionIndices.Contains(currentQuestionIndex));
        }

        askedQuestionIndices.Add(currentQuestionIndex);

        QuestionData currentQuestion = questionData.questions[currentQuestionIndex];
        questionImage.sprite = currentQuestion.questionImage;
        questionImage01.sprite = currentQuestion.questionImage01;
        questionImage02.sprite = currentQuestion.questionImage02;
        questionImage03.sprite = currentQuestion.questionImage03;
        answerWord = currentQuestion.answer;

        ResetQuestion();

        for (int i = 0; i < answerWord.Length; i++)
        {
            charArray[i] = char.ToUpper(answerWord[i]);
        }

        for (int i = answerWord.Length; i < optionWordArray.Length; i++)
        {
            charArray[i] = (char)UnityEngine.Random.Range(65, 91);
        }

        charArray = ShuffleList.ShuffleListItems<char>(charArray.ToList()).ToArray();
        for (int i = 0; i < optionWordArray.Length; i++)
        {
            optionWordArray[i].SetChar(charArray[i]);
        }

        gameStatus = GameStatus.Playing;

        // Save the current game state
        SaveGameData();
        Debug.Log("Current Question Index: " + currentQuestionIndex);
    }

    public void SelectedOption(WordData wordData)
    {
        if (gameStatus == GameStatus.Next || currentAnswerIndex >= answerWord.Length)
        {
            return;
        }

        // Find the next available slot that isn't revealed by a hint
        while (currentAnswerIndex < answerWord.Length && (answerWordArray[currentAnswerIndex].charValue != '_' || revealedLetterIndices.Contains(currentAnswerIndex)))
        {
            currentAnswerIndex++;
        }

        // If no available slot is found, return
        if (currentAnswerIndex >= answerWord.Length)
        {
            return;
        }

        selectedWordIndex.Add(wordData.transform.GetSiblingIndex());
        answerWordArray[currentAnswerIndex].SetChar(wordData.charValue);
        wordData.gameObject.SetActive(false);
        currentAnswerIndex++;

        // Check if the answer is complete
        if (currentAnswerIndex >= answerWord.Length)
        {
            correctAnswer = true;

            for (int i = 0; i < answerWord.Length; i++)
            {
                if (char.ToUpper(answerWord[i]) != char.ToUpper(answerWordArray[i].charValue))
                {
                    correctAnswer = false;
                    break;
                }
            }

            if (correctAnswer)
            {
                FindObjectOfType<AudioManager>().Play("LevelComplete");
                Debug.Log("Correct");
                AddCoins(20); // Add coins for a correct answer

                // Update the level after a correct answer
                currentLevel++;
                UpdateUI();
                SaveGameData();
                SaveAnsweredQuestions();

                gameStatus = GameStatus.Next;

                if (currentQuestionIndex < questionData.questions.Count)
                {
                    Invoke("SetQuestion", 0.5f);
                }
            }
            else
            {
                Debug.Log("Not Correct");
            }
        }
    }


    public void ResetQuestion()
    {
        for (int i = 0; i < answerWordArray.Length; i++)
        {
            answerWordArray[i].gameObject.SetActive(true);
            answerWordArray[i].SetChar('_');
        }

        for (int i = answerWord.Length; i < answerWordArray.Length; i++)
        {
            answerWordArray[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < optionWordArray.Length; i++)
        {
            optionWordArray[i].gameObject.SetActive(true);
        }
    }

    public void ResetLastWord()
    {
        if (selectedWordIndex.Count > 0)
        {
            int index = selectedWordIndex[selectedWordIndex.Count - 1];
            optionWordArray[index].gameObject.SetActive(true);
            selectedWordIndex.RemoveAt(selectedWordIndex.Count - 1);
            currentAnswerIndex--;
            answerWordArray[currentAnswerIndex].SetChar('_');
        }
    }

    public void GiveHint()
    {
        // Check if the player has at least 100 coins
        if (coins >= 100)
        {
            // Deduct 100 coins
            SpendCoins(100);

            // Find the first empty slot in the answer array that hasn't been revealed
            for (int i = 0; i < answerWordArray.Length; i++)
            {
                if (answerWordArray[i].charValue == '_' && !revealedLetterIndices.Contains(i))
                {
                    // Set the correct letter in that slot
                    answerWordArray[i].SetChar(char.ToUpper(answerWord[i]));

                    // Add the index to the revealed letters set
                    revealedLetterIndices.Add(i);

                    // Find the corresponding option in the optionWordArray and deactivate it
                    for (int j = 0; j < optionWordArray.Length; j++)
                    {
                        if (optionWordArray[j].charValue == char.ToUpper(answerWord[i]) && optionWordArray[j].gameObject.activeSelf)
                        {
                            optionWordArray[j].gameObject.SetActive(false);
                            break;
                        }
                    }

                    // Check if the answer is complete after using the hint
                    CheckAnswerCompletion();

                    return;
                }
            }
        }
        else
        {
            notEnoughCoinsUI.SetActive(true);
            Debug.Log("Not enough coins for a hint.");
        }
    }

    private void CheckAnswerCompletion()
    {
        correctAnswer = true;

        // Verify if the current answer matches the correct answer
        for (int i = 0; i < answerWord.Length; i++)
        {
            if (char.ToUpper(answerWord[i]) != char.ToUpper(answerWordArray[i].charValue))
            {
                correctAnswer = false;
                break;
            }
        }

        if (correctAnswer)
        {
            FindObjectOfType<AudioManager>().Play("LevelComplete");
            Debug.Log("Correct");
            AddCoins(20); // Add coins for a correct answer

            // Update the level after a correct answer
            currentLevel++;
            UpdateUI();
            SaveGameData();
            SaveAnsweredQuestions();

            gameStatus = GameStatus.Next;

            if (currentQuestionIndex < questionData.questions.Count)
            {
                Invoke("SetQuestion", 0.5f);
            }
        }
    }


    public void CloseNotEnoughCoinsUI(){
        notEnoughCoinsUI.SetActive(false);
    }
    
    public void ResetConfirmation(){
        endGameCanvas.SetActive(false);
        confirmationUI.SetActive(true);
    }

    public void ResetCancel(){
        endGameCanvas.SetActive(true);
        confirmationUI.SetActive(false);
    }

   public void ClearLetter(int index)
    {
        // Validate index range
        if (index < 0 || index >= answerWordArray.Length)
        {
            Debug.LogWarning($"Invalid index: {index}");
            return;
        }

        // Check if the slot is empty or a hinted slot
        if (answerWordArray[index].charValue == '_' || revealedLetterIndices.Contains(index))
        {
            Debug.LogWarning($"Cannot clear letter at index {index}: Empty or hinted slot.");
            return;
        }

        // Locate and reactivate the matching option in the options array
        for (int i = 0; i < optionWordArray.Length; i++)
        {
            if (optionWordArray[i].charValue == answerWordArray[index].charValue && !optionWordArray[i].gameObject.activeSelf)
            {
                optionWordArray[i].gameObject.SetActive(true);
                break;
            }
        }

        // Clear the answer slot
        answerWordArray[index].SetChar('_');
        Debug.Log($"Cleared letter at index {index}.");

        // Remove the index from selectedWordIndex
        selectedWordIndex.Remove(index);

        // Adjust currentAnswerIndex to enable retyping
        currentAnswerIndex = Mathf.Min(currentAnswerIndex, index);
    }





    public void ClearTypedLetters()
    {
        // Loop through the answerWordArray
        for (int i = 0; i < answerWord.Length; i++)
        {
            // Only clear slots that were not revealed by a hint
            if (!revealedLetterIndices.Contains(i) && answerWordArray[i].charValue != '_')
            {
                // Find the corresponding letter in the optionWordArray
                for (int j = 0; j < optionWordArray.Length; j++)
                {
                    if (optionWordArray[j].charValue == answerWordArray[i].charValue && !optionWordArray[j].gameObject.activeSelf)
                    {
                        // Reactivate the option letter
                        optionWordArray[j].gameObject.SetActive(true);
                        break;
                    }
                }

                // Clear the slot in the answerWordArray
                answerWordArray[i].SetChar('_');
            }
        }

        // Clear the selectedWordIndex list after reactivation
        selectedWordIndex.RemoveAll(index => !revealedLetterIndices.Contains(index));

        // Reset currentAnswerIndex to the first empty slot
        currentAnswerIndex = 0;
        while (currentAnswerIndex < answerWord.Length && (answerWordArray[currentAnswerIndex].charValue != '_' || revealedLetterIndices.Contains(currentAnswerIndex)))
        {
            currentAnswerIndex++;
        }

        Debug.Log("Cleared all typed letters and reactivated options.");
    }




    private void UpdateUI()
    {
        levelText.text = currentLevel.ToString();
        coinsText.text = coins.ToString(); // Update coins UI

        // Debug logs to verify UI update
        Debug.Log("Updated UI - Level: " + levelText.text);
        Debug.Log("Updated UI - Coins: " + coinsText.text);
    }

    private void SaveGameData()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentQuestionIndex", currentQuestionIndex);
        PlayerPrefs.SetInt("Coins", coins); // Save coins
        PlayerPrefs.Save();
        
        // Debug logs
        Debug.Log("Saved Level: " + currentLevel);
        Debug.Log("Saved CurrentQuestionIndex: " + currentQuestionIndex);
        Debug.Log("Saved Coins: " + coins);
    }

    private void LoadGameData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentQuestionIndex = PlayerPrefs.GetInt("CurrentQuestionIndex", -1);
        coins = PlayerPrefs.GetInt("Coins", 0); // Load coins

        LoadAnsweredQuestions();
        
        // Debug logs
        Debug.Log("Loaded Level: " + currentLevel);
        Debug.Log("Loaded CurrentQuestionIndex: " + currentQuestionIndex);
        Debug.Log("Loaded Coins: " + coins);

        // Ensure UI is updated after loading
        UpdateUI();
    }

    private void SaveAnsweredQuestions()
    {
        PlayerPrefs.SetInt("AnsweredQuestionCount", askedQuestionIndices.Count);

        for (int i = 0; i < askedQuestionIndices.Count; i++)
        {
            PlayerPrefs.SetInt("AnsweredQuestion_" + i, askedQuestionIndices[i]);
            // Debug log
            Debug.Log("Saved AnsweredQuestion_" + i + ": " + askedQuestionIndices[i]);
        }

        PlayerPrefs.Save();
    }

    private void LoadAnsweredQuestions()
    {
        int answeredQuestionCount = PlayerPrefs.GetInt("AnsweredQuestionCount", 0);
        askedQuestionIndices.Clear();
        
        for (int i = 0; i < answeredQuestionCount; i++)
        {
            int questionIndex = PlayerPrefs.GetInt("AnsweredQuestion_" + i);
            askedQuestionIndices.Add(questionIndex);
            // Debug log
            Debug.Log("Loaded AnsweredQuestion_" + i + ": " + questionIndex);
        }
    }

    public void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs have been reset.");
        currentQuestionIndex = -1; // Reset to start from a new question
        coins = 0; // Reset coins
        UpdateUI();
        endGameCanvas.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Methods to manage coins
    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    public void SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            UpdateUI();
        }
        else
        {
            Debug.Log("Not enough coins.");
        }
    }
    
    public void BackToMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

}


[System.Serializable]
public class QuestionData
{
    public Sprite questionImage;
    public Sprite questionImage01;
    public Sprite questionImage02;
    public Sprite questionImage03;
    public string answer;
}

public enum GameStatus
{
    Playing,
    Next
}
