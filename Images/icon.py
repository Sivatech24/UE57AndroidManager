from PIL import Image

# Open the source image
img = Image.open('logo.webp')

# Save as ICO
# Pillow handles the internal resizing automatically
img.save('favicon.ico', format='ICO')

print("Icon created successfully!")
