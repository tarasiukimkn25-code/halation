using System.Collections.Generic;

public sealed class MoonFact
{
    public string Id { get; }
    public string CharacterName { get; }
    public string Text { get; }

    public MoonFact(string id, string characterName, string text)
    {
        Id = id;
        CharacterName = characterName;
        Text = text;
    }
}

public static class MoonFactCatalog
{
    // Build strings on access so changing language updates dialogue and encyclopedia entries.
    private static MoonFact[] BuildFacts() => new[]
    {
        new MoonFact(
            id: "helper_bot",
            characterName: GameLocalization.T("fact_helper_bot_name"),
            text: GameLocalization.T("fact_helper_bot_text")
        ),
        new MoonFact(
            id: "orange_astronaut",
            characterName: GameLocalization.T("fact_orange_astronaut_name"),
            text: GameLocalization.T("fact_orange_astronaut_text")
        ),
        new MoonFact(
            id: "flying_drone",
            characterName: GameLocalization.T("fact_flying_drone_name"),
            text: GameLocalization.T("fact_flying_drone_text")
        ),
        new MoonFact(
            id: "researcher",
            characterName: GameLocalization.T("fact_researcher_name"),
            text: GameLocalization.T("fact_researcher_text")
        ),
        new MoonFact(
            id: "astronaut47",
            characterName: GameLocalization.T("fact_astronaut47_name"),
            text: GameLocalization.T("fact_astronaut47_text")
        )
    };

    public static IReadOnlyList<MoonFact> Facts => BuildFacts();

    public static MoonFact Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        MoonFact[] facts = BuildFacts();
        for (int i = 0; i < facts.Length; i++)
        {
            if (facts[i].Id == id)
                return facts[i];
        }

        return null;
    }
}
