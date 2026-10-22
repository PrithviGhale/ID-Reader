# AgeCheck 1.3.1 - Better image scanning

- Image Scanner now tries overlapping regions of a photo after the full-image read fails. This improves detection when a sideways PDF417 barcode occupies only a small part of the picture.
- A failed read now explains that a small or blurry barcode needs a closer, sharper photo. It never guesses an age from an unreadable image.
- Added a synthetic full-photo test with a distant sideways barcode. Photos with insufficient barcode detail may still be unreadable; retake them closer with the barcode in focus or use the physical scanner.
