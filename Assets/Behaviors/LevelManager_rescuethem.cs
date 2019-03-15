/*
 * Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Behaviors.GameOn;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Behaviors
{
    public class LevelManager_rescuethem : MonoBehaviour
    {
        private GameObject activePlayer;
        private Camera cam;
        [FormerlySerializedAs("challangeUI")] public GameObject challangeUi;
        public GameObject creditsCanvas;
        public TextMeshProUGUI finalTimeText;
        private FollowCamera followCam;
        public GameObject gameOnCanvas;
        public GameOnManager gameOnManager;
        public GameObject gameOnObject;

        public GameObject goal;
        public string lastScene;
        public Button mainMenuButton;
        public GameObject matchesCanvas;
        public Button nextButton;

        public GameObject pausecanvas;
        public Button pauseNextButton;
        public Button pauseReplayButton;
        public GameObject player;
        public RacemanBehavior playerController;
        public GameObject readyCanvas;
        public GameObject readyText;
        public Button replayButton;

        public GameObject spawnPoint;

        public Button startButton;
        public string state;
        public TextMeshProUGUI status;
        private bool submittedScore;
        private float timer;
        public TextMeshProUGUI timerText;
        private int lives;

        public Button xButton;
        public GameObject xcanvas;
        public GameObject youwinPrize;
        public GameObject youwinText;

        private void Awake()
        {
            //Set screen size for Standalone
#if UNITY_STANDALONE
        Screen.SetResolution (720, 1280, false);
        Screen.fullScreen = false;
#endif
        }

        // Using this for initialization
        private void Start()
        {
            followCam = FindObjectOfType<FollowCamera>();
            spawnPoint = GameObject.FindGameObjectWithTag("spawnpoint");
            goal = GameObject.FindGameObjectWithTag("goal");


            state = "standby";
            timer = 0.0f;
            lives = 0;
            submittedScore = false;
            Application.targetFrameRate = 60;

            if (replayButton != null) replayButton.onClick.AddListener(reset);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(MainMenu);
            if (xButton != null) xButton.onClick.AddListener(reset);
            if (nextButton != null) nextButton.onClick.AddListener(nextLevel);
            if (startButton != null) startButton.onClick.AddListener(begin);
            if (pauseReplayButton != null) pauseReplayButton.onClick.AddListener(reset);
            if (pauseNextButton != null) pauseNextButton.onClick.AddListener(nextLevel);
            if (SceneManager.GetActiveScene().name == "mainMenu") begin();
        
            try
            {
                
                gameOnManager = GameObject.FindGameObjectWithTag("UICanvas").GetComponentInChildren<GameOnManager>();
                
            }
            catch (Exception e)
            {
                if (e is NullReferenceException) Debug.Log("gameon is OFF");
            }
            
            if (gameOnManager != null)
            {
                Debug.Log("gameon is ON");
                challangeUi.SetActive(false);
            }

            if (SceneManager.GetActiveScene().name != lastScene) return;
            nextButton.onClick.RemoveAllListeners();
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Main Menu";
            nextButton.onClick.AddListener(MainMenu);
        }

        private string TimeToString(float time)
        {
            var seconds = Mathf.FloorToInt(time);
            var centiseconds = Mathf.FloorToInt(time % 1f * 100f);
            return string.Format("{0}.{1:00}",
                seconds, centiseconds);
        }

        // Update is called once per frame
        private void Update()
        {
            //mostly the code here is to detect which state the game is in, and set the correct canvas and UI on
            if (state == "win")
            {
                if (SceneManager.GetActiveScene().name == "mainMenu")
                {
                    nextLevel();
                    return;
                }


                followCam.SetTarget(goal.transform);
                goal.SetActive(false);
                xcanvas.SetActive(false);
                Destroy(activePlayer);
                if (SceneManager.GetActiveScene().name == lastScene)
                {
                    pauseNextButton.onClick.RemoveAllListeners();
                    pauseNextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Main Menu";
                    pauseNextButton.onClick.AddListener(MainMenu);
                }

                double score = timer;
                if (gameOnManager != null)
                {
                    if (!submittedScore)
                    {
                        //this code here is to submit the score if GameOn was on after a level is completed.
                        submittedScore = true;
                        gameOnManager.SubmitScore((int) (score * 1000f), lives, gameOnManager.activeMatch);
                        Debug.Log(score);
                    }
                }
                else
                {
                    youwinText.SetActive(true);
                    score = (int) (score * 1000f);
                    finalTimeText.text = TimeToString(timer) + " seconds";
                }
            }

            switch (state)
            {
                case "gameover":

                    Destroy(activePlayer);
                    Destroy(goal);
                    break;
                case "active":
                {
                    if (SceneManager.GetActiveScene().name != "mainMenu")
                    {
                        xcanvas.SetActive(true);
                        timer += Time.deltaTime;
                        timerText.text = TimeToString(timer);
                    }

                    break;
                }
            }

            if (state != "standby")
                if (state != "pause")
                    return;
        }

// this functions calls the respawning coroutine
        public void Respawn()
        {
            StartCoroutine(respawn());
        }

        private IEnumerator respawn()
        {
            yield return new WaitForSeconds(1);
            activePlayer = Instantiate(player, spawnPoint.transform.position, spawnPoint.transform.rotation);
            followCam.SetTarget(activePlayer.transform);
            lives++;
        }

//this runs once the game begins
        public void begin()
        {
            state = "active";
            activePlayer = Instantiate(player, spawnPoint.transform.position, spawnPoint.transform.rotation);
            activePlayer.SetActive(true);
            followCam.SetTarget(activePlayer.transform);

            playerController = activePlayer.GetComponent<RacemanBehavior>();
            if (SceneManager.GetActiveScene().name != "mainMenu") readyText.SetActive(false);
        }

// resets the level and reloads the active scene
        public void reset()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

//this just reloads the next level in the scene index
        public void nextLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

//this function takes the game back to the main menu
        public void MainMenu()
        {
            if (gameOnManager != null)
            {
                gameOnManager.SubmitScore(9999999,999999, gameOnManager.activeMatch);
                Destroy(GameObject.FindGameObjectWithTag("UICanvas"));
            }

            SceneManager.LoadScene("mainMenu");
        }

//this function activates the GameOn canvas
        public void viewGameOn()
        {
            gameOnObject.SetActive(true);
            gameOnCanvas.SetActive(true);
            readyCanvas.SetActive(false);
            playerController.enabled = false;
        }
//this function activates the credits canvas

        public void viewCredits()
        {
            creditsCanvas.SetActive(true);
            readyCanvas.SetActive(false);
            playerController.enabled = false;
        }
//this function closes the GameOn canvas

        public void cancelGameOn()
        {
            gameOnCanvas.SetActive(false);
            readyCanvas.SetActive(true);
            gameOnObject.SetActive(false);
            playerController.enabled = true;
        }
//this function closes the credits canvas

        private void cancelCredits()
        {
            creditsCanvas.SetActive(false);
            readyCanvas.SetActive(true);
            playerController.enabled = true;
        }
//this function starts the scene that starts the moments demo

        public void startMomentsDemo()
        {
            SceneManager.LoadScene("moments1");
        }
    }
}