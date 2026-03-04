from PIL import Image

img = Image.open('logo.png')

# Define the specific sizes you want included in the .ico file
icon_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]

# Save with the sizes argument
img.save('pro_icon.ico', sizes=icon_sizes)

print("Multi-size icon created!")
