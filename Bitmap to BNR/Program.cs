using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bitmap_to_BNR
{
    class Program
    {
        private static int Tiles = 192;
        private static int Columns = 24;
        private static int Rows = 8;

        private static ushort[] BMP_to_BNR(ushort[] Buffer)
        {
            ushort[] BNR_Buffer = new ushort[Buffer.Length];

            int Row = 0, Column = 0, Output_Index = 0;
            for (int Tile = 0; Tile < Tiles; Tile++)
            {
                for (int Y = 0; Y < 4; Y++)
                {
                    for (int X = 0; X < 4; X++)
                    {
                        int Pixel_Index = (Row + Y) * 96 + (Column * 4) + X;
                        Console.WriteLine(string.Format("Index: {0} | Writing Pixel Value of 0x{1} from Index: {2}", Output_Index.ToString("X"), Buffer[Pixel_Index].ToString("X4"), Pixel_Index.ToString("X")));
                        BNR_Buffer[Output_Index] = Buffer[Pixel_Index];
                        Output_Index++;
                    }
                }

                Column++;
                if (Column >= Columns)
                {
                    Column = 0;
                    Row += 4;
                }
            }

            return BNR_Buffer;
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("==================================");
                Console.WriteLine("BMP2BNR by Cuyler36 (Jeremy Olsen)");
                Console.WriteLine("==================================");
                Console.WriteLine("Usage: BMP2BNR.exe <Bitmap Path> <Banner Path>");
                Console.WriteLine("<Bitmap Path> = The path to the Bitmap File to import (must be 24 bits per pixel and 96x32)");
                Console.WriteLine("<Banner Path> = The path to the Banner File to import the image to");
                Console.WriteLine("Example: BMP2BNR.exe \"C:\\Users\\Cuyler\\Pictures\\Picture.bmp\" \"C:\\Users\\Cuyler\\GameCube Mod Folder\\opening.bnr\"");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue!");
                Console.ReadKey();
            }
            if (args.Length == 2)
            {
                if (File.Exists(args[0]))
                {
                    if (File.Exists(args[1]))
                    {
                        try
                        {
                            byte[] BMP_Data = File.ReadAllBytes(args[0]);

                            if (BMP_Data[0x1C] != 24)
                            {
                                Console.WriteLine("Bitmap to BNR only supports 24 bits per pixel Bitmaps!");
                                Console.WriteLine("Press any key to close the window.");
                                Console.ReadKey();
                                return;
                            }

                            if (BMP_Data[0x12] != 0x60 || BMP_Data[0x16] != 0x20)
                            {
                                Console.WriteLine("The Bitmap must be 90 pixels wide by 32 pixels tall. Please resize your image before converting!");
                                Console.WriteLine("Press any key to close the window.");
                                Console.ReadKey();
                                return;
                            }

                            BMP_Data = BMP_Data.Skip(BMP_Data[0xA]).ToArray(); // Skip to image data
                            ushort[] RGB5A3_Data = new ushort[BMP_Data.Length / 3]; // 3 bytes per pixel
                            for (int i = 0; i < RGB5A3_Data.Length; i++)
                            {
                                int idx = i * 3;
                                RGB5A3_Data[i] = RGBA8_to_RGB5A3(BMP_Data[idx + 2], BMP_Data[idx + 1], BMP_Data[idx]);
                            }

                            // Flip Vertically
                            ushort[] Flipped_Data = new ushort[RGB5A3_Data.Length];
                            int index = 0;
                            for (int i = RGB5A3_Data.Length - 1; i >= 0; i--)
                            {
                                Flipped_Data[index] = RGB5A3_Data[i];
                                index++;
                            }

                            // Flip Horizontally
                            for (int i = 0; i < Flipped_Data.Length; i += 96)
                            {
                                Array.Reverse(Flipped_Data, i, 96);
                            }

                            ushort[] BNR_Data = BMP_to_BNR(Flipped_Data);

                            using (FileStream Stream = new FileStream(args[1], FileMode.OpenOrCreate))
                            {
                                if (Stream.Length != 0x1960)
                                {
                                    Console.WriteLine("Banner file does not seem to be valid. Please make sure it is 0x1960 bytes long.");
                                    Console.WriteLine("Press any key to close the window.");
                                    Console.ReadKey();
                                    return;
                                }

                                for (int i = 0; i < BNR_Data.Length; i++)
                                {
                                    Stream.Position = 0x20 + i * 2;
                                    Stream.Write(BitConverter.GetBytes(BNR_Data[i]).Reverse().ToArray(), 0, 2);
                                }

                                Stream.Flush();
                            }

                            Console.WriteLine("Conversion was successful!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Conversion Failed! Reason: \n" + e.StackTrace);
                            Console.ReadKey();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Could not find the banner file: " + args[1]);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find the bitmap file: " + args[0]);
                }
            }
        }

        private static ushort RGBA8_to_RGB5A3(byte R, byte G, byte B)
        {
            byte r = (byte)((R * (1 << 5)) / 256);
            byte g = (byte)((G * (1 << 5)) / 256);
            byte b = (byte)((B * (1 << 5)) / 256);

            return (ushort)((1 << 15) | (r << 10) | (g << 5) | b);
        }
    }
}
