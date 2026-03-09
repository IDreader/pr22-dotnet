/* This example demonstrates how to read and process different types of data
 * with Carmen(R) ID Recognition Service (cloud authentication engine) and
 * GDS (remote database) solution.
 *
 *  - Reads MRZ locally (fastest way to get MRZ).
 *  - Reads VIZ + barcode with cloud engine - if configured.
 *  - Displays all data as in pr04_analyze.
 *  - Authenticates and reads ECard.
 *  - Analyzes ECard data in bundle.
 *  - Generates a summary document comparing all data read.
 *  - Checking document and personal data in the remote database - if configured.
 *  - Uploads read data to remote database (GDS) - if configured.
 */
namespace tutorial
{
    using Pr22;
    using Pr22.Processing;
    using System.Collections.Generic;

    class MainClass
    {
        DocumentReaderDevice pr;
        Document AllDocs;

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
        /// <summary>
        /// Returns a list of files in a directory.
        /// </summary>
        /// <param name="dirname"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public List<string> ListFiles(string dirname, string mask)
        {
            List<string> list = new List<string>();
            try
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(dirname);
                foreach (System.IO.DirectoryInfo d in dir.GetDirectories())
                    list.AddRange(ListFiles(dir.FullName + "/" + d.Name, mask));
                foreach (System.IO.FileInfo f in dir.GetFiles(mask))
                    list.Add(f.FullName);
            }
            catch (System.Exception) { }
            return list;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Loads certificates from a directory.
        /// </summary>
        /// <param name="dir"></param>
        public void LoadCertificates(string dir)
        {
            string[] exts = { "*.cer", "*.crt", "*.der", "*.pem", "*.crl", "*.cvcert", "*.ldif", "*.ml" };
            int cnt = 0;

            foreach (string ext in exts)
            {
                List<string> list = ListFiles(dir, ext);
                foreach (string file in list)
                {
                    try
                    {
                        BinData fd = new BinData().Load(file);
                        string pk = null;
                        if (ext == "*.cvcert")
                        {
                            //Searching for private key
                            pk = file.Substring(0, file.LastIndexOf('.') + 1) + "pkcs8";
                            if (!System.IO.File.Exists(pk)) pk = null;
                        }
                        if (pk == null)
                        {
                            pr.GlobalCertificateManager.Load(fd);
                            System.Console.WriteLine("Certificate " + file + " is loaded.");
                        }
                        else
                        {
                            pr.GlobalCertificateManager.Load(fd, new BinData().Load(pk));
                            System.Console.WriteLine("Certificate " + file + " is loaded with private key.");
                        }
                        ++cnt;
                    }
                    catch (Pr22.Exceptions.General)
                    {
                        System.Console.WriteLine("Loading certificate " + file + " is failed!");
                    }
                }
            }
            if (cnt == 0) System.Console.WriteLine("No certificates loaded from " + dir);
            System.Console.WriteLine();
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Does an authentication after collecting the necessary information.
        /// </summary>
        /// <param name="SelectedCard"></param>
        /// <param name="CurrentAuth"></param>
        /// <returns></returns>
        public bool Authenticate(Pr22.ECard SelectedCard, Pr22.ECardHandling.AuthProcess CurrentAuth)
        {
            BinData AdditionalAuthData = null;
            int selector = 0;
            switch (CurrentAuth)
            {
                case Pr22.ECardHandling.AuthProcess.BAC:
                case Pr22.ECardHandling.AuthProcess.PACE:
                case Pr22.ECardHandling.AuthProcess.BAP:
                    {
                        Field field = null;
                        try
                        {
                            field = AllDocs.GetField(FieldSource.Mrz, FieldId.All);
                            selector = 1;
                        }
                        catch (Pr22.Exceptions.General)
                        {
                            try
                            {
                                field = AllDocs.GetField(FieldSource.Viz, FieldId.CAN);
                                selector = 2;
                            }
                            catch (Pr22.Exceptions.General) { }
                        }
                        if (selector > 0) AdditionalAuthData = new BinData().SetString(field.GetBasicStringValue());
                        if (field != null) field.Dispose();
                    }
                    break;

                case Pr22.ECardHandling.AuthProcess.Passive:
                case Pr22.ECardHandling.AuthProcess.Terminal:

                    //Load the certificates if not done yet
                    break;

                case Pr22.ECardHandling.AuthProcess.SelectApp:
                    List<Pr22.ECardHandling.Application> apps = SelectedCard.ListApplications();
                    if (apps.Count > 0) selector = (int)apps[0];
                    break;
            }
            try
            {
                System.Console.Write("- " + CurrentAuth + " authentication ");
                SelectedCard.Authenticate(CurrentAuth, AdditionalAuthData, selector);
                System.Console.WriteLine("succeeded");
                return true;
            }
            catch (Pr22.Exceptions.General ex)
            {
                System.Console.WriteLine("failed: " + ex.Message);
                return false;
            }
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

            //Please set the appropriate path
            LoadCertificates(pr.GetProperty("rwdata_dir") + "\\certs");

            DocScanner Scanner = pr.Scanner;
            Engine OcrEngine = pr.Engine;

            System.Console.WriteLine("Preload OCR engine.");
            Engine.Information info = OcrEngine.Info;

            //Without the following check the cloud OCR engine will not work
            if (pr.GetProperty("icldev/password") == "1")
                System.Console.WriteLine("Please set the master password first.");

            System.Console.WriteLine();

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
            VIZReadingTask.Add(FieldSource.Barcode, FieldId.All);
            VIZReadingTask.Add(FieldSource.Mrz, FieldId.B900);
            Document VizDoc = OcrEngine.Analyze(DocPage, VIZReadingTask);

            System.Console.WriteLine();
            using (Pr22.Util.Variant vdoc = VizDoc.ToVariant())
                System.Console.WriteLine("OCR engine: " + vdoc.GetChild((int)Pr22.Util.VariantId.Ocr, 0).ToString());

            AllDocs = MrzDoc + VizDoc;

            System.Console.WriteLine();
            using (Pr22.Util.Variant vdoc = AllDocs.ToVariant())
                System.Console.WriteLine("Document code: " + vdoc.ToInt());
            System.Console.WriteLine("Document type: " + GetDocType(AllDocs));
            System.Console.WriteLine("Status: " + AllDocs.GetStatus().ToString());

            System.Console.WriteLine();
            PrintDocFields(VizDoc);
            VizDoc.Save(Document.FileFormat.Xml).Save("VIZ.xml");

            List<ECardReader> CardReaders = pr.Readers;

            //Connecting to the 1st card of any reader
            ECard SelectedCard = null;
            int CardCount = 0;
            System.Console.WriteLine("Detected readers and cards:");
            foreach (ECardReader reader in CardReaders)
            {
                System.Console.WriteLine("\tReader: " + reader.Info.HwType);
                List<string> cards = reader.ListCards();
                if (SelectedCard == null && cards.Count > 0) SelectedCard = reader.ConnectCard(0);
                foreach (string card in cards)
                {
                    System.Console.WriteLine("\t\t(" + CardCount++ + ")card: " + card);
                }
                System.Console.WriteLine();
            }

            if (SelectedCard == null)
            {
                System.Console.WriteLine("No card selected!");

                System.Console.WriteLine();
                System.Console.WriteLine("All read data:");
                PrintDocFields(AllDocs);
            }
            else
            {
                System.Console.WriteLine("Executing authentications:");
                Pr22.ECardHandling.AuthProcess CurrentAuth = SelectedCard.GetNextAuthentication(false);
                bool PassiveAuthImplemented = false;

                while (CurrentAuth != Pr22.ECardHandling.AuthProcess.NoAuth)
                {
                    if (CurrentAuth == Pr22.ECardHandling.AuthProcess.Passive) PassiveAuthImplemented = true;
                    bool authOk = Authenticate(SelectedCard, CurrentAuth);
                    CurrentAuth = SelectedCard.GetNextAuthentication(!authOk);
                }
                System.Console.WriteLine();

                System.Console.WriteLine("Reading data:");
                List<Pr22.ECardHandling.File> FilesOnSelectedCard = SelectedCard.ListFiles();
                if (PassiveAuthImplemented)
                {
                    FilesOnSelectedCard.Add(Pr22.ECardHandling.FileId.CertDS);
                    FilesOnSelectedCard.Add(Pr22.ECardHandling.FileId.CertCSCA);
                }

                List<string> rfid_filenames = new List<string>();
                List<BinData> rfid_filedatas = new List<BinData>();

                foreach (Pr22.ECardHandling.File File in FilesOnSelectedCard)
                {
                    try
                    {
                        System.Console.Write("File: " + File + ".");
                        BinData RawFileData = SelectedCard.GetFile(File);
                        RawFileData.Save(File + ".dat");

                        //Executing mandatory data integrity check for Passive Authentication
                        if (PassiveAuthImplemented)
                        {
                            Pr22.ECardHandling.File f = File;
                            if (f.Id >= (int)Pr22.ECardHandling.FileId.GeneralData)
                                f = SelectedCard.ConvertFileId(f);
                            if (f.Id >= 1 && f.Id <= 16)
                            {
                                System.Console.Write(" hash check...");
                                System.Console.Write(SelectedCard.CheckHash(f) ? "OK" : "failed");
                            }
                        }

                        ////The Rfid files can be process one by one here
                        //Document FileData = OcrEngine.Analyze(RawFileData);
                        //FileData.Save(Document.FileFormat.Xml).Save(File + ".xml");
                        //AllDocs = AllDocs + FileData;
                        //System.Console.WriteLine();
                        //PrintDocFields(FileData);

                        rfid_filenames.Add(File.ToString());
                        rfid_filedatas.Add(RawFileData);
                    }
                    catch (Pr22.Exceptions.General ex)
                    {
                        System.Console.Write(" Reading failed: " + ex.Message);
                    }
                    System.Console.WriteLine();
                }
                System.Console.WriteLine();

                System.Console.WriteLine("Process RFID files in bundle:");
                List<Document> docs = OcrEngine.Analyze(rfid_filedatas, 0);
                for (int ix = 0; ix < docs.Count; ++ix)
                {
                    Document FileData = docs[ix];
                    FileData.Save(Document.FileFormat.Xml).Save(rfid_filenames[ix] + ".xml");
                    System.Console.WriteLine("File: " + rfid_filenames[ix]);
                    PrintDocFields(FileData);
                }

                System.Console.WriteLine("Authentications:");
                Document AuthData = SelectedCard.GetAuthResult();
                AuthData.Save(Document.FileFormat.Xml).Save("AuthResult.xml");
                PrintDocFields(AuthData);
                System.Console.WriteLine();

                System.Console.WriteLine("Merge all documents:");
                docs.Add(AuthData);
                docs.Add(AllDocs);
                AllDocs = OcrEngine.Merge(docs);
                System.Console.WriteLine();
                PrintDocFields(AllDocs);

                try { SelectedCard.Disconnect(); }
                catch (Pr22.Exceptions.General) { }
            }

            System.Console.WriteLine("Final status: " + AllDocs.GetStatus().ToString());
            System.Console.WriteLine();
            using (Document doc = OcrEngine.GetRootDocument())
                doc.Save(Document.FileFormat.Zipped).Save("AllDocs.zip");

            System.Console.WriteLine("Query GDS:");
            Pr22.Util.Variant chkList = null;
            try
            {
                chkList = pr.DBClient.QueryDataBase();
            }
            catch (Pr22.Exceptions.General ex)
            {
                System.Console.WriteLine(" Query failed: " + ex.Message);
            }

            if (chkList != null && chkList.NItems > 0)
            {
                for (int ix = 0; ix < chkList.NItems; ++ix)
                {
                    Pr22.Util.Variant it = chkList[ix];
                    string name = it.Name;
                    System.Console.Write(" Document is " +
                        (it.ToInt() == 1 ? "found" : "not found") + " on the list " + name);

                    System.Console.WriteLine(" Status: " +
                        ((Status)it.GetChild((int)Pr22.Util.VariantId.Checksum, 0).ToInt()).ToString());
                }
            }

            System.Console.WriteLine("Upload to GDS.");
            try
            {
                pr.DBClient.FinishDataCollection();
            }
            catch (Pr22.Exceptions.General ex)
            {
                System.Console.WriteLine(" Upload failed: " + ex.Message);
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Close the PR system (may take some time to unload the OCR engine).");
            pr.CloseDevice();
            return 0;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Returns the Authentication Method IDentifier of a field.
        /// </summary>
        /// <param name="field">The field to process.</param>
        /// <returns>String representation of AMID.</returns>
        static string GetAmid(Pr22.Processing.Field field)
        {
            try
            {
                using (Pr22.Util.Variant vfield = field.ToVariant())
                    return vfield.GetChild((int)Pr22.Util.VariantId.AMID, 0);
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

        public static string GetDocType(Pr22.Processing.Document OcrDoc)
        {
            string documentTypeName;

            using (Pr22.Util.Variant vdoc = OcrDoc.ToVariant())
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
        /// <summary>
        /// Prints a hexa dump line from a part of an array into a string.
        /// </summary>
        /// <param name="arr">The whole array.</param>
        /// <param name="pos">Position of the first item to print.</param>
        /// <param name="sz">Number of items to print.</param>
        /// <param name="split">Add extra space to some location.</param>
        static string PrintBinary(byte[] arr, int pos, int sz, bool split)
        {
            int p0;
            string str = "", str2 = "";
            for (p0 = pos; p0 < arr.Length && p0 < pos + sz; p0++)
            {
                str += arr[p0].ToString("X2") + " ";
                str2 += arr[p0] < 0x21 || arr[p0] > 0x7e ? '.' : (char)arr[p0];
            }
            for (; p0 < pos + sz; p0++) { str += "   "; str2 += " "; }
            if (split) str = str.Insert(sz / 2 * 3, " ") + " ";
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
        static void PrintDocFields(Pr22.Processing.Document doc)
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
                        //// Binary data can be printed out here
                        //for (int cnt = 0; cnt < binValue.Length; cnt += 16)
                        //    System.Console.WriteLine(PrintBinary(binValue, cnt, 16, true));
                    }
                    else
                    {
                        System.Console.WriteLine("  {0, -20}{1, -17}[{2}]", Fieldname, Status, Value);
                        if (Amid.Length > 0) System.Console.WriteLine("  {0}", Amid);
                        System.Console.WriteLine("\t{1, -31}[{0}]", FormattedValue, "   - Formatted");
                        System.Console.WriteLine("\t{1, -31}[{0}]", StandardizedValue, "   - Standardized");
                    }

                    List<Checking> lst = CurrentField.GetDetailedStatus();
                    foreach (Checking chk in lst)
                    {
                        System.Console.WriteLine(chk);
                    }

                    try
                    {
                        using (Pr22.Imaging.RawImage img = CurrentField.GetImage())
                            img.Save(Pr22.Imaging.RawImage.FileFormat.Png).Save(Fieldname + ".png");
                    }
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
