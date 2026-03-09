
What is Pr22
------------

The Passport Reader is a travel document reader and analyzer system
by ADAPTIVE RECOGNITION.

The main features of the PR system are:
 - Image scanning with different illuminations
 - Document, character and barcode recognition
 - Authentication of visual elements of documents
 - Reading and authenticating of electronic documents

This software component is the general .NET SDK for the Passport Reader
system.

Installation and dependencies
-----------------------------

The Pr22 requires the Passport Reader software to be installed, and it
works with special ID scanner hardware only. The Pr22 is compiled for
AnyCPU so the required bitness (32/64) of the Passport Reader software
depends on the application using it (or on any used third party library)
and not on the operating system architecture.

Usage
-----

The source distribution of Pr22 contains a tutorial folder with command
line sample programs. With the help of these programs, you can go through
the options of different topics. In the folder there are project file
generator scripts. The `vcs160.bat` is for Visual Studio 2019 and for new
.NET versions. The `vcs100.bat` is for compatibility with Visual Studio 2010
and .NET Framework 4.0.

The same tutorial is available in Visual Basic language located in the
vb-tutorial folder.

Also, there is a gui folder with a more
lifelike example but this demonstrates fewer options of the API than
command line samples.

### Tutorial programs:

#### pr01_open

Shows the possibilities of opening a device.

- Lists available document reader devices
- Connects to a device by name or ordinal number

The following tutorials show the easy way of opening in their Open()
methods.

#### pr02_hwinfo

Shows how to get general information about the device capabilities.

- Version information of the hw/sw components
- Usable illumination in the scanner
- Size of the scanner window
- Engine license compliance
- Available licenses for the engine

#### pr03_scanning

Shows how to parametrize the image scanning process.

- Partial scan by selecting illuminations and continuous scan.
- Double page scan
- Cleans up last page in case of failed scan.
- Cropped/scanner window image saving.
- Autostart scanning by motion detection.

The stages of the scan process will be saved into separate zip files
in order to provide the possibility of comparing them to each other.

#### pr04_analyze

Shows the main capabilities of the image processing analyzer function.

- Reading different areas of images (MRZ, VIZ, Barcode).
- Displaying details of the complex result data (called field data).
  - Field identification
  - Raw, formatted and standardized values
  - Composite and detailed result of data checks
  - Results of data field comparisons
- Saving field images.

#### pr05_doctype

Shows how to generate document type string.

- Unique page identifier code
- Descriptive type identifier string
- Related page info string

#### pr06_ecard

Demonstrates how to read and process data from ECards (including RFID
documents).

- ECard selection
- Performing authentications before starting reading ECard data (necessary
  to access certain data files).
- Reading all available files from the ECard.
- Analyzing read binary data and displaying the detailed content in the
  same way as in pr04_analyze.

Note, that this program performs the steps of the reading process one by
one. As an alternate solution, refer to the ReadDoc GUI sample program.

#### pr07_cloud

This example demonstrates how to read and process different types of data
with Carmen® ID Recognition Service (cloud authentication engine) and
GDS (remote database) solution.

- Reads MRZ locally (fastest way to get MRZ).
- Reads VIZ + barcode with cloud engine - if configured.
- Displays all data as in pr04_analyze.
- Authenticates and reads ECard.
- Analyzes ECard data in bundle.
- Generates a summary document comparing all data read.
- Checking document and personal data in the remote database - if configured.
- Uploads read data to remote database (GDS) - if configured.

### GUI program:

This example uses the MAUI framework.

#### ReadDoc

After opening the device via the menu, the reading process, which can be
started by pressing the Scan button or by automatic motion detection, is
executed according to the subtasks set in the options panel.

The program performs the entire reading process in the background. It captures
images with selected illuminations and analyzes the image data. If configured,
it uses Carmen® ID Recognition Service (cloud authentication engine).
In parallel, it reads ECard data after performing the related authentications.
Finally, it compares the data with the GDS database and logs the data entry,
if configured.

The read data is displayed in various representation forms. The display
elements can be opened or closed by clicking on their headers.
