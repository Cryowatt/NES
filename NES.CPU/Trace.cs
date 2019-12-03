namespace NES.CPU
{
    public class Trace
    {
        private const string WriteSymbol = "<=";
        private const string ReadSymbol = "=>";
        public CpuRegisters Registers => this.cpu.Registers;

        private readonly Ricoh2A cpu;

        public Trace(Ricoh2A cpu)
        {
            this.cpu = cpu;
        }

        public Address BusAddress { get; internal set; }
        public byte BusValue { get; internal set; }
        public bool BusWrite { get; internal set; }

        public override string ToString()
        {
            //return "F381  C8        INY                             A:1B X:02 Y:EE P:67 SP:FB PPU: 70,208 CYC:23673";
            return $"{BusAddress} {(BusWrite ? WriteSymbol : ReadSymbol)} #{BusValue:X2} \t{Registers}";
        }
    }
}
