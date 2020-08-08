LaTale Tools
---

The project requires .NET 5.

This repository provides utilities for extracting SPF, TBL and LDT files for the once popular MMORPG LaTale:
* SPF is the game resource archive file; the file is uncompressed but it is a custom format
* TBL is the image resource table files; sprites for the game are usually put together on a single image file for grouping and performance reasons and this file contains the necessary coordinate data for splicing the images into sprites
* LDT is the custom table (Excel-like) file format

Build the solution, and run `LaTaleTools.Program.exe --help` to get started.