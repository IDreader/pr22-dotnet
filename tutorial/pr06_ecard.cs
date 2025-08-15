/* This example demonstrates how to read and process data from ECards (including
 * RFID documents).
 *
 *  - ECard selection
 *  - Performing authentications before starting reading ECard data (necessary
 *    to access certain data files).
 *  - Reading all available files from the ECard.
 *  - Analyzing read binary data and displaying the detailed content in the
 *    same way as in pr04_analyze.
 *
 * Note, that this program performs the steps of the reading process one by
 * one. As an alternate solution, refer to the ReadDoc GUI sample program.
 */
namespace tutorial
{
    using Pr22;
    using Pr22.Processing;
    using System.Collections.Generic;

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
        public bool Authenticate(ECard SelectedCard, Pr22.ECardHandling.AuthProcess CurrentAuth)
        {
            BinData AdditionalAuthData = null;
            int selector = 0;
            switch (CurrentAuth)
            {
                case Pr22.ECardHandling.AuthProcess.BAC:
                case Pr22.ECardHandling.AuthProcess.PACE:
                case Pr22.ECardHandling.AuthProcess.BAP:
                    //Read MRZ (necessary for BAC, PACE and BAP)
                    Pr22.Task.DocScannerTask ScanTask = new Pr22.Task.DocScannerTask();
                    ScanTask.Add(Pr22.Imaging.Light.Infra);
                    Page FirstPage = pr.Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.First);

                    Pr22.Task.EngineTask MrzReadingTask = new Pr22.Task.EngineTask();
                    MrzReadingTask.Add(FieldSource.Mrz, FieldId.All);
                    Document MrzDoc = pr.Engine.Analyze(FirstPage, MrzReadingTask);
                    FirstPage.Dispose();

                    using (Field field = MrzDoc.GetField(FieldSource.Mrz, FieldId.All))
                        AdditionalAuthData = new BinData().SetString(field.GetRawStringValue());
                    selector = 1;
                    MrzDoc.Dispose();
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
                SelectedCard.Authenticate(CurrentAuth, AdditionalAuthData, selector);
                System.Console.WriteLine("- " + CurrentAuth + " authentication succeeded");
                return true;
            }
            catch (Pr22.Exceptions.General ex)
            {
                System.Console.WriteLine("- " + CurrentAuth + " authentication failed: " + ex.Message);
                return false;
            }
        }
        //----------------------------------------------------------------------

        public int Program()
        {
            //Devices can be manipulated only after opening.
            if (Open() != 0) return 1;

            //Please set the appropriate path
            LoadCertificates(pr.GetProperty("rwdata_dir") + "\\certs");

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
                return 1;
            }

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
            foreach (Pr22.ECardHandling.File File in FilesOnSelectedCard)
            {
                try
                {
                    System.Console.Write("File: " + File + ".");
                    BinData RawFileData = SelectedCard.GetFile(File);
                    RawFileData.Save(File + ".dat");
                    Document FileData = pr.Engine.Analyze(RawFileData);
                    FileData.Save(Document.FileFormat.Xml).Save(File + ".xml");

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
                    System.Console.WriteLine();
                    PrintDocFields(FileData);
                }
                catch (Pr22.Exceptions.General ex)
                {
                    System.Console.Write(" Reading failed: " + ex.Message);
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine("Authentications:");
            Document AuthData = SelectedCard.GetAuthResult();
            AuthData.Save(Document.FileFormat.Xml).Save("AuthResult.xml");
            PrintDocFields(AuthData);
            System.Console.WriteLine();

            try { SelectedCard.Disconnect(); }
            catch (Pr22.Exceptions.General) { }

            pr.CloseDevice();
            return 0;
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
                    Status Status = CurrentField.GetStatus();
                    string Fieldname = CurrentFieldRef.ToString();
                    if (binValue != null)
                    {
                        System.Console.WriteLine("  {0, -20}{1, -17}Binary", Fieldname, Status);
                        //// Binary data can be printed out here
                        //for (int cnt = 0; cnt < binValue.Length; cnt += 16)
                        //    System.Console.WriteLine(PrintBinary(binValue, cnt, 16));
                    }
                    else
                    {
                        System.Console.WriteLine("  {0, -20}{1, -17}[{2}]", Fieldname, Status, Value);
                        System.Console.WriteLine("\t{1, -31}[{0}]", FormattedValue, "   - Formatted");
                        System.Console.WriteLine("\t{1, -31}[{0}]", StandardizedValue, "   - Standardized");
                    }

                    List<Checking> lst = CurrentField.GetDetailedStatus();
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
