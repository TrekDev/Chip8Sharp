using Chip8Sharp.Core;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chip8Sharp.Forms
{
    public partial class Form1 : Form, IInputDevice, IOutputDevice
    {
        Chip8 _chip8;
        private const int DisplayScale = 5;
        public Form1()
        {
            InitializeComponent();

            _chip8 = new Chip8("./game_file_here.c8", this, this);

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            Task.Run(() =>
            {
                _chip8.Run();
            });
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            InputKey inputKey;
            if (!TryGetInputKeyForKey(e.KeyCode, out inputKey)) return;

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
            if (!TryGetInputKeyForKey(e.KeyCode, out inputKey)) return;

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

        public event EventHandler<InputKey> KeyPressed;

        public event EventHandler<InputKey> KeyReleased;

        public InputKey? PressedKey
        {
            get;
            private set;
        }

        private bool TryGetInputKeyForKey(Keys key, out InputKey inputKey)
        {
            switch (key)
            {
                case Keys.A:
                    inputKey = InputKey.A;
                    return true;
                case Keys.B:
                    inputKey = InputKey.B;
                    return true;
                case Keys.C:
                    inputKey = InputKey.C;
                    return true;
                case Keys.D:
                    inputKey = InputKey.D;
                    return true;
                case Keys.D0:
                    inputKey = InputKey.Zero;
                    return true;
                case Keys.D1:
                    inputKey = InputKey.One;
                    return true;
                case Keys.D2:
                    inputKey = InputKey.Two;
                    return true;
                case Keys.D3:
                    inputKey = InputKey.Three;
                    return true;
                case Keys.D4:
                    inputKey = InputKey.Four;
                    return true;
                case Keys.D5:
                    inputKey = InputKey.Five;
                    return true;
                case Keys.D6:
                    inputKey = InputKey.Six;
                    return true;
                case Keys.D7:
                    inputKey = InputKey.Seven;
                    return true;
                case Keys.D8:
                    inputKey = InputKey.Eight;
                    return true;
                case Keys.D9:
                    inputKey = InputKey.Nine;
                    return true;
                case Keys.E:
                    inputKey = InputKey.E;
                    return true;
                case Keys.F:
                    inputKey = InputKey.F;
                    return true;
                default:
                    inputKey = InputKey.A;
                    return false;
            }
        }

        public void Draw(byte[] displayBuffer)
        {
            using (var graphics = CreateGraphics())
            {
                using (var blackBrush = new SolidBrush(Color.Black))
                {
                    using (var whiteBrush = new SolidBrush(Color.White))
                    {
                        for (int i = 0; i < displayBuffer.Length; i++)
                        {
                            int x = i % 64;
                            int y = (int)Math.Floor(i / 64.0);
                            if(displayBuffer[i] == 255)
                            {
                                graphics.FillRectangle(whiteBrush, new Rectangle(x * DisplayScale, y * DisplayScale, 1 * DisplayScale, 1 * DisplayScale));
                            }
                            else
                            {
                                graphics.FillRectangle(blackBrush, new Rectangle(x * DisplayScale, y * DisplayScale, 1 * DisplayScale, 1 * DisplayScale));
                            }
                        }
                    }
                }
            }
        }

        public void Beep()
        {
        }
    }
}
