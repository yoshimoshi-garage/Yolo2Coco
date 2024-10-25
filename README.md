# Yolo2Coco

C# utility to convert training data with labels in YOLO5 format to COCO format.

This utility is currently hard-coded for a local dataset on my machine, so it's near certain it would require some changes for any other application, but it provides a foundation of the basics of converting the two formats.  It currently targets Windows machines because it creates hard-links for the image files instead of making copies.
