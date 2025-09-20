namespace Franz.Common.Errors
{
  public static class TestExceptions
  {
    // 🍌 Banana Republic
    public static Exception BananaRepublic() =>
        new Exception("🍌 Banana Republic Exception: simulated DB meltdown!");
    public static Exception BananaRepublicFr() =>
        new Exception("🍌 Exception Banana Republic : effondrement simulé de la base de données !");

    // 🥤 Monster
    public static Exception Monster() =>
        new Exception("🥤 Sorry this demo needs to fail, get a can of Monster!");
    public static Exception MonsterFr() =>
        new Exception("🥤 Désolé, cette démo doit échouer, prends une canette de Monster !");

    // 🍸 Vodka Coffee Pot
    public static Exception VodkaCoffeePot() =>
        new Exception("🍸 Hold my vodka, this system was a coffee pot!");
    public static Exception VodkaCoffeePotFr() =>
        new Exception("🍸 Tiens ma vodka, ce système était une cafetière !");

    // 👋 Friendly Reminder
    public static Exception FriendlyReminder() =>
        new Exception("👋 Just a friendly reminder for you to take a break ☕");
    public static Exception FriendlyReminderFr() =>
        new Exception("👋 Petit rappel amical : prends une pause ☕");
  }
}
