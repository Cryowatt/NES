using System;
using System.IO;

namespace NES.CPU
{
    [Flags]
    public enum RomFlags
    {
        Vertical = 0x1,
        Battery = 0x2,
        Trainer = 0x4,
        FourScreen = 0x8,
    }

    public enum TVSystem
    {
        NTSC = 0x0,
        PAL = 0x1
    }

    public class RomImage
    {
        public byte ProgramRomSize { get; private set; }
        public byte CharacterRomSize { get; private set; }
        public RomFlags RomFlags { get; private set; }
        public int Mapper { get; private set; }
        public ConsoleType ConsoleType { get; private set; }
        public byte ProgramRamSize { get; private set; }
        public TVSystem TVSystem { get; private set; }
        public ReadOnlyMemory<byte> TrainerData { get; private set; }
        public ReadOnlyMemory<byte> ProgramRomData { get; private set; }
        public ReadOnlyMemory<byte> CharacterRomData { get; private set; }

        public static RomImage From(BinaryReader reader)
        {
            //Header(16 bytes)
            var image = ParseHeader(reader);

            //Trainer, if present(0 or 512 bytes)
            if(image.RomFlags.HasFlag(RomFlags.Trainer))
            {
                image.TrainerData = reader.ReadBytes(0x200).AsMemory();
            }

            //PRG ROM data(16384 * x bytes)
            image.ProgramRomData = reader.ReadBytes(0x4000 * image.ProgramRomSize).AsMemory();

            //CHR ROM data, if present(8192 * y bytes)
            image.CharacterRomData = reader.ReadBytes(0x2000 * image.CharacterRomSize).AsMemory();

            //PlayChoice INST-ROM, if present(0 or 8192 bytes)
            //PlayChoice PROM, if present(16 bytes Data, 16 bytes CounterOut)(this is often missing, see PC10 ROM - Images for details)
            return image;
        }

        private static RomImage ParseHeader(BinaryReader reader)
        {
            var image = new RomImage();

            //0 - 3    Identification String. Must be "NES<EOF>".
            if (new string(reader.ReadChars(4)) != "NES\x1a")
            {
                throw new BadImageFormatException();
            }

            //4      PRG-ROM size LSB
            image.ProgramRomSize = reader.ReadByte();
            //5      CHR-ROM size LSB
            image.CharacterRomSize = reader.ReadByte();

            //6      Flags 6
            //D~7654 3210
            //  ---------
            //  NNNN FTBM
            //  |||| |||+-- Hard-wired nametable mirroring type
            //  |||| |||     0: Horizontal or mapper-controlled
            //  |||| |||     1: Vertical
            //  |||| ||+--- "Battery" and other non-volatile memory
            //  |||| ||      0: Not present
            //  |||| ||      1: Present
            //  |||| |+--- 512-byte Trainer
            //  |||| |      0: Not present
            //  |||| |      1: Present between Header and PRG-ROM data
            //  |||| +---- Hard-wired four-screen mode
            //  ||||        0: No
            //  ||||        1: Yes
            //  ++++------ Mapper Number D0..D3
            byte romFlags = reader.ReadByte();
            image.RomFlags = (RomFlags)(romFlags & 0xF);
            image.Mapper = romFlags >> 4;

            //7      Flags 7
            //       D~7654 3210
            //         ---------
            //         NNNN 10TT
            //         |||| ||++- Console type
            //         |||| ||     0: Nintendo Entertainment System/Family Computer
            //         |||| ||     1: Nintendo Vs. System
            //         |||| ||     2: Nintendo Playchoice 10
            //         |||| ||     3: Extended Console Type
            //         |||| ++--- NES 2.0 identifier
            //         ++++------ Mapper Number D4..D7
            byte consoleTypeFlags = reader.ReadByte();
            image.ConsoleType = (ConsoleType)(consoleTypeFlags & 0x3);
            image.Mapper |= consoleTypeFlags & 0xf0;

            if ((consoleTypeFlags & 0xc) == 0x8)
            {
                ParseNes2(image, reader);
            }
            else
            {
                ParseINes(image, reader);
            }

            return image;
        }

        private static void ParseINes(RomImage image, BinaryReader reader)
        {
            //8: Flags 8 - PRG-RAM size (rarely used extension)
            image.ProgramRamSize = reader.ReadByte();

            //9: Flags 9 - TV system (rarely used extension)
            image.TVSystem = (TVSystem)reader.ReadByte();

            //10: Flags 10 - TV system, PRG-RAM presence (unofficial, rarely used extension)
            //76543210
            //  ||  ||
            //  ||  ++- TV system (0: NTSC; 2: PAL; 1/3: dual compatible)
            //  |+----- PRG RAM ($6000-$7FFF) (0: present; 1: not present)
            //  +------ 0: Board has no bus conflicts; 1: Board has bus conflicts
            if (reader.ReadByte() != 0)
            {
                throw new BadImageFormatException();
            }

            //11-15: Unused padding (should be filled with zero, but some rippers put their name across bytes 7-15)
            reader.ReadBytes(5);
        }

        private static void ParseNes2(RomImage image, BinaryReader reader)
        {

            //8      Mapper MSB/Submapper
            //       D~7654 3210
            //         ---------
            //         SSSS NNNN
            //         |||| ++++- Mapper number D8..D11
            //         ++++------ Submapper number

            //9      PRG-ROM/CHR-ROM size MSB
            //       D~7654 3210
            //         ---------
            //         CCCC PPPP
            //         |||| ++++- PRG-ROM size MSB
            //         ++++------ CHR-ROM size MSB

            //10     PRG-RAM/EEPROM size
            //       D~7654 3210
            //         ---------
            //         pppp PPPP
            //         |||| ++++- PRG-RAM (volatile) shift count
            //         ++++------ PRG-NVRAM/EEPROM (non-volatile) shift count
            //       If the shift count is zero, there is no PRG-(NV)RAM.
            //       If the shift count is non-zero, the actual size is
            //       "64 << shift count" bytes, i.e. 8192 bytes for a shift count of 7.

            //11     CHR-RAM size
            //       D~7654 3210
            //         ---------
            //         cccc CCCC
            //         |||| ++++- CHR-RAM size (volatile) shift count
            //         ++++------ CHR-NVRAM size (non-volatile) shift count
            //       If the shift count is zero, there is no CHR-(NV)RAM.
            //       If the shift count is non-zero, the actual size is
            //       "64 << shift count" bytes, i.e. 8192 bytes for a shift count of 7.

            //12     CPU/PPU Timing
            //       D~7654 3210
            //         ---------
            //         .... ..VV
            //                ++- CPU/PPU timing mode
            //                     0: RP2C02 ("NTSC NES")
            //                     1: RP2C07 ("Licensed PAL NES")
            //                     2: Multiple-region
            //                     3: UMC 6527P ("Dendy")

            //13     When Byte 7 AND 3 =1: Vs. System Type
            //       D~7654 3210
            //         ---------
            //         MMMM PPPP
            //         |||| ++++- Vs. PPU Type
            //         ++++------ Vs. Hardware Type

            //       When Byte 7 AND 3 =3: Extended Console Type
            //       D~7654 3210
            //         ---------
            //         .... CCCC
            //              ++++- Extended Console Type

            //14     Miscellaneous ROMs
            //       D~7654 3210
            //         ---------
            //         .... ..RR
            //                ++- Number of miscellaneous ROMs present

            //15     Default Expansion Device
            //       D~7654 3210
            //         ---------
            //         ..DD DDDD
            //           ++-++++- Default Expansion Device
            throw new NotImplementedException();
        }
    }
}
