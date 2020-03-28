Imports Microsoft.VisualBasic.FileIO.FileSystem
Module ApplicationFileSystemInfo
    ''' <summary>
    ''' 表示应用程序的当前工作路径。
    ''' </summary>
    ''' <remarks></remarks>
    Private CurrentDirectory As String
    ''' <summary>
    ''' 产生应用程序的当前工作路径。
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub GenerateCurrentDirectory()
        CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory
        If CurrentDirectory(CurrentDirectory.Length - 1) <> "\" Then
            CurrentDirectory += "\"
        End If
    End Sub
    ''' <summary>
    ''' 获取应用程序的当前工作路径。
    ''' </summary>
    ''' <returns>字符串值，表示应用程序的当前工作路径。</returns>
    ''' <remarks></remarks>
    Public Function GetCurrentDirectory() As String
        GenerateCurrentDirectory()
        Return CurrentDirectory
    End Function
End Module
