<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        RichTextBox1 = New RichTextBox()
        btnImport = New Button()
        btnenviar = New Button()
        btnlimpar = New Button()
        txtbpicagem = New TextBox()
        lblContador = New Label()
        Label2 = New Label()
        lnkAtualizar = New LinkLabel()
        SuspendLayout()
        ' 
        ' RichTextBox1
        ' 
        RichTextBox1.BorderStyle = BorderStyle.FixedSingle
        RichTextBox1.Location = New Point(12, 100)
        RichTextBox1.Name = "RichTextBox1"
        RichTextBox1.ReadOnly = True
        RichTextBox1.Size = New Size(369, 338)
        RichTextBox1.TabIndex = 0
        RichTextBox1.Text = ""
        ' 
        ' btnImport
        ' 
        btnImport.Location = New Point(12, 56)
        btnImport.Name = "btnImport"
        btnImport.Size = New Size(75, 38)
        btnImport.TabIndex = 1
        btnImport.Text = "Importar"
        btnImport.UseVisualStyleBackColor = True
        ' 
        ' btnenviar
        ' 
        btnenviar.Location = New Point(93, 56)
        btnenviar.Name = "btnenviar"
        btnenviar.Size = New Size(124, 38)
        btnenviar.TabIndex = 2
        btnenviar.Text = "Enviar"
        btnenviar.UseVisualStyleBackColor = True
        ' 
        ' btnlimpar
        ' 
        btnlimpar.Location = New Point(223, 56)
        btnlimpar.Name = "btnlimpar"
        btnlimpar.Size = New Size(158, 38)
        btnlimpar.TabIndex = 3
        btnlimpar.Text = "Limpar"
        btnlimpar.UseVisualStyleBackColor = True
        ' 
        ' txtbpicagem
        ' 
        txtbpicagem.Location = New Point(12, 27)
        txtbpicagem.Name = "txtbpicagem"
        txtbpicagem.Size = New Size(369, 23)
        txtbpicagem.TabIndex = 4
        ' 
        ' lblContador
        ' 
        lblContador.AutoSize = True
        lblContador.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblContador.Location = New Point(223, 9)
        lblContador.Name = "lblContador"
        lblContador.Size = New Size(119, 15)
        lblContador.TabIndex = 5
        lblContador.Text = "ITENS CARREGADOS:"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(12, 9)
        Label2.Name = "Label2"
        Label2.Size = New Size(122, 15)
        Label2.TabIndex = 6
        Label2.Text = "SCANNER - PICAGEM"
        '
        ' lnkAtualizar
        '
        lnkAtualizar.AutoSize = True
        lnkAtualizar.Location = New Point(12, 445)
        lnkAtualizar.Name = "lnkAtualizar"
        lnkAtualizar.Size = New Size(126, 15)
        lnkAtualizar.TabIndex = 7
        lnkAtualizar.TabStop = True
        lnkAtualizar.Text = "Verificar atualizações"
        '
        ' Form1
        '
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(393, 470)
        Controls.Add(lnkAtualizar)
        Controls.Add(Label2)
        Controls.Add(lblContador)
        Controls.Add(txtbpicagem)
        Controls.Add(btnlimpar)
        Controls.Add(btnenviar)
        Controls.Add(btnImport)
        Controls.Add(RichTextBox1)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MinimizeBox = False
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "MegaImportador - Números de Série"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents RichTextBox1 As RichTextBox
    Friend WithEvents btnImport As Button
    Friend WithEvents btnenviar As Button
    Friend WithEvents btnlimpar As Button
    Friend WithEvents txtbpicagem As TextBox
    Friend WithEvents lblContador As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents lnkAtualizar As LinkLabel

End Class
