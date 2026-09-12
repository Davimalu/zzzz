with open("Tux.bmp", "rb") as f:
    plain = f.read()

with open("Tux.bmp.enc", "rb") as f:
    enc_full = f.read()

HEADER_SIZE = 54  # standard BITMAPFILEHEADER + BITMAPINFOHEADER

repaired = plain[:HEADER_SIZE] + enc_full[HEADER_SIZE:]

with open("penguin_ecb.bmp", "wb") as f:
    f.write(repaired)
