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

using Behaviors.GameOn.models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Behaviors.GameOn
{
    //This class is attached to the the tournament details canvas and is used to manage the content there
    public class TournamentManager : MonoBehaviour
    {
        public Button cancel;
        public GameObject content;
        public GameOnManager gameOnManager;
        public Button joinButton;
        public Tournament T;

        public GameObject tList;
        // Use this for initialization

        private void OnEnable()
        {
            joinButton.interactable = false;
            joinButton.GetComponentInChildren<TextMeshProUGUI>().text = "Wait";
            content.GetComponent<TextMeshProUGUI>().text = T.title;
            cancel.onClick.AddListener(GoBack);
            if (T.streamingPlatform == null)
            {
                gameOnManager.GetTournamentData(T.tournamentId);
                joinButton.onClick.AddListener(JoinTournament);
            }
            else
            {
                gameOnManager.GetPlayerTournamentData(T);
                joinButton.onClick.AddListener(JoinPlayerTournament);
            }
        }

        private void GoBack()
        {
            tList.SetActive(true);
            gameObject.SetActive(false);
            gameOnManager.status.text = "Tournaments";
        }

        private void JoinTournament()
        {
            gameOnManager.EnterTournament(T.tournamentId, T.title, false);
        }

        private void JoinPlayerTournament()
        {
            gameOnManager.EnterPlayerTournament(T.tournamentId, T.title);
        }
    }
}