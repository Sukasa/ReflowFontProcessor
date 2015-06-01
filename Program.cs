using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace OvenFontProcessor
{
    class Program
    {
        public const string FontData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789,<.>/?;:'\"\\|]}[{-_=+`~!@#$%^&*()°";

        private Bitmap FontBase;

        int XPosition = 0;

        private void _Main(string[] args)
        {
            if (args.Length == 0)
                return;

            List<Character> Characters = new List<Character>();
            Characters.Add(new Character(' '));

            Characters[0].DataOffset = -1;
            Characters[0].Width = 2;
            Characters[0].CharacterData = new byte[] { 0, 0, 0};

            FontBase = new Bitmap(args[0]);

            foreach (char Ch in FontData)
            {
                Character CurrentCharacter = new Character(Ch);


                SeekStart();

                int X = XPosition;

                CurrentCharacter.CharacterData = EatCharacter();
                CurrentCharacter.Width = (byte)((XPosition - X));

                Characters.Add(CurrentCharacter);
            }

            byte[] OutputData = new byte[1536];
            short TablePos = 0;

            // Header Table: 512 bytes, 256x(Width, Data Offset (>> 2))
            Characters.Sort((x, y) => Convert.ToInt32(x.Char).CompareTo(Convert.ToInt32(y.Char)));

            IEnumerator<Character> Enum = Characters.GetEnumerator();

            Enum.Reset();
            Enum.MoveNext();

            for (char CharCode = '\0'; CharCode <= 0xff; CharCode++)
            {
                if (Enum.Current != null && CharCode == Enum.Current.Char)
                {
                    Enum.Current.DataOffset = TablePos;

                    TablePos += RoundUp(Enum.Current.CharacterData.Length, 4);

                    OutputData[CharCode << 1] = (byte)Enum.Current.Width;
                    OutputData[(CharCode << 1) + 1] = (byte)(Enum.Current.DataOffset >> 2);

                    Enum.MoveNext();
                }
                else
                {
                    OutputData[CharCode << 1] = 0;
                    OutputData[(CharCode << 1) + 1] = 0;
                }

            }

            Enum.Dispose();

            // Data table: up to 1024 bytes (character data)

            foreach (Character CharacterData in Characters)
            {
                Array.Copy(CharacterData.CharacterData, 0, OutputData, 512 + CharacterData.DataOffset, CharacterData.CharacterData.Length);
            }

            using (FileStream FS = File.OpenWrite(Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(args[0]) + ".bin")))
            {
                FS.SetLength(OutputData.Length);
                FS.Seek(0, SeekOrigin.Begin);
                FS.Write(OutputData, 0, OutputData.Length);
                FS.Flush();
                FS.Close();
            }
            
        }

        private short RoundUp(int Value, int Rounding)
        {
            int NewValue = Value - (Value % Rounding);
            if (NewValue != Value)
                NewValue += Rounding;
            return (short)NewValue;
        }

        private byte[] EatCharacter()
        {
            byte Data = 0;
            int Bits = 0;
            int Y = 0;

            List<byte> AllData = new List<byte>();


            while (XPosition < FontBase.Width && !IsEmptyColumn(XPosition))
            {
                for (Y = 0; Y < 11; Y++)
                {
                    Data |= (byte)((FontBase.GetPixel(XPosition, Y).ToArgb() == -16777216 ? 1 : 0) << Bits);

                    if (++Bits == 8)
                    {
                        AllData.Add(Data);
                        Data = 0;
                        Bits = 0;
                    }
                }
                XPosition++;
            }
            
            if (Bits > 0)
                AllData.Add(Data);


            return AllData.ToArray();
        }

        private void SeekStart()
        {
            while (IsEmptyColumn(XPosition))
                ++XPosition;
        }

        private bool IsEmptyColumn(int X)
        {
            for (int Y = 0; Y < FontBase.Height; Y++)
            {
                if (FontBase.GetPixel(X, Y).ToArgb() != -1)
                    return false;
            }
            return true;
        }

        static void Main(string[] args)
        {
            (new Program())._Main(args);
        }
    }

    class Character
    {
        public byte Width;
        public short DataOffset;
        public char Char;

        public byte[] CharacterData;

        public Character(char Character)
        {
            Width = 0;
            DataOffset = 0;
            Char = Character;
            CharacterData = null;
        }

        public override string ToString()
        {
            return Char.ToString() + ": " + Width.ToString() + "px @" + DataOffset.ToString();
        }
    }
}
