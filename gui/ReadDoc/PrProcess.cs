using Pr22;
using Pr22.ECardHandling;
using Pr22.Exceptions;
using Pr22.Processing;
using System.Diagnostics;

namespace ReadDoc
{
    public class TextMessage : EventArgs
    {
        private readonly string message;
        public string Message { get { return this.message; } }
        public TextMessage(string txt) { message = txt; }
    }

    public class QueryResultEventArgs : EventArgs
    {
        private readonly Pr22.Util.Variant result;
        public Pr22.Util.Variant Result { get { return this.result; } }
        public QueryResultEventArgs(Pr22.Util.Variant res) { result = res; }
    }

    class PrProcess
    {
        public event EventHandler? ProcBegin;
        public event EventHandler? ProcEnd;
        public event EventHandler<TextMessage>? LogMessage;
        public event EventHandler<Pr22.Events.ImageEventArgs>? ImageScanned;
        public event EventHandler<Pr22.Events.PageEventArgs>? DocFrameFound;
        public event EventHandler<Pr22.Events.AuthEventArgs>? AuthBegin;
        public event EventHandler<Pr22.Events.AuthEventArgs>? AuthFinished;
        public event EventHandler<Pr22.Events.FileEventArgs>? ReadFinished;
        public event EventHandler<Pr22.Events.FileEventArgs>? FileChecked;
        public event EventHandler<QueryResultEventArgs>? QueryDone;

        private Thread? _thread = null;
        DocumentReaderDevice pr = new DocumentReaderDevice();
        Pr22.Task.TaskControl? liveTask = null;

        List<Pr22.Imaging.Light> selectedLights = new List<Pr22.Imaging.Light>();
        bool selectedDocumentView;
        List<Pr22.Processing.FieldSource> selectedOcrReadings = new List<FieldSource>();
        List<int> selectedRfidReaders = new List<int>();
        List<Pr22.ECardHandling.FileId> selectedRfidFiles = new List<FileId>();
        Pr22.ECardHandling.AuthLevel selectedAuthLevel;

        Pr22.ECard? card;
        string pass = "";
        Pr22.Processing.Document? mrzDoc;
        Pr22.Processing.Document? vizDoc;
        Pr22.Processing.Document? bcrDoc;
        Pr22.Processing.Document? summaryDoc;
        Dictionary<string, Pr22.Processing.Document?> rfidDocs = new Dictionary<string, Document?>();
        Dictionary<string, Pr22.Processing.BinData?> rfidFiles = new Dictionary<string, BinData?>();
        List<Pr22.ECardHandling.File> rfidErrorReadings = new List<Pr22.ECardHandling.File>();
        Dictionary<string, string> rfidErrorMsgs = new Dictionary<string, string>();

        private static int rfidInput; // 1: MRZ, 2: CAN, 0: not available -1: error case
        private static int scanNumber;
        private static int scanProcessed;
        private static bool process_rfidfiles_in_bundle = true;

        Stopwatch process_time = new Stopwatch();
        Stopwatch capture_time = new Stopwatch();
        Stopwatch rfid_time = new Stopwatch();
        Stopwatch ocr_time = new Stopwatch();
        Stopwatch readfile_time = new Stopwatch();

        // Thread methods / properties
        public void Start()
        {
            _thread = new Thread(new ThreadStart(this.Run));
            _thread.Start();
        }
        public void Join() { if (_thread != null) _thread.Join(); }
        public bool IsAlive { get { if (_thread != null) return _thread.IsAlive; return false; } }

        #region Init
        //----------------------------------------------------------------------

        public PrProcess()
        {
            // Subscribing to events
            pr.PresenceStateChanged += OnPresenceStateChanged;
            pr.ScanStarted += OnScanStarted;
            pr.ImageScanned += OnImageScanned;
            pr.ScanFinished += OnScanFinished;
            pr.DocFrameFound += OnDocFrameFound;
            pr.AuthBegin += OnAuthBegin;
            pr.AuthFinished += OnAuthFinished;
            pr.AuthWaitForInput += OnAuthWaitForInput;
            pr.ReadBegin += OnReadBegin;
            pr.ReadFinished += OnReadFinished;
            pr.FileChecked += OnFileChecked;
        }

        public void PreloadEngine()
        {
            Engine.Information? info = pr.Engine.Info;
        }

        public void LoadCertificates()
        {
            LogMessage?.Invoke(this, new TextMessage("Loading certificates."));
            LoadCertificates(pr.GetProperty("rwdata_dir") + "/certs");
        }

        public List<string> DeviceList
        {
            get
            {
                try { return DocumentReaderDevice.ListDevices(pr); }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error listing devices: " + ex.Message));
                }
                return new List<string>();
            }
        }

        public bool Open(int device)
        {
            try { pr.UseDevice(device); }
            catch (Pr22.Exceptions.NoSuchDevice) { return false; }
            catch { return false; }
            SetDefaults();
            return true;
        }

        public bool Open(string device)
        {
            try { pr.UseDevice(device); }
            catch (Pr22.Exceptions.NoSuchDevice) { return false; }
            catch { return false; }
            SetDefaults();
            return true;
        }

        public void Close()
        {
            if (liveTask != null)
            {
                try { liveTask.Stop(); }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error stopping task: " + ex.Message));
                }
                liveTask = null;
            }

            try { pr.CloseDevice(); }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error closing device: " + ex.Message));
            }
        }

        public void LoadCertificates(string dir)
        {
            string[] exts = { "*.cer", "*.crt", "*.der", "*.pem", "*.crl", "*.cvcert", "*.ldif", "*.ml" };

            foreach (string ext in exts)
            {
                List<string> list = FileList(dir, ext);
                foreach (string file in list)
                {
                    try
                    {
                        LogMessage?.Invoke(this, new TextMessage(file));
                        BinData fd = new BinData().Load(file);
                        string? pk = null;
                        if (ext == "*.cvcert")
                        {
                            //Searching for private key
                            pk = file.Substring(0, file.LastIndexOf('.') + 1) + "pkcs8";
                            if (!System.IO.File.Exists(pk)) pk = null;
                        }
                        if (pk == null) pr.GlobalCertificateManager.Load(fd);
                        else pr.GlobalCertificateManager.Load(fd, new BinData().Load(pk));
                    }
                    catch (Pr22.Exceptions.General) { }
                }
            }
        }

        public void SetDefaults()
        {
            // all lights are selected by default
            selectedLights = this.Lights;
            selectedDocumentView = true;
            // MRZ & VIZ are selected by default
            if (selectedOcrReadings.Count == 0)
            {
                selectedOcrReadings.Add(FieldSource.Mrz);
                selectedOcrReadings.Add(FieldSource.Viz);
            }
            // Max Auth Level is selected by default
            selectedAuthLevel = AuthLevel.Max;
            // All files is selected by default
            if (selectedRfidFiles.Count == 0)
                selectedRfidFiles.Add(FileId.All);
            // all readers are selected by default
            selectedRfidReaders.Clear();
            for (int i = 0; i < pr.Readers.Count; i++)
                selectedRfidReaders.Add(i);
            try
            {
                liveTask = pr.Scanner.StartTask(Pr22.Task.FreerunTask.Detection());
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting task: " + ex.Message));
            }

            //Without the following check the cloud OCR engine will not work
            pr.GetProperty("icldev/password");
        }

        public List<Pr22.Imaging.Light> Lights
        {
            get
            {
                try { return pr.Scanner.Info.ListLights(); }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error getting light list: " + ex.Message));
                }
                return new List<Pr22.Imaging.Light>();
            }
        }

        public string DeviceName { get { return pr.DeviceName; } }

        public List<Pr22.ECardReader> RfidReaders
        {
            get { return pr.Readers; }
        }

        #endregion

        #region Options
        //----------------------------------------------------------------------

        public bool SendRfidFilesToCloud
        {
            get { return process_rfidfiles_in_bundle; }
            set { process_rfidfiles_in_bundle = value; }
        }

        public List<Pr22.Imaging.Light> SelectedLights
        {
            get { return this.selectedLights; }
            set { this.selectedLights = value; }
        }

        public bool SelectedDocumentView
        {
            get { return this.selectedDocumentView; }
            set { this.selectedDocumentView = value; }
        }

        public List<Pr22.Processing.FieldSource> SelectedOcrReadings
        {
            get { return this.selectedOcrReadings; }
            set { this.selectedOcrReadings = value; }
        }

        public List<Pr22.ECardReader> SelectedRfidReaders
        {
            get
            {
                List<ECardReader> readers = new List<ECardReader>();
                foreach (int i in selectedRfidReaders)
                    readers.Add(pr.Readers[i]);
                return readers;
            }
            set
            {
                selectedRfidReaders.Clear();
                foreach (ECardReader reader in value)
                {
                    for (int i = 0; i < pr.Readers.Count; i++)
                    {
                        if (reader == pr.Readers[i])
                            selectedRfidReaders.Add(i);
                    }
                }
            }
        }

        public List<Pr22.ECardHandling.FileId> SelectedRfidFiles
        {
            get { return selectedRfidFiles; }
            set { selectedRfidFiles = value; }
        }

        public Pr22.ECardHandling.AuthLevel SelectedAuthLevel
        {
            get { return selectedAuthLevel; }
            set { selectedAuthLevel = value; }
        }

        public bool SelectedGdsUpload
        {
            get
            {
                try { return int.Parse(pr.GetProperty("gds/enabled")) != 0; }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error getting GDS upload setting: " + ex.Message));
                    return false;
                }
            }
            set
            {
                try { pr.SetProperty("gds/enabled", value ? "1" : "0"); }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error setting GDS upload: " + ex.Message));
                }
            }
        }

        public bool SelectedGdsActionList
        {
            get
            {
                try { return int.Parse(pr.GetProperty("gdsactlist/enabled")) != 0; }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error getting GDS action list setting: " + ex.Message));
                    return false;
                }
            }
            set
            {
                try { pr.SetProperty("gdsactlist/enabled", value ? "1" : "0"); }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error setting GDS action list: " + ex.Message));
                }
            }
        }

        #endregion

        #region Tools
        //----------------------------------------------------------------------

        public List<string> FileList(string dirname, string mask)
        {
            List<string> list = new List<string>();
            try
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(dirname);
                foreach (System.IO.DirectoryInfo d in dir.GetDirectories())
                    list.AddRange(FileList(dir.FullName + "/" + d.Name, mask));
                foreach (System.IO.FileInfo f in dir.GetFiles(mask))
                    list.Add(f.FullName);
            }
            catch (System.Exception) { }
            return list;
        }

        public MemoryStream? GetImage(int page, Pr22.Imaging.Light light)
        {
            MemoryStream? bm = null;
            try
            {
                using (Pr22.Processing.Page thePage = pr.Scanner.GetPage(page))
                {
                    Pr22.Imaging.DocImage? docImage = thePage.Select(light);

                    if (selectedDocumentView)
                    {
                        try { bm = docImage?.DocView().ToStream(); }
                        catch (Pr22.Exceptions.General) { }
                    }
                    if (bm == null) bm = docImage?.ToStream();
                }
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error getting image: " + ex.Message));
            }
            return bm;
        }

        public MemoryStream? GetFieldImage(Pr22.Processing.Field field)
        {
            try { return field?.GetImage().ToStream(); }
            catch (Pr22.Exceptions.General) { return null; }
        }

        #endregion

        #region Data Output
        //----------------------------------------------------------------------

        public Pr22.Processing.Document? GetRfidDoc(string filename)
        {
            Document? doc = null;
            lock (rfidDocs)
            {
                if (rfidDocs.ContainsKey(filename))
                    doc = rfidDocs[filename];
            }
            return doc;
        }

        public Pr22.Processing.BinData? GetRfidFileData(string filename)
        {
            BinData? data = null;
            lock (rfidFiles)
            {
                if (rfidFiles.ContainsKey(filename))
                    data = rfidFiles[filename];
            }
            return data;
        }

        public MemoryStream? GetRfidImage(Pr22.Processing.FieldId Id)
        {
            lock (rfidDocs)
            {
                foreach (KeyValuePair<string, Document?> pair in rfidDocs)
                {
                    Document? doc = pair.Value;
                    try
                    {
                        using (Field? field = doc?.GetField(new FieldReference(FieldSource.ECard, Id)))
                        {
                            if (field != null) return field?.GetImage().ToStream();
                        }
                    }
                    catch (Exception) { }
                }
            }
            return null;
        }

        public string GetRfidErrorMsg(string filename)
        {
            string errMsg = "";
            lock (rfidErrorMsgs)
            {
                if (rfidErrorMsgs.ContainsKey(filename))
                    errMsg = rfidErrorMsgs[filename];
            }
            return errMsg;
        }

        public Pr22.Processing.Document? MrzDoc
        {
            get { return mrzDoc; }
        }

        public Pr22.Processing.Document? VizDoc
        {
            get { return vizDoc; }
        }

        public Pr22.Processing.Document? BcrDoc
        {
            get { return bcrDoc; }
        }

        public Pr22.Processing.Document? SummaryDoc
        {
            get
            {
                Pr22.Processing.Document? doc = null;
                lock (this)
                {
                    doc = summaryDoc;
                }
                return doc;
            }
        }

        #endregion

        #region Scanning Process
        //----------------------------------------------------------------------

        protected void ClearData()
        {
            card = null;
            if (mrzDoc != null)
            {
                mrzDoc.Dispose();
                mrzDoc = null;
            }
            if (vizDoc != null)
            {
                vizDoc.Dispose();
                vizDoc = null;
            }
            if (bcrDoc != null)
            {
                bcrDoc.Dispose();
                bcrDoc = null;
            }
            if (summaryDoc != null)
            {
                summaryDoc.Dispose();
                summaryDoc = null;
            }
            foreach (KeyValuePair<string, Document?> pair in rfidDocs)
            {
                if (pair.Value != null)
                    pair.Value.Dispose();
            }
            rfidDocs.Clear();
            rfidFiles.Clear();
            rfidErrorReadings.Clear();
            rfidErrorMsgs.Clear();
        }

        public void Run()
        {
            try
            {
                ClearData();

                pass = "";
                rfidInput = 0;
                scanNumber = 0;
                scanProcessed = 0;

                ProcBegin?.Invoke(this, EventArgs.Empty);

                process_time.Restart();

                if (selectedRfidReaders.Count == 0)
                {
                    pass = "NOCARD";
                    rfidInput = -1;
                    if (selectedOcrReadings.Count == 0)
                    {
                        CaptureOnly();
                    }
                    else
                    {
                        CaptureAndOcr();
                        while (Interlocked.CompareExchange(ref scanProcessed, 0, 0) < 2)
                            Thread.Sleep(5);
                    }
                }
                else
                {
                    CaptureWithRf();
                }
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error during processing: " + ex.Message));
            }
            finally
            {
                process_time.Stop();
                ProcEnd?.Invoke(this, EventArgs.Empty);
                LogMessage?.Invoke(this, new TextMessage(string.Format("Total processing time: {0} ms", process_time.ElapsedMilliseconds)));
            }

            HandleDB();
        }

        void CaptureOnly()
        {
            DocScanner? Scanner = pr.Scanner;

            Pr22.Task.DocScannerTask ScanTask = new Pr22.Task.DocScannerTask();
            foreach (var light in selectedLights) ScanTask.Add(light);
            Pr22.Task.TaskControl? onlyScan = null;
            try
            {
                onlyScan = Scanner?.StartScanning(ScanTask, Pr22.Imaging.PagePosition.First);
                onlyScan?.Wait();
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting scanning: " + ex.Message));
            }
        }

        void CaptureAndOcr()
        {
            // 1. Start scanning Infra in background

            DocScanner? Scanner = pr.Scanner;

            Pr22.Task.DocScannerTask ScanTask = new Pr22.Task.DocScannerTask();
            ScanTask.Add(Pr22.Imaging.Light.Infra);
            Pr22.Task.TaskControl? firstScan = null;
            try
            {
                firstScan = Scanner?.StartScanning(ScanTask, Pr22.Imaging.PagePosition.First);
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting scanning: " + ex.Message));
            }

            try
            {
                firstScan?.Wait();
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error waiting for scan task: " + ex.Message));
            }

            // 6. Start scanning other lights in background (PagePosition.Current)

            foreach (var light in selectedLights) ScanTask.Add(light);
            Pr22.Task.TaskControl? secondScan = null;
            try
            {
                secondScan = Scanner?.StartScanning(ScanTask, Pr22.Imaging.PagePosition.Current);
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting scan task: " + ex.Message));
            }

            try
            {
                secondScan?.Wait();
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error waiting for scan task: " + ex.Message));
            }
        }

        void CaptureWithRf()
        {
            // 1. Start scanning Infra in background

            DocScanner? Scanner = pr.Scanner;

            Pr22.Task.DocScannerTask ScanTask = new Pr22.Task.DocScannerTask();

            ScanTask.Add(Pr22.Imaging.Light.Infra);
            Pr22.Task.TaskControl? firstScan = null;
            try
            {
                firstScan = Scanner?.StartScanning(ScanTask, Pr22.Imaging.PagePosition.First);
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting scanning: " + ex.Message));
            }

            // 2. Searching for ECard + Connect + start reading

            rfid_time.Restart();

            List<ECardReader>? readers = pr.Readers;
            Pr22.Task.TaskControl? rfidReadingTask = null;

            for (int r = 0; r < selectedRfidReaders.Count; r++)
            {
                if (selectedRfidReaders[r] >= readers?.Count)
                    continue;

                ECardReader? CardReader = readers?[selectedRfidReaders[r]];

                try
                {
                    List<string>? cards = CardReader?.ListCards();

                    if (cards?.Count > 0)
                    {
                        card = CardReader?.ConnectCard(0);
                        Pr22.Task.ECardTask rfidTask = new Pr22.Task.ECardTask();
                        rfidTask.AuthLevel = selectedAuthLevel;
                        for (int f = 0; f < selectedRfidFiles.Count; f++)
                            rfidTask.Add(selectedRfidFiles[f]);
                        rfidReadingTask = CardReader?.StartRead(card, rfidTask);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error starting ecard reading: " + ex.Message));
                }
            }
            if (card == null) pass = "NOCARD";

            // 4. Wait scan task in main thread

            try
            {
                firstScan?.Wait();
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error waiting for scan task: " + ex.Message));
            }

            // 6. Start scanning other lights in background (PagePosition.Current)

            ScanTask.Add(Pr22.Imaging.Light.White); // required for PACE with CAN
            foreach (var light in selectedLights) ScanTask.Add(light);
            Pr22.Task.TaskControl? secondScan = null;
            try
            {
                secondScan = Scanner?.StartScanning(ScanTask, Pr22.Imaging.PagePosition.Current);
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error starting scan task: " + ex.Message));
            }

            try
            {
                secondScan?.Wait();
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error waiting for scan task: " + ex.Message));
            }

            while (Interlocked.CompareExchange(ref scanProcessed, 0, 0) < 2 && Interlocked.CompareExchange(ref rfidInput, 0, 0) == 0)
                Thread.Sleep(5);

            Interlocked.CompareExchange(ref rfidInput, -1, 0);

            if (rfidReadingTask != null)
            {
                try
                {
                    rfidReadingTask.Wait();
                }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error waiting for rfid reading task: " + ex.Message));
                }
            }

            if (card != null)
            {
                try
                {
                    Document? authData = card.GetAuthResult();
                    if (authData != null)
                    {
                        lock (rfidDocs)
                        {
                            rfidDocs["AuthData"] = authData;
                        }
                    }
                }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error getting auth data: " + ex.Message));
                }
                // Get error details
                foreach (Pr22.ECardHandling.File file in rfidErrorReadings)
                {
                    try
                    {
                        BinData RawFileData = card.GetFile(file);
                    }
                    catch (Pr22.Exceptions.General ex)
                    {
                        lock (rfidErrorMsgs) { rfidErrorMsgs[file.ToString()] = ex.Message; }
                    }
                }
                try
                {
                    card.Disconnect();
                    card = null;
                }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error waiting for disconnect: " + ex.Message));
                }
            }

            if (process_rfidfiles_in_bundle)
            {
                // 9. Send RFID files to cloud
                List<string> fileids = new List<string>();
                List<BinData> filedatas = new List<BinData>();
                foreach (KeyValuePair<string, BinData?> pair in rfidFiles)
                {
                    if (pair.Value == null) continue;
                    fileids.Add(pair.Key);
                    filedatas.Add(pair.Value);
                }

                List<Document>? docs = null;
                try
                {
                    docs = pr.Engine.Analyze(filedatas, 0);
                    for (int i = 0; i < fileids.Count; i++)
                    {
                        lock (rfidDocs) { rfidDocs[fileids[i]] = docs[i]; }
                    }
                }
                catch (Pr22.Exceptions.General ex)
                {
                    LogMessage?.Invoke(this, new TextMessage("Error processing ecard data: " + ex.Message));
                }

                if (docs == null)
                    docs = new List<Document>();

                if (summaryDoc != null)
                    docs.Add(summaryDoc);

                if (docs.Count > 0)
                {
                    try
                    {
                        summaryDoc = pr.Engine.Merge(docs);
                    }
                    catch (Pr22.Exceptions.General ex)
                    {
                        LogMessage?.Invoke(this, new TextMessage("Error summarizing data: " + ex.Message));
                    }
                }
            }
        }

        void HandleDB()
        {
            // Upload to GDS
            try
            {
                if (int.Parse(pr.GetProperty("gds/enabled")) != 0)
                {
                    pr.DBClient.FinishDataCollection();
                }
            }
            catch (Exception e)
            {
                LogMessage?.Invoke(this, new TextMessage(e.Message));
            }

            // Query GDS
            try
            {
                if (SummaryDoc != null && int.Parse(pr.GetProperty("gdsactlist/enabled")) != 0)
                {
                    using (Pr22.Util.Variant chkList = pr.DBClient.QueryDataBase())
                        if (chkList != null) QueryDone?.Invoke(this, new QueryResultEventArgs(chkList));
                }
            }
            catch (Exception e)
            {
                LogMessage?.Invoke(this, new TextMessage(e.Message));
            }
        }

        #endregion

        #region Events
        //----------------------------------------------------------------------

        // To raise this event FreerunTask.Detection() has to be started.
        void OnPresenceStateChanged(object? sender, Pr22.Events.DetectionEventArgs e)
        {
            if (e.State == Pr22.Util.PresenceState.Present)
            {
                Start();
            }
        }

        void OnScanStarted(object? sender, Pr22.Events.PageEventArgs e)
        {
            capture_time.Restart();
            LogMessage?.Invoke(this, new TextMessage(string.Format("Scan started. Page: {0}", e.Page)));
        }

        void OnImageScanned(object? sender, Pr22.Events.ImageEventArgs e)
        {
            capture_time.Stop();
            LogMessage?.Invoke(this, new TextMessage(string.Format("Image scanned. Page: {0} Light: {1} Capture time: {2} ms", e.Page, e.Light, capture_time.ElapsedMilliseconds)));
            ImageScanned?.Invoke(this, e);
            capture_time.Restart();
        }

        void OnScanFinished(object? sender, Pr22.Events.PageEventArgs e)
        {
            scanNumber++;

            if (e.Status != ErrorCodes.NoErr)
            {
                LogMessage?.Invoke(this, new TextMessage(string.Format("Scan error. Page: {0} Status: {1}", e.Page, e.Status)));
                Interlocked.Increment(ref scanProcessed);
                return;
            }

            try
            {
                if (scanNumber == 1)
                {
                    if (pass.Length == 0 || selectedOcrReadings.Contains(FieldSource.Mrz))
                    {
                        // 3. Reading MRZ in PageScanned event

                        Pr22.Task.EngineTask MrzReadingTask = new Pr22.Task.EngineTask();
                        MrzReadingTask.Add(FieldSource.Mrz, FieldId.All);
                        mrzDoc = Analyze(MrzReadingTask, e.Page, "MRZ");
                    }
                }

                // 7. Reading Viz in 2nd PageScanned event

                if (scanNumber == 2)
                {
                    if (pass.Length == 0 || selectedOcrReadings.Contains(FieldSource.Viz))
                    {
                        Pr22.Task.EngineTask VIZReadingTask = new Pr22.Task.EngineTask();
                        if (selectedOcrReadings.Contains(FieldSource.Viz))
                            VIZReadingTask.Add(FieldSource.Viz, FieldId.All);
                        else
                            VIZReadingTask.Add(FieldSource.Viz, FieldId.CAN);
                        // 7.a The FieldId.SecurityFibres should be excluded for speed
                        VIZReadingTask.Del(FieldSource.Viz, FieldId.SecurityFibres);
                        vizDoc = Analyze(VIZReadingTask, e.Page, "VIZ");
                    }

                    if (selectedOcrReadings.Contains(FieldSource.Barcode))
                    {
                        Pr22.Task.EngineTask BCRReadingTask = new Pr22.Task.EngineTask();
                        BCRReadingTask.Add(FieldSource.Barcode, FieldId.All);
                        bcrDoc = Analyze(BCRReadingTask, e.Page, "BARCODE");
                    }
                }
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error processing page: " + ex.Message));
            }
            finally
            {
                Interlocked.Increment(ref scanProcessed);
                LogMessage?.Invoke(this, new TextMessage(string.Format("Page scanned. Page: {0} Status: {1}", e.Page, e.Status)));
            }
        }

        Document? Analyze(Pr22.Task.EngineTask ReadingTask, int pageIndex, string title)
        {
            ocr_time.Restart();
            Document? doc = null;
            try
            {
                using (Pr22.Processing.Page? dataPage = pr.Scanner.GetPage(pageIndex))
                {
                    doc = pr.Engine.Analyze(dataPage, ReadingTask);
                    if (doc != null)
                    {
                        lock (this)
                        {
                            summaryDoc = summaryDoc + doc;
                        }
                        try
                        {
                            Field? mrz = doc?.GetField(FieldSource.Mrz, FieldId.All);
                            if (mrz != null && mrz.GetStatus() < Status.Error)
                            {
                                pass = mrz.GetRawStringValue();
                                Interlocked.Exchange(ref rfidInput, 1);
                            }
                        }
                        catch (Pr22.Exceptions.General) { }
                        try
                        {
                            Field? field = doc?.GetField(FieldSource.Viz, FieldId.CAN);
                            if (field != null && field.GetStatus() < Status.Error)
                            {
                                pass = field.GetRawStringValue();
                                Interlocked.Exchange(ref rfidInput, 2);
                            }
                        }
                        catch (Pr22.Exceptions.General) { }
                    }
                }
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error reading " + title + ": " + ex.Message));
            }
            finally
            {
                ocr_time.Stop();
                LogMessage?.Invoke(this, new TextMessage(string.Format(title + " OCR time: {0}", ocr_time.ElapsedMilliseconds)));
            }
            return doc;
        }

        void OnDocFrameFound(object? sender, Pr22.Events.PageEventArgs e)
        {
            DocFrameFound?.Invoke(this, e);
        }

        void OnAuthBegin(object? sender, Pr22.Events.AuthEventArgs e)
        {
            AuthBegin?.Invoke(this, e);
            LogMessage?.Invoke(this, new TextMessage(string.Format("Auth Begin: {0}", e.Authentication.ToString())));
        }

        void OnAuthFinished(object? sender, Pr22.Events.AuthEventArgs e)
        {
            AuthFinished?.Invoke(this, e);
            string errstr = e.Result.ToString();
            if (!Enum.IsDefined(typeof(Pr22.Exceptions.ErrorCodes), e.Result))
                errstr = ((int)e.Result).ToString("X4");
            LogMessage?.Invoke(this, new TextMessage("Auth Finished: " + e.Authentication.ToString() + " status: " + errstr));
        }

        void OnAuthWaitForInput(object? sender, Pr22.Events.AuthEventArgs e)
        {
            LogMessage?.Invoke(this, new TextMessage(string.Format("Auth Wait For Input: {0}", e.Authentication.ToString())));
            Stopwatch auth_time = new Stopwatch();
            auth_time.Start();
            try
            {
                // 5. If MRZ is present, it must be forwarded to Authenticate method in AuthWaitForInput event

                // 8. If there is no MRZ, but AuthWaitForInput event is raised, then the CAN number should
                // be presented as input to th Authenticate method. If there are no MRZ nor CAN, then a dummy
                // input have to be used to avoid hang up.
                BinData AdditionalAuthData = new BinData();

                int rfInp = -1;

                while ((rfInp = Interlocked.CompareExchange(ref rfidInput, 0, 0)) == 0)
                    Thread.Sleep(5);

                if (e.Authentication == AuthProcess.BAC || e.Authentication == AuthProcess.PACE)
                {
                    if (rfInp >= 1)
                    {
                        AdditionalAuthData.SetString(pass);
                        card?.Authenticate(e.Authentication, AdditionalAuthData, rfInp);
                    }
                    else
                    {
                        AdditionalAuthData.SetString("NONE");
                        card?.Authenticate(e.Authentication, AdditionalAuthData, 1);
                    }
                }
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error setting password: " + ex.Message));
            }
            finally
            {
                auth_time.Stop();
                LogMessage?.Invoke(this, new TextMessage(string.Format("{0} time: {1} ms", e.Authentication.ToString(), auth_time.ElapsedMilliseconds)));
            }
        }

        void OnReadBegin(object? sender, Pr22.Events.FileEventArgs e)
        {
            LogMessage?.Invoke(this, new TextMessage(string.Format("Read file: {0}", e.FileId.ToString())));
            readfile_time.Restart();
        }

        void OnReadFinished(object? sender, Pr22.Events.FileEventArgs e)
        {
            if (e.FileId.Id == (int)FileId.All)
            {
                rfid_time.Stop();
                LogMessage?.Invoke(this, new TextMessage(string.Format("Total RFID time: {0} ms", rfid_time.ElapsedMilliseconds)));
                return;
            }

            try
            {
                if (e.Result == ErrorCodes.NoErr)
                {
                    lock (this)
                    {
                        BinData? RawFileData = card?.GetFile(e.FileId);
                        lock (rfidFiles) { rfidFiles[e.FileId.ToString()] = RawFileData; }

                        if (!process_rfidfiles_in_bundle)
                        {
                            Document? RfidFileData = pr.Engine.Analyze(RawFileData);
                            summaryDoc = summaryDoc + RfidFileData;
                            lock (rfidDocs) { rfidDocs[e.FileId.ToString()] = RfidFileData; }
                        }
                    }
                }
                else
                {
                    // Save error readings
                    lock (rfidErrorReadings)
                    {
                        if (rfidErrorReadings.Where(file => file.Id == e.FileId.Id).Count() == 0)
                            rfidErrorReadings.Add(e.FileId);
                    }
                }
                readfile_time.Stop();
                LogMessage?.Invoke(this, new TextMessage(string.Format("File read: {0} Status: {1} Time: {2} ms", e.FileId.ToString(), e.Result.ToString(), readfile_time.ElapsedMilliseconds)));
            }
            catch (Pr22.Exceptions.General ex)
            {
                LogMessage?.Invoke(this, new TextMessage("Error processing ecard data: " + ex.Message));
            }
            ReadFinished?.Invoke(this, e);
        }

        void OnFileChecked(object? sender, Pr22.Events.FileEventArgs e)
        {
            FileChecked?.Invoke(this, e);
            LogMessage?.Invoke(this, new TextMessage(string.Format("File Checked: {0}", e.FileId.ToString())));
        }

        #endregion
    }
}
