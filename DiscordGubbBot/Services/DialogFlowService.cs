using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Dialogflow.V2;
using Google.Apis.Auth.OAuth2;

namespace DiscordGubbBot.Services
{
    public class DialowFlowService
    {
        private string _userID;
        private string _projectId;
        private SessionsClient _sessionsClient;
        private SessionName _sessionName;

        public DialowFlowService(string userID, string projectId)
        {
            _userID = userID;
            _projectId = projectId;
#if DEBUG
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "X:\\Gulnaras mapp\\gubben\\DiscordGubbBot\\Keys\\sunfleetangulart-1485335477034-3b3d9282d6de.json");
#endif
        }

        private async Task CreateSession()
        {
            // Create client
            _sessionsClient = await SessionsClient.CreateAsync();
            // Initialize request argument(s)
            _sessionName = new SessionName(_projectId, _userID);

        }

        public async Task<QueryResult> CheckIntent(string userInput, string LanguageCode = "en")
        {
            await CreateSession();
            QueryInput queryInput = new QueryInput();
            var queryText = new TextInput();
            queryText.Text = userInput;
            queryText.LanguageCode = LanguageCode;
            queryInput.Text = queryText;

            // Make the request
            DetectIntentResponse response = await _sessionsClient.DetectIntentAsync(_sessionName, queryInput);
            return response.QueryResult;
        }
    }
}
