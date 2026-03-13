/*
Algorithm:
This factory creates the internal chess position and wraps it in the adapter used by the rest of the project.
It gives you one place to switch implementations later without changing the rest of the codebase.
*/

public static class ThirdPartyChessFactory
{
    public static IChessCore CreateStartingPositionAdapter()
    {
        InternalChessPosition position = InternalChessPosition.CreateStartingPosition();
        return new ChessCoreAdapter(position);
    }

    public static IChessCore CreateEmptyAdapter()
    {
        InternalChessPosition position = InternalChessPosition.CreateEmpty();
        return new ChessCoreAdapter(position);
    }
}
