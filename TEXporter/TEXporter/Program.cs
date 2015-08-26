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
            mode = args[0];
            filename = args[1];
            string imageFile = "";

            if(args.Length > 2)
                imageFile = args[2];

            byte[] tex = File.ReadAllBytes(filename);

            byte[] header = new byte[0x14];
            Array.Copy(tex, header, 0x14);

            Array.Copy(tex, 0x14, tex, 0, tex.Length - 0x14);
            Array.Resize(ref tex, tex.Length - 0x14);

            byte mipmap_count = header[8];
            int w = BitConverter.ToInt16(header, 9);
            byte h = header[11];
            byte type = header[13];

            int width = w * 4;
            int height = h * 32;

            if(mode == "e")
            {
                Bitmap image = extract(type, width, height, tex);
                image.Save(filename.Replace(".tex", ".png"));
            }
            else if(mode == "i")
            {
                Bitmap image = (Bitmap)Bitmap.FromFile(imageFile);
                byte[] data = insert(type, width, height, image, header);
                File.WriteAllBytes("out.tex", data);
            }
        }

        static Bitmap extract(int type, int width, int height, byte[] data)
        {
            Bitmap image = new Bitmap(width, height);

            //32bpp RGBA, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            if (type == 3)
            {
                for (int i = 0; i < data.Length; i += 4)
                {
                    byte a = data[i];
                    byte b = data[i + 1];
                    byte g = data[i + 2];
                    byte r = data[i + 3];
                    
                    Point p = getXY(i, 4, width, height);

                    image.SetPixel(p.X, p.Y, Color.FromArgb(a, r, g, b));
                }
            }
            //Likely DXT3 or 5
            else if (type == 12)
            {
                Console.WriteLine("Known format, not implemented");
                return null;
            }
            //8bpp, 4bit luminance, 4bit alpha(?), non mip - mapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)[IMPERFECT]
            else if (type == 16)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    byte pixel = data[i];
                    byte a = (byte)(((pixel << 4) & 0xF0) | (pixel & 0xF));
                    pixel = (byte)((pixel & 0xF0) | (pixel >> 4));
                    
                    Point p = getXY(i, 1, width, height);

                    image.SetPixel(p.X, p.Y, Color.FromArgb(a, pixel, pixel, pixel));
                }
            }
            //24bpp RGB, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            else if (type == 17)
            {
                for (int i = 0; i < data.Length; i += 3)
                {
                    byte b = data[i];
                    byte g = data[i + 1];
                    byte r = data[i + 2];
                    
                    Point p = getXY(i, 3, width, height);

                    image.SetPixel(p.X, p.Y, Color.FromArgb(255, r, g, b));
                }
            }
            else
            {
                Console.WriteLine("Not implemented, " + type);
                return null;
            }

            return image;
        }

        static byte[] insert(int type, int width, int height, Bitmap image, byte[] header)
        {
            byte[] data = {};

            //32bpp RGBA, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            if (type == 3)
            {
                //Append the header first
                data = new byte[header.Length + (width*height*4)];
                for (int i = 0; i < header.Length; i++)
                    data[i] = header[i];

                for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int position = header.Length + getOffset(x, y, 4, width, height);

                        data[position] = ((byte)image.GetPixel(x, y).A);
                        data[position + 1] = ((byte)image.GetPixel(x, y).B);
                        data[position + 2] = ((byte)image.GetPixel(x, y).G);
                        data[position + 3] = ((byte)image.GetPixel(x, y).R);
                    }
                }
            }
            //Likely DXT3 or 5
            else if (type == 12)
            {
                Console.WriteLine("Known format, not implemented");
                return null;
            }
            //8bpp, 4bit luminance, 4bit alpha(?), non mip - mapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            else if (type == 16)
            {
                //Append the header first
                data = new byte[header.Length + (width*height)];
                for (int i = 0; i < header.Length; i++)
                    data[i] = header[i];

                for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int position = header.Length + getOffset(x, y, 1, width, height);

                        byte a = image.GetPixel(x,y).A;
                        byte r = image.GetPixel(x, y).R;
                        byte g = image.GetPixel(x, y).G;
                        byte b = image.GetPixel(x, y).B;

                        int luminance = (r + g + b) / 3;

                        byte pixel = (byte)((luminance & 0xF0) | (a & 0xF));

                        data[position] = pixel;
                    }
                }
            }
            //24bpp RGB, non - mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
            else if (type == 17)
            {
                //Append the header first
                data = new byte[header.Length + (width*height*3)];
                for (int i = 0; i < header.Length; i++)
                    data[i] = header[i];

                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int position = header.Length + getOffset(x, y, 3, width, height);

                        data[position] = ((byte)image.GetPixel(x, y).B);
                        data[position + 1] = ((byte)image.GetPixel(x, y).G);
                        data[position + 2] = ((byte)image.GetPixel(x, y).R);
                    }
                }
            }
            else
            {
                Console.WriteLine("Not implemented, " + type);
                return null;
            }

            return data;
        }

        static Point getXY(int offset, int pitch, int width, int height)
        {
            offset = offset / pitch;
            int l_tile = offset / 64;
            offset -= l_tile * 64;
            int m_tile = offset / 16;
            offset -= m_tile * 16;
            int s_tile = offset / 4;
            offset -= s_tile * 4;
            int remainder = offset;

            int img_tile_x = width / 8;

            int l_tile_x = (l_tile % img_tile_x) * 8;
            int l_tile_y = (l_tile / img_tile_x) * 8;

            int m_tile_x = (m_tile % 2) * 4;
            int m_tile_y = (m_tile / 2) * 4;

            int s_tile_x = (s_tile % 2) * 2;
            int s_tile_y = (s_tile / 2) * 2;

            int rem_tile_x = remainder % 2;
            int rem_tile_y = remainder / 2;

            int x = l_tile_x + m_tile_x + s_tile_x + rem_tile_x;
            int y = l_tile_y + m_tile_y + s_tile_y + rem_tile_y;

            return new Point(x, y);
        }

        static int getOffset(int x, int y, int pitch, int width, int height)
        {   
            int l_tiles_x = x / 8;
            int x_offset = x - (l_tiles_x*8);
            int m_tiles_x = x_offset / 4;
            x_offset = x_offset - (m_tiles_x*4);
            int s_tiles_x = x_offset / 2;
            x_offset = x_offset - (s_tiles_x*2);

            int l_tiles_y = y / 8;
            int y_offset = y - (l_tiles_y * 8);
            int m_tiles_y = y_offset / 4;
            y_offset = y_offset - (m_tiles_y * 4);
            int s_tiles_y = y_offset / 2;
            y_offset = y_offset - (s_tiles_y * 2);
            
            int i = l_tiles_y * (width/8) + l_tiles_x;
            int j = m_tiles_y * (8 / 4) + m_tiles_x;
            int k = s_tiles_y * (4 / 2) + s_tiles_x;
            int l = y_offset * 2 + x_offset;
            
            int final = (i * 64) + (j * 16) + (k * 4) + l;
            final *= pitch;

            return final;
        }
    }
}
