Option Strict On
Option Explicit On

Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class Form1

    ' Pausa entre cada tecla enviada ao Primavera (ms). Sobe para 60-80 se o Primavera perder caracteres.
    Private Const PausaEntreSeriais As Integer = 40
    ' Segundos de contagem decrescente antes de comecar a enviar.
    Private Const SegundosParaFocar As Integer = 4

    ' Tudo o que conta como separador entre seriais: espaco, tab, CR, LF, espaco duro (NBSP)
    ' e espaco de largura zero (aparece em copias de web/Excel).
    Private Shared ReadOnly Separadores As Char() =
        {" "c, ChrW(9), ChrW(10), ChrW(13), ChrW(160), ChrW(8203)}

    ' Lixo colado a volta do serial que nunca faz parte do SN.
    Private Shared ReadOnly LixoNasPontas As Char() =
        {" "c, ChrW(9), ChrW(160), ChrW(8203), """"c, "'"c}

    ' Caracteres que o SendKeys interpreta como modificadores e que tem de ser escapados.
    Private Const CaracteresEspeciaisSendKeys As String = "+^%~(){}[]"

    Private enviando As Boolean
    Private cancelarEnvio As Boolean

    ' Titulo com a versao. Guardado porque o titulo e usado como barra de progresso no envio.
    Private tituloBase As String = "MegaImporter"

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        tituloBase = Me.Text & "  -  v" & Atualizador.VersaoAtual().ToString()
        Me.Text = tituloBase

        AtualizarContador()
        MostrarVersao()
        txtbpicagem.Focus()

        ' Apaga o executavel da versao anterior, se ficou de uma atualizacao.
        Atualizador.LimparResiduos()

        ' Verificacao silenciosa: se nao houver rede ou nova versao, nao incomoda ninguem.
        Await VerificarAtualizacoesAsync(silencioso:=True)
    End Sub

    ' ---------------------------------------------------------------
    ' Normalizacao
    ' ---------------------------------------------------------------

    ''' <summary>
    ''' Parte o texto em seriais limpos. Aceita um por linha, varios por linha
    ''' separados por espacos/tabs, linhas em branco, CRLF ou LF, e espacos a mais
    ''' antes/depois de cada serial.
    ''' </summary>
    Private Shared Function Normalizar(texto As String) As List(Of String)
        Dim resultado As New List(Of String)()
        If String.IsNullOrWhiteSpace(texto) Then Return resultado

        For Each token As String In texto.Split(Separadores, StringSplitOptions.RemoveEmptyEntries)
            Dim sn As String = token.Trim(LixoNasPontas)
            If sn.Length > 0 Then resultado.Add(sn)
        Next

        Return resultado
    End Function

    ''' <summary>Serial pronto para SendKeys, com os caracteres especiais escapados.</summary>
    Private Shared Function EscaparSendKeys(texto As String) As String
        Dim sb As New StringBuilder(texto.Length + 8)
        For Each c As Char In texto
            If CaracteresEspeciaisSendKeys.IndexOf(c) >= 0 Then
                sb.Append("{"c).Append(c).Append("}"c)
            Else
                sb.Append(c)
            End If
        Next
        Return sb.ToString()
    End Function

    ''' <summary>Unica fonte de verdade: o que esta na RichTextBox.</summary>
    Private Function SeriaisAtuais() As List(Of String)
        Return Normalizar(RichTextBox1.Text)
    End Function

    Private Sub MostrarSeriais(lista As List(Of String))
        ' Atribuir Lines de uma vez e muito mais rapido que AppendText em ciclo.
        RichTextBox1.Lines = lista.ToArray()
        AtualizarContador(lista.Count)
    End Sub

    Private Sub AtualizarContador(Optional total As Integer = -1)
        If total < 0 Then total = SeriaisAtuais().Count
        lblContador.Text = "ITENS CARREGADOS: " & total.ToString()
    End Sub

    ' ---------------------------------------------------------------
    ' Importar do clipboard
    ' ---------------------------------------------------------------

    Private Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        If enviando Then Return

        Dim clipText As String
        Try
            If Not Clipboard.ContainsText() Then
                MessageBox.Show("O clipboard nao contem texto.", "Importar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            clipText = Clipboard.GetText()
        Catch ex As Exception
            ' O clipboard pode estar bloqueado por outra aplicacao.
            MessageBox.Show("Nao foi possivel ler o clipboard: " & ex.Message, "Importar",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        Dim lista As List(Of String) = Normalizar(clipText)

        If lista.Count = 0 Then
            MessageBox.Show("Nao foi encontrado nenhum numero de serie no clipboard.", "Importar",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim duplicados As Integer = lista.Count - lista.Distinct(StringComparer.OrdinalIgnoreCase).Count()
        If duplicados > 0 Then
            Dim resp As DialogResult = MessageBox.Show(
                "Foram encontrados " & duplicados.ToString() & " serial(is) repetido(s)." & Environment.NewLine &
                "Remover os repetidos?", "Duplicados",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If resp = DialogResult.Yes Then
                lista = lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            End If
        End If

        MostrarSeriais(lista)
        txtbpicagem.Focus()
    End Sub

    ' ---------------------------------------------------------------
    ' Enviar para o Primavera
    ' ---------------------------------------------------------------

    Private Async Sub btnEnviar_Click(sender As Object, e As EventArgs) Handles btnenviar.Click
        ' Segundo clique durante o envio = parar.
        If enviando Then
            cancelarEnvio = True
            Return
        End If

        Dim lista As List(Of String) = SeriaisAtuais()
        If lista.Count = 0 Then
            MessageBox.Show("Nao ha numeros de serie carregados.", "Enviar",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim confirmar As DialogResult = MessageBox.Show(
            "Vao ser enviados " & lista.Count.ToString() & " seriais." & Environment.NewLine &
            "Tens " & SegundosParaFocar.ToString() & " segundos para colocar o cursor no Primavera." & Environment.NewLine &
            Environment.NewLine &
            "Para parar a meio, volta a esta janela e clica em PARAR.",
            "Enviar", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)

        If confirmar <> DialogResult.OK Then Return

        enviando = True
        cancelarEnvio = False
        btnImport.Enabled = False
        btnlimpar.Enabled = False
        txtbpicagem.Enabled = False
        btnenviar.Text = "PARAR"

        Try
            ' Contagem decrescente sem congelar a janela (o Thread.Sleep antigo bloqueava a UI).
            For s As Integer = SegundosParaFocar To 1 Step -1
                Me.Text = "A comecar em " & s.ToString() & "..."
                Await Task.Delay(1000)
                If cancelarEnvio Then Exit For
            Next

            Dim enviados As Integer = 0
            For Each sn As String In lista
                If cancelarEnvio Then Exit For

                SendKeys.SendWait(EscaparSendKeys(sn))
                SendKeys.SendWait("{ENTER}") ' trocar por "{TAB}" se o Primavera pedir Tab
                enviados += 1

                Me.Text = "A enviar " & enviados.ToString() & "/" & lista.Count.ToString()
                Await Task.Delay(PausaEntreSeriais)
            Next

            If cancelarEnvio Then
                MessageBox.Show("Cancelado. Enviados " & enviados.ToString() & " de " &
                                lista.Count.ToString() & ".", "Enviar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                MessageBox.Show("Concluido! " & enviados.ToString() & " seriais enviados.", "Enviar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show("Erro durante o envio: " & ex.Message, "Enviar",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            enviando = False
            cancelarEnvio = False
            btnenviar.Text = "Enviar"
            btnImport.Enabled = True
            btnlimpar.Enabled = True
            txtbpicagem.Enabled = True
            Me.Text = tituloBase
            txtbpicagem.Focus()
        End Try
    End Sub

    ' ---------------------------------------------------------------
    ' Limpar
    ' ---------------------------------------------------------------

    Private Sub btnlimpar_Click(sender As Object, e As EventArgs) Handles btnlimpar.Click
        If enviando Then Return

        If SeriaisAtuais().Count > 0 Then
            If MessageBox.Show("Limpar a lista toda?", "Limpar",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                Return
            End If
        End If

        RichTextBox1.Clear()
        AtualizarContador(0)
        txtbpicagem.Focus()
    End Sub

    ' ---------------------------------------------------------------
    ' Scanner (pica SN + Enter)
    ' ---------------------------------------------------------------

    Private Sub txtbpicagem_KeyDown(sender As Object, e As KeyEventArgs) Handles txtbpicagem.KeyDown
        If e.KeyCode <> Keys.Enter Then Return
        e.SuppressKeyPress = True

        ' O scanner pode mandar espacos/tabs a mais, ou ate varios seriais de uma vez.
        Dim novos As List(Of String) = Normalizar(txtbpicagem.Text)
        txtbpicagem.Clear()
        If novos.Count = 0 Then Return

        Dim lista As List(Of String) = SeriaisAtuais()
        lista.AddRange(novos)
        MostrarSeriais(lista)

        ' Mantem a ultima linha visivel.
        RichTextBox1.SelectionStart = RichTextBox1.TextLength
        RichTextBox1.ScrollToCaret()
    End Sub

    ' ---------------------------------------------------------------
    ' Atualizacoes automaticas (releases publicas do GitHub)
    ' ---------------------------------------------------------------

    Private Sub MostrarVersao()
        lnkAtualizar.Text = "Versao " & Atualizador.VersaoAtual().ToString() & " - verificar atualizacoes"
    End Sub

    Private Async Sub lnkAtualizar_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) _
        Handles lnkAtualizar.LinkClicked

        If aVerificar OrElse enviando Then Return
        Await VerificarAtualizacoesAsync(silencioso:=False)
    End Sub

    Private aVerificar As Boolean

    Private Async Function VerificarAtualizacoesAsync(silencioso As Boolean) As Task
        If aVerificar Then Return
        aVerificar = True
        lnkAtualizar.Enabled = False
        If Not silencioso Then lnkAtualizar.Text = "A verificar atualizacoes..."

        Try
            Dim info As Atualizador.InfoRelease = Await Atualizador.ProcurarAsync()

            If info Is Nothing Then
                If Not silencioso Then
                    MessageBox.Show("Ja estas na versao mais recente (" &
                                    Atualizador.VersaoAtual().ToString() & ").",
                                    "Atualizacoes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
                Return
            End If

            Dim notas As String = info.Notas
            If notas.Length > 700 Then notas = notas.Substring(0, 700) & "..."

            Dim resp As DialogResult = MessageBox.Show(
                "Esta disponivel a versao " & info.Versao.ToString() &
                " (tens a " & Atualizador.VersaoAtual().ToString() & ")." & Environment.NewLine &
                "Download: " & Atualizador.FormatarTamanho(info.Tamanho) & Environment.NewLine &
                Environment.NewLine &
                notas & Environment.NewLine & Environment.NewLine &
                "Atualizar agora? A aplicacao reinicia no fim.",
                "Nova versao disponivel", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If resp <> DialogResult.Yes Then
                MostrarVersao()
                Return
            End If

            ' Nao vale a pena atualizar a meio de um envio.
            If enviando Then
                MessageBox.Show("Termina o envio em curso antes de atualizar.", "Atualizacoes",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim progresso As New Progress(Of Integer)(
                Sub(pct) lnkAtualizar.Text = "A descarregar... " & pct.ToString() & "%")

            Dim ficheiro As String = Await Atualizador.DescarregarAsync(info, progresso)

            lnkAtualizar.Text = "A instalar..."
            Atualizador.AplicarEReiniciar(ficheiro)

        Catch ex As Exception
            MostrarVersao()
            If Not silencioso Then
                MessageBox.Show("Nao foi possivel atualizar: " & ex.Message & Environment.NewLine &
                                Environment.NewLine &
                                "Podes descarregar manualmente em:" & Environment.NewLine &
                                Atualizador.UrlPaginaReleases,
                                "Atualizacoes", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Finally
            aVerificar = False
            lnkAtualizar.Enabled = True
            If lnkAtualizar.Text.StartsWith("A ") Then MostrarVersao()
        End Try
    End Function

End Class
