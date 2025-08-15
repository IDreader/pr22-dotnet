/* This example shows how to generate document type string.
 *
 *  - Unique page identifier code
 *  - Descriptive type identifier string
 *  - Related page info string
 */
namespace tutorial
{
    using System.Collections.Generic;
    using Pr22;
    using Pr22.Processing;
    using Pr22.Util;

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

            System.Console.WriteLine("Reading all the field data.");
            Pr22.Task.EngineTask ReadingTask = new Pr22.Task.EngineTask();
            //Specify the fields we would like to receive.
            ReadingTask.Add(FieldSource.All, FieldId.All);

            Document OcrDoc = OcrEngine.Analyze(DocPage, ReadingTask);

            System.Console.WriteLine();
            using (Variant vdoc = OcrDoc.ToVariant())
                System.Console.WriteLine("Document code: " + vdoc.ToInt());
            System.Console.WriteLine("Document type: " + GetDocType(OcrDoc));
            System.Console.WriteLine("Status: " + OcrDoc.GetStatus().ToString());

            string related = GetRelatedPages(OcrDoc);
            if (related.Length == 0) related = "none";
            else related = Pr22.Extension.DocumentType.GetPageName(related.Substring(0, 1));
            if (related.Length == 0) related = "other";
            System.Console.WriteLine();
            System.Console.WriteLine("Related pages: " + related);

            pr.CloseDevice();
            return 0;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Returns related pages to scan according to the OCR engine.
        /// </summary>
        /// <param name="doc">The document to process.</param>
        /// <returns>String containing one letter identifiers for each related page.</returns>
        static string GetRelatedPages(Document doc)
        {
            try
            {
                using (Pr22.Util.Variant vdoc = doc.ToVariant())
                    return vdoc.GetChild((int)Pr22.Util.VariantId.RelatedPages, 0);
            }
            catch (Pr22.Exceptions.General) { return ""; }
        }
        //----------------------------------------------------------------------

        public static string GetFieldValue(Pr22.Processing.Document Doc, Pr22.Processing.FieldId Id)
        {
            FieldReference filter = new FieldReference(FieldSource.All, Id);
            List<FieldReference> Fields = Doc.ListFields(filter);
            foreach (FieldReference FR in Fields)
            {
                Field fld = null;
                try
                {
                    fld = Doc.GetField(FR);
                    string value = fld.GetBestStringValue();
                    if (value != "") return value;
                }
                catch (Pr22.Exceptions.EntryNotFound) { }
                finally { if (fld != null) fld.Dispose(); }
            }
            return "";
        }
        //----------------------------------------------------------------------

        public static string GetDocType(Document OcrDoc)
        {
            string documentTypeName;

            using (Variant vdoc = OcrDoc.ToVariant())
                documentTypeName = Pr22.Extension.DocumentType.GetDocumentName(vdoc.ToInt());

            if (documentTypeName == "")
            {
                string issue_country = GetFieldValue(OcrDoc, FieldId.IssueCountry);
                string issue_state = GetFieldValue(OcrDoc, FieldId.IssueState);
                string doc_type = GetFieldValue(OcrDoc, FieldId.DocType);
                string doc_page = GetFieldValue(OcrDoc, FieldId.DocPage);
                string doc_subtype = GetFieldValue(OcrDoc, FieldId.DocTypeDisc);

                string tmpval = Pr22.Extension.CountryCode.GetName(issue_country);
                if (tmpval != "") issue_country = tmpval;

                documentTypeName = issue_country + new StrCon() + issue_state
                    + new StrCon() + Pr22.Extension.DocumentType.GetDocTypeName(doc_type)
                    + new StrCon("-") + Pr22.Extension.DocumentType.GetPageName(doc_page)
                    + new StrCon(",") + doc_subtype;

            }
            return documentTypeName;
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

    /// <summary>
    /// This class makes string concatenation with spaces and prefixes.
    /// </summary>
    public class StrCon
    {
        string fstr = "";
        string cstr = "";

        public StrCon() { }

        public StrCon(string bounder) { cstr = bounder + " "; }

        public static string operator +(StrCon csv, string str)
        {
            if (str != "") str = csv.cstr + str;
            if (csv.fstr != "" && str != "" && str[0] != ',') csv.fstr += " ";
            return csv.fstr + str;
        }

        public static StrCon operator +(string str, StrCon csv)
        {
            csv.fstr = str;
            return csv;
        }
    }
}
