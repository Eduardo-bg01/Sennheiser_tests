Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form = New-Object System.Windows.Forms.Form
$form.Text = "Desconectar Dispositivo Bluetooth"
$form.Size = New-Object System.Drawing.Size(520, 520)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::White

# Encabezado
$headerPanel = New-Object System.Windows.Forms.Panel
$headerPanel.Size = New-Object System.Drawing.Size(520, 70)
$headerPanel.Location = New-Object System.Drawing.Point(0, 0)
$headerPanel.BackColor = [System.Drawing.Color]::FromArgb(180, 50, 50)
$form.Controls.Add($headerPanel)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = "Desconectar Dispositivo Bluetooth"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$titleLabel.Size = New-Object System.Drawing.Size(480, 40)
$titleLabel.Location = New-Object System.Drawing.Point(20, 15)
$headerPanel.Controls.Add($titleLabel)

# Subtitulo
$subLabel = New-Object System.Windows.Forms.Label
$subLabel.Text = "Las pruebas han finalizado. Retira el dispositivo Bluetooth:"
$subLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$subLabel.ForeColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
$subLabel.Size = New-Object System.Drawing.Size(460, 24)
$subLabel.Location = New-Object System.Drawing.Point(30, 85)
$form.Controls.Add($subLabel)

# Pasos
$steps = @(
    "1.  Haz clic en 'Abrir Bluetooth' para abrir Configuracion.",
    "2.  Busca el dispositivo en la lista de 'Dispositivos'.",
    "3.  Haz clic en el dispositivo y selecciona 'Quitar dispositivo'.",
    "4.  Confirma haciendo clic en 'Si'.",
    "5.  Cierra Configuracion y haz clic en 'Listo' aqui."
)

$yPos = 120
foreach ($step in $steps) {
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Text = $step
    $lbl.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $lbl.ForeColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
    $lbl.Size = New-Object System.Drawing.Size(460, 26)
    $lbl.Location = New-Object System.Drawing.Point(30, $yPos)
    $form.Controls.Add($lbl)
    $yPos += 28
}

# Nota informativa
$noteLabel = New-Object System.Windows.Forms.Label
$noteLabel.Text = "Nota: Al hacer clic en 'Listo' se limpiaran automaticamente los dispositivos restantes."
$noteLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Italic)
$noteLabel.ForeColor = [System.Drawing.Color]::FromArgb(120, 120, 120)
$noteLabel.Size = New-Object System.Drawing.Size(460, 22)
$noteLabel.Location = New-Object System.Drawing.Point(30, 290)
$form.Controls.Add($noteLabel)

# Separador
$sep = New-Object System.Windows.Forms.Label
$sep.BorderStyle = "Fixed3D"
$sep.Size = New-Object System.Drawing.Size(460, 2)
$sep.Location = New-Object System.Drawing.Point(30, 318)
$form.Controls.Add($sep)

# Boton Abrir Bluetooth
$btnBT = New-Object System.Windows.Forms.Button
$btnBT.Text = "Abrir Bluetooth"
$btnBT.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$btnBT.Size = New-Object System.Drawing.Size(160, 38)
$btnBT.Location = New-Object System.Drawing.Point(30, 336)
$btnBT.BackColor = [System.Drawing.Color]::FromArgb(180, 50, 50)
$btnBT.ForeColor = [System.Drawing.Color]::White
$btnBT.FlatStyle = "Flat"
$btnBT.Add_Click({ Start-Process "ms-settings:bluetooth" })
$form.Controls.Add($btnBT)

# Boton Listo
$btnOK = New-Object System.Windows.Forms.Button
$btnOK.Text = "Listo, Continuar"
$btnOK.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$btnOK.Size = New-Object System.Drawing.Size(160, 38)
$btnOK.Location = New-Object System.Drawing.Point(310, 336)
$btnOK.BackColor = [System.Drawing.Color]::FromArgb(0, 150, 80)
$btnOK.ForeColor = [System.Drawing.Color]::White
$btnOK.FlatStyle = "Flat"
$btnOK.DialogResult = [System.Windows.Forms.DialogResult]::OK
$form.AcceptButton = $btnOK
$form.Controls.Add($btnOK)

$form.ShowDialog() | Out-Null
