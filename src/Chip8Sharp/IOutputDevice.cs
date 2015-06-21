
namespace Chip8Sharp.Core
{
    /// <summary>
    /// Monochrome 64x32 display and audio output for Chip8
    /// </summary>
    public interface IOutputDevice
    {
        void Draw(byte[] displayBuffer);
        void Beep();
    }
}
