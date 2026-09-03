namespace Trivia_Game_Server;

public class TriviaSettings : TriviaTable
{
    public int QuestionsPerGame { get; set; }
    public float TimePerQuestion { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
}