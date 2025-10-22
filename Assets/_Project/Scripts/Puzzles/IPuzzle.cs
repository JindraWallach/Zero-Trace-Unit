using System;

public interface IPuzzle
{
    event Action OnPuzzleSuccess;
    event Action OnPuzzleCancelled;

    void Show();
    void Hide();
}