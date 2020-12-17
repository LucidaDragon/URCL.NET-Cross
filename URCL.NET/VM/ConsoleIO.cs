using System;

namespace URCL.NET.VM
{
    public class ConsoleIO : UrclMachine.IO
    {
        private int DrawX = 0;
        private int DrawY = 0;
        private ConsoleColor Color = ConsoleColor.Black;

        public override ulong this[ulong port]
        {
            get
            {
                switch (port)
                {
                    case 43:
                        return (ulong)(DrawX & 0xF);
                    case 47:
                        return (ulong)(DrawX & 0x3F);
                    case 51:
                        return (ulong)(DrawX & 0xFF);
                    case 55:
                        return (ulong)(DrawX & 0xFFFF);
                    case 44:
                        return (ulong)(DrawY & 0xF);
                    case 48:
                        return (ulong)(DrawY & 0x3F);
                    case 52:
                        return (ulong)(DrawY & 0xFF);
                    case 56:
                        return (ulong)(DrawY & 0xFFFF);
                    case 59:
                        return (ulong)(Color == ConsoleColor.Black ? 0 : 1);
                    case 60:
                        return Color switch
                        {
                            ConsoleColor.Black => 0b00,
                            ConsoleColor.DarkGray => 0b01,
                            ConsoleColor.Gray => 0b10,
                            _ => 0b11,
                        };
                    case 61:
                        return (ulong)((int)Color & 0xE) >> 1;
                    case 62:
                        return (ulong)((int)Color & 0xF);
                    case 78:
                        return (ulong)(Console.ReadKey().KeyChar & 0x7F);
                    case 79:
                        return (ulong)(Console.ReadKey().KeyChar & 0xFF);
                    default:
                        UnsupportedPort();
                        return 0;
                }
            }
            set
            {
                switch (port)
                {
                    case 43:
                        DrawX = (int)(value & 0xF);
                        break;
                    case 47:
                        DrawX = (int)(value & 0x3F);
                        break;
                    case 51:
                        DrawX = (int)(value & 0xFF);
                        break;
                    case 55:
                        DrawX = (int)(value & 0xFFFF);
                        break;
                    case 44:
                        DrawY = (int)(value & 0xF);
                        break;
                    case 48:
                        DrawY = (int)(value & 0x3F);
                        break;
                    case 52:
                        DrawY = (int)(value & 0xFF);
                        break;
                    case 56:
                        DrawY = (int)(value & 0xFFFF);
                        break;
                    case 59:
                        Color = (value == 0 ? ConsoleColor.Black : ConsoleColor.White);
                        DrawPixel();
                        break;
                    case 60:
                        Color = value switch
                        {
                            0b00 => ConsoleColor.Black,
                            0b01 => ConsoleColor.DarkGray,
                            0b10 => ConsoleColor.Gray,
                            _ => ConsoleColor.White,
                        };
                        DrawPixel();
                        break;
                    case 61:
                        Color = (ConsoleColor)((value << 1) & 0xE);
                        DrawPixel();
                        break;
                    case 62:
                        Color = (ConsoleColor)(value & 0xF);
                        DrawPixel();
                        break;
                    case 78:
                        Console.Write((char)(value & 0x7F));
                        break;
                    case 79:
                        Console.Write((char)(value & 0xFF));
                        break;
                    case 91:
                        PrintVCS(value);
                        break;
                    default:
                        UnsupportedPort();
                        break;
                }
            }
        }

        public override void Init(UrclMachine host)
        {
            base.Init(host);
            Console.Clear();
        }

        private void DrawPixel()
        {
            try
            {
                Console.CursorLeft = DrawX;
                Console.CursorTop = DrawY;

                var orig = Console.BackgroundColor;
                Console.BackgroundColor = Color;
                Console.Write(' ');
                Console.BackgroundColor = orig;

                Console.CursorLeft = 0;
                Console.CursorTop = Console.WindowHeight - 1;
            }
            catch (Exception)
            {
                IOException();
            }
        }

        private static void PrintVCS(ulong value)
        {
            switch (value)
            {
                case 0x01:
                    Console.Write('\n');
                    break;
                case 0x04:
                    Console.Write('█');
                    break;
                case 0x05:
                    Console.Write('▄');
                    break;
                case 0x06:
                    Console.Write('▀');
                    break;
                case 0x07:
                    Console.Write('▶');
                    break;
                case 0x08:
                    Console.Write('◀');
                    break;
                case 0x09:
                    Console.Write('§');
                    break;
                case 0x0A:
                    Console.Write('¶');
                    break;
                case 0x0B:
                    Console.Write('☺');
                    break;
                case 0x0C:
                    Console.Write('«');
                    break;
                case 0x0D:
                    Console.Write('»');
                    break;
                case 0x0E:
                    Console.Write('‹');
                    break;
                case 0x0F:
                    Console.Write('›');
                    break;
                case 0x10:
                    Console.Write('•');
                    break;
                case 0x11:
                    Console.Write('…');
                    break;
                case 0x12:
                    Console.Write('♣');
                    break;
                case 0x13:
                    Console.Write('♦');
                    break;
                case 0x14:
                    Console.Write('♥');
                    break;
                case 0x15:
                    Console.Write('♠');
                    break;
                case 0x16:
                    Console.Write('←');
                    break;
                case 0x17:
                    Console.Write('↑');
                    break;
                case 0x18:
                    Console.Write('→');
                    break;
                case 0x19:
                    Console.Write('↓');
                    break;
                case 0x1A:
                    Console.Write('✭');
                    break;
                case 0x1B:
                    Console.Write('ƒ');
                    break;
                case 0x20:
                    Console.Write(' ');
                    break;
                case 0x21:
                    Console.Write('!');
                    break;
                case 0x22:
                    Console.Write('\"');
                    break;
                case 0x23:
                    Console.Write('#');
                    break;
                case 0x24:
                    Console.Write('$');
                    break;
                case 0x25:
                    Console.Write('%');
                    break;
                case 0x26:
                    Console.Write('&');
                    break;
                case 0x27:
                    Console.Write('\'');
                    break;
                case 0x28:
                    Console.Write('(');
                    break;
                case 0x29:
                    Console.Write(')');
                    break;
                case 0x2A:
                    Console.Write('*');
                    break;
                case 0x2B:
                    Console.Write('+');
                    break;
                case 0x2C:
                    Console.Write(',');
                    break;
                case 0x2D:
                    Console.Write('-');
                    break;
                case 0x2E:
                    Console.Write('.');
                    break;
                case 0x2F:
                    Console.Write('/');
                    break;
                case 0x30:
                    Console.Write('0');
                    break;
                case 0x31:
                    Console.Write('1');
                    break;
                case 0x32:
                    Console.Write('2');
                    break;
                case 0x33:
                    Console.Write('3');
                    break;
                case 0x34:
                    Console.Write('4');
                    break;
                case 0x35:
                    Console.Write('5');
                    break;
                case 0x36:
                    Console.Write('6');
                    break;
                case 0x37:
                    Console.Write('7');
                    break;
                case 0x38:
                    Console.Write('8');
                    break;
                case 0x39:
                    Console.Write('9');
                    break;
                case 0x3A:
                    Console.Write(':');
                    break;
                case 0x3B:
                    Console.Write(';');
                    break;
                case 0x3C:
                    Console.Write('<');
                    break;
                case 0x3D:
                    Console.Write('=');
                    break;
                case 0x3E:
                    Console.Write('>');
                    break;
                case 0x3F:
                    Console.Write('?');
                    break;
                case 0x40:
                    Console.Write('@');
                    break;
                case 0x41:
                    Console.Write('A');
                    break;
                case 0x42:
                    Console.Write('B');
                    break;
                case 0x43:
                    Console.Write('C');
                    break;
                case 0x44:
                    Console.Write('D');
                    break;
                case 0x45:
                    Console.Write('E');
                    break;
                case 0x46:
                    Console.Write('F');
                    break;
                case 0x47:
                    Console.Write('G');
                    break;
                case 0x48:
                    Console.Write('H');
                    break;
                case 0x49:
                    Console.Write('I');
                    break;
                case 0x4A:
                    Console.Write('J');
                    break;
                case 0x4B:
                    Console.Write('K');
                    break;
                case 0x4C:
                    Console.Write('L');
                    break;
                case 0x4D:
                    Console.Write('M');
                    break;
                case 0x4E:
                    Console.Write('N');
                    break;
                case 0x4F:
                    Console.Write('O');
                    break;
                case 0x50:
                    Console.Write('P');
                    break;
                case 0x51:
                    Console.Write('Q');
                    break;
                case 0x52:
                    Console.Write('R');
                    break;
                case 0x53:
                    Console.Write('S');
                    break;
                case 0x54:
                    Console.Write('T');
                    break;
                case 0x55:
                    Console.Write('U');
                    break;
                case 0x56:
                    Console.Write('V');
                    break;
                case 0x57:
                    Console.Write('W');
                    break;
                case 0x58:
                    Console.Write('X');
                    break;
                case 0x59:
                    Console.Write('Y');
                    break;
                case 0x5A:
                    Console.Write('Z');
                    break;
                case 0x5B:
                    Console.Write('[');
                    break;
                case 0x5C:
                    Console.Write('\\');
                    break;
                case 0x5D:
                    Console.Write(']');
                    break;
                case 0x5E:
                    Console.Write('^');
                    break;
                case 0x5F:
                    Console.Write('_');
                    break;
                case 0x60:
                    Console.Write('`');
                    break;
                case 0x61:
                    Console.Write('a');
                    break;
                case 0x62:
                    Console.Write('b');
                    break;
                case 0x63:
                    Console.Write('c');
                    break;
                case 0x64:
                    Console.Write('d');
                    break;
                case 0x65:
                    Console.Write('e');
                    break;
                case 0x66:
                    Console.Write('f');
                    break;
                case 0x67:
                    Console.Write('g');
                    break;
                case 0x68:
                    Console.Write('h');
                    break;
                case 0x69:
                    Console.Write('i');
                    break;
                case 0x6A:
                    Console.Write('j');
                    break;
                case 0x6B:
                    Console.Write('k');
                    break;
                case 0x6C:
                    Console.Write('l');
                    break;
                case 0x6D:
                    Console.Write('m');
                    break;
                case 0x6E:
                    Console.Write('n');
                    break;
                case 0x6F:
                    Console.Write('o');
                    break;
                case 0x70:
                    Console.Write('p');
                    break;
                case 0x71:
                    Console.Write('q');
                    break;
                case 0x72:
                    Console.Write('r');
                    break;
                case 0x73:
                    Console.Write('s');
                    break;
                case 0x74:
                    Console.Write('t');
                    break;
                case 0x75:
                    Console.Write('u');
                    break;
                case 0x76:
                    Console.Write('v');
                    break;
                case 0x77:
                    Console.Write('w');
                    break;
                case 0x78:
                    Console.Write('x');
                    break;
                case 0x79:
                    Console.Write('y');
                    break;
                case 0x7A:
                    Console.Write('z');
                    break;
                case 0x7B:
                    Console.Write('{');
                    break;
                case 0x7C:
                    Console.Write('|');
                    break;
                case 0x7D:
                    Console.Write('}');
                    break;
                case 0x7E:
                    Console.Write('~');
                    break;
                case 0x80:
                    Console.Write('¡');
                    break;
                case 0x81:
                    Console.Write('¿');
                    break;
                case 0x82:
                    Console.Write('¢');
                    break;
                case 0x83:
                    Console.Write('€');
                    break;
                case 0x84:
                    Console.Write('£');
                    break;
                case 0x85:
                    Console.Write('¥');
                    break;
                case 0x86:
                    Console.Write('≠');
                    break;
                case 0x87:
                    Console.Write('≈');
                    break;
                case 0x88:
                    Console.Write('∞');
                    break;
                case 0x89:
                    Console.Write('¬');
                    break;
                case 0x8A:
                    Console.Write('π');
                    break;
                case 0x8B:
                    Console.Write('ß');
                    break;
                case 0x8C:
                    Console.Write('°');
                    break;
                case 0x8D:
                    Console.Write('™');
                    break;
                case 0x8E:
                    Console.Write('©');
                    break;
                case 0x8F:
                    Console.Write('®');
                    break;
                case 0x90:
                    Console.Write('±');
                    break;
                case 0x91:
                    Console.Write('≤');
                    break;
                case 0x92:
                    Console.Write('≥');
                    break;
                case 0x93:
                    Console.Write('÷');
                    break;
                case 0x94:
                    Console.Write('×');
                    break;
                case 0x95:
                    Console.Write('₂');
                    break;
                case 0x96:
                    Console.Write('¼');
                    break;
                case 0x97:
                    Console.Write('½');
                    break;
                case 0x98:
                    Console.Write('¾');
                    break;
                case 0x99:
                    Console.Write('⌐');
                    break;
                case 0x9A:
                    Console.Write('²');
                    break;
                case 0x9B:
                    Console.Write('³');
                    break;
                case 0x9C:
                    Console.Write('░');
                    break;
                case 0x9D:
                    Console.Write('▒');
                    break;
                case 0x9E:
                    Console.Write('▓');
                    break;
                case 0x9F:
                    Console.Write('Σ');
                    break;
                case 0xA0:
                    Console.Write('Á');
                    break;
                case 0xA1:
                    Console.Write('Â');
                    break;
                case 0xA2:
                    Console.Write('Ä');
                    break;
                case 0xA3:
                    Console.Write('Å');
                    break;
                case 0xA4:
                    Console.Write('Ã');
                    break;
                case 0xA5:
                    Console.Write('Ā');
                    break;
                case 0xA6:
                    Console.Write('Æ');
                    break;
                case 0xA7:
                    Console.Write('Ç');
                    break;
                case 0xA8:
                    Console.Write('É');
                    break;
                case 0xA9:
                    Console.Write('Ê');
                    break;
                case 0xAA:
                    Console.Write('Ë');
                    break;
                case 0xAB:
                    Console.Write('Ē');
                    break;
                case 0xAC:
                    Console.Write('Í');
                    break;
                case 0xAD:
                    Console.Write('Î');
                    break;
                case 0xAE:
                    Console.Write('Ï');
                    break;
                case 0xAF:
                    Console.Write('Ī');
                    break;
                case 0xB0:
                    Console.Write('Đ');
                    break;
                case 0xB1:
                    Console.Write('Ń');
                    break;
                case 0xB2:
                    Console.Write('Ñ');
                    break;
                case 0xB3:
                    Console.Write('Ó');
                    break;
                case 0xB4:
                    Console.Write('Ô');
                    break;
                case 0xB5:
                    Console.Write('Ö');
                    break;
                case 0xB6:
                    Console.Write('Ō');
                    break;
                case 0xB7:
                    Console.Write('Œ');
                    break;
                case 0xB8:
                    Console.Write('Ø');
                    break;
                case 0xB9:
                    Console.Write('Ú');
                    break;
                case 0xBA:
                    Console.Write('Û');
                    break;
                case 0xBB:
                    Console.Write('Ü');
                    break;
                case 0xBC:
                    Console.Write('Ý');
                    break;
                case 0xBD:
                    Console.Write('Ÿ');
                    break;
                case 0xBE:
                    Console.Write('Ş');
                    break;
                case 0xBF:
                    Console.Write('Þ');
                    break;
                case 0xC0:
                    Console.Write('á');
                    break;
                case 0xC1:
                    Console.Write('â');
                    break;
                case 0xC2:
                    Console.Write('ä');
                    break;
                case 0xC3:
                    Console.Write('å');
                    break;
                case 0xC4:
                    Console.Write('ã');
                    break;
                case 0xC5:
                    Console.Write('ā');
                    break;
                case 0xC6:
                    Console.Write('æ');
                    break;
                case 0xC7:
                    Console.Write('ç');
                    break;
                case 0xC8:
                    Console.Write('é');
                    break;
                case 0xC9:
                    Console.Write('ê');
                    break;
                case 0xCA:
                    Console.Write('ë');
                    break;
                case 0xCB:
                    Console.Write('ē');
                    break;
                case 0xCC:
                    Console.Write('í');
                    break;
                case 0xCD:
                    Console.Write('î');
                    break;
                case 0xCE:
                    Console.Write('ï');
                    break;
                case 0xCF:
                    Console.Write('ī');
                    break;
                case 0xD0:
                    Console.Write('ð');
                    break;
                case 0xD1:
                    Console.Write('ń');
                    break;
                case 0xD2:
                    Console.Write('ñ');
                    break;
                case 0xD3:
                    Console.Write('ó');
                    break;
                case 0xD4:
                    Console.Write('ô');
                    break;
                case 0xD5:
                    Console.Write('ö');
                    break;
                case 0xD6:
                    Console.Write('ō');
                    break;
                case 0xD7:
                    Console.Write('œ');
                    break;
                case 0xD8:
                    Console.Write('ø');
                    break;
                case 0xD9:
                    Console.Write('ú');
                    break;
                case 0xDA:
                    Console.Write('û');
                    break;
                case 0xDB:
                    Console.Write('ü');
                    break;
                case 0xDC:
                    Console.Write('ý');
                    break;
                case 0xDD:
                    Console.Write('ÿ');
                    break;
                case 0xDE:
                    Console.Write('ş');
                    break;
                case 0xDF:
                    Console.Write('þ');
                    break;
                case 0xE0:
                    Console.Write('Ꝥ');
                    break;
                case 0xE1:
                    Console.Write('ꝥ');
                    break;
                case 0xE2:
                    Console.Write('µ');
                    break;
                case 0xE3:
                    Console.Write('Ŕ');
                    break;
                case 0xE4:
                    Console.Write('ŕ');
                    break;
                case 0xE5:
                    Console.Write('∫');
                    break;
                case 0xE6:
                    Console.Write('Ū');
                    break;
                case 0xE7:
                    Console.Write('ū');
                    break;
                case 0xE8:
                    Console.Write('◊');
                    break;
                case 0xE9:
                    Console.Write('∩');
                    break;
                case 0xEA:
                    Console.Write('∪');
                    break;
                case 0xEB:
                    Console.Write('⊃');
                    break;
                case 0xEC:
                    Console.Write('⊂');
                    break;
                case 0xED:
                    Console.Write('‰');
                    break;
                case 0xEE:
                    Console.Write('∨');
                    break;
                case 0xEF:
                    Console.Write('∠');
                    break;
            }
        }
    }
}
