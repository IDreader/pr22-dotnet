/* This example shows the main capabilities of the image processing analyzer function.
 *
 *  - Reading different area of images (MRZ, VIZ, Barcode).
 *  - Displaying details of the complex result data (called field data).
 *    - Field identification
 *    - Raw, formatted and standardized values
 *    - Composite and detailed result of data checks
 *    - Results of data field comparisons
 *  - Saving field images.
 */
namespace tutorial
{
    using Pr22;
    using Pr22.Processing;

    class MainClass
    {
        DocumentReaderDevice pr;

        //----------------------------------------------------------------------
        /// <summary>
        /// Opens the first document reader device.
        /// </summary>
        /// <returns></returns>
        public int Open()
        {
            System.Console.WriteLine("Opening a device");
            System.Console.WriteLine();
            pr = new DocumentReaderDevice();

            pr.Connection += onDeviceConnected;
            pr.DeviceUpdate += onDeviceUpdate;

            try { pr.UseDevice(0); }
            catch (Pr22.Exceptions.NoSuchDevice)
            {
                System.Console.WriteLine("No device found!");
                return 1;
            }

            System.Console.WriteLine("The device " + pr.DeviceName + " is opened.");
            System.Console.WriteLine();
            return 0;
        }
        //----------------------------------------------------------------------

        public int Program()
        {
            //Devices can be manipulated only after opening.
            if (Open() != 0) return 1;

            //Subscribing to scan events
            pr.ScanStarted += ScanStarted;
            pr.ImageScanned += ImageScanned;
            pr.ScanFinished += ScanFinished;
            pr.DocFrameFound += DocFrameFound;

            DocScanner Scanner = pr.Scanner;
            Engine OcrEngine = pr.Engine;

            System.Console.WriteLine("Scanning some images to read from.");
            Pr22.Task.DocScannerTask ScanTask = new Pr22.Task.DocScannerTask();
            //For OCR (MRZ) reading purposes, IR (infrared) image is recommended.
            ScanTask.Add(Pr22.Imaging.Light.White).Add(Pr22.Imaging.Light.Infra);
            Page DocPage = Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.First);
            System.Console.WriteLine();

            System.Console.WriteLine("Reading all the field data of the Machine Readable Zone.");
            Pr22.Task.EngineTask MrzReadingTask = new Pr22.Task.EngineTask();
            //Specify the fields we would like to receive.
            MrzReadingTask.Add(FieldSource.Mrz, FieldId.All);
            Document MrzDoc = OcrEngine.Analyze(DocPage, MrzReadingTask);

            System.Console.WriteLine();
            PrintDocFields(MrzDoc);
            //Returned fields by the Analyze function can be saved to an XML file:
            MrzDoc.Save(Document.FileFormat.Xml).Save("MRZ.xml");

            System.Console.WriteLine("Scanning more images for VIZ reading and image authentication.");
            //Reading from VIZ -except face photo- is available in special OCR engines only.
            ScanTask.Add(Pr22.Imaging.Light.All);
            DocPage = Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.Current);
            System.Console.WriteLine();

            System.Console.WriteLine("Reading all the textual and graphical field data as well as " +
                "authentication result from the Visual Inspection Zone.");
            Pr22.Task.EngineTask VIZReadingTask = new Pr22.Task.EngineTask();
            VIZReadingTask.Add(FieldSource.Viz, FieldId.All);
            Document VizDoc = OcrEngine.Analyze(DocPage, VIZReadingTask);

            System.Console.WriteLine();
            PrintDocFields(VizDoc);
            VizDoc.Save(Document.FileFormat.Xml).Save("VIZ.xml");

            System.Console.WriteLine("Reading barcodes.");
            Pr22.Task.EngineTask BCReadingTask = new Pr22.Task.EngineTask();
            BCReadingTask.Add(FieldSource.Barcode, FieldId.All);
            Document BcrDoc = OcrEngine.Analyze(DocPage, BCReadingTask);

            System.Console.WriteLine();
            PrintDocFields(BcrDoc);
            BcrDoc.Save(Document.FileFormat.Xml).Save("BCR.xml");

            pr.CloseDevice();
            return 0;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Returns the Authentication Method IDentifier of a field.
        /// </summary>
        /// <param name="field">The field to process.</param>
        /// <returns>String representation of AMID.</returns>
        static string GetAmid(Field field)
        {
            try
            {
                using (Pr22.Util.Variant vfield = field.ToVariant())
                    return vfield.GetChild((int)Pr22.Util.VariantId.AMID, 0);
            }
            catch (Pr22.Exceptions.General) { return ""; }
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Prints a hexa dump line from a part of an array into a string.
        /// </summary>
        /// <param name="arr">The whole array.</param>
        /// <param name="pos">Position of the first item to print.</param>
        /// <param name="sz">Number of items to print.</param>
        static string PrintBinary(byte[] arr, int pos, int sz)
        {
            int p0;
            string str = "", str2 = "";
            for (p0 = pos; p0 < arr.Length && p0 < pos + sz; p0++)
            {
                str += arr[p0].ToString("X2") + " ";
                str2 += arr[p0] < 0x21 || arr[p0] > 0x7e ? '.' : (char)arr[p0];
            }
            for (; p0 < pos + sz; p0++) { str += "   "; str2 += " "; }
            return str + str2;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Prints out all fields of a document structure to console.
        /// </summary>
        /// <remarks>
        /// Values are printed in three different forms: raw, formatted and standardized.
        /// Status (checksum result) is printed together with fieldname and raw value.
        /// At the end, images of all fields are saved into png format.
        /// </remarks>
        /// <param name="doc"></param>
        static void PrintDocFields(Document doc)
        {
            System.Collections.Generic.List<FieldReference> Fields = doc.ListFields();

            System.Console.WriteLine("  {0, -20}{1, -17}{2}", "FieldId", "Status", "Value");
            System.Console.WriteLine("  {0, -20}{1, -17}{2}", "-------", "------", "-----");
            System.Console.WriteLine();

            foreach (FieldReference CurrentFieldRef in Fields)
            {
                Field CurrentField = null;
                try
                {
                    CurrentField = doc.GetField(CurrentFieldRef);
                    string Value = "", FormattedValue = "", StandardizedValue = "";
                    byte[] binValue = null;
                    try { Value = CurrentField.GetRawStringValue(); }
                    catch (Pr22.Exceptions.EntryNotFound) { }
                    catch (Pr22.Exceptions.InvalidParameter) { binValue = CurrentField.GetBinaryValue(); }
                    try { FormattedValue = CurrentField.GetFormattedStringValue(); }
                    catch (Pr22.Exceptions.EntryNotFound) { }
                    try { StandardizedValue = CurrentField.GetStandardizedStringValue(); }
                    catch (Pr22.Exceptions.EntryNotFound) { }
                    string Amid = GetAmid(CurrentField);
                    Status Status = CurrentField.GetStatus();
                    string Fieldname = CurrentFieldRef.ToString();
                    if (binValue != null)
                    {
                        System.Console.WriteLine("  {0, -20}{1, -17}Binary", Fieldname, Status);
                        for (int cnt = 0; cnt < binValue.Length; cnt += 16)
                            System.Console.WriteLine(PrintBinary(binValue, cnt, 16));
                    }
                    else
                    {
                        System.Console.WriteLine("  {0, -20}{1, -17}[{2}]", Fieldname, Status, Value);
                        if (Amid.Length > 0) System.Console.WriteLine("  {0}", Amid);
                        System.Console.WriteLine("\t{1, -31}[{0}]", FormattedValue, "   - Formatted");
                        System.Console.WriteLine("\t{1, -31}[{0}]", StandardizedValue, "   - Standardized");
                    }

                    System.Collections.Generic.List<Checking> lst = CurrentField.GetDetailedStatus();
                    foreach (Checking chk in lst)
                    {
                        System.Console.WriteLine(chk);
                    }

                    try { CurrentField.GetImage().Save(Pr22.Imaging.RawImage.FileFormat.Png).Save(Fieldname + ".png"); }
                    catch (Pr22.Exceptions.General) { }
                }
                catch (Pr22.Exceptions.General) { }
                finally { if (CurrentField != null) CurrentField.Dispose(); }
            }
            System.Console.WriteLine();

            foreach (FieldCompare comp in doc.GetFieldCompareList())
            {
                System.Console.WriteLine("Comparing " + comp.field1 + " vs. "
                    + comp.field2 + " results " + comp.confidence);
            }
            System.Console.WriteLine();
        }
        //----------------------------------------------------------------------
        // Event handlers
        //----------------------------------------------------------------------

        void onDeviceConnected(object a, Pr22.Events.ConnectionEventArgs ev)
        {
            System.Console.WriteLine("Connection event. Device number: " + ev.DeviceNumber);
        }
        //----------------------------------------------------------------------

        void onDeviceUpdate(object a, Pr22.Events.UpdateEventArgs ev)
        {
            System.Console.WriteLine("Update event.");
            switch (ev.part)
            {
                case 1:
                    System.Console.WriteLine("  Reading calibration file from device.");
                    break;
                case 2:
                    System.Console.WriteLine("  Scanner firmware update.");
                    break;
                case 4:
                    System.Console.WriteLine("  RFID reader firmware update.");
                    break;
                case 5:
                    System.Console.WriteLine("  License update.");
                    break;
            }
        }
        //----------------------------------------------------------------------

        void ScanStarted(object a, Pr22.Events.PageEventArgs ev)
        {
            System.Console.WriteLine("Scan started. Page: " + ev.Page);
        }
        //----------------------------------------------------------------------

        void ImageScanned(object a, Pr22.Events.ImageEventArgs ev)
        {
            System.Console.WriteLine("Image scanned. Page: " + ev.Page + " Light: " + ev.Light);
        }
        //----------------------------------------------------------------------

        void ScanFinished(object a, Pr22.Events.PageEventArgs ev)
        {
            System.Console.WriteLine("Page scanned. Page: " + ev.Page + " Status: " + ev.Status);
        }
        //----------------------------------------------------------------------

        void DocFrameFound(object a, Pr22.Events.PageEventArgs ev)
        {
            System.Console.WriteLine("Document frame found. Page: " + ev.Page);
        }
        //----------------------------------------------------------------------

        public static int Main(string[] args)
        {
            try
            {
                MainClass prog = new MainClass();
                prog.Program();
            }
            catch (Pr22.Exceptions.General ex)
            {
                System.Console.Error.WriteLine(ex.Message);
            }
            System.Console.WriteLine("Press any key to exit!");
            System.Console.ReadKey(true);
            return 0;
        }
        //----------------------------------------------------------------------
    }
}
