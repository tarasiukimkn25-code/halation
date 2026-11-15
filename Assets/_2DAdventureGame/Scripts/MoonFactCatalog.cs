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
    // Add future discoveries here. Each fact needs a unique ID and an NPC with the same ID.
    private static readonly MoonFact[] s_Facts =
    {
        new MoonFact(
            id: "helper_bot",
            characterName: "Робот-помічник",
            text:
                "Біп-буп! Попередження: обережно зі скафандрами біля панелей.\n\n" +
                "У 1969 році астронавти «Аполлона-11» випадково зламали тумблер запуску двигуна. " +
                "Щоб повернутися додому, їм довелося замкнути контакт звичайним пластиковим фломастером!"
        ),

        new MoonFact(
            id: "orange_astronaut",
            characterName: "Помаранчевий астронавт",
            text:
                "Армований скотч рятує навіть у космосі!\n\n" +
                "Під час місії «Аполлон-17» ми випадково відламали молотком крило ровера, і пил почав засипати прилади. " +
                "Довелося майструвати нове крило з паперових карт та скотчу — витримало до кінця поїздки!"
        ),

        new MoonFact(
            id: "flying_drone",
            characterName: "Літаючий дрон",
            text:
                "«Бз-з-з... Помилка датування... 1976 рік. Вони думали, що стерли мені пам'ять після тієї висадки...\n\n" +
                "Кратер Дельпорт, 19° пд. ш., 117° сх. д. Ми бачили його на власні сенсори. " +
                "Майже чотири кілометри чужого металу. Він пролежав там півтора мільярда років і просто... чекав»"
        ),

        new MoonFact(
            id: "researcher",
            characterName: "Дослідник",
            text:
                "Ти знав, що на Місяці вже проростала земна рослина?\n\n" +
                "У 2019 році всередині герметичної біосфери випустило листки насіння бавовнику! " +
                "Правда, вижило воно лише до настання місячної ночі, коли мороз вдарив до -190 °C."
        ),

        new MoonFact(
            id: "astronaut47",
            characterName: "Астронавт №47",
            text:
                "Прапор встановили на Місяці у 1969 році астронавти «Аполлона-11» — Ніл Армстронг і Базз Олдрін.\n\n" +
                "Тут немає вітру, тож прапор тримає металева перекладина. Сонце з часом вибілило тканину."
        )
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