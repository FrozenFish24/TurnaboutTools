import os
import argparse
import struct
import zlib

cmp_levels = {0x7801: 0,
              0x789C: 6,
              0x78DA: 9}


def main():
    parser = argparse.ArgumentParser(description='Unpack Dai Gyakuten Saiban .arc files')
    parser.add_argument('arc', metavar='file.arc', help='the .arc file to be extracted')
    parser.add_argument('-o', dest='outdir', help='the output directory')
    args = parser.parse_args()

    if args.outdir is None:
        args.outdir = os.path.splitext(args.arc)[0]

    with open(args.arc, 'rb') as f:
        data = f.read()

    file_count = struct.unpack('<H', data[6:8])[0]

    path_list = []
    file_cmp = []

    start = 0xC
    entry_length = 0x50

    if not os.path.exists(args.outdir):
        os.makedirs(args.outdir)
    os.chdir(args.outdir)

    for i in range(0, file_count):
        file_entry = struct.unpack('<64sLLLL',
                                   data[start + (entry_length * i):start + (entry_length * i) + entry_length])

        full_path = file_entry[0].replace('\\', '/')
        full_path = full_path.split('\0', 1)[0]
        path_only = os.path.split(full_path)[0]

        data_length = file_entry[2]
        uncompressed_length = file_entry[3] - 0x40000000
        data_start = file_entry[4]

        data_end = data_start + data_length

        try:
            dec = zlib.decompress(data[data_start:data_end], zlib.MAX_WBITS, uncompressed_length)

            if not os.path.exists(path_only):
                os.makedirs(path_only)

            full_path += '.' + "%08x" % file_entry[1]
            full_path += '.' + struct.unpack('3s', dec[0:3])[0].lower()

            path_list.append(full_path)
            file_cmp.append(struct.unpack('>H', data[data_start:data_start + 2])[0])

            print('Unpacking ' + full_path)

            with open(full_path, 'wb') as f:
                f.write(dec)
        except zlib.error as e:
            print(full_path + ' failed to decompress, ' + e.message)

    with open('manifest.txt', 'w') as f:
        for i in range(0, len(path_list)):
            f.write(path_list[i] + ',' + str(cmp_levels.get(file_cmp[i], 6)) + '\n')

    os.chdir('..')


main()
