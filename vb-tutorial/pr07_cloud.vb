' This example demonstrates how to read and process different types of data
' with Carmen(R) ID Recognition Service (cloud authentication engine) and
' GDS (remote database) solution.
'
'  - Reads MRZ locally (fastest way to get MRZ).
'  - Reads VIZ + barcode with cloud engine - if configured.
'  - Displays all data as in pr04_analyze.
'  - Authenticates and reads ECard.
'  - Analyzes ECard data in bundle.
'  - Generates a summary document comparing all data read.
'  - Checking document and personal data in the remote database - if configured.
'  - Uploads read data to remote database (GDS) - if configured.

Option Explicit On
Imports Pr22
Imports Pr22.Processing
Imports System.Collections.Generic
Namespace tutorial

    Class MainClass

        Private pr As DocumentReaderDevice
        Private AllDocs As Document

        '----------------------------------------------------------------------
        ''' <summary>
        ''' Opens the first document reader device.
        ''' </summary>
        ''' <returns></returns>
        Public Function Open() As Integer

            System.Console.WriteLine("Opening a device")
            System.Console.WriteLine()
            pr = New DocumentReaderDevice()

            AddHandler pr.Connection, AddressOf onDeviceConnected
            AddHandler pr.DeviceUpdate, AddressOf onDeviceUpdate

            Try
                pr.UseDevice(0)
            Catch ex As Pr22.Exceptions.NoSuchDevice
                System.Console.WriteLine("No device found!")
                Return 1
            End Try

            System.Console.WriteLine("The device " + pr.DeviceName + " is opened.")
            System.Console.WriteLine()
            Return 0
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Returns a list of files in a directory.
        ''' </summary>
        ''' <param name="dirname"></param>
        ''' <param name="mask"></param>
        ''' <returns></returns>
        Public Function ListFiles(ByVal dirname As String, ByVal mask As String) As List(Of String)

            Dim list As New List(Of String)()
            Try
                Dim dir As New System.IO.DirectoryInfo(dirname)
                For Each d As System.IO.DirectoryInfo In dir.GetDirectories()
                    list.AddRange(ListFiles(dir.FullName + "/" + d.Name, mask))
                Next
                For Each f As System.IO.FileInfo In dir.GetFiles(mask)
                    list.Add(f.FullName)
                Next
            Catch ex As System.Exception
            End Try
            Return list
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Loads certificates from a directory.
        ''' </summary>
        ''' <param name="dir"></param>
        Public Sub LoadCertificates(ByVal dir As String)

            Dim exts As String() = {"*.cer", "*.crt", "*.der", "*.pem", "*.crl", "*.cvcert", "*.ldif", "*.ml"}
            Dim cnt As Integer = 0

            For Each ext As String In exts
                Dim list As List(Of String) = ListFiles(dir, ext)
                For Each file As String In list
                    Try
                        Dim fd As BinData = New BinData().Load(file)
                        Dim pk As String = Nothing
                        If ext = "*.cvcert" Then
                            'Searching for private key
                            pk = file.Substring(0, file.LastIndexOf("."c) + 1) + "pkcs8"
                            If Not System.IO.File.Exists(pk) Then pk = Nothing
                        End If
                        If pk Is Nothing Then
                            pr.GlobalCertificateManager.Load(fd)
                            System.Console.WriteLine("Certificate " + file + " is loaded.")
                        Else
                            pr.GlobalCertificateManager.Load(fd, New BinData().Load(pk))
                            System.Console.WriteLine("Certificate " + file + " is loaded with private key.")
                        End If
                        cnt += 1
                    Catch ex As Pr22.Exceptions.General
                        System.Console.WriteLine("Loading certificate " + file + " is failed!")
                    End Try
                Next
            Next
            If cnt = 0 Then System.Console.WriteLine("No certificates loaded from " + dir)
            System.Console.WriteLine()
        End Sub
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Does an authentication after collecting the necessary information.
        ''' </summary>
        ''' <param name="SelectedCard"></param>
        ''' <param name="CurrentAuth"></param>
        ''' <returns></returns>
        Public Function Authenticate(ByVal SelectedCard As Pr22.ECard, ByVal CurrentAuth As Pr22.ECardHandling.AuthProcess) As Boolean

            Dim AdditionalAuthData As BinData = Nothing
            Dim selector As Integer = 0
            Select Case CurrentAuth
                Case Pr22.ECardHandling.AuthProcess.BAC, Pr22.ECardHandling.AuthProcess.PACE, Pr22.ECardHandling.AuthProcess.BAP
                    Dim field As Field = Nothing
                    Try
                        field = AllDocs.GetField(FieldSource.Mrz, FieldId.All)
                        selector = 1
                    Catch ex As Pr22.Exceptions.General
                        Try
                            field = AllDocs.GetField(FieldSource.Viz, FieldId.CAN)
                            selector = 2
                        Catch ex2 As Pr22.Exceptions.General
                        End Try
                    End Try
                    If selector > 0 Then AdditionalAuthData = New BinData().SetString(field.GetBasicStringValue())
                    If field IsNot Nothing Then field.Dispose()

                Case Pr22.ECardHandling.AuthProcess.Passive, Pr22.ECardHandling.AuthProcess.Terminal

                    'Load the certificates if not done yet

                Case Pr22.ECardHandling.AuthProcess.SelectApp
                    Dim apps As List(Of Pr22.ECardHandling.Application) = SelectedCard.ListApplications()
                    If apps.Count > 0 Then selector = CInt(apps(0))

            End Select
            Try
                System.Console.Write("- " & CurrentAuth.ToString() & " authentication ")
                SelectedCard.Authenticate(CurrentAuth, AdditionalAuthData, selector)
                System.Console.WriteLine("succeeded")
                Return True
            Catch ex As Pr22.Exceptions.General
                System.Console.WriteLine("failed: " & ex.Message)
                Return False
            End Try
        End Function
        '----------------------------------------------------------------------

        Public Function Program() As Integer

            'Devices can be manipulated only after opening.
            If Open() <> 0 Then Return 1

            'Subscribing to scan events
            AddHandler pr.ScanStarted, AddressOf ScanStarted
            AddHandler pr.ImageScanned, AddressOf ImageScanned
            AddHandler pr.ScanFinished, AddressOf ScanFinished
            AddHandler pr.DocFrameFound, AddressOf DocFrameFound

            'Please set the appropriate path
            LoadCertificates(pr.GetProperty("rwdata_dir") & "\certs")

            Dim Scanner = pr.Scanner
            Dim OcrEngine = pr.Engine

            System.Console.WriteLine("Preload OCR engine.")
            Dim info = OcrEngine.Info

            'Without the following check the cloud OCR engine will not work
            If pr.GetProperty("icldev/password") = "1" Then
                System.Console.WriteLine("Please set the master password first.")
            End If

            System.Console.WriteLine()

            System.Console.WriteLine("Scanning some images to read from.")
            Dim ScanTask As New Pr22.Task.DocScannerTask()
            'For OCR (MRZ) reading purposes, IR (infrared) image is recommended.
            ScanTask.Add(Pr22.Imaging.Light.White).Add(Pr22.Imaging.Light.Infra)
            Dim DocPage As Page = Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.First)
            System.Console.WriteLine()

            System.Console.WriteLine("Reading all the field data of the Machine Readable Zone.")
            Dim MrzReadingTask As New Pr22.Task.EngineTask()
            'Specify the fields we would like to receive.
            MrzReadingTask.Add(FieldSource.Mrz, FieldId.All)
            Dim MrzDoc As Document = OcrEngine.Analyze(DocPage, MrzReadingTask)

            System.Console.WriteLine()
            PrintDocFields(MrzDoc)
            'Returned fields by the Analyze function can be saved to an XML file:
            MrzDoc.Save(Document.FileFormat.Xml).Save("MRZ.xml")

            System.Console.WriteLine("Scanning more images for VIZ reading and image authentication.")
            'Reading from VIZ -except face photo- is available in special OCR engines only.
            ScanTask.Add(Pr22.Imaging.Light.All)
            DocPage = Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.Current)
            System.Console.WriteLine()

            System.Console.WriteLine("Reading all the textual and graphical field data as well as " & _
                "authentication result from the Visual Inspection Zone.")
            Dim VIZReadingTask As New Pr22.Task.EngineTask()
            VIZReadingTask.Add(FieldSource.Viz, FieldId.All)
            VIZReadingTask.Add(FieldSource.Barcode, FieldId.All)
            VIZReadingTask.Add(FieldSource.Mrz, FieldId.B900)
            Dim VizDoc As Document = OcrEngine.Analyze(DocPage, VIZReadingTask)

            System.Console.WriteLine()
            Using vdoc As Pr22.Util.Variant = VizDoc.ToVariant()
                System.Console.WriteLine("OCR engine: " & vdoc.GetChild(Pr22.Util.VariantId.Ocr, 0).ToString())
            End Using

            AllDocs = MrzDoc + VizDoc

            System.Console.WriteLine()
            Using vdoc As Pr22.Util.Variant = AllDocs.ToVariant()
                System.Console.WriteLine("Document code: " & vdoc.ToInt())
            End Using
            System.Console.WriteLine("Document type: " & GetDocType(AllDocs))
            System.Console.WriteLine("Status: " & AllDocs.GetStatus().ToString())

            System.Console.WriteLine()
            PrintDocFields(VizDoc)
            VizDoc.Save(Document.FileFormat.Xml).Save("VIZ.xml")

            Dim CardReaders As List(Of ECardReader) = pr.Readers

            'Connecting to the 1st card of any reader
            Dim SelectedCard As ECard = Nothing
            Dim CardCount As Integer = 0
            System.Console.WriteLine("Detected readers and cards:")
            For Each reader As ECardReader In CardReaders
                System.Console.WriteLine(vbTab & "Reader: " & reader.Info.HwType.ToString())
                Dim cards As List(Of String) = reader.ListCards()
                If SelectedCard Is Nothing AndAlso cards.Count > 0 Then SelectedCard = reader.ConnectCard(0)
                For Each card As String In cards
                    System.Console.WriteLine(vbTab & vbTab & "(" & CardCount & ")card: " & card)
                    CardCount += 1
                Next
                System.Console.WriteLine()
            Next

            If SelectedCard Is Nothing Then
                System.Console.WriteLine("No card selected!")

                System.Console.WriteLine()
                System.Console.WriteLine("All read data:")
                PrintDocFields(AllDocs)
            Else
                System.Console.WriteLine("Executing authentications:")
                Dim CurrentAuth As Pr22.ECardHandling.AuthProcess = SelectedCard.GetNextAuthentication(False)
                Dim PassiveAuthImplemented As Boolean = False

                While CurrentAuth <> Pr22.ECardHandling.AuthProcess.NoAuth
                    If CurrentAuth = Pr22.ECardHandling.AuthProcess.Passive Then PassiveAuthImplemented = True
                    Dim authOk As Boolean = Authenticate(SelectedCard, CurrentAuth)
                    CurrentAuth = SelectedCard.GetNextAuthentication(Not authOk)
                End While
                System.Console.WriteLine()

                System.Console.WriteLine("Reading data:")
                Dim FilesOnSelectedCard As List(Of Pr22.ECardHandling.File) = SelectedCard.ListFiles()
                If PassiveAuthImplemented Then
                    FilesOnSelectedCard.Add(Pr22.ECardHandling.FileId.CertDS)
                    FilesOnSelectedCard.Add(Pr22.ECardHandling.FileId.CertCSCA)
                End If

                Dim rfid_filenames As New List(Of String)()
                Dim rfid_filedatas As New List(Of BinData)()

                For Each File As Pr22.ECardHandling.File In FilesOnSelectedCard
                    Try
                        System.Console.Write("File: " & File.ToString() & ".")
                        Dim RawFileData As BinData = SelectedCard.GetFile(File)
                        RawFileData.Save(File.ToString() & ".dat")

                        'Executing mandatory data integrity check for Passive Authentication
                        If PassiveAuthImplemented Then
                            Dim f As Pr22.ECardHandling.File = File
                            If f.Id >= Pr22.ECardHandling.FileId.GeneralData Then
                                f = SelectedCard.ConvertFileId(f)
                            End If
                            If f.Id >= 1 AndAlso f.Id <= 16 Then
                                System.Console.Write(" hash check...")
                                System.Console.Write(IIf(SelectedCard.CheckHash(f), "OK", "failed"))
                            End If
                        End If

                        '''The Rfid files can be process one by one here
                        'Dim FileData As Document = OcrEngine.Analyze(RawFileData)
                        'FileData.Save(Document.FileFormat.Xml).Save(File.ToString() + ".xml")
                        'AllDocs = AllDocs + FileData
                        'System.Console.WriteLine()
                        'PrintDocFields(FileData)

                        rfid_filenames.Add(File.ToString())
                        rfid_filedatas.Add(RawFileData)
                    Catch ex As Pr22.Exceptions.General
                        System.Console.Write(" Reading failed: " & ex.Message)
                    End Try
                    System.Console.WriteLine()
                Next
                System.Console.WriteLine()

                System.Console.WriteLine("Process RFID files in bundle:")
                Dim docs As List(Of Document) = OcrEngine.Analyze(rfid_filedatas, 0)

                For ix = 0 To docs.Count - 1
                    Dim FileData As Document = docs(ix)
                    FileData.Save(Document.FileFormat.Xml).Save(rfid_filenames(ix) & ".xml")
                    System.Console.WriteLine("File: " & rfid_filenames(ix))
                    PrintDocFields(FileData)
                Next ix

                System.Console.WriteLine("Authentications:")
                Dim AuthData As Document = SelectedCard.GetAuthResult()
                AuthData.Save(Document.FileFormat.Xml).Save("AuthResult.xml")
                PrintDocFields(AuthData)
                System.Console.WriteLine()

                System.Console.WriteLine("Merge all documents:")
                docs.Add(AuthData)
                docs.Add(AllDocs)
                AllDocs = OcrEngine.Merge(docs)
                System.Console.WriteLine()
                PrintDocFields(AllDocs)

                Try
                    SelectedCard.Disconnect()
                Catch ex As Pr22.Exceptions.General
                End Try
            End If

            System.Console.WriteLine("Final status: " & AllDocs.GetStatus().ToString())
            System.Console.WriteLine()
            Using doc As Document = OcrEngine.GetRootDocument()
                doc.Save(Document.FileFormat.Zipped).Save("AllDocs.zip")
            End Using

            System.Console.WriteLine("Query GDS:")
            Dim chkList As Pr22.Util.Variant = Nothing
            Try
                chkList = pr.DBClient.QueryDataBase()
            Catch ex As Pr22.Exceptions.General
                System.Console.WriteLine(" Query failed: " & ex.Message)
            End Try

            If chkList IsNot Nothing AndAlso chkList.NItems > 0 Then
                For ix = 0 To chkList.NItems - 1
                    Dim it As Pr22.Util.Variant = chkList(ix)
                    Dim name As String = it.Name
                    System.Console.Write(" Document is " & _
                        IIf(it.ToInt() = 1, "found", "not found") & " on the list " & name)

                    System.Console.WriteLine(" Status: " & _
                        CType(it.GetChild(Pr22.Util.VariantId.Checksum, 0).ToInt(), Status).ToString())
                Next ix
            End If

            System.Console.WriteLine("Upload to GDS.")
            Try
                pr.DBClient.FinishDataCollection()
            Catch ex As Pr22.Exceptions.General
                System.Console.WriteLine(" Upload failed: " & ex.Message)
            End Try

            System.Console.WriteLine()
            System.Console.WriteLine("Close the PR system (may take some time to unload the OCR engine).")
            pr.CloseDevice()
            Return 0
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Returns the Authentication Method IDentifier of a field.
        ''' </summary>
        ''' <param name="field">The field to process.</param>
        ''' <returns>String representation of AMID.</returns>
        Private Shared Function GetAmid(ByVal field As Pr22.Processing.Field) As String
            Try
                Using vfield As Pr22.Util.Variant = field.ToVariant()
                    Return vfield.GetChild(Pr22.Util.VariantId.AMID, 0)
                End Using
            Catch ex As Pr22.Exceptions.General
                Return ""
            End Try
        End Function
        '----------------------------------------------------------------------

        Public Shared Function GetFieldValue(ByVal Doc As Pr22.Processing.Document, ByVal Id As Pr22.Processing.FieldId) As String
            Dim filter As FieldReference = New FieldReference(FieldSource.All, Id)
            Dim Fields As List(Of FieldReference) = Doc.ListFields(filter)
            For Each FR As FieldReference In Fields
                Dim fld As Field = Nothing
                Try
                    fld = Doc.GetField(FR)
                    Dim value As String = fld.GetBestStringValue()
                    If value <> "" Then Return value
                Catch ex As Pr22.Exceptions.EntryNotFound
                Finally
                    If fld IsNot Nothing Then fld.Dispose()
                End Try
            Next
            Return ""
        End Function
        '----------------------------------------------------------------------

        Public Function GetDocType(ByVal OcrDoc As Pr22.Processing.Document) As String

            Dim documentTypeName As String = ""

            Using vdoc As Util.Variant = OcrDoc.ToVariant()
                documentTypeName = Pr22Extension.DocumentType.GetDocumentName(vdoc.ToInt())
            End Using

            If documentTypeName = "" Then

                Dim issue_country As String = GetFieldValue(OcrDoc, FieldId.IssueCountry)
                Dim issue_state As String = GetFieldValue(OcrDoc, FieldId.IssueState)
                Dim doc_type As String = GetFieldValue(OcrDoc, FieldId.DocType)
                Dim doc_page As String = GetFieldValue(OcrDoc, FieldId.DocPage)
                Dim doc_subtype As String = GetFieldValue(OcrDoc, FieldId.DocTypeDisc)

                Dim tmpval As String = Pr22Extension.CountryCode.GetName(issue_country)
                If tmpval <> "" Then issue_country = tmpval

                documentTypeName = issue_country + New StrCon() + issue_state _
                    + New StrCon() + Pr22Extension.DocumentType.GetDocTypeName(doc_type) _
                    + New StrCon("-") + Pr22Extension.DocumentType.GetPageName(doc_page) _
                    + New StrCon(",") + doc_subtype

            End If
            Return documentTypeName

        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Prints a hexa dump line from a part of an array into a string.
        ''' </summary>
        ''' <param name="arr">The whole array.</param>
        ''' <param name="pos">Position of the first item to print.</param>
        ''' <param name="sz">Number of items to print.</param>
        ''' <param name="split">Add extra spaces to some location.</param>
        Private Shared Function PrintBinary(ByVal arr As Byte(), ByVal pos As Integer, ByVal sz As Integer,
            ByVal split As Boolean) As String

            Dim p0 As Integer = pos
            Dim str As String = "", str2 As String = ""

            While p0 < arr.Length AndAlso p0 < pos + sz
                str += arr(p0).ToString("X2") + " "
                str2 += CChar(IIf(arr(p0) < 33 OrElse arr(p0) > 126, "."c, ChrW(arr(p0))))
                p0 += 1
            End While

            While p0 < pos + sz
                str += "   " : str2 += " " : p0 += 1
            End While

            If split Then str = str.Insert((sz \ 2) * 3, " ") + " "
            Return str + str2
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Prints out all fields of a document structure to console.
        ''' </summary>
        ''' <remarks>
        ''' Values are printed in three different forms: raw, formatted and standardized.
        ''' Status (checksum result) is printed together with fieldname and raw value.
        ''' At the end, images of all fields are saved into png format.
        ''' </remarks>
        ''' <param name="doc"></param>
        Private Shared Sub PrintDocFields(ByVal doc As Pr22.Processing.Document)

            Dim Fields As List(Of FieldReference) = doc.ListFields()

            System.Console.WriteLine("  {0, -20}{1, -17}{2}", "FieldId", "Status", "Value")
            System.Console.WriteLine("  {0, -20}{1, -17}{2}", "-------", "------", "-----")
            System.Console.WriteLine()

            For Each CurrentFieldRef As FieldReference In Fields
                Dim CurrentField As Field = Nothing
                Try
                    CurrentField = doc.GetField(CurrentFieldRef)
                    Dim Value As String = "", FormattedValue As String = "", StandardizedValue As String = ""
                    Dim binValue As Byte() = Nothing
                    Try
                        Value = CurrentField.GetRawStringValue()
                    Catch ex As Pr22.Exceptions.EntryNotFound
                    Catch ex As Pr22.Exceptions.InvalidParameter
                        binValue = CurrentField.GetBinaryValue()
                    End Try
                    Try
                        FormattedValue = CurrentField.GetFormattedStringValue()
                    Catch ex As Pr22.Exceptions.EntryNotFound
                    End Try
                    Try
                        StandardizedValue = CurrentField.GetStandardizedStringValue()
                    Catch ex As Pr22.Exceptions.EntryNotFound
                    End Try
                    Dim Amid As String = GetAmid(CurrentField)
                    Dim Status As Status = CurrentField.GetStatus()
                    Dim Fieldname As String = CurrentFieldRef.ToString()
                    If binValue IsNot Nothing Then
                        System.Console.WriteLine("  {0, -20}{1, -17}Binary", Fieldname, Status)
                        ''' Binary data can be printed out here
                        'For cnt As Integer = 0 To binValue.Length - 1 Step 16
                        'System.Console.WriteLine(PrintBinary(binValue, cnt, 16, True))
                        'Next
                    Else
                        System.Console.WriteLine("  {0, -20}{1, -17}[{2}]", Fieldname, Status, Value)
                        If Amid.Length > 0 Then System.Console.WriteLine("  {0}", Amid)
                        System.Console.WriteLine(vbTab & "{1, -31}[{0}]", FormattedValue, "   - Formatted")
                        System.Console.WriteLine(vbTab & "{1, -31}[{0}]", StandardizedValue, "   - Standardized")
                    End If

                    Dim lst As List(Of Checking) = CurrentField.GetDetailedStatus()
                    For Each chk As Checking In lst
                        System.Console.WriteLine(chk.ToString())
                    Next

                    Try
                        Using img As Pr22.Imaging.RawImage = CurrentField.GetImage()
                            img.Save(Pr22.Imaging.RawImage.FileFormat.Png).Save(Fieldname + ".png")
                        End Using
                    Catch ex As Pr22.Exceptions.General
                    End Try
                Catch ex As Pr22.Exceptions.General
                Finally
                    If CurrentField IsNot Nothing Then CurrentField.Dispose()
                End Try
            Next
            System.Console.WriteLine()

            For Each comp As FieldCompare In doc.GetFieldCompareList()
                System.Console.WriteLine("Comparing " & comp.field1.ToString() & " vs. " & _
                    comp.field2.ToString() & " results " & comp.confidence)
            Next
            System.Console.WriteLine()
        End Sub
        '----------------------------------------------------------------------
        ' Event handlers
        '----------------------------------------------------------------------

        Private Sub onDeviceConnected(ByVal a As Object, ByVal ev As Pr22.Events.ConnectionEventArgs)

            System.Console.WriteLine("Connection event. Device number: " & ev.DeviceNumber)
        End Sub
        '----------------------------------------------------------------------

        Private Sub onDeviceUpdate(ByVal a As Object, ByVal ev As Pr22.Events.UpdateEventArgs)

            System.Console.WriteLine("Update event.")
            Select Case ev.part
                Case 1
                    System.Console.WriteLine("  Reading calibration file from device.")
                Case 2
                    System.Console.WriteLine("  Scanner firmware update.")
                Case 4
                    System.Console.WriteLine("  RFID reader firmware update.")
                Case 5
                    System.Console.WriteLine("  License update.")
            End Select
        End Sub
        '----------------------------------------------------------------------

        Private Sub ScanStarted(ByVal a As Object, ByVal ev As Pr22.Events.PageEventArgs)

            System.Console.WriteLine("Scan started. Page: " & ev.Page)
        End Sub
        '----------------------------------------------------------------------

        Private Sub ImageScanned(ByVal a As Object, ByVal ev As Pr22.Events.ImageEventArgs)

            System.Console.WriteLine("Image scanned. Page: " & ev.Page & " Light: " & ev.Light.ToString())
        End Sub
        '----------------------------------------------------------------------

        Private Sub ScanFinished(ByVal a As Object, ByVal ev As Pr22.Events.PageEventArgs)

            System.Console.WriteLine("Page scanned. Page: " & ev.Page & " Status: " & ev.Status.ToString())
        End Sub
        '----------------------------------------------------------------------

        Private Sub DocFrameFound(ByVal a As Object, ByVal ev As Pr22.Events.PageEventArgs)

            System.Console.WriteLine("Document frame found. Page: " & ev.Page)
        End Sub
        '----------------------------------------------------------------------

        Public Shared Function Main(ByVal args As String()) As Integer

            Try
                Dim prog As New MainClass()
                prog.Program()
            Catch ex As Pr22.Exceptions.General
                System.Console.Error.WriteLine(ex.Message)
            End Try
            System.Console.WriteLine("Press any key to exit!")
            System.Console.ReadKey(True)
            Return 0
        End Function
        '----------------------------------------------------------------------
    End Class

    ''' <summary>
    ''' This class makes string concatenation with spaces and prefixes.
    ''' </summary>
    Public Class StrCon
        Private fstr As String = ""
        Private [cstr] As String = ""

        Public Sub New()
        End Sub

        Public Sub New(ByVal bounder As String)
            [cstr] = bounder + " "
        End Sub

        Public Shared Operator +(ByVal csv As StrCon, ByVal str As String) As String
            If str <> "" Then str = csv.[cstr] + str
            If csv.fstr <> "" AndAlso str <> "" AndAlso str(0) <> ","c Then csv.fstr += " "
            Return csv.fstr + str
        End Operator

        Public Shared Operator +(ByVal str As String, ByVal csv As StrCon) As StrCon
            csv.fstr = str
            Return csv
        End Operator
    End Class
End Namespace
