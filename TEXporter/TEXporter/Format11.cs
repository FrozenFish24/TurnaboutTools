using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace TEXporter
{
    static class Format11
    {
        public static void Extract(ref byte[] tex, int width, int height, string filename)
        {
            //Make the PKM header
            List<byte> header = new List<byte>() { 0x50, 0x4B, 0x4D, 0x20, 0x31, 0x30, 0x00, 0x00 }; //"PKM 10", 0x0000
            byte[] wBytes = BitConverter.GetBytes((short)width);
            byte[] hBytes = BitConverter.GetBytes((short)height);
            Array.Reverse(wBytes);
            Array.Reverse(hBytes);
            header.AddRange(wBytes);
            header.AddRange(hBytes);
            header.AddRange(wBytes);
            header.AddRange(hBytes);
            byte[] headerArray = header.ToArray();

            FlipEndianness(ref tex);
            byte[] deinterlaced = Deinterlace(ref tex, width, height);

            //Combine PKM header with deinterlaced file
            byte[] final = new byte[tex.Length + headerArray.Length];
            headerArray.CopyTo(final, 0);
            deinterlaced.CopyTo(final, headerArray.Length);
            
            File.WriteAllBytes(filename + ".pkm", final);
            
            ProcessStartInfo etc1tool = new ProcessStartInfo();
            etc1tool.FileName = "etc1tool.exe";
            etc1tool.Arguments = "--decode " + "\"" + filename + ".pkm\"";
            
            try
            {
                using (Process exeProcess = Process.Start(etc1tool))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("etc1tool related problem!");
            }
            
            File.Delete(filename + ".pkm");
        }

        public static byte[] Insert(int width, int height, string imageFile, byte[] header)
        {
            ProcessStartInfo etc1tool = new ProcessStartInfo();
            etc1tool.FileName = "etc1tool.exe";
            etc1tool.Arguments = "--encodeNoHeader" + " \"" + imageFile + "\"" + " -o \"" + imageFile + ".pkm\"";

            try
            {
                using (Process exeProcess = Process.Start(etc1tool))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("etc1tool related problem!");
            }

            byte[] pkm = File.ReadAllBytes(imageFile + ".pkm");

            FlipEndianness(ref pkm);
            byte[] interlaced = Interlace(ref pkm, width, height);

            File.Delete(imageFile + ".pkm");

            byte[] final = new byte[header.Length + interlaced.Length];
            header.CopyTo(final, 0);
            interlaced.CopyTo(final, header.Length);

            File.WriteAllBytes("out.tex", final);

            return null;
        }

        public static byte[] Interlace(ref byte[] pkm, int width, int height)
        {
            const int ETC1_BLOCK_WIDTH = 4;
            const int BLOCK_WIDTH = ETC1_BLOCK_WIDTH * 4;
        
            byte[] interlaced = new byte[pkm.Length];
        
            int imageHorBlockLength = ((width / ETC1_BLOCK_WIDTH) / 2);
        
            int pkmIndex = 0;
            int finalIndex = 0;
        
            int currentBlock = 0;
            while (pkmIndex < pkm.Length)
            {
                Array.Copy(pkm, pkmIndex, interlaced, finalIndex, BLOCK_WIDTH); //Copy the first block to the final array
                Array.Copy(pkm, pkmIndex + (imageHorBlockLength * BLOCK_WIDTH), interlaced, finalIndex + BLOCK_WIDTH, BLOCK_WIDTH); //Skip to the next row of blocks in the image and copy to final array
                pkmIndex += BLOCK_WIDTH; //Head to the next adjacent block
                finalIndex += BLOCK_WIDTH * 2; //Two blocks have been added to the final array
        
                currentBlock++;
        
                if (currentBlock == imageHorBlockLength) //Skip the next row as it's already been parsed
                {
                    pkmIndex += imageHorBlockLength * BLOCK_WIDTH;
                    currentBlock = 0;
                }
            }
        
            return interlaced;
        }

        public static byte[] Deinterlace(ref byte[] tex, int width, int height)
        {
            const int ETC1_BLOCK_WIDTH = 4;
            const int BLOCK_WIDTH = ETC1_BLOCK_WIDTH * 4;

            int imageHorBlockLength = ((width / ETC1_BLOCK_WIDTH) / 2); //In this instance a block is a 2x2 grid of ETC1 blocks (which are each 4x4 pixels)

            byte[] row1 = new byte[imageHorBlockLength * BLOCK_WIDTH]; //blocks wide * bytes per block
            byte[] row2 = new byte[imageHorBlockLength * BLOCK_WIDTH];
            byte[] final = new byte[tex.Length];

            int texIndex = 0;
            int rowPos = 0;
            while (texIndex < tex.Length)
            {
                Array.Copy(tex, texIndex, row1, rowPos * BLOCK_WIDTH, BLOCK_WIDTH); //Add first block to row1
                Array.Copy(tex, texIndex + BLOCK_WIDTH, row2, rowPos * BLOCK_WIDTH, BLOCK_WIDTH); //Add second block to row2

                rowPos++; //Now a block has been added to each row, increment
                texIndex += BLOCK_WIDTH * 2; // Move forward to next 2 blocks

                //When row length reached append deinterlaced rows 1 and 2 to the final image
                if (rowPos == imageHorBlockLength)
                {
                    row1.CopyTo(final, texIndex - ((imageHorBlockLength * BLOCK_WIDTH) * 2));
                    row2.CopyTo(final, texIndex - (imageHorBlockLength * BLOCK_WIDTH));

                    rowPos = 0;
                }
            }

            return final;
        }

        public static void FlipEndianness(ref byte[] data)
        {
            //Flip endianness of each 64 bit word
            for (int i = 0; i < data.Length; i += 8)
            {
                byte[] qword = new byte[8];
                Array.Copy(data, i, qword, 0, 8);
                Array.Reverse(qword);

                Array.Copy(qword, 0, data, i, 8);
            }
        }
    }
}
