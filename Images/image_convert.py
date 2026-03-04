# pip install Pillow

from PIL import Image

# Open the image file
img = Image.open('your_image.jpg')

# Save it as a PNG
# Pillow automatically handles the format conversion based on the extension
img.save('converted_image.png', 'PNG')

print("Conversion successful!")
