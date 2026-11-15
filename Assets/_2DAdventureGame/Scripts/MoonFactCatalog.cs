using System.Collections.Generic;

public sealed class MoonFact
{
    public string Id { get; }
    public string CharacterName { get; }
    public string Text { get; }

    // Keep Text empty until the encyclopedia copy is ready to be written.
    public MoonFact(string id, string characterName, string text)
    {
        Id = id;
        CharacterName = characterName;
        Text = text;
    }
}

public static class MoonFactCatalog
{
    // Add future discoveries here. Each fact needs a unique ID and an NPC with the same ID.
    private static readonly MoonFact[] s_Facts =
    {
        new MoonFact(
            "helper_bot",
            "Робот-помічник",
            ""),
        new MoonFact(
            "orange_astronaut",
            "Помаранчевий астронавт",
            ""),
        new MoonFact(
            "flying_drone",
            "Літаючий дрон",
            ""),
        new MoonFact(
            "researcher",
            "Дослідник",
            ""),
        new MoonFact(
            "astronaut47",
            "Астронавт №47",
            "Прапор встановили на Місяці у 1969 році астронавти «Аполлона-11» — Ніл Армстронг і Базз Олдрін. Тут немає вітру, тож прапор тримає металева перекладина. Сонце з часом вибілило тканину.")
    };

    public static IReadOnlyList<MoonFact> Facts => s_Facts;

    public static MoonFact Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < s_Facts.Length; i++)
        {
            if (s_Facts[i].Id == id)
                return s_Facts[i];
        }

        return null;
    }
}
