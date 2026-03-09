' This example shows the main capabilities of the image processing analyzer function.
'
'  - Reading different areas of images (MRZ, VIZ, Barcode).
'  - Displaying details of the complex result data (called field data).
'    - Field identification
'    - Raw, formatted and standardized values
'    - Composite and detailed result of data checks
'    - Results of data field comparisons
'  - Saving field images.

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

            System.Console.WriteLine("Reading all the textual and graphical field data as well as " + _
                "authentication result from the Visual Inspection Zone.")
            Dim VIZReadingTask As New Pr22.Task.EngineTask()
            VIZReadingTask.Add(FieldSource.Viz, FieldId.All)
            Dim VizDoc As Document = OcrEngine.Analyze(DocPage, VIZReadingTask)

            System.Console.WriteLine()
            PrintDocFields(VizDoc)
            VizDoc.Save(Document.FileFormat.Xml).Save("VIZ.xml")

            System.Console.WriteLine("Reading barcodes.")
            Dim BCReadingTask As New Pr22.Task.EngineTask()
            BCReadingTask.Add(FieldSource.Barcode, FieldId.All)
            Dim BcrDoc As Document = OcrEngine.Analyze(DocPage, BCReadingTask)

            System.Console.WriteLine()
            PrintDocFields(BcrDoc)
            BcrDoc.Save(Document.FileFormat.Xml).Save("BCR.xml")

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

            Dim Fields As System.Collections.Generic.List(Of FieldReference) = doc.ListFields()

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
                        For cnt As Integer = 0 To binValue.Length - 1 Step 16
                            System.Console.WriteLine(PrintBinary(binValue, cnt, 16, True))
                        Next
                    Else
                        System.Console.WriteLine("  {0, -20}{1, -17}[{2}]", Fieldname, Status, Value)
                        If Amid.Length > 0 Then System.Console.WriteLine("  {0}", Amid)
                        System.Console.WriteLine(vbTab & "{1, -31}[{0}]", FormattedValue, "   - Formatted")
                        System.Console.WriteLine(vbTab & "{1, -31}[{0}]", StandardizedValue, "   - Standardized")
                    End If

                    Dim lst As System.Collections.Generic.List(Of Checking) = CurrentField.GetDetailedStatus()
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
End Namespace
