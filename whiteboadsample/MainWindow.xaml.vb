Imports System.Windows.Ink
Imports System.Drawing

Class MainWindow

    '描画属性用変数
    Private inkDA As New DrawingAttributes
    Private filename As String
    Private filepath As String

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Dim args = Environment.GetCommandLineArgs()
        If args.Length <> 3 Then
            MessageBox.Show("不明な引数:" & args.Length)
            Me.Close()
            Exit Sub
        Else
            filename = args(1).ToString
            filepath = args(2).ToString
            If Right(filepath, 1) <> System.IO.Path.DirectorySeparatorChar Then
                filepath = filepath + System.IO.Path.DirectorySeparatorChar
            End If
        End If

        Me.WindowState = Windows.WindowState.Maximized
        Me.WindowStyle = Windows.WindowStyle.SingleBorderWindow
        Me.Topmost = True


        Slider1.Maximum = 42
        Slider1.Minimum = 2
        Slider1.SmallChange = 2
        Slider1.LargeChange = 2
        Slider1.TickFrequency = 2
        Slider1.TickPlacement = Primitives.TickPlacement.TopLeft
        Slider1.IsSnapToTickEnabled = True
        Slider1.Value = 4
        textbox1.Text = Slider1.Value

        Dim image = New BitmapImage()
        image.BeginInit()
        image.UriSource = New Uri(System.IO.Path.Combine(filepath, "製品状態テンプレ.bmp"), UriKind.Relative)
        image.EndInit()
        MyInkCanvas.Background = New ImageBrush(image)

        'inkCanvas初期設定
        inkDA.Width = textbox1.Text             'ペンの幅
        inkDA.Height = textbox1.Text            'ペンの高さ
        inkDA.StylusTip = StylusTip.Ellipse     'ペン形状
        MyInkCanvas.DefaultDrawingAttributes = inkDA

        openStroke(filename)

    End Sub


    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
    End Sub

    Private Sub openStroke(ByVal filename As String)
        If IsNothing(MyInkCanvas) Then Exit Sub

        If System.IO.File.Exists(filepath & filename & ".isf") Then
            Using fs As New System.IO.FileStream(filepath & filename & ".isf", IO.FileMode.Open)
                MyInkCanvas.Strokes = New Ink.StrokeCollection(fs)
            End Using
        End If
    End Sub

    Private Sub saveStroke(ByVal filename As String)
        Using fs As New System.IO.FileStream(filename, IO.FileMode.Create)
            MyInkCanvas.Strokes.Save(fs)
        End Using
    End Sub

    Private Sub saveJpeg(ByVal filename As String)
        ''ウインドウ領域のキャプチャ
        ''Bitmapの作成
        'Dim bmp As New Bitmap(CType(Me.Width, Integer) - 10, CType(Me.Height, Integer) - 105)
        'Debug.WriteLine(bmp.Width & ", " & bmp.Height)
        ''Graphicsの作成
        'Dim g As Graphics = Graphics.FromImage(bmp)
        ''画面全体をコピーする
        'g.CopyFromScreen(Me.Left + 5, Me.Top + 26, 0, 0, bmp.Size)
        'bmp.Save(filename)
        ''解放
        'g.Dispose()

        '2015-11-13追記   ウィンドウ領域のキャプチャからキャンバス描画内容の保存に変更  描画境界を全体の□に変更
        'ストロークが描画されている境界を取得
        'キャンバスではなくウインドウサイズの短形を描画範囲にする
        'こうするとsurfaceで実行時にずれなくなる
        'Dim rectBounds As Rect = MyInkCanvas.Strokes.GetBounds()
        Dim rectBounds As Rect = New Rect(0, 0, Me.Width, Me.Height)

        '描画先を作成
        Dim dv As New DrawingVisual
        Dim dc As DrawingContext = dv.RenderOpen()
        '描画エリアの位置補正（補正しないと黒い部分ができてしまう）
        dc.PushTransform(New TranslateTransform(-rectBounds.X, -rectBounds.Y))

        '描画エリア(dc)に四角形を作成
        '四角形の大きさはストロークが描画されている枠サイズとし、背景はInkCanvasコントロールと同じにする
        dc.DrawRectangle(MyInkCanvas.Background, Nothing, rectBounds)

        '上記で作成した描画エリアに(dc)にInkCanvasのストロークを描画
        MyInkCanvas.Strokes.Draw(dc)
        dc.Close()

        'ビジュアルオブジェクトをビットマップに変換する
        '96dpi固定,動作環境毎にdpiを取得するようにしたい
        Dim rtb As New RenderTargetBitmap(rectBounds.Width, rectBounds.Height, 96, 96, PixelFormats.Default)
        rtb.Render(dv)

        'ビットマップエンコーダー変数の宣言
        Dim enc As BitmapEncoder = Nothing
        enc = New JpegBitmapEncoder()

        If Not IsNothing(enc) Then
            'ビットマップフレームを作成してエンコーダーにフレームを追加する
            enc.Frames.Add(BitmapFrame.Create(rtb))
            'ファイルに書き込む
            Dim stream As System.IO.Stream = System.IO.File.Create(filename)
            enc.Save(stream)
            stream.Close()
        End If

    End Sub

    Private Sub SaveButton_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        saveJpeg(filepath & filename & ".jpg")
        saveStroke(filepath & filename & ".isf")
        Me.Close()
    End Sub

    Private Sub CancelButton_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub PenButton_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        inkDA.Width = textbox1.Text             'ペンの幅
        inkDA.Height = textbox1.Text            'ペンの高さ
        MyInkCanvas.DefaultDrawingAttributes = inkDA
        MyInkCanvas.EditingMode = InkCanvasEditingMode.Ink
    End Sub

    Private Sub EraserButton_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        MyInkCanvas.EraserShape = New RectangleStylusShape(Slider1.Value, Slider1.Value)
        MyInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint
    End Sub

    Private Sub ClearButton_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        MyInkCanvas.Strokes.Clear()
    End Sub


    Private Sub Slider1_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles Slider1.ValueChanged
        If IsNothing(MyInkCanvas) Then Exit Sub

        textbox1.Text = Math.Round(Slider1.Value)
    End Sub

    Private Sub textbox1_TextChanged(sender As Object, e As TextChangedEventArgs) Handles textbox1.TextChanged
        If IsNothing(MyInkCanvas) Then Exit Sub

        If MyInkCanvas.EditingMode = InkCanvasEditingMode.Ink Then
            MyInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint
            inkDA.Width = textbox1.Text             'ペンの幅
            inkDA.Height = textbox1.Text            'ペンの高さ
            MyInkCanvas.DefaultDrawingAttributes = inkDA
            MyInkCanvas.EditingMode = InkCanvasEditingMode.Ink
        ElseIf MyInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint Then
            MyInkCanvas.EditingMode = InkCanvasEditingMode.Ink
            MyInkCanvas.EraserShape = New RectangleStylusShape(Slider1.Value, Slider1.Value)
            MyInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint
        End If
    End Sub
End Class
