Module TimeStampBuilder
    ''' <summary>
    ''' 生成YYYYMMMDD-HHMM格式的时间戳。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function BuildTimeStamp() As String
        Dim TimeStamp As New String(String.Format("{0:D4}", Now.Year) & String.Format("{0:D2}", Now.Month) & String.Format("{0:D2}", Now.Day) & "-" & String.Format("{0:D2}", Now.Hour) & String.Format("{0:D2}", Now.Minute))
        Return TimeStamp
    End Function
End Module
