using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    Ukrainian = 0,
    English = 1
}

/// <summary>Small, project-wide Ukrainian / English string catalog.</summary>
public static class GameLocalization
{
    private const string LanguageKey = "Parsec.Settings.Language.v1";
    private static GameLanguage? cachedLanguage;
    public static event Action LanguageChanged;

    private static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
    {
        { "language_button", new[] { "Мова: УКР", "Language: EN" } },
        { "skins", new[] { "Скіни", "Skins" } },
        { "skins_locked", new[] { "Скіни — заблоковано", "Skins — locked" } },
        { "skin_title", new[] { "Вибір скафандра", "Choose your suit" } },
        { "skin_default", new[] { "Стандартний астронавт", "Default astronaut" } },
        { "skin_engineer", new[] { "Інженер", "Engineer" } },
        { "skin_commander", new[] { "Командир", "Commander" } },
        { "skin_default_desc", new[] { "Поточний скафандр", "Current suit" } },
        { "skin_free_desc", new[] { "Безкоштовний скін", "Free skin" } },
        { "skin_paid_desc", new[] { "Командир із червоним лазером", "Commander with a red laser" } },
        { "skin_select", new[] { "Вибрати", "Select" } },
        { "skin_selected", new[] { "Обрано: {0}", "Selected: {0}" } },
        { "skin_active", new[] { "Обрано", "Selected" } },
        { "crystals_count", new[] { "Кристали: {0}", "Crystals: {0}" } },
        { "skin_buy", new[] { "Купити за 60 кристалів", "Buy for 60 crystals" } },
        { "skin_owned", new[] { "Вибрати", "Select" } },
        { "skin_need_crystals", new[] { "Потрібно 60 кристалів", "You need 60 crystals" } },
        { "skin_unlock_hint", new[] { "Відкрий рівень 2, щоб вибирати скіни", "Unlock level 2 to choose skins" } },
        { "close", new[] { "Закрити", "Close" } },
        { "level_menu", new[] { "Меню рівнів", "Level menu" } },
        { "play", new[] { "Почати", "Play" } },
        { "continue_moon", new[] { "Далі: місячна база", "Continue: Moon base" } },
        { "level1", new[] { "1. Політ ракети", "1. Rocket flight" } },
        { "level2", new[] { "2. Місячна база", "2. Moon base" } },
        { "level3", new[] { "3. Мінігра запуску", "3. Rocket launch puzzle" } },
        { "completed", new[] { " (пройдено)", " (completed)" } },
        { "locked", new[] { " (закрито)", " (locked)" } },
        { "talk", new[] { "Поговорити", "Talk" } },
        { "unlock_flight", new[] { "Розблокувати політ", "Unlock flight" } },
        { "shoot", new[] { "Стріляти", "Shoot" } },
        { "claim_artifact", new[] { "Забрати артефакт", "Claim artifact" } },
        { "give_artifacts", new[] { "Віддати 5 артефактів", "Give 5 artifacts" } },
        { "open_minigame", new[] { "Відкрити мінігру", "Open minigame" } },
        { "need_rocket_key", new[] { "Потрібен ключ від ракети", "Rocket key required" } },
        { "research", new[] { "ДОСЛІДЖЕННЯ  {0}/{1}", "RESEARCH  {0}/{1}" } },
        { "learned", new[] { "Вивчено: {0} із {1}", "Learned: {0} of {1}" } },
        { "encyclopedia", new[] { "Енциклопедія Місяця", "Moon encyclopedia" } },
        { "wanderer_name", new[] { "Місячний мандрівник", "Lunar wanderer" } },
        { "rocket_name", new[] { "Ракета", "Rocket" } },
        { "wanderer_intro", new[]
        {
            "Не кожен, хто ступив на Місяць, готовий розкрити його таємниці. Поговори з п’ятьма мешканцями бази та забери їхні артефакти.\n\nПринеси мені весь набір — отримаєш 50 кристалів, а я відкрию шлях далі.\n\nЗібрано: {0}/5. Я зачекаю.",
            "Not everyone who steps on the Moon is ready to uncover its secrets. Talk to five base residents and collect their artifacts.\n\nBring me the full set and you'll get 50 crystals. Then I'll open the way forward.\n\nCollected: {0}/5. I'll be waiting."
        } },
        { "wanderer_ready", new[]
        {
            "Усі п’ять… Навіть я не був певен, що ти впораєшся.\n\nПередай мені артефакти — отримаєш 50 кристалів і ключ від ракети для наступного польоту.",
            "All five… I wasn't sure you could do it.\n\nGive me the artifacts and you'll receive 50 crystals and the rocket key for your next flight."
        } },
        { "wanderer_done", new[]
        {
            "Бачу, ти не такий уже й слабкий, мандрівнику. Ти відшукав усі п’ять реліквій — я тримаю слово.\n\nТвої 50 кристалів уже в гаманці, а ключ від ракети — у тебе. Тепер можна вирушати далі!",
            "You're stronger than you look, traveler. You found all five relics, and I keep my promises.\n\nYour 50 crystals are in your wallet and you have the rocket key. The way forward is open!"
        } },
        { "rocket_locked", new[]
        {
            "Ракета заблокована. Спершу збери п’ять артефактів, повернися до мандрівника та отримай ключ від ракети.",
            "The rocket is locked. Collect five artifacts, return to the wanderer, and get the rocket key first."
        } },
        { "rocket_unlocked", new[]
        {
            "Ключ від ракети прийнято. Заряди три контури на панелі запуску, щоб підготувати старт.",
            "Rocket key accepted. Charge the three circuits on the launch panel to prepare for takeoff."
        } },
        { "fact_helper_bot_name", new[] { "Робот-помічник", "Helper bot" } },
        { "fact_helper_bot_text", new[]
        {
            "Біп-буп! Попередження: обережно зі скафандрами біля панелей.\n\nУ 1969 році астронавти «Аполлона-11» випадково зламали тумблер запуску двигуна. Щоб повернутися додому, їм довелося замкнути контакт звичайним пластиковим фломастером!",
            "Beep-boop! Warning: keep spacesuits clear of the control panels.\n\nIn 1969, the Apollo 11 astronauts accidentally broke an engine-start switch. To get home, they bridged the contact with an ordinary plastic pen!"
        } },
        { "fact_orange_astronaut_name", new[] { "Помаранчевий астронавт", "Orange astronaut" } },
        { "fact_orange_astronaut_text", new[]
        {
            "Армований скотч рятує навіть у космосі!\n\nПід час місії «Аполлон-17» молотком випадково відламали крило ровера, і пил почав засипати прилади. Довелося майструвати нове крило з паперових карт та скотчу — воно витримало до кінця поїздки!",
            "Duct tape can save the day, even in space!\n\nDuring Apollo 17, a hammer accidentally broke a rover fender and dust began covering the instruments. The crew made a new fender from paper maps and tape—and it lasted for the rest of the trip!"
        } },
        { "fact_flying_drone_name", new[] { "Літаючий дрон", "Flying drone" } },
        { "fact_flying_drone_text", new[]
        {
            "«Бз-з-з... Помилка датування... 1976 рік. Вони думали, що стерли мені пам’ять після тієї висадки...\n\nКратер Дельпорт, 19° пд. ш., 117° сх. д. Ми бачили його на власні сенсори. Майже чотири кілометри чужого металу. Він пролежав там півтора мільярда років і просто... чекав»",
            "“Bzzzt... Date error... 1976. They thought they erased my memory after that landing...\n\nDelport crater, 19° S, 117° E. Our own sensors saw it. Nearly four kilometers of unknown metal. It lay there for one and a half billion years, just... waiting.”"
        } },
        { "fact_researcher_name", new[] { "Дослідник", "Researcher" } },
        { "fact_researcher_text", new[]
        {
            "Ти знав, що на Місяці вже проростала земна рослина?\n\nУ 2019 році всередині герметичної біосфери випустило листки насіння бавовнику! Щоправда, воно вижило лише до місячної ночі, коли мороз сягнув −190 °C.",
            "Did you know that an Earth plant once sprouted on the Moon?\n\nIn 2019, cotton seeds inside a sealed biosphere grew their first leaves! They survived only until lunar night, when the temperature dropped to −190 °C."
        } },
        { "fact_astronaut47_name", new[] { "Астронавт №47", "Astronaut #47" } },
        { "fact_astronaut47_text", new[]
        {
            "Прапор встановили на Місяці у 1969 році астронавти «Аполлона-11» — Ніл Армстронг і Базз Олдрін.\n\nТут немає вітру, тож прапор тримає металева перекладина. Сонце з часом вибілило тканину.",
            "Apollo 11 astronauts Neil Armstrong and Buzz Aldrin planted the flag on the Moon in 1969.\n\nThere is no wind here, so a metal crossbar holds it out. Sunlight has faded the fabric over time."
        } },
        { "minigame_instructions", new[] { "Натискай контури по порядку: 1 → 2 → 3", "Tap the circuits in order: 1 → 2 → 3" } },
        { "minigame_wrong_order", new[] { "Не той порядок — почни з 1", "Wrong order—start with 1" } },
        { "minigame_circuit_charged", new[] { "Контур {0} з 3 заряджено", "Circuit {0} of 3 charged" } },
        { "minigame_all_charged", new[] { "Усі контури заряджено", "All circuits charged" } },
        { "flight_success", new[] { "Політ успішний!\nРакета вирушила до наступної планети.", "Flight successful!\nThe rocket is on its way to the next planet." } },
        { "flight_success_short", new[] { "Політ успішний!\nГотуйся до зльоту.", "Flight successful!\nPrepare for takeoff." } },
        { "finish_flight", new[] { "Готово", "Done" } },
        { "return_lobby", new[] { "Повернутися в лобі", "Return to lobby" } },
        { "reset_progress", new[] { "Скинути прогрес", "Reset progress" } },
        { "reset_prompt", new[] { "Скинути весь прогрес?\nРівні, кристали й артефакти буде обнулено.", "Reset all progress?\nLevels, crystals, and artifacts will be cleared." } },
        { "cancel", new[] { "Скасувати", "Cancel" } },
        { "confirm_reset", new[] { "Так, скинути", "Yes, reset" } },
        { "tap_to_start", new[] { "Натисни, щоб почати", "Tap to start" } },
        { "tap", new[] { "НАТИСНИ!", "TAP!" } },
        { "win", new[] { "ПЕРЕМОГА", "WIN" } },
        { "lose", new[] { "ПОРАЗКА", "LOSE" } }
    };

    public static GameLanguage CurrentLanguage
    {
        get
        {
            if (!cachedLanguage.HasValue)
                cachedLanguage = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
            return cachedLanguage.Value;
        }
    }

    public static bool IsEnglish => CurrentLanguage == GameLanguage.English;

    public static string T(string key)
    {
        if (Strings.TryGetValue(key, out string[] values))
            return values[(int)CurrentLanguage];
        return key;
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language) return;
        cachedLanguage = language;
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static void ToggleLanguage()
    {
        SetLanguage(IsEnglish ? GameLanguage.Ukrainian : GameLanguage.English);
    }
}
