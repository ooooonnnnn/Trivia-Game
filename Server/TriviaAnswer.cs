namespace Trivia_Game_Server;

public class TriviaAnswer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = "";
    public bool IsCorrectAnswer { get; set; }
}