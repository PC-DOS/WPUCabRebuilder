Imports System.IO
Imports System.Windows.Forms
Imports System.Windows.Window
Imports System.Xml
Class MainWindow
    Dim InputDirectory As String
    Dim OutputDiectory As String
    Dim EmptyList As New List(Of String)
    Dim MessageList As New List(Of String)
    Dim IsLogginEnabled As Boolean = True
    Dim LogTimeStamp As String
    Dim ApplicationDirectory As String
    Sub RefreshMessageList()
        lstMessage.ItemsSource = EmptyList
        lstMessage.ItemsSource = MessageList
        DoEvents()
    End Sub
    Sub AddMessage(MessageText As String)
        MessageList.Add(MessageText)
        RefreshMessageList()
        lstMessage.SelectedIndex = lstMessage.Items.Count - 1
        lstMessage.ScrollIntoView(lstMessage.SelectedItem)
        If IsLogginEnabled Then
            Try
                Dim LogFileStream As New StreamWriter(ApplicationDirectory & "WPUCabRebuilderRuntimeLog_" & LogTimeStamp & ".log", True)
                LogFileStream.WriteLine(Now.ToShortTimeString & ":")
                LogFileStream.WriteLine(MessageText)
                LogFileStream.Flush()
                LogFileStream.Close()
            Catch ex As Exception

            End Try
        End If
    End Sub
    Sub LockUI()
        txtInputDir.IsEnabled = False
        txtOutputDir.IsEnabled = False
        btnBrowseInput.IsEnabled = False
        btnBrowseOutput.IsEnabled = False
        btnStart.IsEnabled = False
        chkMergeRegistry.IsEnabled = False
        chkProcRegistry.IsEnabled = False
        chkMergeRegistryMainOSOnly.IsEnabled = False
        chkUsePartition.IsEnabled = False
    End Sub
    Sub UnlockUI()
        txtInputDir.IsEnabled = True
        txtOutputDir.IsEnabled = True
        btnBrowseInput.IsEnabled = True
        btnBrowseOutput.IsEnabled = True
        btnStart.IsEnabled = True
        chkMergeRegistry.IsEnabled = True
        chkProcRegistry.IsEnabled = True
        chkMergeRegistryMainOSOnly.IsEnabled = chkMergeRegistry.IsEnabled
        chkUsePartition.IsEnabled = True
    End Sub
    Private Sub SetTaskbarProgess(MaxValue As Integer, MinValue As Integer, CurrentValue As Integer, Optional State As Shell.TaskbarItemProgressState = Shell.TaskbarItemProgressState.Normal)
        If MaxValue <= MinValue Or CurrentValue < MinValue Or CurrentValue > MaxValue Then
            Exit Sub
        End If
        TaskbarItem.ProgressValue = (CurrentValue - MinValue) / (MaxValue - MinValue)
        TaskbarItem.ProgressState = State
    End Sub
    Function GetPathFromFile(FilePath As String) As String
        If FilePath.Trim = "" Then
            Return ""
        End If
        If FilePath(FilePath.Length - 1) = "\" Then
            Return FilePath
        End If
        Try
            Return FilePath.Substring(0, FilePath.LastIndexOf("\"))
        Catch ex As Exception
            Return ""
        End Try
    End Function
    Private Sub btnBrowseInput_Click(sender As Object, e As RoutedEventArgs) Handles btnBrowseInput.Click
        Dim FolderBrowser As New FolderBrowserDialog
        With FolderBrowser
            .Description = "请指定 Windows Phone Update CAB 文件的位置，然后单击""确定""按钮。"
        End With
        If FolderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            InputDirectory = FolderBrowser.SelectedPath
            If InputDirectory(InputDirectory.Length - 1) <> "\" Then
                InputDirectory = InputDirectory & "\"
            End If
            txtInputDir.Text = InputDirectory
        End If
    End Sub

    Private Sub btnBrowseOutput_Click(sender As Object, e As RoutedEventArgs) Handles btnBrowseOutput.Click
        Dim FolderBrowser As New FolderBrowserDialog
        With FolderBrowser
            .Description = "请指定重建完成的目录结构要输出的位置，然后单击""确定""按钮。"
        End With
        If FolderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            OutputDiectory = FolderBrowser.SelectedPath
            If OutputDiectory(OutputDiectory.Length - 1) <> "\" Then
                OutputDiectory = OutputDiectory & "\"
            End If
            txtOutputDir.Text = OutputDiectory
        End If
    End Sub

    Private Sub btnStart_Click(sender As Object, e As RoutedEventArgs) Handles btnStart.Click
        LockUI()
        If txtInputDir.Text.Trim = "" Then
            MessageBox.Show("CAB 输入路径不能为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            UnlockUI()
            Exit Sub
        End If
        If txtOutputDir.Text.Trim = "" Then
            MessageBox.Show("输出路径不能为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            UnlockUI()
            Exit Sub
        End If
        If Not Directory.Exists(OutputDiectory) Then
            Try
                Directory.CreateDirectory(OutputDiectory)
            Catch ex As Exception
                MessageBox.Show("试图创建输出目录""" & OutputDiectory & """时发生错误: " & vbCrLf & ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
                UnlockUI()
                Exit Sub
            End Try
        End If
        With prgProgress
            .Minimum = 0
            .Maximum = 100
            .Value = 0
        End With
        MessageList.Clear()
        RefreshMessageList()
        AddMessage("正在确定 CAB 文件总数。")
        Dim nCabFileCount As Integer = Directory.GetFiles(InputDirectory, "*.cab", SearchOption.TopDirectoryOnly).Length
        If nCabFileCount = 0 Then
            MessageBox.Show("输入目录""" & InputDirectory & """中不包含任何 CAB 文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AddMessage("输入目录""" & InputDirectory & """中不包含任何 CAB 文件。")
            AddMessage("发生错误，取消操作。")
            UnlockUI()
            Exit Sub
        End If
        AddMessage("计算完毕，共有 " & nCabFileCount.ToString & " 个 CAB 文件。")
        With prgProgress
            .Minimum = 0
            .Maximum = nCabFileCount
            .Value = 0
        End With
        SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
        Dim nSuccess As UInteger = 0
        Dim nFail As UInteger = 0
        Dim nIgnored As UInteger = 0
        Dim IsErrorOccurred As Boolean = False

        If chkMergeRegistry.IsChecked Then
            Dim OutputFileStream As New IO.StreamWriter(OutputDiectory & "import.reg", False)
            OutputFileStream.WriteLine("Windows Registry Editor Version 5.00")
            OutputFileStream.WriteLine()
            If chkProcRegistry.IsChecked Then
                OutputFileStream.WriteLine("[HKEY_LOCAL_MACHINE\RTSYSTEM\ControlSet001\Control\ProductOptions]")
            Else
                OutputFileStream.WriteLine("[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\ProductOptions]")
            End If
            OutputFileStream.WriteLine("""AnotherSuite""=hex(7):50,00,68,00,6f,00,6e,00,65,00,4e,00,54,00,00,00,00,00")
            OutputFileStream.WriteLine()
            OutputFileStream.Flush()
            OutputFileStream.Close()
        End If

        For Each CabFilePath In Directory.EnumerateFiles(InputDirectory, "*.cab", SearchOption.TopDirectoryOnly)
            Dim TempFilePath As String
            TempFilePath = CabFilePath.Substring(0, CabFilePath.Length - 4) & "\"
            AddMessage("正在解压缩 CAB 文件""" & CabFilePath & """到""" & TempFilePath & """。")
            If Directory.Exists(TempFilePath) Then
                Try
                    Directory.Delete(TempFilePath, True)
                Catch ex As Exception
                    AddMessage("无法解压缩 CAB 文件""" & TempFilePath & """，发生错误: " & ex.Message)
                    nFail += 1
                    prgProgress.Value += 1
                    SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
                    Try
                        Directory.Delete(TempFilePath, True)
                    Catch ex2 As Exception

                    End Try
                    Continue For
                End Try
            End If
            Dim SevenZipProcess As New Process
            With SevenZipProcess
                .StartInfo = New ProcessStartInfo("7z.exe", "x " & "-o""" & TempFilePath & """ """ & CabFilePath & """")
                .StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                .Start()
                .WaitForExit()
            End With

            Dim UpdateInfoFile As New XmlDocument
            AddMessage("正在打开包描述文件""" & TempFilePath & "man.dsm.xml""。")
            Try
                UpdateInfoFile.Load(TempFilePath & "man.dsm.xml")
            Catch ex As Exception
                AddMessage("无法打开包描述文件""" & TempFilePath & "man.dsm.xml""，发生错误: " & ex.Message)
                nFail += 1
                prgProgress.Value += 1
                SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
                Try
                    Directory.Delete(TempFilePath, True)
                Catch ex2 As Exception

                End Try
                Continue For
            End Try

            Dim nsMgr As New XmlNamespaceManager(UpdateInfoFile.NameTable)
            nsMgr.AddNamespace("ns", "http://schemas.microsoft.com/embedded/2004/10/ImageUpdate")

            Dim PartitionNode As XmlNode = UpdateInfoFile.SelectSingleNode("/ns:Package/ns:Partition", nsMgr)
            AddMessage("正在定位 XML 节点""/Package/Partition""。")
            If IsNothing(PartitionNode) Then
                AddMessage("XML 节点定位失败，退出操作。")
                nFail += 1
                prgProgress.Value += 1
                SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
                Try
                    Directory.Delete(TempFilePath, True)
                Catch ex2 As Exception

                End Try
                Continue For
            End If
            Dim PartitionName As String
            PartitionName = PartitionNode.InnerText
            AddMessage("XML 节点""/Package/PartitionName""定位成功，CAB 包""" & CabFilePath & """适用于分区""" & PartitionName & """。")

            Dim CustomInformationNode As XmlNode = UpdateInfoFile.SelectSingleNode("/ns:Package/ns:Files", nsMgr)
            AddMessage("正在定位 XML 节点""/Package/Files""。")
            If IsNothing(CustomInformationNode) Then
                AddMessage("XML 节点定位失败，退出操作。")
                nFail += 1
                prgProgress.Value += 1
                SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
                Try
                    Directory.Delete(TempFilePath, True)
                Catch ex2 As Exception

                End Try
                Continue For
            End If
            AddMessage("XML 节点""/Package/Files""定位成功，共有 " & CustomInformationNode.ChildNodes.Count & " 条记录。")
            Dim TempFileInfo As New WindowsUpdatePackageFileNodeProperties
            Dim FileList As XmlNodeList = CustomInformationNode.ChildNodes
            For Each FileNode As XmlNode In FileList
                IsErrorOccurred = False
                Dim FileElement As XmlElement = FileNode
                If FileElement.Name <> "FileEntry" Then
                    Continue For
                End If
                Try
                    With TempFileInfo
                        .CabPath = TempFilePath & FileElement.GetElementsByTagName("CabPath")(0).InnerText
                        .DevicePath = OutputDiectory & IIf(chkUsePartition.IsChecked, PartitionName & "\", "") & FileElement.GetElementsByTagName("DevicePath")(0).InnerText
                        .FileType = FileElement.GetElementsByTagName("FileType")(0).InnerText
                    End With
                    Dim CopyDest As String = GetPathFromFile(TempFileInfo.DevicePath)
                    If Not Directory.Exists(CopyDest) Then
                        Directory.CreateDirectory(CopyDest)
                    End If
                    If File.Exists(TempFileInfo.DevicePath) Then
                        File.Delete(TempFileInfo.DevicePath)
                    End If
                    File.Copy(TempFileInfo.CabPath, TempFileInfo.DevicePath)
                    AddMessage("已成功从""" & TempFileInfo.CabPath & """复制文件到""" & TempFileInfo.DevicePath & """。")
                    If chkProcRegistry.IsChecked And (TempFileInfo.FileType = "Registry" Or TempFileInfo.FileType = "RegistryMultiStringAppend") Then
                        Dim InputFileStream As New IO.StreamReader(TempFileInfo.CabPath)
                        Dim FileContent As String
                        FileContent = InputFileStream.ReadToEnd()
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\SYSTEM]", "[HKEY_LOCAL_MACHINE\RTSYSTEM]")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\System]", "[HKEY_LOCAL_MACHINE\RTSystem]")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\SOFTWARE]", "[HKEY_LOCAL_MACHINE\RTSOFTWARE]")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\Software]", "[HKEY_LOCAL_MACHINE\RTSoftware]")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\SYSTEM\", "[HKEY_LOCAL_MACHINE\RTSYSTEM\")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\System\", "[HKEY_LOCAL_MACHINE\RTSystem\")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\SOFTWARE\", "[HKEY_LOCAL_MACHINE\RTSOFTWARE\")
                        FileContent = FileContent.Replace("[HKEY_LOCAL_MACHINE\Software\", "[HKEY_LOCAL_MACHINE\RTSoftware\")
                        Dim OutputFileStream As New IO.StreamWriter(TempFileInfo.DevicePath, False)
                        OutputFileStream.WriteLine(FileContent)
                        OutputFileStream.Flush()
                        OutputFileStream.Close()
                        InputFileStream.Close()
                    End If
                    If chkMergeRegistry.IsChecked And (TempFileInfo.FileType = "Registry" Or TempFileInfo.FileType = "RegistryMultiStringAppend") Then
                        If (Not chkMergeRegistryMainOSOnly.IsChecked) Or (chkMergeRegistryMainOSOnly.IsChecked And PartitionName = "MainOS") Then
                            Dim InputFileStream As New IO.StreamReader(TempFileInfo.DevicePath)
                            Dim FileContent As String
                            FileContent = InputFileStream.ReadToEnd()
                            FileContent = FileContent.Replace("Windows Registry Editor Version 5.00", "")
                            InputFileStream.Close()
                            If Not FileContent.Contains("[HKEY_LOCAL_MACHINE\BCD") Then
                                Dim OutputFileStream As New IO.StreamWriter(OutputDiectory & "import.reg", True)
                                OutputFileStream.WriteLine(";Registry File imported from " & TempFileInfo.CabPath.Replace(InputDirectory, ""))
                                OutputFileStream.WriteLine(";You can find original file at " & TempFileInfo.DevicePath.Replace(OutputDiectory, ""))
                                OutputFileStream.WriteLine(FileContent)
                                OutputFileStream.WriteLine()
                                OutputFileStream.Flush()
                                OutputFileStream.Close()
                            End If
                        End If
                    End If
                    DoEvents()
                Catch ex As Exception
                    AddMessage("处理 CAB 包""" & CabFilePath & """时发生错误: " & ex.Message)
                    nFail += 1
                    prgProgress.Value += 1
                    SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
                    Try
                        Directory.Delete(TempFilePath, True)
                    Catch ex2 As Exception

                    End Try
                    IsErrorOccurred = True
                    Exit For
                End Try
            Next
            If Not IsErrorOccurred Then
                AddMessage("对 CAB 包文件""" & CabFilePath & """的操作成功完成。")
                Try
                    Directory.Delete(TempFilePath, True)
                Catch ex2 As Exception

                End Try
                nSuccess += 1
                prgProgress.Value += 1
                SetTaskbarProgess(prgProgress.Maximum, 0, prgProgress.Value)
            End If
        Next

        MessageBox.Show("操作完成，共有 " & nSuccess.ToString & "个文件被成功复制，有 " & nIgnored.ToString & " 个文件被忽略，处理 " & nFail.ToString & " 个文件时出错。", "大功告成!", MessageBoxButtons.OK, MessageBoxIcon.Information)
        UnlockUI()
        With prgProgress
            .Minimum = 0
            .Maximum = 100
            .Value = 0
        End With
        SetTaskbarProgess(100, 0, 0)
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LogTimeStamp = BuildTimeStamp() & "_" & (New Random).Next.ToString
        ApplicationDirectory = GetCurrentDirectory()
    End Sub
End Class
