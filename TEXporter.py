import sys
import struct
from PIL import Image


def main():
    if len(sys.argv) < 2:
        print('Please specify a .tex file')
        return

    filename = sys.argv[1]

    with open(filename, 'rb') as f:
        header = f.read(0x14)
        data = f.read()

    header = struct.unpack('>10xH8x', header)

    # This is almost certainly wrong
    h = header[0]*32
    w = (len(data)/3)/h

    image = Image.new('RGB', (w, h))

    for i in range(0, len(data), 3):
        b, g, r = struct.unpack('BBB', data[i:i + 3])

        x, y = get_xy(i, 3, image.width, image.height)
        image.putpixel((x, y), (r, g, b))

    image.save(filename + '-out.png')


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
