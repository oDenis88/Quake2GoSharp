namespace GoQuake2.Client;

public enum PlayerAction
{
    Forward,
    Backward,
    Left,
    Right,
    Fly,
    Quit
}

public enum PlayerKey
{
    W,
    A,
    S,
    D,
    Space,
    Escape
}

public sealed class InputState
{
    private readonly HashSet<PlayerKey> pressed = new();

    public void KeyDown(PlayerKey key)
    {
        pressed.Add(key);
    }

    public void KeyUp(PlayerKey key)
    {
        pressed.Remove(key);
    }

    public bool IsDown(PlayerKey key)
    {
        return pressed.Contains(key);
    }

    public void Clear()
    {
        pressed.Clear();
    }
}

public sealed class InputHandler
{
    public bool IsActive(PlayerAction action, InputState input)
    {
        return action switch
        {
            PlayerAction.Forward => input.IsDown(PlayerKey.W),
            PlayerAction.Backward => input.IsDown(PlayerKey.S),
            PlayerAction.Left => input.IsDown(PlayerKey.A),
            PlayerAction.Right => input.IsDown(PlayerKey.D),
            PlayerAction.Fly => input.IsDown(PlayerKey.Space),
            PlayerAction.Quit => input.IsDown(PlayerKey.Escape),
            _ => false
        };
    }
}
