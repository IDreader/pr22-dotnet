' This example shows the possibilities of opening a device.
'
'  - Lists available document reader devices
'  - Connects to a device by name or ordinal number
'
' The following tutorials show the easy way of opening in their Open methods.

Option Explicit On
Imports Microsoft.VisualBasic
Imports Pr22
Namespace tutorial

    Class MainClass

        Private pr As DocumentReaderDevice

        '----------------------------------------------------------------------

        Public Function Program() As Integer

            ' To open more than one device simultaneously, create more DocumentReaderDevice objects
            System.Console.WriteLine("Opening system")
            System.Console.WriteLine()
            pr = New DocumentReaderDevice()

            AddHandler pr.Connection, AddressOf onDeviceConnected
            AddHandler pr.DeviceUpdate, AddressOf onDeviceUpdate

            Dim deviceList As System.Collections.Generic.List(Of String) = DocumentReaderDevice.ListDevices(pr)

            If deviceList.Count = 0 Then
                System.Console.WriteLine("No device found!")
                Return 0
            End If

            System.Console.WriteLine(deviceList.Count & " device" & CStr(IIf(deviceList.Count > 1, "s", "")) & " found.")
            For Each devName As String In deviceList
                System.Console.WriteLine("  Device: " & devName)
            Next
            System.Console.WriteLine()

            System.Console.WriteLine("Connecting to the first device by its name: " + deviceList(0))
            System.Console.WriteLine()
            System.Console.WriteLine("If this is the first usage of this device on this PC,")
            System.Console.WriteLine("the ""calibration file"" will be downloaded from the device.")
            System.Console.WriteLine("This can take a while.")
            System.Console.WriteLine()

            pr.UseDevice(deviceList(0))

            System.Console.WriteLine("The device is opened.")

            System.Console.WriteLine("Closing the device.")
            pr.CloseDevice()
            System.Console.WriteLine()

            ' Opening the first device without using any device lists.

            System.Console.WriteLine("Connecting to the first device by its ordinal number: 0")

            pr.UseDevice(0)

            System.Console.WriteLine("The device is opened.")

            System.Console.WriteLine("Closing the device.")
            pr.CloseDevice()
            Return 0
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
