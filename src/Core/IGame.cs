namespace Shiron.OuroLab.Core;

public interface IGame {
    string Name { get; }
    int Rows { get; }
    int Columns { get; }
    int MaxClicks { get; }
    IValueConverter ValueConverter { get; }

    void NewGame();
    IReadOnlyDictionary<Sphere, double> GetPossibleSpheres(int index);
    Sphere Reveal(int index);
    bool IsRevealed(int index);
    bool IsVisible(int index);
    Sphere GetRevealedSphere(int index);

    bool IsSolved { get; }
    int Score { get; }
    int RevealedCount { get; }

    string? GoalDescription { get; }
    Sphere? GoalSphere { get; }
    bool GoalAchieved { get; }
    bool ConsumeClick(int index);
    int TheoreticalMaxScore { get; }

    IGame Fork();
    void ApplyHypothetical(int index, Sphere sphere);
}
