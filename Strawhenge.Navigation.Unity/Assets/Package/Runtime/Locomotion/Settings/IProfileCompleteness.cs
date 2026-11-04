namespace Strawhenge.Navigation.Unity
{
    // Implemented by the pivot and landing profiles: false when the profile has no pivot or landing
    // asset assigned, so it can't say which animation to play. ProfileSources leaves those out.
    interface IProfileCompleteness
    {
        bool IsComplete { get; }
    }
}
