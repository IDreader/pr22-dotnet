Imports Microsoft.VisualBasic
Namespace Pr22Extension
    ''' <summary>
    ''' Document identifier name converter.
    ''' </summary>
    Partial Public Class DocumentType

        ''' <summary>
        ''' Returns the name of a general document.
        ''' </summary>
        ''' <param name="id">The document code.</param>
        ''' <returns>The name of a general document.</returns>
        Public Shared Function GetDocumentName(ByVal id As Integer) As String
            Dim values As String() = { _
            "Unknown document", _
            "ICAO standard Passport (MRP)", _
            "ICAO standard 2 row Travel Document (TD-2)", _
            "ICAO standard 3 row Travel Document (TD-1)", _
            "ICAO standard visa (MRV-A) (MRV-B)", _
            "French ID card", _
            "Pre ICAO standard 3 row Travel Document", _
            "Slovak ID card", _
            "AAMVA standard driving license", _
            "Belgian ID", _
            "Swiss driving license", _
            "ID of Cote d'Ivoire", _
            "Financial Transaction Card", _
            "IATA boarding pass", _
            "ICAO Travel Document (TD-1, front page, named)", _
            "ICAO Travel Document (TD-1, front page, typed)", _
            "ISO standard driving license", _
            "Mail item", _
            "ICAO standard electronic document (Passport/ID)", _
            "EID", _
            "ESign", _
            "NFC", _
            "European standard driving license", _
            "Portuguese ID", _
            "Ecuadorian ID", _
            "ID card with MRZ", _
            "USA military ID", _
            "Vehicle Registration Document", _
            "Local border traffic permit"}
            If id < 0 OrElse id >= values.Length Then Return ""
            Return values(id)
        End Function

        ''' <summary>
        ''' Returns the document type name.
        ''' </summary>
        ''' <param name="doc_type">Document type identifier string.</param>
        ''' <returns>The name of the document type.</returns>
        Public Shared Function GetDocTypeName(ByVal doc_type As String) As String
            If doc_type.StartsWith("DL") Then
                If doc_type = "DLL" Then : Return "driving license for learner"
                Else : Return "driving license"
                End If
            ElseIf doc_type.StartsWith("ID") Then
                If doc_type = "IDF" Then : Return "ID card for foreigner"
                ElseIf doc_type = "IDC" Then : Return "ID card for children"
                Else : Return "ID card"
                End If
            ElseIf doc_type.StartsWith("PP") Then
                If doc_type = "PPD" Then : Return "diplomatic passport"
                ElseIf doc_type = "PPS" Then : Return "service passport"
                ElseIf doc_type = "PPE" Then : Return "emergency passport"
                ElseIf doc_type = "PPC" Then : Return "passport for children"
                Else : Return "passport"
                End If
            ElseIf doc_type.StartsWith("TD") Then : Return "travel document"
            ElseIf doc_type.StartsWith("RP") Then : Return "residence permit"
            ElseIf doc_type.StartsWith("VS") Then : Return "visa"
            ElseIf doc_type.StartsWith("WP") Then : Return "work permit"
            ElseIf doc_type.StartsWith("SI") Then : Return "social insurance document"
            Else
                Return "document"
            End If
        End Function

        ''' <summary>
        ''' Returns the page name.
        ''' </summary>
        ''' <param name="doc_page">Document page identifier.</param>
        ''' <returns>Document page name.</returns>
        Public Shared Function GetPageName(ByVal doc_page As String) As String
            If doc_page = "F" Then : Return "front"
            ElseIf doc_page = "B" Then : Return "back"
            End If
            Return ""
        End Function
    End Class
End Namespace
