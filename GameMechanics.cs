using System.Collections.Generic;

namespace Rock_paper_scissors_bot
{
    enum GameState : byte { Neutral, Option1Request, Option2Request, Playing };
    public struct UserStats
    {
        public byte gameState;
        public byte gameSteps;
        public sbyte humanScore;
        public Dictionary<string, int> choice;
    }
    public class GameMechanics
    {
        public const byte NumberOfGestures = 4;
        public static readonly string[] Hands = new string[NumberOfGestures] { "✌", "✊", "🤚", "👌" },
                                        RealThings = new string[NumberOfGestures] { "✂",  "🪨", "📝", "🚰" };

        public const string StartCommand = "/start", ChooseCommand = "/choose", StopCommand = "/stop", ChangeEmojiCommand = "/emoji";
                            /*MenuText = "Могут быть такие команды:\n"+
                                        StartCommand + " - запуск бота и вывод меню\n"+
                                        ChooseCommand + " - задать выбор между 2 сообщениями перед игрой. Сообщения могут быть любые, не только текст\n"+
                                        StopCommand + " - прервать и сбросить игру\n"+
                                        ChangeEmojiCommand + " - поменять внешний вид смайликов\n";*/

        private static sbyte[][] userScoreDeltasMatrix = new sbyte[NumberOfGestures][]
        {
            new sbyte[NumberOfGestures] { 0, -1, 1, -1},
            new sbyte[NumberOfGestures] { 1, 0, -1,  1},
            new sbyte[NumberOfGestures] { -1, 1, 0, 1 },
            new sbyte[NumberOfGestures] { 1, 1, -1, 0 }
        };

        public static sbyte GetUserScoreDelta(byte userGestureIndex, byte botGestureIndex)
        {
            return userScoreDeltasMatrix[userGestureIndex][botGestureIndex];
        }
    }
}
