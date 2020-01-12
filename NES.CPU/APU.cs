using System;

namespace NES.CPU
{
    public class NullInputDevice : IInputDevice
    {
        public bool IsStrobeEnabled { get; set; }

        public void Latch() { }

        public byte ReadNext() => 0;
    }

    public interface IInputDevice
    {
        bool IsStrobeEnabled { set; }

        void Latch();
        byte ReadNext();
    }

    public class APU : IBusDevice
    {
        private IInputDevice Input1;
        private IInputDevice Input2;

        public APU(IInputDevice input1 = null, IInputDevice input2 = null)
        {
            this.Input1 = input1 ?? new NullInputDevice();
            this.Input2 = input2 ?? new NullInputDevice();
        }

        public byte Read(Address address)
        {
            //if(address.Ptr == 0x4013)
            //{

            //}
            if (address.Ptr == 0x4016)
            {
                return ReadInput(this.Input1);
            }
            else if (address.Ptr == 0x4017)
            {
                return ReadInput(this.Input2);
            }
            else
            {
                return 0;
            }
        }

        private byte ReadInput(IInputDevice input)
        {
            return input.ReadNext();
        }

        public void Write(Address address, byte value)
        {
            if(address.Ptr == 0x4015)
            {
                // TODO: APU Status 
            }
            else if (address.Ptr == 0x4016)
            {
                this.Input1.IsStrobeEnabled = (value & 0x1) > 0;
            }
            else if (address.Ptr == 0x4017)
            {
                this.Input2.IsStrobeEnabled = (value & 0x1) > 0;
            }
            //else
            //{
            //    throw new System.NotImplementedException();
            //}
        }
    }
}