using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace TEXporter
{
    class Program
    {
        static string mode;
        static string filename;

        static void Main(string[] args)
        {
            Console.WriteLine("TEXporter 0.2\n");

            string imageFile = "";

            if (args.Length < 1)
            {
                Console.WriteLine("No arguments provided");
                return;
            }

            if (args[0] == "e")
            {
                mode = args[0];
                filename = args[1];
            }
            else if (args[0] == "i")
            {
                mode = args[0];
                filename = args[1];
                imageFile = args[2];
            }
            else
            {
                mode = "e";
                filename = args[0];
            }

            byte[] tex = File.ReadAllBytes(filename);

            string magic = System.Text.Encoding.ASCII.GetString(tex, 0, 4);

            uint[] dwords = new uint[3];
            for (int i = 0; i < 3; i++)
                dwords[i] = BitConverter.ToUInt32(tex, i * 4 + 4);

            int constant = (int)(dwords[0] & 0xfff);
            int unknown1 = (int)((dwords[0] >> 12) & 0xfff);
            int sizeShift = (int)((dwords[0] >> 24) & 0xf);
            int unknown2 = (int)((dwords[0] >> 28) & 0xf);

            int mipmapCount = (int)(dwords[1] & 0x3f);
            int width = (int)((dwords[1] >> 6) & 0x1fff);
            int height = (int)((dwords[1] >> 19) & 0x1fff);

            int unknown3 = (int)(dwords[2] & 0xff);
            int type = (int)((dwords[2] >> 8) & 0xff);
            int unknown5 = (int)((dwords[2] >> 16) & 0x1fff);

            int headerLength = 0x10 + (4 * mipmapCount);

            byte[] header = new byte[headerLength];
            Array.Copy(tex, header, headerLength);

            Array.Copy(tex, headerLength, tex, 0, tex.Length - headerLength);
            Array.Resize(ref tex, tex.Length - headerLength);

            Console.WriteLine("Type: " + type);
            Console.WriteLine("Mipmaps: " + mipmapCount);
            Console.WriteLine("Size: " + width + ", " + height);

            if (mode == "e")
            {
                Bitmap image = Extract(type, width, height, tex);
                if (image != null)
                    image.Save(filename.Replace(".tex", ".png"));
            }
            else if (mode == "i")
            {
                Bitmap image = (Bitmap)Bitmap.FromFile(imageFile);

                if (image.Width != width || image.Height != height)
                {
                    Console.WriteLine("Image dimensions don't match, required dimensions are " + width + ", " + height);
                    return;
                }

                byte[] data = Insert(type, width, height, image, imageFile, header);

                if(type != 11)
                    File.WriteAllBytes("out.tex", data);
            }
        }

        static Bitmap Extract(int type, int width, int height, byte[] data)
        {
            Bitmap image = new Bitmap(width, height);

            //32bpp RGBA, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            switch (type)
            {
                case 3:
                    {
                        for (int i = 0; i < width * height * 4; i += 4)
                        {
                            byte a = data[i];
                            byte b = data[i + 1];
                            byte g = data[i + 2];
                            byte r = data[i + 3];

                            Point p = GetXY(i, 4, width, height);

                            image.SetPixel(p.X, p.Y, Color.FromArgb(a, r, g, b));
                        }
                        break;
                    }
                //ETC1
                case 11:
                    {
                        if (!File.Exists("etc1tool.exe"))
                        {
                            Console.Write("\nERROR: TEX file contains ETC1 compressed data, etc1tool is required to process this format. ");
                            Console.Write("Please aquire it from the Android SDK and place it in the same folder as TEXporter.exe");
                            return null;
                        }

                        Format11.Extract(ref data, width, height, filename);
                        return null;
                    }
                //DXT2, 3, 4 or 5?
                case 12:
                    {
                        //image = Format11.dump(data, width, height);
                        //break;
                        Console.WriteLine("Known format, not implemented");
                        return null;
                    }
                case 14:
                    {
                        for (int i = 0; i < (width * height) / 2; i++)
                        {
                            //byte pixel = data[i];
                            //byte a = (byte)(((pixel << 4) & 0xF0) | (pixel & 0xF));
                            //pixel = (byte)((pixel & 0xF0) | (pixel >> 4));

                            Point p = GetXY(i, 1, width, height);

                            image.SetPixel(p.X, p.Y, Color.FromArgb(data[i], 255, 255, 255));
                        }
                        break;
                    }
                //8bpp, 4bit luminance, 4bit alpha(?), non mip - mapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
                case 16:
                    {
                        for (int i = 0; i < width * height; i++)
                        {
                            byte pixel = data[i];
                            byte a = (byte)(((pixel << 4) & 0xF0) | (pixel & 0xF));
                            pixel = (byte)((pixel & 0xF0) | (pixel >> 4));

                            Point p = GetXY(i, 1, width, height);

                            image.SetPixel(p.X, p.Y, Color.FromArgb(a, pixel, pixel, pixel));
                        }
                        break;
                    }
                //24bpp RGB, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
                case 17:
                    {
                        for (int i = 0; i < width * height * 3; i += 3)
                        {
                            byte b = data[i];
                            byte g = data[i + 1];
                            byte r = data[i + 2];

                            Point p = GetXY(i, 3, width, height);

                            image.SetPixel(p.X, p.Y, Color.FromArgb(255, r, g, b));
                        }
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Not implemented, " + type);
                        return null;
                    }
            }

            return image;
        }

        static byte[] Insert(int type, int width, int height, Bitmap image, string imageFile, byte[] header)
        {
            byte[] data = { };

            //32bpp RGBA, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            switch (type)
            {
                case 3:
                    {
                        //Append the header first
                        data = new byte[header.Length + (width * height * 4)];
                        for (int i = 0; i < header.Length; i++)
                            data[i] = header[i];

                        for (int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                int position = header.Length + GetOffset(x, y, 4, width, height);

                                data[position] = ((byte)image.GetPixel(x, y).A);
                                data[position + 1] = ((byte)image.GetPixel(x, y).B);
                                data[position + 2] = ((byte)image.GetPixel(x, y).G);
                                data[position + 3] = ((byte)image.GetPixel(x, y).R);
                            }
                        }
                        break;
                    }
                //ETC1
                case 11:
                    {
                        if (!File.Exists("etc1tool.exe"))
                        {
                            Console.Write("\nERROR: TEX file contains ETC1 compressed data, etc1tool is required to process this format. ");
                            Console.Write("Please aquire it from the Android SDK and place it in the same folder as TEXporter.exe");
                            return null;
                        }

                        Format11.Insert(width, height, imageFile, header);
                        return null;
                    }
                //DXT2, 3, 4 or 5?
                case 12:
                    {
                        Console.WriteLine("Known format, not implemented");
                        return null;
                    }
                //8bpp, 4bit luminance, 4bit alpha(?), non mip - mapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
                case 16:
                    {
                        //Append the header first
                        data = new byte[header.Length + (width * height)];
                        for (int i = 0; i < header.Length; i++)
                            data[i] = header[i];

                        for (int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                int position = header.Length + GetOffset(x, y, 1, width, height);

                                byte a = image.GetPixel(x, y).A;
                                byte r = image.GetPixel(x, y).R;
                                byte g = image.GetPixel(x, y).G;
                                byte b = image.GetPixel(x, y).B;

                                int luminance = (r + g + b) / 3;

                                byte pixel = (byte)((luminance & 0xF0) | (a & 0xF));

                                data[position] = pixel;
                            }
                        }
                        break;
                    }
                //24bpp RGB, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
                case 17:
                    {
                        //Append the header first
                        data = new byte[header.Length + (width * height * 3)];
                        for (int i = 0; i < header.Length; i++)
                            data[i] = header[i];

                        for (int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                int position = header.Length + GetOffset(x, y, 3, width, height);

                                data[position] = ((byte)image.GetPixel(x, y).B);
                                data[position + 1] = ((byte)image.GetPixel(x, y).G);
                                data[position + 2] = ((byte)image.GetPixel(x, y).R);
                            }
                        }
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Not implemented, " + type);
                        return null;
                    }
            }

            return data;
        }

        static Point GetXY(int offset, int bytesPerPixel, int width, int height)
        {
            const int pixelsInBlock = 64; //Each block is 4px * 4 * 4
            const int blockWidth = 8;

            offset /= bytesPerPixel; //Convert file offset to pixel index

            int indexWithinBlock = offset % pixelsInBlock;
            int blockIndex = (offset - indexWithinBlock) / pixelsInBlock;

            Point xyWithinBlock = Morton.ZtoXY((uint)indexWithinBlock); //Find pixels local position within block

            int imageBlocksWide = width / blockWidth;
            int imageBlocksHigh = height / blockWidth;

            //Calculate block x,y
            int blockX = (blockIndex % imageBlocksWide);
            int blockY = (blockIndex / imageBlocksWide);

            //Add block position to pixel position to find absolute position
            xyWithinBlock.X += blockWidth * blockX;
            xyWithinBlock.Y += blockWidth * blockY;

            return xyWithinBlock;
        }

        static int GetOffset(int x, int y, int bytesPerPixel, int width, int height)
        {
            const int pixelsInBlock = 64; //Each block is 4px * 4 * 4
            const int blockWidth = 8;

            int imageBlocksWide = width / blockWidth;
            int imageBlocksHigh = height / blockWidth;

            //Calculate block x,y
            int blockX = x / blockWidth;
            int blockY = y / blockWidth;

            //Calculate linear block index
            int blockIndex = (blockY * imageBlocksWide) + blockX;

            //Get local x, y coordinate
            x -= blockWidth * blockX;
            y -= blockWidth * blockY;

            int offset = (int)Morton.XYtoZ((uint)x, (uint)y); //Transform local position to offset

            offset += (blockIndex * pixelsInBlock);

            return offset * bytesPerPixel;
        }
    }
}
