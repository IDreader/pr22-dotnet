' This example shows how to get general information about the device capabilities.
'
'  - Version information of the hw/sw components
'  - Usable illumination in the scanner
'  - Size of the scanner window
'  - Engine license compliance
'  - Available licenses for the engine

Option Explicit On
Imports System.Collections.Generic
Imports Microsoft.VisualBasic
Imports Pr22
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

            System.Console.WriteLine("SDK versions:")
            System.Console.WriteLine(vbTab & "Assembly: " & pr.GetType().Assembly.GetName().Version.ToString())
            System.Console.WriteLine(vbTab & "Interface: " & pr.GetVersion("A"c))
            System.Console.WriteLine(vbTab & "System: " & pr.GetVersion("S"c))
            System.Console.WriteLine()

            Dim scannerinfo As Pr22.DocScanner.Information = pr.Scanner.Info

            'Devices provide proper image quality only if they are calibrated.
            'Devices are calibrated by default. If you receive the message "not calibrated"
            'then please contact your hardware supplier.
            System.Console.WriteLine("Calibration state of the device:")
            If scannerinfo.IsCalibrated() Then
                System.Console.WriteLine(vbTab & "calibrated")
            Else
                System.Console.WriteLine(vbTab & "not calibrated")
            End If
            System.Console.WriteLine()

            System.Console.WriteLine("Available lights for image scanning:")
            Dim lights As List(Of Pr22.Imaging.Light) = scannerinfo.ListLights()
            For Each light As Pr22.Imaging.Light In lights
                System.Console.WriteLine(vbTab & light.ToString())
            Next
            System.Console.WriteLine()

            System.Console.WriteLine("Available object windows for image scanning:")
            For ix As Integer = 0 To scannerinfo.GetWindowCount() - 1
                Dim frame As System.Drawing.Rectangle = scannerinfo.GetSize(ix)
                System.Console.WriteLine(vbTab & ix & ": " & frame.Width / 1000.0F & " x " & frame.Height / 1000.0F & " mm")
            Next
            System.Console.WriteLine()

            System.Console.WriteLine("Scanner component versions:")
            System.Console.WriteLine(vbTab & "Firmware: " & scannerinfo.GetVersion("F"c))
            System.Console.WriteLine(vbTab & "Hardware: " & scannerinfo.GetVersion("H"c))
            System.Console.WriteLine(vbTab & "Software: " & scannerinfo.GetVersion("S"c))
            System.Console.WriteLine()

            System.Console.WriteLine("Available card readers:")
            Dim readers As List(Of ECardReader) = pr.Readers
            For ix As Integer = 0 To readers.Count - 1
                System.Console.WriteLine(vbTab & ix & ": " & readers(ix).Info.HwType.ToString())
                System.Console.WriteLine(vbTab & vbTab & "Firmware: " & readers(ix).Info.GetVersion("F"c))
                System.Console.WriteLine(vbTab & vbTab & "Hardware: " & readers(ix).Info.GetVersion("H"c))
                System.Console.WriteLine(vbTab & vbTab & "Software: " & readers(ix).Info.GetVersion("S"c))
            Next
            System.Console.WriteLine()

            System.Console.WriteLine("Available status LEDs:")
            Dim leds As List(Of Pr22.Control.StatusLed) = pr.Peripherals.StatusLedList
            For ix As Integer = 0 To leds.Count - 1
                System.Console.WriteLine(vbTab & ix & ": color " & leds(ix).Light.ToString())
            Next
            System.Console.WriteLine()

            Dim EngineInfo As Pr22.Engine.Information = pr.Engine.Info

            System.Console.WriteLine("Engine version: " & EngineInfo.GetVersion("E"c))
            Dim licok As String() = {"no presence info", "not available", "present", "expired"}
            Dim lictxt As String = EngineInfo.RequiredLicense.ToString()
            If EngineInfo.RequiredLicense = Pr22.Processing.EngineLicense.MrzOcrBarcodeReading Then
                lictxt = "MrzOcrBarcodeReadingL or MrzOcrBarcodeReadingF"
            End If
            System.Console.WriteLine("Required license: " & lictxt & " - " & licok(TestLicense(EngineInfo)))
            System.Console.WriteLine("Engine release date: " & EngineInfo.RequiredLicenseDate)
            System.Console.WriteLine()

            System.Console.WriteLine("Available licenses:")
            Dim licenses As List(Of Pr22.Processing.EngineLicense) = EngineInfo.ListLicenses()
            For Each lic As Pr22.Processing.EngineLicense In licenses
                System.Console.WriteLine(vbTab & lic.ToString() & " (" & EngineInfo.GetLicenseDate(lic) & ")")
            Next
            System.Console.WriteLine()

            System.Console.WriteLine("Closing the device.")
            pr.CloseDevice()
            Return 0
        End Function
        '----------------------------------------------------------------------
        ''' <summary>
        ''' Tests if the required OCR license is present.
        ''' </summary>
        Private Function TestLicense(ByVal info As Pr22.Engine.Information) As Integer

            If info.RequiredLicense = Pr22.Processing.EngineLicense.Unknown Then Return 0
            Dim availdate As String = info.GetLicenseDate(info.RequiredLicense)
            If availdate = "-" Then Return 1
            If info.RequiredLicenseDate = "-" Then Return 2
            If availdate(0) <> "X"c AndAlso availdate.CompareTo(info.RequiredLicenseDate) > 0 Then Return 2
            Return 3
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
