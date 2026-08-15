namespace Trivia_Game_Server;

public class TriviaAnswer : TriviaTable
{
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = "";
    public bool IsCorrectAnswer { get; set; }
}