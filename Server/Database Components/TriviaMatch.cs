namespace Trivia_Game_Server;

public class TriviaMatch : TriviaTable
{
    public bool IsActive { get; set; }
    public int DifficultyID { get; set; }
    public int Winner_PlayerID { get; set; }
    public bool IsCompleted { get; set; }
}