import os
import struct
import argparse
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description='Convert Dai Gyakuten Saiban .tex files to common image formats')
    parser.add_argument('tex', metavar='file.tex', help='the .tex file to be converted')
    parser.add_argument('-o', dest='outfile', help='the file name of the output image file')
    parser.add_argument('-wh', type=int, metavar=('w', 'h'), nargs=2, help='manually specify a width and height')
    parser.add_argument('-f', type=int, choices=[3, 12, 16, 17],
                        help='force the .tex file to be interpreted as the specified image type')
    args = parser.parse_args()

    if args.outfile is None:
        args.outfile = os.path.splitext(args.tex)[0] + '.png'

    with open(args.tex, 'rb') as f:
        header = f.read(0x14)
        data = f.read()

    header = struct.unpack('<9xHBxB6x', header)

    if args.f is None:
        type = header[2]
    else:
        type = args.f

    if args.wh is None:
        w = header[0] * 4
        h = header[1] * 32
    else:
        w = args.wh[0]
        h = args.wh[1]

    image = Image.new('RGBA', (w, h))

    #  32bpp RGBA, non-mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
    if type == 3:
        for i in range(0, len(data), 4):
            a, b, g, r = struct.unpack('BBBB', data[i:i + 4])
            x, y = get_xy(i, 4, image.width, image.height)
            image.putpixel((x, y), (r, g, b, a))
    # Likely DXT3 or 5
    elif type == 12:
        print('Format not implemented')
        return
    # 8bpp, 4bit luminance, 4bit alpha(?), non mip-mapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px) [IMPERFECT]
    elif type == 16:
        for i in range(0, len(data)):
            p = struct.unpack('B', data[i])[0]
            a = ((p << 4) & 0xF0) | (p & 0xF)
            p = (p & 0xF0) | (p >> 4)
            x, y = get_xy(i, 1, image.width, image.height)
            image.putpixel((x, y), (p, p, p, a))
    #  24bpp RGB, non-mipmapped, tiled 2x2 -> 2x2 -> 2x2 (8x8px)
    elif type == 17:
        for i in range(0, len(data), 3):
            b, g, r = struct.unpack('BBB', data[i:i + 3])
            x, y = get_xy(i, 3, image.width, image.height)
            image.putpixel((x, y), (r, g, b, 255))
    else:
        print('Unknown format, ' + str(type))
        return

    image.save(args.outfile)


def get_xy(offset, pitch, width, height):
    offset = offset / pitch
    l_tile = offset / 64
    offset -= l_tile * 64
    m_tile = offset / 16
    offset -= m_tile * 16
    s_tile = offset / 4
    offset -= s_tile * 4
    remainder = offset

    img_tile_x = width / 8

    l_tile_x = (l_tile % img_tile_x) * 8
    l_tile_y = (l_tile / img_tile_x) * 8

    m_tile_x = (m_tile % 2) * 4
    m_tile_y = (m_tile / 2) * 4

    s_tile_x = (s_tile % 2) * 2
    s_tile_y = (s_tile / 2) * 2

    rem_tile_x = remainder % 2
    rem_tile_y = remainder / 2

    x = l_tile_x + m_tile_x + s_tile_x + rem_tile_x
    y = l_tile_y + m_tile_y + s_tile_y + rem_tile_y

    return x, y


main()
