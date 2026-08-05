Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net.Http
Imports System.Reflection
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>
''' Verifica e aplica atualizacoes a partir das releases publicas do GitHub.
''' Nao precisa de token nem de conta GitHub: a repo de releases e publica e
''' o download e anonimo.
''' </summary>
Public NotInheritable Class Atualizador

    ' Repo publica que aloja o codigo e as releases. Tem de ser publica para o
    ' download funcionar sem conta GitHub nem token embutido no executavel.
    Public Const RepoReleases As String = "RicardoTentugal99/MegaImporter"

    Private Const UrlUltimaRelease As String =
        "https://api.github.com/repos/" & RepoReleases & "/releases/latest"

    Public Const UrlPaginaReleases As String =
        "https://github.com/" & RepoReleases & "/releases/latest"

    Private Const SufixoAntigo As String = ".old"

    Private Shared ReadOnly cliente As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(10)}

    Shared Sub New()
        ' O GitHub rejeita pedidos sem User-Agent com 403.
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd("MegaImporter-Updater")
        cliente.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
    End Sub

    Private Sub New()
        ' Classe apenas com membros partilhados.
    End Sub

    ''' <summary>Dados da release mais recente publicada no GitHub.</summary>
    Public NotInheritable Class InfoRelease
        Public Property Versao As Version
        Public Property Tag As String
        Public Property Notas As String
        Public Property UrlDownload As String
        Public Property NomeFicheiro As String
        Public Property Tamanho As Long
    End Class

    ''' <summary>Versao do executavel em execucao, normalizada a 3 partes.</summary>
    Public Shared Function VersaoAtual() As Version
        Dim v As Version = Assembly.GetExecutingAssembly().GetName().Version
        If v Is Nothing Then Return New Version(0, 0, 0)
        Return Normalizar(v)
    End Function

    Private Shared Function Normalizar(v As Version) As Version
        Return New Version(v.Major, v.Minor, Math.Max(v.Build, 0))
    End Function

    ''' <summary>
    ''' Consulta a ultima release. Devolve Nothing se nao houver versao mais recente
    ''' ou se a consulta falhar (sem rede, GitHub em baixo, etc.) — nunca lanca excecao.
    ''' </summary>
    Public Shared Async Function ProcurarAsync(Optional versaoInstalada As Version = Nothing) As Task(Of InfoRelease)
        If versaoInstalada Is Nothing Then versaoInstalada = VersaoAtual()

        Try
            Dim json As String = Await cliente.GetStringAsync(UrlUltimaRelease).ConfigureAwait(False)

            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim raiz As JsonElement = doc.RootElement

                Dim tag As String = LerTexto(raiz, "tag_name")
                If String.IsNullOrWhiteSpace(tag) Then Return Nothing

                Dim versaoRemota As Version = Nothing
                If Not Version.TryParse(tag.TrimStart("v"c, "V"c), versaoRemota) Then Return Nothing
                versaoRemota = Normalizar(versaoRemota)

                If versaoRemota <= Normalizar(versaoInstalada) Then Return Nothing

                ' Procura o primeiro asset .exe da release.
                Dim assets As JsonElement
                If Not raiz.TryGetProperty("assets", assets) Then Return Nothing

                For Each asset As JsonElement In assets.EnumerateArray()
                    Dim nome As String = LerTexto(asset, "name")
                    If nome Is Nothing OrElse Not nome.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then Continue For

                    Dim url As String = LerTexto(asset, "browser_download_url")
                    If String.IsNullOrWhiteSpace(url) Then Continue For

                    Dim tamanho As Long = 0
                    Dim elemTamanho As JsonElement
                    If asset.TryGetProperty("size", elemTamanho) Then elemTamanho.TryGetInt64(tamanho)

                    Return New InfoRelease With {
                        .Versao = versaoRemota,
                        .Tag = tag,
                        .Notas = If(LerTexto(raiz, "body"), ""),
                        .UrlDownload = url,
                        .NomeFicheiro = nome,
                        .Tamanho = tamanho
                    }
                Next
            End Using

        Catch ex As Exception
            ' Sem rede, timeout, JSON invalido: a app continua a funcionar normalmente.
            System.Diagnostics.Debug.WriteLine("Falha ao procurar atualizacoes: " & ex.Message)
        End Try

        Return Nothing
    End Function

    Private Shared Function LerTexto(elem As JsonElement, nome As String) As String
        Dim valor As JsonElement
        If elem.TryGetProperty(nome, valor) AndAlso valor.ValueKind = JsonValueKind.String Then
            Return valor.GetString()
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Descarrega o novo executavel para a pasta temporaria e devolve o caminho.
    ''' <paramref name="progresso"/> recebe a percentagem (0-100).
    ''' </summary>
    Public Shared Async Function DescarregarAsync(info As InfoRelease,
                                                  Optional progresso As IProgress(Of Integer) = Nothing) As Task(Of String)
        Dim pasta As String = Path.Combine(Path.GetTempPath(), "MegaImporter_update")
        Directory.CreateDirectory(pasta)

        Dim destino As String = Path.Combine(pasta, info.NomeFicheiro)
        If File.Exists(destino) Then File.Delete(destino)

        Using resposta As HttpResponseMessage =
            Await cliente.GetAsync(info.UrlDownload, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(False)

            resposta.EnsureSuccessStatusCode()

            Dim total As Long = If(resposta.Content.Headers.ContentLength.HasValue,
                                   resposta.Content.Headers.ContentLength.Value,
                                   info.Tamanho)

            Using origem As Stream = Await resposta.Content.ReadAsStreamAsync().ConfigureAwait(False),
                  ficheiro As New FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 81920, True)

                Dim buffer(81919) As Byte
                Dim acumulado As Long = 0
                Dim ultimaPercentagem As Integer = -1

                Do
                    Dim lidos As Integer = Await origem.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(False)
                    If lidos = 0 Then Exit Do

                    Await ficheiro.WriteAsync(buffer, 0, lidos).ConfigureAwait(False)
                    acumulado += lidos

                    If progresso IsNot Nothing AndAlso total > 0 Then
                        Dim pct As Integer = CInt(acumulado * 100L \ total)
                        If pct <> ultimaPercentagem Then
                            ultimaPercentagem = pct
                            progresso.Report(pct)
                        End If
                    End If
                Loop
            End Using
        End Using

        ' Um ficheiro vazio ou minusculo significa download truncado.
        Dim tamanhoFinal As Long = New FileInfo(destino).Length
        If tamanhoFinal < 1024 Then
            File.Delete(destino)
            Throw New IOException("O ficheiro descarregado esta incompleto.")
        End If

        Return destino
    End Function

    ''' <summary>
    ''' Substitui o executavel em execucao pelo novo e reinicia a aplicacao.
    ''' Nao e possivel apagar um .exe em execucao, mas e possivel renomea-lo:
    ''' o antigo passa a .exe.old e e apagado no arranque seguinte.
    ''' </summary>
    Public Shared Sub AplicarEReiniciar(ficheiroNovo As String)
        Dim exeAtual As String = Environment.ProcessPath
        If String.IsNullOrEmpty(exeAtual) Then
            Throw New InvalidOperationException("Nao foi possivel determinar o caminho do executavel.")
        End If

        Dim antigo As String = exeAtual & SufixoAntigo
        If File.Exists(antigo) Then File.Delete(antigo)

        File.Move(exeAtual, antigo)
        Try
            File.Copy(ficheiroNovo, exeAtual)
        Catch
            ' Repoe o executavel original se a copia falhar a meio.
            If Not File.Exists(exeAtual) Then File.Move(antigo, exeAtual)
            Throw
        End Try

        Process.Start(New ProcessStartInfo(exeAtual) With {.UseShellExecute = True})
        Environment.Exit(0)
    End Sub

    ''' <summary>Apaga o executavel da versao anterior. Chamar no arranque.</summary>
    Public Shared Sub LimparResiduos()
        Try
            Dim exeAtual As String = Environment.ProcessPath
            If String.IsNullOrEmpty(exeAtual) Then Return

            Dim antigo As String = exeAtual & SufixoAntigo
            If File.Exists(antigo) Then File.Delete(antigo)

            Dim pasta As String = Path.Combine(Path.GetTempPath(), "MegaImporter_update")
            If Directory.Exists(pasta) Then Directory.Delete(pasta, True)
        Catch
            ' Residuos nao sao criticos: se falhar, tenta-se no arranque seguinte.
        End Try
    End Sub

    ''' <summary>Formata bytes para leitura humana (ex.: "68,2 MB").</summary>
    Public Shared Function FormatarTamanho(bytes As Long) As String
        If bytes >= 1048576L Then Return (bytes / 1048576.0).ToString("0.0") & " MB"
        If bytes >= 1024L Then Return (bytes / 1024.0).ToString("0") & " KB"
        Return bytes.ToString() & " B"
    End Function

End Class
