using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Rock_paper_scissors_bot
{
    internal static class DbController
    {
        private const string dbRoot = "users", jsonNull = "null";
        private static FirebaseClient client;

        static DbController()
        {
            string[] credentials = File.ReadAllLines("firebase_credentials.txt");
            FirebaseConfig config = new FirebaseConfig() { BasePath = credentials[0], AuthSecret = credentials[1] };
            client = new FirebaseClient(config); 
        }
        static public bool UserExists(long userId)
        {
            return client.Get($"{dbRoot}/{userId}/stats").Body != jsonNull;
        }
        static public void AddOrResetUser(long userId) => client.Set($"{dbRoot}/{userId}/stats", new UserStats());
    

        static public GameState GetUserState(long userId)
        {
            return (GameState)JsonConvert.DeserializeObject<byte>(client.Get($"{dbRoot}/{userId}/stats/{nameof(UserStats.gameState)}").Body);
        }
        static public void SetUserState(long userId, GameState state)
        {
            client.Set($"{dbRoot}/{userId}/stats/{nameof(UserStats.gameState)}", (byte)state);
        }

        static public void SetDecisionOption(long userId, int optionMsgId, bool ifHumanWins)
        {
            client.Set($"{dbRoot}/{userId}/stats/{nameof(UserStats.choice)}/{(ifHumanWins ? "human_w" : "bot_w")}", optionMsgId);
        }

        static public bool? ModifyUserScore(long userId, sbyte scoreDelta)
        {
            string path = $"{dbRoot}/{userId}/stats";
            UserStats stats = JsonConvert.DeserializeObject<UserStats>(client.Get(path).Body);
            stats.humanScore += scoreDelta;
            if(stats.gameSteps < 2)
            {
                stats.gameSteps++;
                client.Set(path, stats);
                return null;
            }
            return stats.humanScore > 0;
        }
        static public int TryGetWinningOptionId(long userId, bool humanWon)
        {
            Dictionary<string, int> options = JsonConvert.DeserializeObject<Dictionary<string, int>>(client.Get($"{dbRoot}/{userId}/stats/{nameof(UserStats.choice)}").Body);
            if(options == null) { return -1; }
            return humanWon ? options["human_w"] : options["bot_w"];
        }
        static public bool GetUserViewMode(long userId)
        {
            string bool_as_str = client.Get($"{dbRoot}/{userId}/view").Body;
            if(bool_as_str == jsonNull) { return false; }
            return Convert.ToBoolean(bool_as_str);
        }
        static public void SetUserViewMode(long userId, bool viewModeHands) => client.Set($"{dbRoot}/{userId}/view", viewModeHands);

        public static void DeleteUser(long userId) => client.Delete($"{dbRoot}/{userId}/");     
    }
}
