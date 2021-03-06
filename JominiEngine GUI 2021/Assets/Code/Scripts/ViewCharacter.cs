using ProtoMessageClient;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ViewCharacter : Controller
{
    [SerializeField] private Button btnAddToEntourage;
    [SerializeField] private Button btnRemoveFromEntourage;
    [SerializeField] private Button btnViewArmy;
    [SerializeField] private Button btnHireNPC;
    [SerializeField] private Button btnFireNPC;

    [SerializeField] private Text lblPageTitle;
    [SerializeField] private Text lblCharInfo;
    [SerializeField] private Text lblMessageForUser;

    [SerializeField] private InputField txtOffer;

    private ProtoCharacter currentlyViewedCharacter;
    private string fullName;

    // Start is called before the first frame update
    new void Start()
    {
        btnViewArmy.onClick.AddListener(BtnViewArmy);
        btnHireNPC.onClick.AddListener(BtnHireNPC);
        btnFireNPC.onClick.AddListener(BtnFireNPC);
        btnAddToEntourage.onClick.AddListener(BtnAddToEntourage);
        btnRemoveFromEntourage.onClick.AddListener(BtnRemoveFromEntourage);
        
        lblMessageForUser.text = "";
        txtOffer.interactable = false;
        btnViewArmy.interactable = false;
        btnHireNPC.interactable = false;
        btnFireNPC.interactable = false;
        btnAddToEntourage.interactable = false;
        btnRemoveFromEntourage.interactable = false;

        ProtoMessage reply = GetCharacterDetails(characterToViewID, tclient);
        if(reply.ResponseType == DisplayMessages.Success) {
            currentlyViewedCharacter = (ProtoCharacter)reply;
            fullName = currentlyViewedCharacter.firstName + " " + currentlyViewedCharacter.familyName;
            DisplayCharacterDetails();

            if(!string.IsNullOrWhiteSpace(currentlyViewedCharacter.armyID)) { // Viewed char is leading an army.
                btnViewArmy.interactable = true;
            }
            if(currentlyViewedCharacter is ProtoNPC) {
                var temp = currentlyViewedCharacter as ProtoNPC;

                if(temp.employer != null) {
                    if(temp.employer.charID.Equals(protoClient.playerChar.charID)) { // This NPC is employed by you.
                        btnFireNPC.interactable = true; // Can only fire NPCs you employ.

                        if(temp.inEntourage) { // Is in YOUR playerchar's entourage.
                            btnRemoveFromEntourage.interactable = true;
                        }
                        else if(temp.location.Equals(protoClient.playerChar.location)) { // Not in entourage but is in same place as playerchar.
                            btnAddToEntourage.interactable = true;
                        }
                    }
                    else { // Can try to poach NPCs hired by other players (not 100% sure this is true).
                        txtOffer.interactable = true;
                        btnHireNPC.interactable = true;
                    }
                }
                else { // Can hire NPCs who aren't employed.
                    txtOffer.interactable = true;
                    btnHireNPC.interactable = true;
                }
            }

            if(currentlyViewedCharacter is ProtoNPC && currentlyViewedCharacter.location == protoClient.playerChar.location) {
                
            }
        }
        else {
            DisplayMessageToUser("ERROR: Response type: " + reply.ResponseType.ToString());
        }

        if(string.IsNullOrWhiteSpace(currentlyViewedCharacter.armyID)) {
            btnViewArmy.interactable = false;
        }
    }

    void DisplayMessageToUser(string message) {
        lblMessageForUser.text = "[" + DateTime.Now.ToString("h:mm:ss tt") + "] " + message;
    }

    private void BtnAddToEntourage() {
        ProtoMessage reply = AddRemoveEntourage(characterToViewID, tclient);
        if(reply.ResponseType == DisplayMessages.Success) {
            DisplayMessageToUser(fullName + " has joined his liege's entourage.");
            btnAddToEntourage.interactable = false;
            btnRemoveFromEntourage.interactable = true;
        }
        else {
            DisplayMessageToUser("ERROR: Response type: " + reply.ResponseType.ToString());
        }
    }

    private void BtnRemoveFromEntourage() {
        ProtoMessage reply = AddRemoveEntourage(characterToViewID, tclient);
        if(reply.ResponseType == DisplayMessages.Success) {
            DisplayMessageToUser(fullName + " has left his liege's entourage.");
            btnAddToEntourage.interactable = true;
            btnRemoveFromEntourage.interactable = false;
        }
        else {
            DisplayMessageToUser("ERROR: Response type: " + reply.ResponseType.ToString());
        }
    }

    private void BtnFireNPC() {
        ProtoMessage reply = FireNPC(characterToViewID, tclient);
        if(reply.ResponseType == DisplayMessages.Success) {
            DisplayMessageToUser(currentlyViewedCharacter.firstName + " " + currentlyViewedCharacter.familyName + " has been fired from your employ.");
            btnFireNPC.interactable = false;
            txtOffer.interactable = true;
            btnHireNPC.interactable = true;
        }
        else {
            DisplayMessageToUser("ERROR: Response type: " + reply.ResponseType.ToString());
        }
    }

    private void BtnHireNPC() {
        string offer = txtOffer.text;
        if(string.IsNullOrWhiteSpace(offer)) {
            DisplayMessageToUser("You must make them a salary offer!");
        }
        else if(!uint.TryParse(offer, out uint bid)) {
            DisplayMessageToUser(fullName + " is interested in your money, not your words!");
        }
        else {
            ProtoMessage reply = HireNPC(characterToViewID, offer, tclient);
            switch(reply.ResponseType) {
                case DisplayMessages.CharacterOfferHigh:
                case DisplayMessages.CharacterOfferOk: {
                    DisplayMessageToUser(fullName + " has accepted your offer of employment.");
                    txtOffer.text = "";
                    txtOffer.interactable = false;
                    btnHireNPC.interactable = false;
                    break;
                }
                case DisplayMessages.CharacterOfferLow: {
                    DisplayMessageToUser(fullName + " has rejected your offer as too low.");
                    break;
                }
                case DisplayMessages.CharacterOfferHaggle: {
                    DisplayMessageToUser(fullName + " is insulted as your offer is lower than you previously offered!");
                    break;
                }
                case DisplayMessages.CharacterOfferAlmost: {
                    DisplayMessageToUser(fullName + " was very tempted by your offer but has ultimately decided to reject it.");
                    break;
                }
                case DisplayMessages.CharacterRecruitInsufficientFunds: {
                    DisplayMessageToUser("You do not have enough money to make that offer!");
                    break;
                }
                default: {
                    DisplayMessageToUser("ERROR: Response type: " + reply.ResponseType.ToString());
                    break;
                }
            }
        }
    }

    private void BtnViewArmy() {
        armyToViewID = currentlyViewedCharacter.armyID;
        GoToScene(SceneName.ViewArmy);
    }

    private void DisplayCharacterDetails() {
        lblPageTitle.text = currentlyViewedCharacter.firstName + " " + currentlyViewedCharacter.familyName;

        string lifeStatus = currentlyViewedCharacter.isAlive ? "Alive" : "Dead";
        string season = SeasonToString(currentlyViewedCharacter.birthSeason);
        string sex = currentlyViewedCharacter.isMale ? "Male" : "Female";

        lblCharInfo.text = ""
            + "\nStatus:\t\t" + lifeStatus
            + "\nLocation:\t\t" + fiefNames[currentlyViewedCharacter.location]
            + "\n"
            + "\nDate of Birth:\t" + season + " " + currentlyViewedCharacter.birthYear
            + "\nSex:\t\t\t" + sex
            + "\nNationality:\t" + currentlyViewedCharacter.nationality
            + "\nLanguage:\t\t" + currentlyViewedCharacter.language
            + "\n"
            ;
        if(!string.IsNullOrWhiteSpace(currentlyViewedCharacter.captor)) {
            lblCharInfo.text += "\nCaptor:\t\t\t" + currentlyViewedCharacter.captor;
        }
        if(currentlyViewedCharacter is ProtoPlayerCharacter) {
            var temp = currentlyViewedCharacter as ProtoPlayerCharacter;
            lblCharInfo.text += "\nHeir:\t\t\t" + temp.myHeir.charName;
        }
        else if(currentlyViewedCharacter is ProtoNPC) {
            var temp = currentlyViewedCharacter as ProtoNPC;
            if(temp.employer == null) {
                lblCharInfo.text += "\nEmployer:\t\tNone";
            }
            else {
                lblCharInfo.text += "\nEmployer:\t\t" + temp.employer.charName;
            }
        }
        if(!string.IsNullOrWhiteSpace(currentlyViewedCharacter.spouse)) {
            lblCharInfo.text += "\nSpouse:\t\t" + currentlyViewedCharacter.spouse;
        }
        if(!string.IsNullOrWhiteSpace(currentlyViewedCharacter.fiancee)) {
            lblCharInfo.text += "\nFiancé:\t\t\t" + currentlyViewedCharacter.fiancee;
        }
        lblCharInfo.text += ""
            + "\nFather:\t\t\t" + (string.IsNullOrWhiteSpace(currentlyViewedCharacter.father) ? "Unknown" : currentlyViewedCharacter.father)
            + "\nMother:\t\t" + (string.IsNullOrWhiteSpace(currentlyViewedCharacter.mother) ? "Unknown" : currentlyViewedCharacter.mother)
            + "\n"
            ;
        // Titles are fiefs that the character owns the 'title' of. Maybe like deeds?
        if(currentlyViewedCharacter.titles != null) {
            lblCharInfo.text += "\nTitles:";
            foreach(var title in currentlyViewedCharacter.titles) {
                string toPrint = fiefNames.TryGetValue(title, out toPrint) ? toPrint : title;
                lblCharInfo.text += "\n\t\t\t\t" + toPrint;
            }
            lblCharInfo.text += "\n";
        }
        if(currentlyViewedCharacter.ailments != null) {
            lblCharInfo.text += "\nAilments:";
            foreach(var ailment in currentlyViewedCharacter.ailments) {
                lblCharInfo.text += "\n\t\t\t\t" + ailment.value;
            }
            lblCharInfo.text += "\n";
        }
    }
}
