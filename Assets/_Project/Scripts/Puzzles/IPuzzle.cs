using System;

public interface IPuzzle
{
    event Action OnPuzzleSuccess;

    void Show();
    void Hide();
}