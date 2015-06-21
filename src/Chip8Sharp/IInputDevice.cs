using System;

namespace Chip8Sharp.Core
{
    /// <summary>
    /// Hexadecimal keyboard for the Chip8.
    /// Events for interupts, property for polling.
    /// </summary>
    public interface IInputDevice
    {
        event EventHandler<InputKey> KeyPressed;
        event EventHandler<InputKey> KeyReleased;
        InputKey? PressedKey { get; }
    }
}
