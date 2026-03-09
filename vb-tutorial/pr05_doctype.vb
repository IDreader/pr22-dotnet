' This example shows how to generate document type string.
'
'  - Unique page identifier code
'  - Descriptive type identifier string
'  - Related page info string

Option Explicit On
Imports Microsoft.VisualBasic
Imports Pr22
Imports Pr22.Processing
Namespace tutorial

    Class MainClass

        Private pr As DocumentReaderDevice

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

        Public Function Program() As Integer

            'Devices can be manipulated only after opening.
            If Open() <> 0 Then Return 1

            'Subscribing to scan events
            AddHandler pr.ScanStarted, AddressOf ScanStarted
            AddHandler pr.ImageScanned, AddressOf ImageScanned
            AddHandler pr.ScanFinished, AddressOf ScanFinished
            AddHandler pr.DocFrameFound, AddressOf DocFrameFound

            Dim Scanner As DocScanner = pr.Scanner
            Dim OcrEngine As Engine = pr.Engine

            System.Console.WriteLine("Scanning some images to read from.")
            Dim ScanTask As New Pr22.Task.DocScannerTask()
            'For OCR (MRZ) reading purposes, IR (infrared) image is recommended.
            ScanTask.Add(Pr22.Imaging.Light.White).Add(Pr22.Imaging.Light.Infra)
            Dim DocPage As Page = Scanner.Scan(ScanTask, Pr22.Imaging.PagePosition.First)
            System.Console.WriteLine()

            System.Console.WriteLine("Reading all the field data.")
            Dim ReadingTask As New Pr22.Task.EngineTask()
            'Specify the fields we would like to receive.
            ReadingTask.Add(FieldSource.All, FieldId.All)

            Dim OcrDoc As Document = OcrEngine.Analyze(DocPage, ReadingTask)

            System.Console.WriteLine()
            Using vdoc As Pr22.Util.Variant = OcrDoc.ToVariant()
                System.Console.WriteLine("Document code: " & vdoc.ToInt())
            End Using
            System.Console.WriteLine("Document type: " & GetDocType(OcrDoc))
            System.Console.WriteLine("Status: " & OcrDoc.GetStatus().ToString())

            Dim related As String = GetRelatedPages(OcrDoc)
            If related.Length = 0 Then : related = "none"
            Else : related = Pr22Extension.DocumentType.GetPageName(related.Substring(0, 1))
            End If
            If related.Length = 0 Then related = "other"
            System.Console.WriteLine()
            System.Console.WriteLine("Related pages: " & related)

            pr.CloseDevice()
            Return 0
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Returns related pages to scan according to the OCR engine.
        ''' </summary>
        ''' <param name="doc">The document to process.</param>
        ''' <returns>String containing one letter identifiers for each related page.</returns>
        Private Shared Function GetRelatedPages(ByVal doc As Pr22.Processing.Document) As String
            Try
                Using vdoc As Pr22.Util.Variant = doc.ToVariant()
                    Return vdoc.GetChild(Pr22.Util.VariantId.RelatedPages, 0)
                End Using
            Catch ex As Pr22.Exceptions.General
                Return ""
            End Try
        End Function
        '----------------------------------------------------------------------

        Private Shared Function GetFieldValue(ByVal Doc As Pr22.Processing.Document, ByVal Id As Pr22.Processing.FieldId) As String

            Dim filter As FieldReference = New FieldReference(FieldSource.All, Id)
            Dim Fields As System.Collections.Generic.List(Of FieldReference) = Doc.ListFields(filter)
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

        Private Shared Function GetDocType(ByVal OcrDoc As Pr22.Processing.Document) As String

            Dim documentTypeName As String = ""

            Using vdoc As Pr22.Util.Variant = OcrDoc.ToVariant()
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
