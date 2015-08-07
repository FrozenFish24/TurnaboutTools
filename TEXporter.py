import os
import struct
import argparse
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description='Convert Dai Gyakuten Saiban .tex files to common image formats')
    parser.add_argument('tex', metavar='file.tex', help='the .tex file to be converted')
    parser.add_argument('-o', dest='outfile', help='the file name of the output image file')
    parser.add_argument('-wh', type=int, metavar=('w', 'h'), nargs=2, help='manually specify a width and height')
    args = parser.parse_args()

    if args.outfile is None:
        args.outfile = os.path.splitext(args.tex)[0] + '.png'

    with open(args.tex, 'rb') as f:
        header = f.read(0x14)
        data = f.read()

    header = struct.unpack('>9xBxBxB6x', header)
    type = header[2]

    if args.wh is None:
        w = header[0] * 4
        h = header[1] * 32
    else:
        w = args.wh[0]
        h = args.wh[1]

    image = Image.new('RGBA', (w, h))

    if type == 3:
        for i in range(0, len(data), 4):
            a, b, g, r = struct.unpack('BBBB', data[i:i + 4])
            x, y = get_xy(i, 4, image.width, image.height)
            image.putpixel((x, y), (r, g, b, a))
    else:
        for i in range(0, len(data), 3):
            b, g, r = struct.unpack('BBB', data[i:i + 3])
            x, y = get_xy(i, 3, image.width, image.height)
            image.putpixel((x, y), (r, g, b, 255))

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
