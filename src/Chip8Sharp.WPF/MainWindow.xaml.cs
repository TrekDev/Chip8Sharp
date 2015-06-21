using Chip8Sharp.Core;
using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chip8Sharp.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInputDevice, IOutputDevice
    {
        public event EventHandler<InputKey> KeyPressed;
        public event EventHandler<InputKey> KeyReleased;

        private readonly Chip8 _chip8;
        private readonly WriteableBitmap _writeableBitmap;
        private readonly Random _random;
        private readonly Int32Rect _refreshArea;
        public MainWindow()
        {
            InitializeComponent();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            _writeableBitmap = new WriteableBitmap(640, 320, 1, 1, PixelFormats.BlackWhite, null);
            canvas.Source = _writeableBitmap;
            _random = new Random();
            _refreshArea = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);

            _chip8 = new Chip8("./pong2.c8", this, this);

            Task.Run(() =>
            {
                _chip8.Run();
            });
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            InputKey inputKey;
            if (!TryGetInputKeyForKey(e.Key, out inputKey)) return;

            if (PressedKey == inputKey)
            {
                PressedKey = null;
            }

            var handler = KeyReleased;

            if (handler != null)
            {
                handler(this, inputKey);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            InputKey inputKey;
            if (!TryGetInputKeyForKey(e.Key, out inputKey)) return;

            if (PressedKey == null)
            {
                PressedKey = inputKey;
            }

            var handler = KeyPressed;

            if (handler != null)
            {
                handler(this, inputKey);
            }
        }

        public InputKey? PressedKey { get; private set; }

        private bool TryGetInputKeyForKey(Key key, out InputKey inputKey)
        {
            switch (key)
            {
                case Key.A:
                    inputKey = InputKey.A;
                    return true;
                case Key.B:
                    inputKey = InputKey.B;
                    return true;
                case Key.C:
                    inputKey = InputKey.C;
                    return true;
                case Key.D:
                    inputKey = InputKey.D;
                    return true;
                case Key.D0:
                    inputKey = InputKey.Zero;
                    return true;
                case Key.D1:
                    inputKey = InputKey.One;
                    return true;
                case Key.D2:
                    inputKey = InputKey.Two;
                    return true;
                case Key.D3:
                    inputKey = InputKey.Three;
                    return true;
                case Key.D4:
                    inputKey = InputKey.Four;
                    return true;
                case Key.D5:
                    inputKey = InputKey.Five;
                    return true;
                case Key.D6:
                    inputKey = InputKey.Six;
                    return true;
                case Key.D7:
                    inputKey = InputKey.Seven;
                    return true;
                case Key.D8:
                    inputKey = InputKey.Eight;
                    return true;
                case Key.D9:
                    inputKey = InputKey.Nine;
                    return true;
                case Key.E:
                    inputKey = InputKey.E;
                    return true;
                case Key.F:
                    inputKey = InputKey.F;
                    return true;
                default:
                    inputKey = InputKey.A;
                    return false;
            }
        }

        public void Draw(byte[] displayBuffer)
        {
            Application.Current.Dispatcher.Invoke(() => _writeableBitmap.WritePixels(_refreshArea, displayBuffer, 64, 0));
        }


        public void Beep()
        {
            SystemSounds.Beep.Play();
        }
    }
}
