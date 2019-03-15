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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Behaviors.GameOn.models;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Behaviors.GameOn
{
    public class GameOnManager : MonoBehaviour
    {
        private static readonly string PUBLIC_KEY_PLAYERPREFS_KEY = "encryptedPublicKey";
        private static readonly string PRIVATE_KEY_PLAYERPREFS_KEY = "encryptedPrivateKey";
        private static readonly string ENCRYPTED_DEVICE_APP_TOKEN_KEY = "deviceAppTokenKey";


        [Header("UI Elements DO NOT EDIT")] public TextMeshProUGUI accessKey;

        public string activeLevel;
        public string activeMatch;
        public GameObject allUiCanvas;
        public Button claimButton;
        public Button claimPrizesButton;
        public GameObject gameonCanvas;

        private GameObject dontDestroyCanvasAndGameOn;
        /*PlayerPrefs Keys for the various required data*/

        [Header("GameOn Configuration EDIT FOR YOUR GAME ID and ")]
        public string gameApiKey;

        public GameObject gameOnPrizesScreenGo;
        public GameObject gameOnWelcomeScreenGo;
        public string gamePublicKey = "gamePublicKeyHere";
        public Button joinButton;
        public GameObject matchCanvas;
        public TextMeshProUGUI matchDetailsText;
        public GameObject passkeyText;
        public Button playButton;
        public string playerName;
        public TextMeshProUGUI playerNameObject;
        public GameObject prizeCanvas;
        public GameObject prizeDetailsCanvas;
        public TextMeshProUGUI prizeDetailsText;
        public GameObject prizeImage;

        [Header("GameOn Inspection DO NOT EDIT")]
        private string sessionId = "do not fill";

        private Int64 sessionExpirationDate;

        public TextMeshProUGUI status;
        public GameObject tList;
        public GameObject tournamentButton;
        public GameObject tournamentCanvas;
        public TextMeshProUGUI tournamentDetailsText;
        public TextMeshProUGUI twitchCode;
        public GameObject twitchCodeObj;

        //if playerPrefs don't have private and public keys generated, we are generate them and save them so we can get them later.
        private static string PublicKey
        {
            get
            {
                var myPublicKey = PlayerPrefs.GetString(PUBLIC_KEY_PLAYERPREFS_KEY);
                if (string.IsNullOrEmpty(myPublicKey))
                {
                    var keyPair = GameOnTools.KeyPair.Generate();
                    myPublicKey = keyPair.Public;
                    PlayerPrefs.SetString(PUBLIC_KEY_PLAYERPREFS_KEY, keyPair.Public);
                    PlayerPrefs.SetString(PRIVATE_KEY_PLAYERPREFS_KEY, keyPair.Private);
                }

                Debug.Log("PUBLIC KEY IS: " + myPublicKey);
                return myPublicKey;
            }
        }

        private static string PrivateKey
        {
            get
            {
                var myEncryptedPrivateKey = PlayerPrefs.GetString(PRIVATE_KEY_PLAYERPREFS_KEY);
                Debug.Log("PRIVATE KEY IS: " + myEncryptedPrivateKey);
                return myEncryptedPrivateKey;
            }
        }

        // we get the device token here that we will be saving to player prefs later
        private static string EncryptedDeviceAppToken => PlayerPrefs.GetString(ENCRYPTED_DEVICE_APP_TOKEN_KEY);

        // Use this for initialization
        private void Start()
        {
            dontDestroyCanvasAndGameOn = GameObject.FindGameObjectWithTag("UICanvas");
        }

        //this is called once every time the gameobject is enabled, we start a coroutine to get a play session.
        private void OnEnable()
        {
            //claimPrizesButton.onClick.AddListener(prizesButtonPressed);
            SetName();
            StartCoroutine(startTheShow());
        }

        /*
     * A unity post web request for making post calls.
     * url - The rest end url
     * parameters - the request body
     * apiKey - You game api key
     * successCallback - a delegate that will text in the response body text
     * failureCallback - a delegate to handle any failures
     */
        private IEnumerator GameOnPostRequest(string url, string parameters, string apiKey,
            Action<string> successCallback,
            Action<string> failureCallback)
        {
            UnityWebRequest postRequest = null;
            postRequest = UnityWebRequest.Post(url, new Dictionary<string, string>());
            UploadHandler uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(parameters));
            postRequest.uploadHandler = uploadHandler;
            postRequest.SetRequestHeader("X-Api-Key", apiKey);
            postRequest.SetRequestHeader("Content-Type", "application/json");
            postRequest.SetRequestHeader("Session-Id", sessionId);

            if (!string.IsNullOrEmpty(sessionId) && sessionId != "Expired" && HasSessionExpired())
            {

                sessionId = "Expired";
                AuthenticateDevice();
                yield return new WaitUntil(() => sessionId != "Expired");
                postRequest.SetRequestHeader("Session-Id", sessionId);

                yield return postRequest.SendWebRequest();
                if (postRequest.isNetworkError)
                {
                    failureCallback(postRequest.error);
                }
                else
                {

                    var returnedText = Encoding.UTF8.GetString(postRequest.downloadHandler.data);
                    successCallback(returnedText);

                }

            }
            else
            {

                yield return postRequest.SendWebRequest();
                if (postRequest.isNetworkError)
                {
                    failureCallback(postRequest.error);
                }
                else
                {

                    var returnedText = Encoding.UTF8.GetString(postRequest.downloadHandler.data);
                    successCallback(returnedText);

                }
            }
        }

        /*
     * A unity get web request for making post calls.
     * url - The rest end urlx
     * parameters - the request body
     * apiKey - You game api key
     * successCallback - a delegate that will text in the response body text
     * failureCallback - a delegate to handle any failures
     */
        private IEnumerator GameOnGetRequest(string url, string apiKey, Action<string> successCallback,
            Action<string> failureCallback)
        {
            UnityWebRequest getRequest = null;
            getRequest = UnityWebRequest.Get(url);
            getRequest.SetRequestHeader("X-Api-Key", apiKey);
            getRequest.SetRequestHeader("Content-Type", "application/json");
            getRequest.SetRequestHeader("Session-Id", sessionId);
            
           if (HasSessionExpired())
                {
                    sessionId = "Expired";
                    AuthenticateDevice();
                    yield return new WaitUntil(() => sessionId != "Expired");
                    getRequest.SetRequestHeader("Session-Id", sessionId);
                    yield return getRequest.SendWebRequest();
                    if (getRequest.isNetworkError)
                    {
                        failureCallback(getRequest.error);
                    }
                    else
                    {
                        var returnedText = Encoding.UTF8.GetString(getRequest.downloadHandler.data);
                        successCallback(returnedText);

                    }

                }
           else
           {

               yield return getRequest.SendWebRequest();
               if (getRequest.isNetworkError)
               {
                   failureCallback(getRequest.error);
               }
               else
               {
                   var returnedText = Encoding.UTF8.GetString(getRequest.downloadHandler.data);
                   successCallback(returnedText);

               }
           }
        }

        /**
     * A unity put web request for making post calls.
     * url - The rest end url
     * parameters - the request body
     * apiKey - You game api key
     * successCallback - a delegate that will text in the response body text
     * failureCallback - a delegate to handle any failures
     */
        private IEnumerator GameOnPutRequest(string url, string parameters, string apiKey,
            Action<string> successCallback,
            Action<string> failureCallback)
        {
            UnityWebRequest putRequest = null;
            putRequest = UnityWebRequest.Put(url, Encoding.UTF8.GetBytes(parameters));
            UploadHandler uploadHanlder = new UploadHandlerRaw(Encoding.UTF8.GetBytes(parameters));
            putRequest.uploadHandler = uploadHanlder;
            putRequest.SetRequestHeader("X-Api-Key", apiKey);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            putRequest.SetRequestHeader("Session-Id", sessionId);

            if (HasSessionExpired())
            {
                sessionId = "Expired";
                AuthenticateDevice();
                yield return new WaitUntil(() => sessionId != "Expired");
                putRequest.SetRequestHeader("Session-Id", sessionId);


                yield return putRequest.SendWebRequest();
                if (putRequest.isNetworkError)
                    failureCallback(putRequest.error);
                else
                {

                    successCallback(putRequest.responseCode.ToString());

                }
            }
            else
            {
                yield return putRequest.SendWebRequest();
                if (putRequest.isNetworkError)
                    failureCallback(putRequest.error);
                else
                {

                    successCallback(putRequest.responseCode.ToString());

                }
            }
    }

        //we run this on a coroutine every time the main menu is shown.
        //if the device was registered earlier, we just used the saves session
        private IEnumerator startTheShow()
        {
            Debug.Log("game public key :" + gamePublicKey);
            Debug.Log("public key :" + PublicKey);
            Debug.Log("private key : " + PrivateKey);
            var myEncryptedPublicKey = GameOnTools.Crypto.Encrypt(gamePublicKey, PublicKey);
            Debug.Log("ENCRYPTED PUBLIC KEY:" + myEncryptedPublicKey);
            Debug.Log("is it saved: " + PlayerPrefs.GetString(ENCRYPTED_DEVICE_APP_TOKEN_KEY));
            sessionId = PlayerPrefs.GetString("sessionId");


            if (string.IsNullOrEmpty(PlayerPrefs.GetString("sessionExpirationDate")))
                PlayerPrefs.SetString("sessionExpirationDate", "0");


            if (string.IsNullOrEmpty(EncryptedDeviceAppToken))
            {
                sessionId = PlayerPrefs.GetString("sessionId");
                sessionExpirationDate =
                    Convert.ToInt64(PlayerPrefs.GetString("sessionExpirationDate"));

                // Getting here means our device is not yet registered with GameOn So let's do it.

                StartCoroutine(RegisterDevice(myEncryptedPublicKey, successCallback =>
                {
                    //this is the callback function to be called when the coroutine is done nd response is received from the server as "text"
                    RegisterResponse response = JsonUtility
                        .FromJson<RegisterResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    var deviceAppToken = GameOnTools.Crypto.Decrypt(PrivateKey, response.encryptedPlayerToken);
                    Debug.Log("DEVICEAPPTOKEN IS " + deviceAppToken);
                    var myEncryptedDeviceAppToken = GameOnTools.Crypto.Encrypt(gamePublicKey, deviceAppToken);
                    Debug.Log("EncryptedDEVICEAPPTOKEN IS " + myEncryptedDeviceAppToken);
                    PlayerPrefs.SetString(ENCRYPTED_DEVICE_APP_TOKEN_KEY, myEncryptedDeviceAppToken);

                    //start the process of getting the tournaments
                    GetTournaments();
                }, null));
            }
            else
            {
                GetTournaments();
            }
            yield return status.text = "done loading";
        }

        // This is where we get the sessions
        private IEnumerator RegisterDevice(string encryptedDevicePublicKey,Action<string> successCallback,
            Action<string> failureCallback )
        {
            //this is the URL for the registering the device
            var url = "https://api.amazongameon.com/v1/players/register";
            //these are the parameters in JSON form
            var param = "{\"encryptedPayload\":\"" +
                        encryptedDevicePublicKey +
                        "\"}";
            //we start a coroutine using post, and have the callback function written below
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback,
                null));
            yield return true;
        }

        //We check if the player token is expired
        private bool HasSessionExpired()
        {
            
            var nowDate = DateTime.UtcNow;
            var sessionExpirationDateUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(sessionExpirationDate);
            //var timeSpan = sessionExpirationDateUtc.Subtract(nowDate);
            var timeSpan = nowDate.Subtract(sessionExpirationDateUtc);
           
            
            if (timeSpan.TotalMinutes > 0) // authenticate only if the token has expired
            {
                Debug.Log("are we expired? NO\ntimespan: " + timeSpan.TotalMinutes);

                return false;
            }
            else
            {
                Debug.Log("are we expired? YES\ntimespan: " + timeSpan.TotalMinutes);

                return true;
            }
        }


        private void AuthenticateDevice()
        {
            var authRequest = new AuthPlayerRequest();
            authRequest.appBuildType = "release";
            authRequest.deviceOSType = "Android";
            authRequest.encryptedPayload = PlayerPrefs.GetString(ENCRYPTED_DEVICE_APP_TOKEN_KEY);
            authRequest.playerName = playerName;
            
            var url = "https://api.amazongameon.com/v1/players/auth";
            AuthPlayerResponse response = null;
            var param = JsonUtility
                .ToJson(authRequest); // Demonstrating a request to json here. You can use whatever Json utility.
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility.FromJson<AuthPlayerResponse>(successCallback);
                    sessionId = response.sessionId;
                    PlayerPrefs.SetString("sessionId", sessionId);
                    Debug.Log("EXPIRED, BUT WAIT!! YOU NOW HAVE A VALID SESSION " + response.sessionId);
                    //we will be saving the session ID now so that we can skip authentication later and use this session for the next time.
                    // we start the game
                    sessionExpirationDate = response.sessionExpirationDate;
                    PlayerPrefs.SetString("sessionExpirationDate", response.sessionExpirationDate.ToString());
                    //Debug.Log("\njust stored this date"+PlayerPrefs.GetString("sessionExpirationDate")+"\n");
                    
                },
                null));
        }
            
        
        

        //we get the list of tournaments here we can add filters in the params, check the documentation for more filters.
        private void GetTournaments()
        {
            var url = "https://api.amazongameon.com/v1/tournaments?playerAttributes={\"stats\": \"lives\"}";
            GetDeveloperTournamentListResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    
                    // Demonstrating a json to response here. You can use whatever Json utility.
                    response = JsonUtility.FromJson<GetDeveloperTournamentListResponse>(successCallback);
                    //now that we got the list of Tournaments, now lets get streamer tournaments
                    ListTournaments(response);
                    GetPlayerTournaments();
                },
                null));
        }

        private void GetPlayerTournaments()
        {
            var url = "https://api.amazongameon.com/v1/player-tournaments?queryBy=GAME&streamingPlatform=TWITCH";
            GetPlayerTournamentListResponse response = null;
            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    // Demonstrating a json to response here. You can use whatever Json utility.
                    response = JsonUtility.FromJson<GetPlayerTournamentListResponse>(successCallback);
                    //now that we got the list of all Tournaments, lets show them
                    ListPlayerTournaments(response);
                },
                null));
        }

        //here we just list the tournaments in buttons on a canvas
        private void ListTournaments(GetDeveloperTournamentListResponse tournamentsList)
        {
            //we go through every tournament in the list we got from the API and instantiate a button in the list
            //with information about the tournament.
            foreach (Transform child in gameOnWelcomeScreenGo.transform) Destroy(child.gameObject);
            if (tournamentsList != null)
            {
                foreach (var t in tournamentsList.tournaments)
                {
                    var b = Instantiate(tournamentButton, gameOnWelcomeScreenGo.transform);
                    b.GetComponent<Button>().onClick.AddListener(delegate { tButtonPressed(t); });
                    b.transform.Find("title").gameObject.GetComponent<TextMeshProUGUI>().text = t.title;
                    b.transform.Find("subtitle").gameObject.GetComponent<TextMeshProUGUI>().text = t.subtitle;
                    if (t.imageUrl != null) StartCoroutine(getButtonImage(b, t.imageUrl));
                }

                status.text = "Tournaments";
            }
            else
            {
                status.text = "no tournaments";
            }
        }

        //here we just list the tournaments in buttons on a canvas
        private void ListPlayerTournaments(GetPlayerTournamentListResponse tournamentsList)
        {
            //we go through every tournament in the list we got from the API and instantiate a button in the list
            //with information about the tournament.
            if (tournamentsList != null)
            {
                foreach (var t in tournamentsList.tournaments)
                {
                    var b = Instantiate(tournamentButton, gameOnWelcomeScreenGo.transform);
                    b.GetComponent<Button>().onClick.AddListener(delegate { ptButtonPressed(t); });
                    b.transform.Find("title").gameObject.GetComponent<TextMeshProUGUI>().text = t.title;
                    // b.transform.Find("subtitle").gameObject.GetComponent<TextMeshProUGUI>().text = t.subtitle;
                    b.transform.Find("subtitle").gameObject.GetComponent<TextMeshProUGUI>().text =
                        "(Twitch Tournament)";
                    if (t.imageUrl != null) StartCoroutine(getButtonImage(b, t.imageUrl));
                }

                status.text = "All Tournaments";
            }
            else
            {
                status.text = "no user tournaments";
            }
        }

        //this will be the onclick function for the tournament button, it will open the tournament details canvas
        private void tButtonPressed(Tournament t)
        {
            tournamentCanvas.GetComponent<TournamentManager>().T = t;
            tournamentCanvas.SetActive(true);
            tList.SetActive(false);
        }

        private void ptButtonPressed(Tournament t)
        {
            tournamentCanvas.GetComponent<TournamentManager>().T = t;
            tournamentCanvas.SetActive(true);
            tList.SetActive(false);
        }

        public void prizesButtonPressed()
        {
            prizeCanvas.SetActive(true);
            tList.SetActive(false);
        }


        //this is a function that the tournament list function uses to fill out the images in the tournament buttons
        private IEnumerator getButtonImage(GameObject b, string imageUrl)
        {
            // Start a download of the given URL
            using (var www = new WWW(imageUrl))
            {
                // Wait for download to complete
                yield return www;
                // assign texture
                b.GetComponent<Image>().sprite = Sprite.Create(www.texture,
                    new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
            }
        }

        //in this function, we call details about a tournament when a tournament button is pressed
        public void GetTournamentData(string id)
        {
            var url = "https://api.amazongameon.com/v1/tournaments/" + id;
            GetDeveloperTournamentDetailsResponse response = null;
            //check the API documentation for more information about filters

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetDeveloperTournamentDetailsResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    // in this callback function, we just show the tournament information to the player
                    // you can choose to display this information however you want.
                    tournamentDetailsText.text =
                        "" + response.title + "\n" +
                        "" + response.subtitle + "\n" +
                        "" + response.description + "\n\n" +
                        "" + response.winType + " score wins\n" +
                        "Attempts left: " + response.playerAttemptsPerMatch + "\n";
                    if (!response.canEnter)
                    {
                        // if the player cant join, lets show some data about their position on the leaderboard instead.
                        joinButton.interactable = false;
                        joinButton.GetComponentInChildren<TextMeshProUGUI>().text = "Joined";
                        ViewMatchCanvas();
                        GetMatchData(PlayerPrefs.GetString(response.tournamentId));
                    }
                    else
                    {
                        // if the player can join, lets allow the player to join
                        joinButton.GetComponentInChildren<TextMeshProUGUI>().text = "JOIN";
                        
                        if (!response.hasAccessKey)
                        {
                            passkeyText.SetActive(false);
                            joinButton.interactable = true;
                        }
                        else
                        {
                            passkeyText.SetActive(true);
                        }
                    }
                },
                null));
        }

        public void GetPlayerTournamentData(Tournament response)
        {
            tournamentDetailsText.text =
                "" + response.title + "\n" +
                "" + response.subtitle + "\n" +
                "" + response.winType + " score wins\n" +
                "Attempts left: " + response.playerAttemptsPerMatch + "\n";
            //if can join
            // if the player can join, lets allow the player to join
            joinButton.GetComponentInChildren<TextMeshProUGUI>().text = "JOIN";
            //do twitch stuff
            if (bool.Parse(response.hasAccessKey))
            {
                passkeyText.SetActive(true);
            }
            else
            {
                passkeyText.SetActive(false);
                joinButton.interactable = true;
            }

            if (response.streamingPlatform == null) return;
            if (string.IsNullOrEmpty(PlayerPrefs.GetString("twitchCode")))
                twitchCodeObj.SetActive(true);
        }

        //If a player joins a tournament, this function is called
        public void EnterTournament(string tournamentId, string title, bool playerTournament)
        {
            var entTourRequest = new EnterTournamentRequest();
            entTourRequest.accessKey = Regex.Replace(accessKey.text, @"[^0-9a-zA-Z:,]+", "");
            //we set the player here to be active in the API as an attribute
            var url = "https://api.amazongameon.com/v1/tournaments/" + tournamentId + "/enter";
            EnterTournamentResponse response = null;
            string param;
            if (passkeyText.activeSelf)
                param = "{\"playerAttributes\":{ \"isActive\":\"true\"},\"accessKey\":\"" + entTourRequest.accessKey +
                        "\"}";
            else
                param = "{\"playerAttributes\":{ \"isActive\":\"true\"}}";
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility.FromJson<EnterTournamentResponse>(successCallback);
                    status.text = title;

                    //we start a level, if its a player generated tournament, then we randomize the level through the hash function, if not, then we use the level that is written in the metadata of the tournament.
                    if (playerTournament || string.IsNullOrEmpty(response.metadata))
                    {
                        if (response.tournamentId == null)
                        {
                            GetMatches(tournamentId, playerTournament);
                            return;
                        }

                        var c = new MD5CryptoServiceProvider();
                        var bytes = c.ComputeHash(new UTF8Encoding().GetBytes(response.matchId));
                        var s = string.Empty;
                        for (var i = 0; i < bytes.Length; i++) s += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
                        s.PadLeft(32, '0');
                        activeLevel = (Math.Abs(s.GetHashCode()) % 5 + 8).ToString();
                    }
                    else
                    {
                        activeLevel = response.metadata;
                    }

                    tournamentCanvas.SetActive(false);
                    gameonCanvas.SetActive(false);
                    DontDestroyOnLoad(dontDestroyCanvasAndGameOn);
                    activeMatch = response.matchId;
                    PlayerPrefs.SetString(tournamentId, activeMatch);
                    if (playerTournament)
                        SceneManager.LoadScene(int.Parse(activeLevel));
                    else
                        SceneManager.LoadScene(activeLevel);
                },
                null));
        }

        //If a player joins a tournament, this function is called
        public void EnterPlayerTournament(string tournamentId, string title)
        {
            Debug.Log("'" + PlayerPrefs.GetString("twitchCode") + "'");
            var url = "https://api.amazongameon.com/v1/players/streaming-platform-account-linking-code";

            var link = new LinkAccountRequest();
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("twitchCode")))
            {
                EnterTournament(tournamentId, title, true);
            }
            else
            {
                twitchCode.text = Regex.Replace(twitchCode.text, @"[^0-9a-zA-Z:,]+", "");
                var param = "{ \"code\" : \"" + twitchCode.text + "\"}";
                link.code = twitchCode.text;
                link.code = Regex.Replace(link.code, @"[^0-9a-zA-Z:,]+", "");
                param = JsonUtility.ToJson(link, false);
                StartCoroutine(GameOnPutRequest(url,
                    param,
                    gameApiKey,
                    successCallback =>
                    {
                        if (successCallback == "200")
                        {
                            PlayerPrefs.SetString("twitchCode", twitchCode.text);
                            EnterTournament(tournamentId, title, true);
                        }
                        else
                        {
                            status.text = "wrong twitch code";
                        }
                    },
                    null));
            }
        }

        //in this function, we call details about a tournament when a tournament button is pressed
        private void GetMatches(string tournamentId, bool playerTournament = false)
        {
            var url = "https://api.amazongameon.com/v1/matches?filterBy=live";
            GetMatchListResponse response = null;
            //check the API documentation for more information about filters

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetMatchListResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    if (!playerTournament) return;
                    for (var i = 0; i < response.playerMatches.Length; i++)
                        if (response.playerMatches[i].tournamentId == tournamentId)
                        {
                            PlayMatch(response.playerMatches[i].matchId, true);
                            break;
                        }
                        else
                        {
                            for (var j = 0; j < response.matches.Length; j++)
                                if (response.matches[j].tournamentId == tournamentId)
                                {
                                    PlayMatch(response.matches[j].matchId, false);
                                    break;
                                }
                        }
                },
                null));
        }

        //in this function, I am getting data about the specific match.
        //it will print to screen some info about the player's score
        // as well as their position in the leaderboard
        //if the player has more tries, it will ask the player to play again,
        //if not, then the player can go back to the main menu
        private void GetMatchData(string id)
        {
            var url = "https://api.amazongameon.com/v1/matches/" + id;
            GetMatchDetailsResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetMatchDetailsResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    matchDetailsText.text =
                        "Your score: " + response.score / 1000f + " Seconds\n" +
                        "Attempts remaining: " + response.attemptsRemaining;
                    if (!String.IsNullOrEmpty(response.tournamentDetails.leaderboardStat))
                    {
                        matchDetailsText.text =  "Lives used: " + response.score + "\n" +
                                                 "Attempts remaining: " + response.attemptsRemaining;
                    }
                    GetMatchLeaderboard(id, "");
                    if (response.canEnter)
                    {
                        playButton.interactable = true;
                        playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play Again";
                        playButton.onClick.RemoveAllListeners();
                        activeMatch = response.matchId;
                        
                        if (string.IsNullOrEmpty(response.tournamentDetails.creatorPlayerName))
                            playButton.onClick.AddListener(delegate { Replay(false); });
                        else
                            playButton.onClick.AddListener(delegate { Replay(true); });
                    }
                    else
                    {
                        playButton.interactable = true;
                        playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Main Menu";
                        playButton.onClick.RemoveAllListeners();
                        playButton.onClick.AddListener(MainMenu);
                    }
                },
                null));
        }

        //this function gets details on the leaderboard, depending on the query, the player can ask
        // for their position on the board as well as their neighboring playings, or can ask for an overall
        //top scores in the leaderboard.
        private void GetMatchLeaderboard(string matchId, string query)
        {
            var url = "https://api.amazongameon.com/v1/matches/" + matchId + "/leaderboard?" + query;
            GetMatchLeaderboardResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetMatchLeaderboardResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    if (response.currentPlayer.externalPlayerId == null)
                    {
                        if (!string.IsNullOrEmpty(response.leaderboardStat))
                        {
                            matchDetailsText.text += "\n\n" +
                                                     "Lowest lives used: " + response.leaderboard[0].score  +
                                                     "\nBy: " + response.leaderboard[0].playerName;
                            

                        }
                        else
                        {
                            matchDetailsText.text += "\n\n" +
                                                     "Best Score: " + response.leaderboard[0].score / 1000f +
                                                     "  Seconds\n" +
                                                     "By: " + response.leaderboard[0].playerName;
                            
                        }
                        GetMatchLeaderboard(matchId, "currentPlayerNeighbors=1");
                    }
                    else
                    {
                        matchDetailsText.text += "\n\n" +
                                                 "Your rank is: " + response.currentPlayer.rank + "\n";
                    }
                },
                null));
        }

        private void Replay(bool playerTournament)
        {
            PlayMatch(activeMatch, playerTournament);
        }

        private void MainMenu()
        {
            Destroy(dontDestroyCanvasAndGameOn);
            SceneManager.LoadScene("mainMenu");
        }

        //I call this function when a player entered a tournament, but has more attempts left.
        //This marks the start of another match
        //**************************************
        //IMPORTANT
        //**************************************
        //I found out that the Unity JsonUtility class is not able to serialize complex objects such
        //as Dictionary, so instead I used the Json.net plugin, which is a port of Newtonsoft.Json https://www.newtonsoft.com/json
        //for unity it can be found here https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347
        private void PlayMatch(string matchId, bool playerTournament)
        {
            //usually we would create an entMatchRequest, but Unity doesn't support serializing dictionaries. uncomment these lines if you
            //are using Newtonsoft.Json
            //EnterMatchRequest entMatchRequest = new EnterMatchRequest();
            //entMatchRequest.playerAttributes.Add ("isActive", "true");
            //comment this line if you are using Newtonsoft.Json
            var url = "https://api.amazongameon.com/v1/matches/" + matchId + "/enter";
            EnterMatchResponse response = null;
            //uncomment if using Newtonsoft.Json
            //string param = JsonConvert.SerializeObject(entMatchRequest);
            //comment if using Newtonsoft.Json
            var paramJson = "{ \"playerAttributes\":{ \"isActive\":\"true\"}}";
            var param = paramJson;
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility.FromJson<EnterMatchResponse>(successCallback);
                    //start a new scene, play the game, then submit the score, show leaderboards.
                    tournamentCanvas.SetActive(false);
                    allUiCanvas.SetActive(true);
                    gameonCanvas.SetActive(false);
                    DontDestroyOnLoad(dontDestroyCanvasAndGameOn);
                    activeMatch = response.matchId;
                    activeLevel = response.metadata;
                    if (string.IsNullOrEmpty(activeLevel))
                    {
                        var c = new MD5CryptoServiceProvider();
                        var bytes = c.ComputeHash(new UTF8Encoding().GetBytes(response.matchId));
                        var s = string.Empty;
                        for (var i = 0; i < bytes.Length; i++) s += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
                        s.PadLeft(32, '0');
                        activeLevel = (Math.Abs(s.GetHashCode()) % 5 + 8).ToString();
                    }

                    if (playerTournament)
                        SceneManager.LoadScene(int.Parse(activeLevel));
                    else
                    {
                        
                        SceneManager.LoadScene(activeLevel);
                    }
                },
                null));
        }

        //submitting score was tricky. It uses a PUT request and not a POST request
        public void SubmitScore(int score,int lives, string matchId)
        {
            var paramJson = "{\"score\":" +score+", \"stats\":[{\"name\": \"lives\",\"value\":"+lives+"}]}";
            //submitScoreRequest.score = score;
            var url = "https://api.amazongameon.com/v1/matches/" + matchId + "/score";
            //var param2 = JsonUtility.ToJson(submitScoreRequest); // Demonstrating a request to json here. You can use whatever Json utility.
            var param = paramJson;
            StartCoroutine(GameOnPutRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    ViewMatchCanvas();
                    GetMatchData(matchId);
                },
                null));
        }

        //just a function to manage the different UI elements
        private void ViewMatchCanvas()
        {
            allUiCanvas.SetActive(true);
            matchCanvas.SetActive(true);
            gameonCanvas.SetActive(true);
            tournamentCanvas.SetActive(false);
        }

        //we set a player's GameOn name
        //If we find that their is a name saved in PlayerPrefs, we just use that one
        private void SetName()
        {
            if (PlayerPrefs.GetString("playerName") == "")
            {
                if (playerNameObject.text == "")
                {
                    playerName = "coward player";
                }
                else
                {
                    playerName = playerNameObject.text;
                    PlayerPrefs.SetString("playerName", playerName);
                }
            }
            else
            {
                playerName = PlayerPrefs.GetString("playerName");
            }
        }

        public void GetPrizesToClaim()
        {
            var url = "https://api.amazongameon.com/v1/matches?filterBy=unclaimed-prizes";
            GetMatchListResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility.FromJson<GetMatchListResponse>(successCallback);

                    ListPrizes(response);
                },
                null));
        }

        private void ListPrizes(GetMatchListResponse prizesList)
        {
            var url = "https://api.amazongameon.com/v1/matches?filterBy=unclaimed-prizes";

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    //we go through every tournament in the list we got from the API and instantiate a button in the list
                    //with information about the tournament.
                    foreach (Transform child in gameOnPrizesScreenGo.transform) Destroy(child.gameObject);
                    if (prizesList != null)
                    {
                        if (prizesList.matches != null)
                            foreach (var m in prizesList.matches)
                            {
                                var b = Instantiate(tournamentButton, gameOnPrizesScreenGo.transform);
                                b.GetComponent<Button>().onClick.AddListener(delegate
                                {
                                    prizeDetailsCanvas.GetComponent<PrizeDetailsScript>().matchId = m.matchId;
                                    pButtonPressed(m.prizeBundles[0].prizeIds[0]);
                                });
                                b.transform.Find("title").gameObject.GetComponent<TextMeshProUGUI>().text =
                                    m.prizeBundles[0].title;
                                b.transform.Find("subtitle").gameObject.GetComponent<TextMeshProUGUI>().text = "";
                            }

                        if (prizesList.playerMatches != null)
                            foreach (var m in prizesList.playerMatches)
                            {
                                var b = Instantiate(tournamentButton, gameOnPrizesScreenGo.transform);
                                b.GetComponent<Button>().onClick.AddListener(delegate
                                {
                                    prizeDetailsCanvas.GetComponent<PrizeDetailsScript>().matchId = m.matchId;
                                    pButtonPressed(m.prizeBundles[0].prizeIds[0]);
                                });
                                b.transform.Find("title").gameObject.GetComponent<TextMeshProUGUI>().text =
                                    m.prizeBundles[0].title;
                                b.transform.Find("subtitle").gameObject.GetComponent<TextMeshProUGUI>().text = "";
                            }

                        status.text = "Prizes";
                    }
                    else
                    {
                        status.text = "No Prizes";
                    }
                },
                null));
        }

        private void pButtonPressed(string prizeId)
        {
            prizeDetailsCanvas.GetComponent<PrizeDetailsScript>().prizeId = prizeId;
            prizeDetailsCanvas.SetActive(true);
            prizeCanvas.SetActive(false);
        }

        private void GetPrizeDetails(string awardedPrizeId, string prizeId)
        {
            var url = "https://api.amazongameon.com/v1/prizes/" + prizeId;
            GetPrizeDetailsResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetPrizeDetailsResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    prizeDetailsText.text = response.title + "\n\n" +
                                            response.description;
                    if (response.imageUrl != null) StartCoroutine(getButtonImage(prizeImage, response.imageUrl));
                    claimButton.onClick.AddListener(delegate { ClaimPrize(awardedPrizeId); });
                    claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Get Prize";
                    claimButton.interactable = true;
                }, null));
        }

        private void ClaimPrize(string awardedPrizeId)
        {
            //this is the URL for the registering the device
            var url = "https://api.amazongameon.com/v1/prizes/claim";
            ClaimPrizeListResponse response = null;
            var request = new ClaimPrizeListRequest();
            request.awardedPrizeIds = new string[1];
            request.awardedPrizeIds[0] = awardedPrizeId;
            var param = JsonUtility
                .ToJson(request); // Demonstrating a request to json here. You can use whatever Json utility.
            param = "{\"awardedPrizeIds\" : [ \"" + awardedPrizeId + "\"]}";
            //we start a coroutine using post, and have the callback function written below
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility.FromJson<ClaimPrizeListResponse>(successCallback);
                    if (response.prizes[0].status != "FULFILLED")
                    {
                        if (response.prizes[0].prizeInfo != null)
                        {
                            RedeemPrize(response.prizes[0].prizeInfo, response, awardedPrizeId);
                        }
                        else
                        {
                            if (response.prizes[0].encryptedPrizeInfoV2 != null)
                            {
                                RedeemPrize(
                                    GameOnTools.Crypto.Decrypt(PrivateKey, response.prizes[0].encryptedPrizeInfoV2),
                                    response, awardedPrizeId);
                            }
                            else
                            {
                                if (response.prizes[0].encryptedPrizeInfo != null)
                                    RedeemPrize(
                                        GameOnTools.Crypto.Decrypt(PrivateKey, response.prizes[0].encryptedPrizeInfo),
                                        response, awardedPrizeId);
                            }
                        }
                    }
                    else
                    {
                        claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Already Fulfilled";
                        claimButton.interactable = false;
                    }
                },
                null));
        }

        private void RedeemPrize(string prizeUrl, ClaimPrizeListResponse response, string awardedPrizeId)
        {
            if (response.prizes[0].prizeInfoType == "AMAZON_PHYSICAL")
            {
                Application.OpenURL(prizeUrl);
            }
            else
            {
                status.text = "claimed IAP";
                claimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Fulfilled";
                claimButton.interactable = false;
                FulfillPrize(awardedPrizeId);
            }
        }

        public void GetPrizeData(string matchId, string prizeId)
        {
            var url = "https://api.amazongameon.com/v1/matches/" + matchId;
            GetMatchDetailsResponse response = null;

            StartCoroutine(GameOnGetRequest(url,
                gameApiKey,
                successCallback =>
                {
                    response = JsonUtility
                        .FromJson<GetMatchDetailsResponse
                        >(successCallback); // Demonstrating a json to response here. You can use whatever Json utility.
                    GetPrizeDetails(response.awardedPrizes[0].awardedPrizeId, prizeId);
                },
                null));
        }

        private void FulfillPrize(string awardedPrizeId)
        {
            //this is the URL for the registering the device
            var url = "https://api.amazongameon.com/v1/prizes/fulfill";
            var request = new ClaimPrizeListRequest();
            request.awardedPrizeIds = new string[1];
            request.awardedPrizeIds[0] = awardedPrizeId;
            var param = JsonUtility
                .ToJson(request); // Demonstrating a request to json here. You can use whatever Json utility.
            param = "{\"awardedPrizeIds\" : [ \"" + awardedPrizeId + "\"]}";
            //we start a coroutine using post, and have the callback function written below
            StartCoroutine(GameOnPostRequest(url,
                param,
                gameApiKey,
                successCallback =>
                {
                    var response = JsonUtility.FromJson<ClaimPrizeListResponse>(successCallback);
                    Debug.Log(response.prizes[0].status);
                },
                null));
        }
    }
}