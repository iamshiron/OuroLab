namespace Shiron.OuroLab.Core;

public interface IGame {
    string Name { get; }
    int Rows { get; }
    int Columns { get; }
    IValueConverter ValueConverter { get; }

    void NewGame();
    IReadOnlySet<Sphere> GetPossibleSpheres(int index);
    Sphere Reveal(int index);
    bool IsRevealed(int index);
    Sphere GetRevealedSphere(int index);

    bool IsSolved { get; }
    int Score { get; }
    int RevealedCount { get; }

    string? GoalDescription { get; }
    bool GoalAchieved { get; }
    bool ConsumeClick(int index);
    int TheoreticalMaxScore { get; }
}
