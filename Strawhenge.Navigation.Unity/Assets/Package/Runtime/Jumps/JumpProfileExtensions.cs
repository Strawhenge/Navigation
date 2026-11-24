namespace Strawhenge.Navigation.Unity
{
    // Matching used when a jump starts; nothing allocates.
    static class JumpProfileExtensions
    {
        public static bool Matches(this IJumpProfile jump, float speed) => jump.SpeedRange.Contains(speed);

        // Why settings can't use the profile, or null if they can.
        public static string FindProblem(this IJumpProfile jump) =>
            jump.Id == null ? "has no jump asset assigned" : null;
    }
}
