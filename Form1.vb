Imports System.Drawing
Imports System.Drawing.Imaging
Imports QRCoder
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Public Class Form1

#Disable Warning
    ' 声明控件
    Private WithEvents labelContent As New Label
    Private WithEvents textBoxContent As New TextBox
    Private WithEvents labelFilename As New Label
    Private WithEvents textBoxFilename As New TextBox
    Private WithEvents labelPath As New Label
    Private WithEvents textBoxPath As New TextBox
    Private WithEvents buttonBrowse As New Button
    Private WithEvents buttonGenerate As New Button
    Private WithEvents buttonClear As New Button
    Private WithEvents statusLabel As New Label
    Private WithEvents textLoFile As New TextBox
    Private WithEvents btnBrowerLoFile As New Button

    ' 实时预览相关控件
    Private WithEvents previewBox As New PictureBox
    Private WithEvents previewNameLabel As New Label
    Private WithEvents pageLabel As New Label
    Private WithEvents buttonPrev As New Button
    Private WithEvents buttonNext As New Button

    ' 预览状态
    Private previewIndex As Integer = 0
    Private previewImages As New List(Of Bitmap)
    Private previewNames As New List(Of String)


    Public Sub New()
        ' 设置窗体属性
        Me.Text = "批量二维码生成工具"
        Me.Size = New Size(900, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        InitializeControls()
    End Sub

    Private Sub InitializeControls()
        ' 二维码内容标签
        labelContent.Text = "二维码内容（每行一个）："
        labelContent.Location = New Point(20, 20)
        labelContent.Size = New Size(200, 20)

        textLoFile.Location = New Point(220, 20)
        textLoFile.Size = New Size(200, 20)
        textLoFile.ReadOnly = True

        btnBrowerLoFile.Location = New Point(420, 20)
        btnBrowerLoFile.Size = New Size(95, 25)
        btnBrowerLoFile.Text = "选择原始文件"

        ' 二维码内容文本框
        textBoxContent.Multiline = True
        textBoxContent.ScrollBars = ScrollBars.Vertical
        textBoxContent.Location = New Point(20, 45)
        textBoxContent.Size = New Size(500, 120)
        textBoxContent.AcceptsReturn = True
        AddHandler textBoxContent.TextChanged, AddressOf textBoxContent_TextChanged


        ' 文件名标签
        labelFilename.Text = "自定义文件名（可选，与上面行对应）："
        labelFilename.Location = New Point(20, 175)
        labelFilename.Size = New Size(300, 20)


        ' 文件名文本框
        textBoxFilename.Multiline = True
        textBoxFilename.ScrollBars = ScrollBars.Vertical
        textBoxFilename.Location = New Point(20, 200)
        textBoxFilename.Size = New Size(500, 120)
        textBoxFilename.AcceptsReturn = True
        AddHandler textBoxFilename.TextChanged, AddressOf textBoxContent_TextChanged


        ' 保存路径标签
        labelPath.Text = "保存路径："
        labelPath.Location = New Point(20, 330)
        labelPath.Size = New Size(80, 20)


        ' 保存路径文本框
        textBoxPath.Location = New Point(100, 330)
        textBoxPath.Size = New Size(320, 20)
        textBoxPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\QR_Codes"


        ' 浏览按钮
        buttonBrowse.Text = "浏览..."
        buttonBrowse.Location = New Point(425, 328)
        buttonBrowse.Size = New Size(95, 25)


        ' 生成按钮
        buttonGenerate.Text = "生成二维码"
        buttonGenerate.Location = New Point(20, 370)
        buttonGenerate.Size = New Size(150, 40)
        buttonGenerate.BackColor = Color.FromArgb(0, 120, 212)
        buttonGenerate.ForeColor = Color.White
        buttonGenerate.FlatStyle = FlatStyle.Flat


        ' 清空按钮
        buttonClear.Text = "清空内容"
        buttonClear.Location = New Point(180, 370)
        buttonClear.Size = New Size(150, 40)
        buttonClear.BackColor = Color.FromArgb(240, 240, 240)
        buttonClear.FlatStyle = FlatStyle.Flat


        ' 状态标签
        statusLabel.Text = "就绪"
        statusLabel.Location = New Point(20, 430)
        statusLabel.Size = New Size(500, 20)
        statusLabel.ForeColor = Color.Green


        ' 添加分隔线
        Dim separator As New Label
        separator.BorderStyle = BorderStyle.Fixed3D
        separator.Location = New Point(20, 425)
        separator.Size = New Size(500, 2)


        ' 添加使用说明
        Dim instruction As New Label
        instruction.Text = "使用说明：" & vbCrLf &
                         "1. 在上方文本框每行输入一个二维码内容" & vbCrLf &
                         "2. 在下方文本框输入对应文件名（可选，不填则使用默认名称）" & vbCrLf &
                         "3. 选择保存路径(不选则为默认路径)" & vbCrLf &
                         "4. 点击生成按钮" & vbCrLf &
                         "5. 可以选择需要转换成二维码的文档，每一行格式必须是:" & vbCrLf &
                         """" & "需要生成二维码的内容" & "丨" & "二维码图片名称" & """" & vbCrLf &
                         "确认无异常后，选择保存路径，然后点击生成即可"
        instruction.Location = New Point(20, 460)
        instruction.Size = New Size(500, 150)
        instruction.ForeColor = Color.Red
        ' 实时预览面板标题
        Dim previewTitle As New Label
        previewTitle.Text = "二维码实时预览"
        previewTitle.Location = New Point(560, 20)
        previewTitle.Size = New Size(320, 20)
        previewTitle.Font = New Font("Microsoft YaHei", 9, FontStyle.Bold)

        ' 预览图片显示区域
        previewBox.Location = New Point(570, 45)
        previewBox.Size = New Size(300, 300)
        previewBox.BackColor = Color.White
        previewBox.BorderStyle = BorderStyle.FixedSingle
        previewBox.SizeMode = PictureBoxSizeMode.Zoom

        ' 文件名预览标签
        previewNameLabel.Text = ""
        previewNameLabel.Location = New Point(570, 350)
        previewNameLabel.Size = New Size(300, 25)
        previewNameLabel.ForeColor = Color.FromArgb(0, 120, 212)
        previewNameLabel.Font = New Font("Microsoft YaHei", 10)
        previewNameLabel.AutoEllipsis = True

        ' 页码标签
        pageLabel.Text = "0 / 0"
        pageLabel.Location = New Point(560, 380)
        pageLabel.Size = New Size(320, 20)
        pageLabel.TextAlign = ContentAlignment.MiddleCenter

        ' 上一页 / 下一页按钮
        buttonPrev.Text = "◀ 上一页"
        buttonPrev.Location = New Point(580, 410)
        buttonPrev.Size = New Size(130, 35)
        buttonPrev.Enabled = False

        buttonNext.Text = "下一页 ▶"
        buttonNext.Location = New Point(730, 410)
        buttonNext.Size = New Size(130, 35)
        buttonNext.Enabled = False

        Me.Controls.AddRange({instruction, separator,
                             statusLabel, buttonClear, buttonGenerate, buttonBrowse, textLoFile, btnBrowerLoFile,
                             labelPath, textBoxPath, labelContent, labelFilename, textBoxContent, textBoxFilename,
                             previewTitle, previewBox, previewNameLabel, pageLabel, buttonPrev, buttonNext})
    End Sub

    ' 内容文本框变更时，实时刷新预览
    Private Sub textBoxContent_TextChanged(sender As Object, e As EventArgs)
        RefreshPreview()
    End Sub

    ' 根据内容行与自定义文件名，生成最终文件名列表（含 .jpg 扩展名）
    Private Function BuildFileNames(contents As List(Of String)) As List(Of String)
        Dim customNames As String() = textBoxFilename.Lines
        Dim fileNames As New List(Of String)

        For i As Integer = 0 To contents.Count - 1
            Dim fileName As String = ""

            ' 检查是否有对应的自定义文件名
            If i < customNames.Length AndAlso Not String.IsNullOrWhiteSpace(customNames(i).Trim()) Then
                Dim customName As String = customNames(i).Trim()

                ' 清理文件名中的非法字符
                For Each invalidChar In IO.Path.GetInvalidFileNameChars()
                    customName = customName.Replace(invalidChar, "_")
                Next

                ' 确保文件名不为空
                If String.IsNullOrWhiteSpace(customName) Then
                    customName = $"二维码{i + 1}"
                End If

                fileName = customName
            Else
                ' 使用默认命名
                fileName = $"二维码{i + 1}"
            End If

            ' 确保扩展名
            If Not fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) Then
                fileName &= ".jpg"
            End If

            fileNames.Add(fileName)
        Next

        Return fileNames
    End Function

    ' 刷新二维码预览列表
    Private Sub RefreshPreview()
        ' 释放旧图片资源
        For Each bmp As Bitmap In previewImages
            bmp.Dispose()
        Next
        previewImages.Clear()

        ' 过滤有效内容行
        Dim filteredContents As New List(Of String)
        For Each line As String In textBoxContent.Lines
            If Not String.IsNullOrWhiteSpace(line) Then
                filteredContents.Add(line.Trim())
            End If
        Next

        ' 逐行生成预览二维码
        For Each content As String In filteredContents
            Dim bmp As Bitmap = MakePreviewImage(content)
            If bmp IsNot Nothing Then
                previewImages.Add(bmp)
            End If
        Next

        ' 计算文件名预览列表
        previewNames = BuildFileNames(filteredContents)

        ' 调整当前页码到有效范围
        If previewImages.Count > 0 Then
            If previewIndex >= previewImages.Count Then
                previewIndex = previewImages.Count - 1
            End If
            If previewIndex < 0 Then
                previewIndex = 0
            End If
        Else
            previewIndex = 0
        End If

        ShowPreview()
    End Sub

    ' 为单行内容生成预览二维码图片
    Private Function MakePreviewImage(content As String) As Bitmap
        Try
            Dim qrGenerator As New QRCodeGenerator()
            Dim qrCodeData As QRCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q)
            Dim qrCode As New QRCode(qrCodeData)
            Return qrCode.GetGraphic(10, Color.Black, Color.White, True)
        Catch
            Return Nothing
        End Try
    End Function

    ' 显示当前预览页
    Private Sub ShowPreview()
        If previewImages.Count = 0 Then
            previewBox.Image = Nothing
            previewNameLabel.Text = ""
            pageLabel.Text = "0 / 0"
            buttonPrev.Enabled = False
            buttonNext.Enabled = False
            Return
        End If

        previewBox.Image = previewImages(previewIndex)

        ' 显示文件名预览
        If previewIndex < previewNames.Count Then
            previewNameLabel.Text = $"文件名：{previewNames(previewIndex)}"
        Else
            previewNameLabel.Text = ""
        End If

        pageLabel.Text = $"{previewIndex + 1} / {previewImages.Count}"
        buttonPrev.Enabled = previewIndex > 0
        buttonNext.Enabled = previewIndex < previewImages.Count - 1
    End Sub

    ' 上一页
    Private Sub buttonPrev_Click(sender As Object, e As EventArgs) Handles buttonPrev.Click
        If previewImages.Count > 0 AndAlso previewIndex > 0 Then
            previewIndex -= 1
            ShowPreview()
        End If
    End Sub

    ' 下一页
    Private Sub buttonNext_Click(sender As Object, e As EventArgs) Handles buttonNext.Click
        If previewImages.Count > 0 AndAlso previewIndex < previewImages.Count - 1 Then
            previewIndex += 1
            ShowPreview()
        End If
    End Sub

    ' 浏览文件夹事件
    Private Sub buttonBrowse_Click(sender As Object, e As EventArgs) Handles buttonBrowse.Click
        Using folderDialog As New FolderBrowserDialog()
            folderDialog.Description = "选择二维码保存目录"
            folderDialog.SelectedPath = If(IO.Directory.Exists(textBoxPath.Text), textBoxPath.Text,
                                          Environment.GetFolderPath(Environment.SpecialFolder.Desktop))

            If folderDialog.ShowDialog() = DialogResult.OK Then
                textBoxPath.Text = folderDialog.SelectedPath
            End If
        End Using
    End Sub

    ' 生成二维码事件
    Private Sub buttonGenerate_Click(sender As Object, e As EventArgs) Handles buttonGenerate.Click
        ' 验证数据源
        Dim contents As String() = textBoxContent.Lines
        ' 过滤掉空行
        Dim filteredContents As New List(Of String)
        For Each content As String In contents
            If Not String.IsNullOrWhiteSpace(content) Then
                filteredContents.Add(content.Trim())
            End If
        Next

        If filteredContents.Count = 0 Then
            MessageBox.Show("请输入至少一行有效的二维码内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' 获取保存路径
        Dim savePath As String = textBoxPath.Text.Trim()
        If String.IsNullOrWhiteSpace(savePath) Then
            savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\QR_Codes"
            textBoxPath.Text = savePath
        End If

        ' 创建目录（如果不存在）
        Try
            If Not IO.Directory.Exists(savePath) Then
                IO.Directory.CreateDirectory(savePath)
            End If
        Catch ex As Exception
            MessageBox.Show($"无法创建目录：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ' 生成文件名列表（与预览保持一致）
        Dim fileNames As List(Of String) = BuildFileNames(filteredContents)

        ' 生成二维码
        Dim qrGenerator As New QRCodeGenerator()
        Dim successCount As Integer = 0

        ' 临时禁用生成按钮
        buttonGenerate.Enabled = False
        buttonGenerate.Text = "生成中..."
        statusLabel.Text = "正在生成二维码..."
        Me.Refresh()

        Try
            For i As Integer = 0 To filteredContents.Count - 1
                Dim content As String = filteredContents(i)

                ' 更新状态
                statusLabel.Text = $"正在生成第 {i + 1} 个二维码..."
                Me.Refresh()

                ' 生成二维码数据
                Dim qrCodeData As QRCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q)
                Dim qrCode As New QRCode(qrCodeData)

                ' 渲染为位图
                Using qrImage As Bitmap = qrCode.GetGraphic(20, Color.Black, Color.White, True)
                    ' 构建完整保存路径
                    Dim fileName As String = fileNames(i)
                    Dim fullPath As String = IO.Path.Combine(savePath, fileName)

                    ' 处理文件名重复
                    Dim counter As Integer = 1
                    Dim originalFileName As String = IO.Path.GetFileNameWithoutExtension(fileName)
                    Dim extension As String = IO.Path.GetExtension(fileName)

                    While IO.File.Exists(fullPath)
                        fileName = $"{originalFileName}_{counter}{extension}"
                        fullPath = IO.Path.Combine(savePath, fileName)
                        counter += 1
                    End While

                    ' 保存为 JPG
                    qrImage.Save(fullPath, ImageFormat.Jpeg)
                End Using

                successCount += 1
            Next

            statusLabel.Text = $"成功生成 {successCount} 个二维码！"

            '生成成功时清空数据
            textBoxContent.Clear()
            textBoxFilename.Clear()
            textLoFile.Clear()
            previewIndex = 0
            RefreshPreview()
            Dim msg = MessageBox.Show($"成功生成 {successCount} 个二维码文件！" & vbCrLf &
                          $"保存位置：{savePath}" & vbCrLf &
                          "是否打开保存目录？", "完成",
                          MessageBoxButtons.YesNo, MessageBoxIcon.Information)

            ' 询问是否打开目录
            If msg = DialogResult.Yes Then
                Process.Start("explorer.exe", savePath)
            End If

        Catch ex As Exception
            MessageBox.Show($"生成过程中出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            statusLabel.Text = "生成失败"
        Finally
            ' 恢复按钮状态
            buttonGenerate.Enabled = True
            buttonGenerate.Text = "生成二维码"
        End Try
    End Sub

    ' 清空内容事件
    Private Sub buttonClear_Click(sender As Object, e As EventArgs) Handles buttonClear.Click
        If MessageBox.Show("确定要清空所有内容吗？", "确认",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            textBoxContent.Clear()
            textBoxFilename.Clear()
            textLoFile.Clear()
            statusLabel.Text = "已清空"
            previewIndex = 0
            RefreshPreview()
        End If
    End Sub

    ' 窗体关闭时的事件
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If MessageBox.Show("确定要退出程序吗？", "退出确认",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then
            e.Cancel = True
        End If
    End Sub

    Private Sub btnBrowerLoFile_Click(sender As Object, e As EventArgs) Handles btnBrowerLoFile.Click
        If btnBrowerLoFile.Text = "选择原始文件" Then
            Using dialog As New OpenFileDialog()
                dialog.Filter = "文档 (*.txt)|*.txt|所有文件 (*.*)|*.*"
                dialog.Title = "选择需要生成二维码的源文件"

                If dialog.ShowDialog() = DialogResult.OK Then
                    textLoFile.Text = dialog.FileName

                    MessageBox.Show("首选文档已经选择完成，再点击(生成二维码内容与名称按钮)，生成多行二维码内容与名称", "首选文档选择成功提示")
                End If
            End Using
        Else
            If Not String.IsNullOrEmpty(textLoFile.Text) Then
                Dim filedata = ReadFileData(textLoFile.Text)
                Dim lines As String() = filedata.Split(vbCrLf)
                Dim line As String(,)

                Dim linePart As String()
                linePart = lines(0).Split("丨"c)

                If linePart.Length <> 2 Then
                    MessageBox.Show("首选文档格式不满足要求，请重新选择!!!", "文档选择错误提示")
                    textLoFile.Clear()
                    Return
                End If

                ReDim line(lines.Length - 1, 1)
                For i As Integer = 0 To lines.Length - 1
                    If lines(i) <> "" Then
                        linePart = lines(i).Split("丨"c)
                        line(i, 0) = linePart(0)
                        line(i, 1) = linePart(1)
                    End If


                Next

                OuttoText(textBoxContent, textBoxFilename, line)
                previewIndex = 0
                RefreshPreview()

            End If
        End If
    End Sub
    Private Sub OuttoText(tf As TextBox, tn As TextBox, lines As String(,))
        Dim i As Integer, j As Integer

        For i = 0 To UBound(lines, 1)
            If lines(i, 0) <> "" Then
                tf.AppendText(lines(i, 0) & vbCrLf)
                tn.AppendText(lines(i, 1) & vbCrLf)
            End If

        Next


    End Sub
    Private Sub textLoFile_TextChanged(sender As Object, e As EventArgs) Handles textLoFile.TextChanged
        If String.IsNullOrEmpty(textLoFile.Text) Then
            btnBrowerLoFile.Text = "选择原始文件"
            btnBrowerLoFile.Width = 95
            btnBrowerLoFile.BackColor = Color.White
        Else
            btnBrowerLoFile.Text = "生成二维码内容与名称"
            btnBrowerLoFile.Width = 150
            btnBrowerLoFile.BackColor = Color.LightGreen
        End If

    End Sub

    Public Function ReadFileData(ByVal filePath As String, Optional ByVal encod As System.Text.Encoding = Nothing) As String

        Try
            If encod Is Nothing Then
                encod = System.Text.Encoding.Default
            End If

            Return System.IO.File.ReadAllText(filePath, encod)

        Catch ex As Exception
            MessageBox.Show("读取文件时出错: " & ex.Message & vbCrLf & "文件路径: " & filePath)
            Return ""

        End Try
    End Function
End Class
