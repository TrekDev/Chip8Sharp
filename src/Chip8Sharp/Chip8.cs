using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Chip8Sharp.Core
{
    public class Chip8
    {
        /// <summary>
        /// 4096 byte main _memory
        /// </summary>
        private readonly byte[] _memory;

        /// <summary>
        /// 16 Registers V0 -> VF
        /// </summary>
        private readonly byte[] _registers;

        /// <summary>
        /// Pointer to the current instruction to be executed (a.k.a program counter)
        /// </summary>
        private UInt16 _instructionPointer;

        /// <summary>
        /// The address register, I
        /// </summary>
        private UInt16 _addressRegister;

        /// <summary>
        /// Pointer to current subroutine address on stack
        /// </summary>
        private byte _stackPointer;

        /// <summary>
        /// Stack (subroute addresses only), 12 levels of nesting.
        /// </summary>
        private readonly UInt16[] _stack;

        /// <summary>
        /// Display buffer;
        /// </summary>
        private readonly byte[] _displayBuffer;

        private byte _delayTimer;
        private byte _soundTimer;

        private readonly IOutputDevice _outputDevice;
        private readonly IInputDevice _inputDevice;
        private readonly Random _random;

        private const int ProgramMemoryOffset = 0x200; //Where the program begins in _memory after being loaded.
        private const int FontMemoryOffset = 0x50;
        private const int MemorySize = 4096; //bytes;
        private const int StackDepth = 16;
        private const int RegisterCount = 0x10; //16 registers V0->VF
        private const int RefreshRate = 120000; //Hz

        public const int DisplayHeight = 32; //pixels
        public const int DisplayWidth = 64; //pixels

        public Chip8(string romPath, IInputDevice inputDevice, IOutputDevice outputDevice)
        {
            _memory = new byte[MemorySize];

            //Load font data into _memory
            using (var memoryStream = new MemoryStream(FontSet))
            {
                memoryStream.Read(_memory, FontMemoryOffset, _memory.Length - FontMemoryOffset);
            }

            //Load program into _memory
            using (var fileStream = File.OpenRead(romPath))
            {
                fileStream.Read(_memory, ProgramMemoryOffset, _memory.Length - ProgramMemoryOffset);
            }

            _registers = new byte[RegisterCount];
            _stack = new UInt16[StackDepth];
            _displayBuffer = new byte[DisplayWidth * DisplayHeight];


            _instructionPointer = ProgramMemoryOffset;

            _random = new Random();
            _inputDevice = inputDevice;
            _outputDevice = outputDevice;
        }

        public void Run()
        {
            TimeSpan refreshSeconds = TimeSpan.FromSeconds(1.0 / RefreshRate);

            while (true)
            {
                Step();
                Thread.Sleep(refreshSeconds);
            }
        }

        public void Step()
        {
            //Opcode is 2 bytes long.
            UInt16 opcode = (UInt16)(_memory[_instructionPointer] << 8 | _memory[_instructionPointer + 1]);

            Debug.WriteLine("Step: InstructionPointer = 0x{0:X}, OpCode = 0x{1:X}", _instructionPointer, opcode);

            //Chip8 has 35 opcodes -> https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    {
                        switch (opcode & 0x00FF)
                        {
                            case 0x00E0: //clear screen
                                {
                                    for (int i = 0; i < _displayBuffer.Length; i++)
                                    {
                                        _displayBuffer[i] = 0x0;
                                    }

                                    _outputDevice.Draw(_displayBuffer);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x00EE: //return from subroutine
                                {
                                    _stackPointer--;
                                    _instructionPointer = (UInt16)(_stack[_stackPointer] + 2);
                                    break;
                                }

                            default:
                                throw new InvalidOperationException(String.Format("Opcode 0x{0:X} not supported", opcode));
                        }
                        break;
                    }

                case 0x1000: //1NNN -> jump to address NNN;
                    {
                        _instructionPointer = (UInt16)(opcode & 0x0FFF);
                        break;
                    }

                case 0x2000: //2NNN -> call subroutine at address NNN
                    {
                        _stack[_stackPointer] = _instructionPointer;
                        _stackPointer++;
                        _instructionPointer = (UInt16)(opcode & 0x0FFF);
                        break;
                    }

                case 0x3000: //3XNN Skip the next instruction if VX equals NN
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int nn = opcode & 0x00FF;
                        if (_registers[x] == nn) _instructionPointer += 2;
                        _instructionPointer += 2;
                        break;
                    }

                case 0x4000: //4XNN Skip the next instruction if VX NOT equals NN
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int nn = opcode & 0x00FF;
                        if (_registers[x] != nn) _instructionPointer += 2;
                        _instructionPointer += 2;
                        break;
                    }

                case 0x5000: //5XY0 Skips the next instruction if VX equals VY.
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int y = (opcode & 0x00F0) >> 4;
                        if (_registers[x] == _registers[y]) _instructionPointer += 2;
                        _instructionPointer += 2;
                        break;
                    }

                case 0x6000: //6XNN Sets VX to NN
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int nn = opcode & 0x00FF;
                        _registers[x] = (byte)nn;
                        _instructionPointer += 2;
                        break;
                    }

                case 0x7000: //7XNN add NN to VX
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int nn = opcode & 0x00FF;
                        _registers[x] = (byte)(_registers[x] + nn);
                        _instructionPointer += 2;
                        break;
                    }

                case 0x8000:
                    {
                        switch (opcode & 0x000F)
                        {
                            case 0x0000: // 8XY0 set VX to value of VY
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;
                                    _registers[x] = _registers[y];
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0001: // 8XY1 set VX to VX | VY
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;
                                    _registers[x] = (byte)((_registers[x] | _registers[y]) & 0xFF);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0002: // 8XY2 set VX to VX & VY
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;
                                    _registers[x] = (byte)((_registers[x] & _registers[y]) & 0xFF);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0003: // 8XY3 set VX to VX ^ VY
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;
                                    _registers[x] = (byte)((_registers[x] ^ _registers[y]) & 0xFF);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0004: // 8XY4 Adds VY to VX. VF is set to 1 when carry applies else to 0
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;
                                    if (_registers[y] > 0xFF - _registers[x])
                                    {
                                        _registers[0xF] = 1;
                                    }
                                    else
                                    {
                                        _registers[0xF] = 0;
                                    }
                                    _registers[x] = (byte)((_registers[x] + _registers[y]) & 0xFF);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0005:
                                { //VY is subtracted from VX. VF is set to 0 when there is a borrow else 1
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;

                                    if (_registers[x] > _registers[y])
                                    {
                                        _registers[0xF] = 1;

                                    }
                                    else
                                    {
                                        _registers[0xF] = 0;

                                    }
                                    _registers[x] = (byte)((_registers[x] - _registers[y]) & 0xFF);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0006:
                                { //8XY6: Shift VX right by one, VF is set to the least significant bit of VX
                                    int x = (opcode & 0x0F00) >> 8;
                                    _registers[0xF] = (byte)(_registers[x] & 0x1);
                                    _registers[x] = (byte)(_registers[x] >> 1);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0007:
                                { //8XY7 Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                                    int x = (opcode & 0x0F00) >> 8;
                                    int y = (opcode & 0x00F0) >> 4;

                                    if (_registers[x] > _registers[y])
                                        _registers[0xF] = 0;
                                    else
                                        _registers[0xF] = 1;

                                    _registers[x] = (byte)((_registers[y] - _registers[x]) & 0xFF);

                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x000E:
                                { //8XYE Shifts VX left by one. VF is set to the value of the most significant bit of VX before the shift.
                                    int x = (opcode & 0x0F00) >> 8;
                                    _registers[0xF] = (byte)(_registers[x] & 0x80);
                                    _registers[x] = (byte)(_registers[x] << 1);
                                    _instructionPointer += 2;

                                    break;
                                }

                            default:
                                throw new InvalidOperationException(String.Format("Opcode 0x{0:X} not supported", opcode));
                        }

                        break;
                    }

                case 0x9000: //9XY0 skip next instruction if VX != VY
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int y = (opcode & 0x00F0) >> 4;
                        if (_registers[x] != _registers[y]) _instructionPointer += 2;
                        _instructionPointer += 2;
                        break;
                    }

                case 0xA000: //ANNN set I to NNN
                    {
                        _addressRegister = (UInt16)(opcode & 0x0FFF);
                        _instructionPointer += 2;
                        break;
                    }

                case 0xB000: //BNNN jump to address NNN plus v0
                    {
                        int nnn = opcode & 0x0FFF;
                        _instructionPointer = (UInt16)(nnn + _registers[0]);
                        break;
                    }

                case 0xC000: //CXNN set VX to random number masked by NN
                    {
                        int x = (opcode & 0x0F00) >> 8;
                        int nn = opcode & 0x00FF;
                        _registers[x] = (byte)(_random.Next(255) & nn);
                        _instructionPointer += 2;
                        break;
                    }

                case 0xD000:
                    { //DXYN
                        int x = _registers[(opcode & 0x0F00) >> 8];
                        int y = _registers[(opcode & 0x00F0) >> 4];
                        int height = opcode & 0x000F;

                        _registers[0xF] = 0;

                        for (int _y = 0; _y < height; _y++)
                        {
                            int line = _memory[_addressRegister + _y];
                            for (int _x = 0; _x < 8; _x++)
                            {
                                int pixel = line & (0x80 >> _x);
                                if (pixel != 0)
                                {
                                    int totalX = x + _x;
                                    int totalY = y + _y;

                                    totalX = totalX % 64;
                                    totalY = totalY % 32;

                                    int index = (totalY * 64) + totalX;

                                    if (_displayBuffer[index] == 255)
                                        _registers[0xF] = 255;

                                    _displayBuffer[index] ^= 255;
                                }
                            }
                        }

                        _outputDevice.Draw(_displayBuffer);
                        _instructionPointer += 2;
                        break;
                    }

                case 0xE000:
                    {
                        switch (opcode & 0x00FF)
                        {
                            case 0x009E:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    byte key = _registers[x];
                                    if (_inputDevice.PressedKey.HasValue && _inputDevice.PressedKey.Value == (InputKey)key)
                                    {
                                        _instructionPointer += 4;
                                    }
                                    else
                                    {
                                        _instructionPointer += 2;
                                    }
                                    break;
                                }

                            case 0x00A1:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    byte key = _registers[x];
                                    if (!_inputDevice.PressedKey.HasValue || _inputDevice.PressedKey.Value != (InputKey)key)
                                    {
                                        _instructionPointer += 4;
                                    }
                                    else
                                    {
                                        _instructionPointer += 2;
                                    }
                                    break;
                                }

                            default:
                                throw new InvalidOperationException(String.Format("Opcode 0x{0:X} not supported", opcode));
                        }
                        break;
                    }

                case 0xF000:
                    {
                        switch (opcode & 0x00FF)
                        {

                            case 0x0007:
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    _registers[x] = (byte)_delayTimer;
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x000A:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;

                                    if (_inputDevice.PressedKey.HasValue)
                                    {
                                        _registers[x] = (byte)_inputDevice.PressedKey.Value;
                                        _instructionPointer += 2;
                                        break;
                                    }

                                    break;
                                }

                            case 0x0015:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    _delayTimer = _registers[x];
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0018:
                                {
                                    int x = (opcode & 0x0F00) >> 8;
                                    _soundTimer = _registers[x];
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x001E:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    _addressRegister = (UInt16)(_addressRegister + _registers[x]);
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0029:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    int character = _registers[x];
                                    _addressRegister = (UInt16)(0x050 + (character * 5));
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0033:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    int value = _registers[x];
                                    int hundreds = (value - (value % 100)) / 100;
                                    value -= hundreds * 100;
                                    int tens = (value - (value % 10)) / 10;
                                    value -= tens * 10;
                                    _memory[_addressRegister] = (byte)hundreds;
                                    _memory[_addressRegister + 1] = (byte)tens;
                                    _memory[_addressRegister + 2] = (byte)value;
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0055:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    for (int i = 0; i <= x; i++)
                                    {
                                        _memory[_addressRegister + i] = _registers[i];
                                    }
                                    _instructionPointer += 2;
                                    break;
                                }

                            case 0x0065:
                                { 
                                    int x = (opcode & 0x0F00) >> 8;
                                    for (int i = 0; i <= x; i++)
                                    {
                                        _registers[i] = _memory[_addressRegister + i];
                                    }
                                    _addressRegister = (UInt16)(_addressRegister + x + 1);
                                    _instructionPointer += 2;
                                    break;
                                }

                            default:
                                throw new InvalidOperationException(String.Format("Opcode 0x{0:X} not supported", opcode));
                        }

                        break;
                    }

                default:
                    throw new InvalidOperationException(String.Format("Opcode 0x{0:X} not supported", opcode));
            }

            if (_soundTimer > 0)
            {
                _outputDevice.Beep();
                _soundTimer--;
            }

            if (_delayTimer > 0)
            {
                _delayTimer--;
            }
        }

        private static byte[] FontSet =
	    { 
	      0xF0, 0x90, 0x90, 0x90, 0xF0, // 0   
	      0x20, 0x60, 0x20, 0x20, 0x70, // 1
	      0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
	      0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
	      0x90, 0x90, 0xF0, 0x10, 0x10, // 4
	      0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
	      0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
	      0xF0, 0x10, 0x20, 0x40, 0x40, // 7
	      0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
	      0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
	      0xF0, 0x90, 0xF0, 0x90, 0x90, // A
	      0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
	      0xF0, 0x80, 0x80, 0x80, 0xF0, // C
	      0xE0, 0x90, 0x90, 0x90, 0xE0, // D
	      0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
	      0xF0, 0x80, 0xF0, 0x80, 0x80  // F
	    };
    }
}
