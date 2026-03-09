using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using Pr22.Processing;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ReadDoc
{
    public class ImageResult
    {
        public ImageResult() { }
        [SetsRequiredMembers]
        public ImageResult(string name, Func<Stream> stream)
        {
            Name = name; Image = ImageSource.FromStream(stream);
        }
        public required string Name { get; set; }
        public ImageSource? Image { get; set; }
    }

    public class ScanResult
    {
        public int CapPage { get; set; }
        public required string CapName { get; set; }
        public ImageSource? CapImage { get; set; }
    }

    public class OcrResult
    {
        public Pr22.Processing.FieldReference FieldRef;
        public required string FieldName { get; set; }
        public string? FieldValue { get; set; }
        public string? FormattedValue { get; set; }
        public string? StandardizedValue { get; set; }
        public required string FieldStatus { get; set; }
    }

    public class RfidFile
    {
        public required string Name { get; set; }
        public Pr22.ECardHandling.File file { get; set; }
        public Pr22.Exceptions.ErrorCodes result { get; set; }
        public required Color DisplayForecolor { get; set; }
    }

    public class RfidField
    {
        public Pr22.Processing.FieldReference FieldRef;
        public required string FieldName { get; set; }
        public string? FieldValue { get; set; }
        public ImageSource? FieldImage { get; set; }
        public int DisplayRowHeight { get; set; }
    }

    public class SummaryResult
    {
        public required string FieldName { get; set; }
        public string? FieldValue { get; set; }
    }

    public class CrossCheckResult
    {
        public required string Field1 { get; set; }
        public required string Field2 { get; set; }
        public int Confidence { get; set; }
    }

    public partial class MainPage : ContentPage
    {
        private readonly IFileSaver _fileSaver;

        PrProcess proc = new PrProcess();

        internal ObservableCollection<ScanResult> capImages = new ObservableCollection<ScanResult>();

        internal ObservableCollection<OcrResult> mrzFields = new ObservableCollection<OcrResult>();
        internal ObservableCollection<OcrResult> vizFields = new ObservableCollection<OcrResult>();
        internal ObservableCollection<OcrResult> bcrFields = new ObservableCollection<OcrResult>();
        internal ObservableCollection<OcrResult> sumFields = new ObservableCollection<OcrResult>();

        internal ObservableCollection<SummaryResult> personalData = new ObservableCollection<SummaryResult>();
        internal ObservableCollection<SummaryResult> documentData = new ObservableCollection<SummaryResult>();

        internal ObservableCollection<CrossCheckResult> crossChecks = new ObservableCollection<CrossCheckResult>();

        internal ObservableCollection<ImageResult> photos = new ObservableCollection<ImageResult>();
        internal ObservableCollection<ImageResult> fingerprints = new ObservableCollection<ImageResult>();
        internal ObservableCollection<ImageResult> signatures = new ObservableCollection<ImageResult>();
        internal ObservableCollection<RfidFile> files = new ObservableCollection<RfidFile>();

        public MainPage(IFileSaver fileSaver)
        {
            InitializeComponent();

            proc.ProcBegin += ProcBegin;
            proc.ProcEnd += ProcEnd;
            proc.LogMessage += LogMessage;
            proc.ImageScanned += ImageScanned;
            proc.ReadFinished += ReadFinished;
            proc.QueryDone += QueryDone;

            var PiTimer = Dispatcher.CreateTimer();
            PiTimer.Interval = TimeSpan.FromMilliseconds(200);
            PiTimer.IsRepeating = false;
            PiTimer.Tick += PostInitEvent;
            PiTimer.Start();

            Capture_Results.ItemsSource = capImages;

            Files_List.ItemsSource = files;

            MRZ_Results.ItemsSource = mrzFields;
            VIZ_Results.ItemsSource = vizFields;
            BCR_Results.ItemsSource = bcrFields;
            All_Results.ItemsSource = sumFields;
            CrossCheck_Results.ItemsSource = crossChecks;

            Personal_data.ItemsSource = personalData;
            Document_data.ItemsSource = documentData;
            Face_Image.ItemsSource = photos;
            Signature_Image.ItemsSource = signatures;

            Unloaded += MainPage_Unloaded;
            _fileSaver = fileSaver;
        }

        private void MainPage_Unloaded(object? sender, EventArgs e)
        {
            proc.Close();
        }

        internal static string? GetSecureData(string key)
        {
            try
            {
                Task<string?> task = SecureStorage.Default.GetAsync(key);
                if (task.Wait(100))
                    return task.Result;
            }
            catch { }
            return null;
        }

        internal static void SetSecureData(string key, string value)
        {
            try
            {
                Task task = SecureStorage.Default.SetAsync(key, value);
                task.Wait(100);
            }
            catch { }
        }

        private void PostInitEvent(object? sender, EventArgs e)
        {
            LogMessage(this, new TextMessage("Initializing pr system."));
            proc.PreloadEngine();
            proc.LoadCertificates();
            LogMessage(this, new TextMessage("Ready."));
        }

        private void ProcBegin(object? sender, EventArgs e)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                cmdOptions.IsEnabled = false;
                cmdScan.IsEnabled = false;
                cmdClose.IsEnabled = false;

                cmdDownload.IsEnabled = false;
                cmdSaveXml.IsEnabled = false;

                MainLogs.Text = "";

                capImages.Clear();
                Capture_Results.ItemsSource = capImages;

                mrzFields.Clear();
                vizFields.Clear();
                bcrFields.Clear();
                sumFields.Clear();

                crossChecks.Clear();

                personalData.Clear();
                documentData.Clear();

                photos.Clear();
                fingerprints.Clear();
                signatures.Clear();
                files.Clear();
            });
        }

        private void ProcEnd(object? sender, EventArgs e)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                Document? auth = proc.GetRfidDoc("AuthData");
                if (auth != null)
                    files.Add(new RfidFile()
                    {
                        Name = "AuthData",
                        file = new Pr22.ECardHandling.File(0),
                        result = 0,
                        DisplayForecolor = Color.FromRgb(0, 0, 0)
                    });

                Document? mrzDoc = proc.MrzDoc;
                FillTableData(mrzFields, mrzDoc);
                Document? vizDoc = proc.VizDoc;
                FillTableData(vizFields, vizDoc);
                Document? bcrDoc = proc.BcrDoc;
                FillTableData(bcrFields, bcrDoc);
                Document? sumDoc = proc.SummaryDoc;
                FillTableData(sumFields, sumDoc);

                FillTableFieldCompare(crossChecks, sumDoc, auth);

                FillDataPage();

                cmdOptions.IsEnabled = true;
                cmdScan.IsEnabled = true;
                cmdClose.IsEnabled = true;
            });
        }

        private void LogMessage(object? sender, TextMessage e)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                MainLogs.Text += e.Message + Environment.NewLine;
                MainLogsScrollView.ScrollToAsync(MainLogs, ScrollToPosition.End, true);
            });
        }

        private void ImageScanned(object? sender, Pr22.Events.ImageEventArgs e)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                ScanResult result = new ScanResult
                {
                    CapPage = e.Page,
                    CapName = e.Light.ToString(),
                    CapImage = ImageSource.FromStream(() => proc.GetImage(e.Page, e.Light))
                };
                capImages.Add(result);
            });
        }

        private void ReadFinished(object? sender, Pr22.Events.FileEventArgs e)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                files.Add(new RfidFile()
                {
                    Name = e.FileId.ToString(),
                    file = e.FileId,
                    result = e.Result,
                    DisplayForecolor = e.Result != 0 ? Color.FromRgb(255, 0, 0) : Color.FromRgb(0, 0, 0)
                });
            });
        }

        private void QueryDone(object? sender, QueryResultEventArgs e)
        {
            var popup = new QueryResultPage();
            popup.SetResult(e.Result);

            _ = Dispatcher.Dispatch(() =>
            {
                this.ShowPopupAsync(popup, CancellationToken.None);
            });
        }

        private async void cmdOpen_Clicked(object sender, EventArgs e)
        {
            var popup = new DevicesPage();
            popup.SetDeviceList(proc.DeviceList);

            var result = await this.ShowPopupAsync(popup, CancellationToken.None);
            if (result is string selected)
            {
                if (string.IsNullOrEmpty(selected))
                {
                    await DisplayAlert("Error", "No device!", "OK");
                }
                else
                {
                    if (proc.Open(selected))
                    {
                        cmdOpen.IsEnabled = false;
                        cmdClose.IsEnabled = true;
                        cmdScan.IsEnabled = true;
                        cmdOptions.IsEnabled = true;
                        LogMessage(this, new TextMessage(string.Format("{0} opened.", selected)));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to open device", "OK");
                    }
                }
            }
        }

        private void cmdClose_Clicked(object sender, EventArgs e)
        {
            proc.Close();
            cmdOpen.IsEnabled = true;
            cmdClose.IsEnabled = false;
            cmdScan.IsEnabled = false;
            cmdOptions.IsEnabled = false;
            LogMessage(this, new TextMessage("Device closed."));
        }

        private void cmdScan_Clicked(object sender, EventArgs e)
        {
            proc.Start();
        }

        private async void cmdOptions_Clicked(object sender, EventArgs e)
        {
            var popup = new OptionsPage();

            popup.SetLights(proc.Lights);
            popup.SelectedLights = proc.SelectedLights;
            popup.SelectedDocumentView = proc.SelectedDocumentView;
            popup.SelectedOcrReadings = proc.SelectedOcrReadings;
            popup.SetRfidReaders(proc.RfidReaders);
            popup.SelectedRfidReaders = proc.SelectedRfidReaders;
            popup.SelectedRfidFiles = proc.SelectedRfidFiles;
            popup.SelectedAuthLevel = proc.SelectedAuthLevel;
            popup.SelectedGdsUpload = proc.SelectedGdsUpload;
            popup.SelectedGdsActionList = proc.SelectedGdsActionList;

            var result = await this.ShowPopupAsync(popup, CancellationToken.None);
            if (result is bool accepted)
            {
                if (accepted)
                {
                    proc.SelectedLights = popup.SelectedLights;
                    proc.SelectedDocumentView = popup.SelectedDocumentView;
                    proc.SelectedOcrReadings = popup.SelectedOcrReadings;
                    proc.SelectedRfidReaders = popup.SelectedRfidReaders;
                    proc.SelectedRfidFiles = popup.SelectedRfidFiles;
                    proc.SelectedAuthLevel = popup.SelectedAuthLevel;
                    proc.SelectedGdsUpload = popup.SelectedGdsUpload;
                    proc.SelectedGdsActionList = popup.SelectedGdsActionList;
                }
            }
        }

        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            ScanResult? scan = Capture_Results.SelectedItem as ScanResult;
            if (scan != null && scan.CapImage != null)
            {
                var popup = new ImageViewerPage();
                popup.Size = new Size(this.Width, this.Height);
                popup.SetImage(scan.CapImage);
                await this.ShowPopupAsync(popup, CancellationToken.None);
            }
        }

        private void Files_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Fields_List.ItemsSource = null;

            if (e.CurrentSelection.Count == 0)
            {
                cmdDownload.IsEnabled = false;
                cmdSaveXml.IsEnabled = false;
                return;
            }
            cmdDownload.IsEnabled = true;
            cmdSaveXml.IsEnabled = true;
            ObservableCollection<RfidField> fields = new ObservableCollection<RfidField>();

            RfidFile? rfidfile = e.CurrentSelection[0] as RfidFile;
            if (rfidfile != null)
            {
                Document? doc = proc.GetRfidDoc(rfidfile.Name);
                if (doc != null)
                {
                    List<FieldReference> references = doc.ListFields();
                    foreach (FieldReference reference in references)
                    {
                        Field field = doc.GetField(reference);
                        string? val = null;
                        try
                        {
                            val = field.GetBasicStringValue();
                        }
                        catch { }

                        ImageSource? image = null;
                        try
                        {
                            if (proc.GetFieldImage(field) != null)
                                image = ImageSource.FromStream(() => proc.GetFieldImage(field));
                        }
                        catch { }

                        fields.Add(new RfidField()
                        {
                            FieldRef = reference,
                            FieldName = reference.ToString(),
                            FieldValue = val,
                            FieldImage = image,
                            DisplayRowHeight = image == null ? 25 : 160
                        });
                    }
                    Fields_List.ItemsSource = fields;
                }
                else
                {
                    fields.Add(new RfidField()
                    {
                        FieldName = "Error message",
                        FieldValue = proc.GetRfidErrorMsg(rfidfile.Name),
                        FieldImage = null,
                        DisplayRowHeight = 25
                    });
                    Fields_List.ItemsSource = fields;
                }
            }
        }

        private async void cmdDownload_Clicked(object sender, EventArgs e)
        {
            RfidFile? rfidfile = Files_List.SelectedItem as RfidFile;
            if (rfidfile != null)
            {
                BinData? raw = proc.GetRfidFileData(rfidfile.Name);
                if (raw == null)
                {
                    await DisplayAlert("Download", "File is empty", "Close");
                    return;
                }
                using var stream = new MemoryStream(raw.ToByteArray());
                var fileSaverResult = await _fileSaver.SaveAsync(rfidfile.Name + ".bin", stream);
                if (fileSaverResult.IsSuccessful)
                {
                    if (fileSaverResult.FilePath != null)
                        await DisplayAlert("Download", "File was saved to: " + fileSaverResult.FilePath, "Close");
                }
                else
                {
                    string msg = fileSaverResult.Exception.Message;
                    if (string.IsNullOrEmpty(msg)) msg = fileSaverResult.Exception.ToString().Split('\n')[0];
                    await DisplayAlert("Download", "Error occured: " + msg, "Close");
                }
            }
            else
            {
                await DisplayAlert("Download", "File is empty", "Close");
            }
        }

        private async void cmdSaveXml_Clicked(object sender, EventArgs e)
        {
            RfidFile? rfidfile = Files_List.SelectedItem as RfidFile;
            if (rfidfile != null)
            {
                BinData? raw = proc.GetRfidDoc(rfidfile.Name)?.Save(Document.FileFormat.Xml);
                if (raw == null)
                {
                    await DisplayAlert("Save", "Document is empty", "Close");
                    return;
                }
                using var stream = new MemoryStream(raw.ToByteArray());
                var fileSaverResult = await _fileSaver.SaveAsync(rfidfile.Name + ".xml", stream);
                if (fileSaverResult.IsSuccessful)
                {
                    if (fileSaverResult.FilePath != null)
                        await DisplayAlert("Save", "Document was saved to: " + fileSaverResult.FilePath, "Close");
                }
                else
                {
                    string msg = fileSaverResult.Exception.Message;
                    if (string.IsNullOrEmpty(msg)) msg = fileSaverResult.Exception.ToString().Split('\n')[0];
                    await DisplayAlert("Save", "Error occured: " + msg, "Close");
                }
            }
            else
            {
                await DisplayAlert("Save", "Document is empty", "Close");
            }
        }

        private void SetFieldImage(Microsoft.Maui.Controls.Image image, SelectionChangedEventArgs e, Pr22.Processing.Document? doc)
        {
            _ = Dispatcher.Dispatch(() =>
            {
                try
                {
                    if (e.CurrentSelection.Count == 0)
                    {
                        image.Source = null;
                        image.IsVisible = false;  // workaround https://github.com/dotnet/maui/commit/06cab833dee1754c4caaf26d0601cb3e72ccaec9
                        return;
                    }
                    OcrResult? ocrResult = e.CurrentSelection[0] as OcrResult;
                    if (ocrResult == null)
                    {
                        image.Source = null;
                        image.IsVisible = false;
                        return;
                    }
                    FieldReference fieldRef = ocrResult.FieldRef;
                    Field? field = doc?.GetField(fieldRef);
                    if (field != null)
                    {
                        image.Source = ImageSource.FromStream(() => proc.GetFieldImage(field));
                        image.IsVisible = true;  // workaround https://github.com/dotnet/maui/commit/06cab833dee1754c4caaf26d0601cb3e72ccaec9
                    }
                }
                catch (Pr22.Exceptions.General ex)
                {
                    DisplayAlert("Get field image", ex.ToString(), "Close");
                }
                catch (Exception ex)
                {
                    DisplayAlert("Get field image", ex.ToString(), "Close");
                }
            });
        }

        private void MRZ_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFieldImage(MRZ_Field_Image, e, proc.MrzDoc);
        }

        private void VIZ_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFieldImage(VIZ_Field_Image, e, proc.VizDoc);
        }

        private void BCR_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFieldImage(BCR_Field_Image, e, proc.BcrDoc);
        }

        private void All_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFieldImage(All_Field_Image, e, proc.SummaryDoc);
        }

        void FillTableData(ObservableCollection<OcrResult> table, Pr22.Processing.Document? doc)
        {
            if (doc == null) return;
            List<FieldReference> Fields = doc.ListFields();
            for (int i = 0; i < Fields.Count; i++)
            {
                try
                {
                    using (Field field = doc.GetField(Fields[i]))
                    {
                        OcrResult result = new OcrResult
                        {
                            FieldRef = field.FieldRef,
                            FieldName = Fields[i].ToString(" ") + new StrCon() + GetAmid(field),
                            FieldStatus = field.GetStatus().ToString()
                        };

                        try { result.FieldValue = field.GetRawStringValue(); }
                        catch (Pr22.Exceptions.InvalidParameter)
                        {
                            result.FieldValue = PrintBinary(field.GetBinaryValue(), 0, 16, true);
                        }
                        catch (Pr22.Exceptions.General) { }

                        try { result.FormattedValue = field.GetFormattedStringValue(); }
                        catch (Pr22.Exceptions.General) { }

                        try { result.StandardizedValue = field.GetStandardizedStringValue(); }
                        catch (Pr22.Exceptions.General) { }

                        table.Add(result);
                    }
                }
                catch (Pr22.Exceptions.General) { }
            }
        }

        void FillTableFieldCompare(ObservableCollection<CrossCheckResult> table,
            Pr22.Processing.Document? sumdoc, Pr22.Processing.Document? authresult)
        {
            if (sumdoc != null)
            {
                List<FieldCompare> compares = sumdoc.GetFieldCompareList();
                foreach (var comp in compares)
                {
                    string fieldname1, fieldname2;
                    using (Field field1 = sumdoc.GetField(comp.field1))
                        fieldname1 = comp.field1.ToString(" ") + new StrCon() + GetAmid(field1);
                    using (Field field2 = sumdoc.GetField(comp.field2))
                        fieldname2 = comp.field2.ToString(" ") + new StrCon() + GetAmid(field2);
                    CrossCheckResult result = new CrossCheckResult
                    {
                        Field1 = fieldname1,
                        Field2 = fieldname2,
                        Confidence = comp.confidence
                    };
                    table.Add(result);
                }
            }
            if (authresult != null)
            {
                List<FieldCompare> compares = authresult.GetFieldCompareList();
                foreach (var comp in compares)
                {
                    CrossCheckResult result = new CrossCheckResult
                    {
                        Field1 = comp.field1.ToString(),
                        Field2 = comp.field2.ToString(),
                        Confidence = comp.confidence
                    };
                    table.Add(result);
                }
            }
        }

        void FillDataPage()
        {
            Document? sumDoc = proc.SummaryDoc;

            if (sumDoc != null)
            {
                SummaryResult name = new SummaryResult { FieldName = "Name" };
                name.FieldValue = GetFieldValue(sumDoc, FieldId.Surname);
                if (name.FieldValue != "")
                {
                    name.FieldValue += " " + GetFieldValue(sumDoc, FieldId.Surname2);
                    string name2 = GetFieldValue(sumDoc, FieldId.Givenname) + new StrCon()
                        + GetFieldValue(sumDoc, FieldId.MiddleName);
                    if (!string.IsNullOrEmpty(name2))
                    {
                        name.FieldValue += "\n" + name2;
                    }
                }
                else name.FieldValue = GetFieldValue(sumDoc, FieldId.Name);

                personalData.Add(name);

                SummaryResult birth = new SummaryResult { FieldName = "Born" };
                birth.FieldValue = new StrCon("on") + GetFieldValue(sumDoc, FieldId.BirthDate)
                    + new StrCon("in") + GetFieldValue(sumDoc, FieldId.BirthPlace);
                personalData.Add(birth);

                SummaryResult nationality = new SummaryResult { FieldName = "Nationality" };
                nationality.FieldValue = GetFieldValue(sumDoc, FieldId.Nationality);
                personalData.Add(nationality);

                SummaryResult sex = new SummaryResult { FieldName = "Sex" };
                sex.FieldValue = GetFieldValue(sumDoc, FieldId.Sex);
                personalData.Add(sex);

                SummaryResult issuer = new SummaryResult { FieldName = "Issuer" };
                issuer.FieldValue = GetFieldValue(sumDoc, FieldId.IssueCountry) + new StrCon()
                    + GetFieldValue(sumDoc, FieldId.IssueState);
                documentData.Add(issuer);

                SummaryResult type = new SummaryResult { FieldName = "Type" };
                type.FieldValue = GetFieldValue(sumDoc, FieldId.DocType) + new StrCon()
                    + GetFieldValue(sumDoc, FieldId.DocTypeDisc);
                if (type.FieldValue == "") type.FieldValue = GetFieldValue(sumDoc, FieldId.Type);
                documentData.Add(type);

                SummaryResult page = new SummaryResult { FieldName = "Page" };
                page.FieldValue = GetFieldValue(sumDoc, FieldId.DocPage);
                documentData.Add(page);

                SummaryResult number = new SummaryResult { FieldName = "Number" };
                number.FieldValue = GetFieldValue(sumDoc, FieldId.DocumentNumber);
                documentData.Add(number);

                SummaryResult validity = new SummaryResult { FieldName = "Validity" };
                validity.FieldValue = new StrCon("from") + GetFieldValue(sumDoc, FieldId.IssueDate)
                    + new StrCon("to") + GetFieldValue(sumDoc, FieldId.ExpiryDate);
                documentData.Add(validity);
            }

            Document? vizDoc = proc.VizDoc;
            if (vizDoc != null)
            {
                try
                {
                    var mem = proc.GetFieldImage(vizDoc.GetField(FieldSource.Viz, FieldId.Face));
                    if (mem != null) photos.Add(new ImageResult("VIZ photo", () => mem));
                }
                catch (Pr22.Exceptions.General) { }
                try
                {
                    var mem = proc.GetFieldImage(vizDoc.GetField(FieldSource.Viz, FieldId.Signature));
                    if (mem != null) signatures.Add(new ImageResult("VIZ signature", () => mem));
                }
                catch (Pr22.Exceptions.General) { }
            }

            try
            {
                var mem = proc.GetRfidImage(FieldId.Face);
                if (mem != null) photos.Add(new ImageResult("RFID photo", () => mem));
            }
            catch (Pr22.Exceptions.General) { }

            try
            {
                var mem = proc.GetRfidImage(FieldId.Signature);
                if (mem != null) signatures.Add(new ImageResult("RFID signature", () => mem));
            }
            catch (Pr22.Exceptions.General) { }
        }

        #region General tools
        //----------------------------------------------------------------------

        string GetAmid(Pr22.Processing.Field field)
        {
            try
            {
                using (Pr22.Util.Variant vfield = field.ToVariant())
                    return vfield.GetChild((int)Pr22.Util.VariantId.AMID, 0);
            }
            catch (Pr22.Exceptions.General) { return ""; }
        }

        string GetFieldValue(Pr22.Processing.Document doc, Pr22.Processing.FieldId Id)
        {
            string value;
            FieldReference filter = new FieldReference(FieldSource.All, Id);
            List<FieldReference> Fields = doc.ListFields(filter);
            foreach (FieldReference FR in Fields)
            {
                try
                {
                    using (Field fld = doc.GetField(FR))
                        if ((value = fld.GetBestStringValue()) != "") return value;
                }
                catch (Pr22.Exceptions.EntryNotFound) { }
            }
            return "";
        }

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

        #endregion

    }

}
