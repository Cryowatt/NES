using NES.CPU;
using System;
using Veldrid;

namespace NES
{
    public class KeyboardInputDevice : IInputDevice
    {
        private InputSnapshot lastInput;
        // A, B, Select, Start, Up, Down, Left, Right.
        private readonly Key[] keyOrder = new[]
        {
            Key.A, Key.B, Key.ShiftRight, Key.Enter, Key.Up, Key.Down, Key.Left, Key.Right
        };
        private readonly byte[] inputState = new byte[8];
        private int readIndex = 0;

        public bool IsStrobeEnabled { get; set; }

        public void Latch()
        {
            foreach (var keyEvent in lastInput.KeyEvents)
            {
                var stateIndex = keyEvent.Key switch
                {
                    Key.A => 0,
                    Key.B => 1,
                    Key.ShiftRight => 2,
                    Key.Enter => 3,
                    Key.Up => 4,
                    Key.Down => 5,
                    Key.Left => 6,
                    Key.Right => 7,
                    _ => -1,
                };

                if (stateIndex < 0)
                {
                    continue;
                }

                inputState[stateIndex] = (byte)(keyEvent.Down ? 1 : 0);
            }
        }

        public byte ReadNext()
        {
            if (this.IsStrobeEnabled)
            {
                readIndex = 0;
            }

            if (readIndex < 8)
            {
                return inputState[readIndex++];
            }
            else
            {
                return 0;
            }
        }

        public void SetInputSnapshot(InputSnapshot input)
        {
            this.lastInput = input;
        }
    }
}
