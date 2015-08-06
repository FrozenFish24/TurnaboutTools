import os
import struct
import zlib
import argparse


def main():
    parser = argparse.ArgumentParser(description='Repack extracted Dai Gyakuten Saiban .arc files')
    parser.add_argument('dir', help='the directory containing the extracted .arc and manifest.txt')
    parser.add_argument('-o', dest='outfile', help='the file name of the output .arc file')
    args = parser.parse_args()

    if args.outfile is None:
        args.outfile = args.dir + '-repacked.arc'

    if not os.path.exists(args.dir):
        print("Directory doesn't exist")
        return
    else:
        os.chdir(args.dir)

    if not os.path.isfile('manifest.txt'):
        print("manifest.txt does not exist, arc files will not work in-game if file order isn't preserved")
        return

    with open('manifest.txt', 'r') as f:
        file_list = f.readlines()

    arc_index = struct.pack('<3sxBxH4x', 'ARC', 0x11, len(file_list))
    arc_data = ''
    arc_index_length = 0xC + 0x50 * len(file_list)

    if arc_index_length % 32 != 0:
        arc_index_length = ((arc_index_length / 32) + 1) * 32

    for filename in file_list:

        filename = filename.rstrip('\n')

        my_tuple = os.path.splitext(filename)  # Re-name and un-tuple this

        if len(my_tuple[1]) < 8:
            my_tuple = os.path.splitext(my_tuple[0])

        name_only = my_tuple[0]
        ext = int(my_tuple[1][1:], 16)

        arc_index += struct.pack('<64sI', name_only, ext)

        with open(filename, 'rb') as f:
            file_str = f.read()

        data_position = (arc_index_length + len(arc_data))

        comp = zlib.compress(file_str, 6)  # 6 Gives identical compression to original archive
        arc_data += comp
        real_len = len(file_str) | 0x40000000

        arc_index += struct.pack('<III', len(comp), real_len, data_position)

        print('Packing ' + filename)

    os.chdir('..')

    for i in range(len(arc_index), arc_index_length):
        arc_index += struct.pack('<B', 0)

    arc_index += arc_data

    with open(args.outfile, 'wb') as f:
        f.write(arc_index)


main()
