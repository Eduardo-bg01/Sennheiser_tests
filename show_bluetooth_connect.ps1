Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form = New-Object System.Windows.Forms.Form
$form.Text = "Conectar Dispositivo Bluetooth"
$form.Size = New-Object System.Drawing.Size(520, 520)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::White

# Icono Bluetooth (encabezado)
$headerPanel = New-Object System.Windows.Forms.Panel
$headerPanel.Size = New-Object System.Drawing.Size(520, 70)
$headerPanel.Location = New-Object System.Drawing.Point(0, 0)
$headerPanel.BackColor = [System.Drawing.Color]::FromArgb(0, 103, 192)
$form.Controls.Add($headerPanel)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = "Conectar Dispositivo Bluetooth"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$titleLabel.Size = New-Object System.Drawing.Size(480, 40)
$titleLabel.Location = New-Object System.Drawing.Point(20, 15)
$headerPanel.Controls.Add($titleLabel)

# Subtitulo
$subLabel = New-Object System.Windows.Forms.Label
$subLabel.Text = "Sigue los pasos antes de continuar con las pruebas:"
$subLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$subLabel.ForeColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
$subLabel.Size = New-Object System.Drawing.Size(460, 24)
$subLabel.Location = New-Object System.Drawing.Point(30, 85)
$form.Controls.Add($subLabel)

# Pasos
$steps = @(
    "1.  Enciende los audifonos y activa el modo emparejamiento.",
    "2.  Haz clic en 'Abrir Bluetooth' para abrir Configuracion.",
    "3.  Haz clic en '+ Agregar dispositivo'.",
    "4.  Selecciona 'Bluetooth' en la ventana que aparece.",
    "5.  Elige tu dispositivo de la lista.",
    "6.  Espera a que muestre 'Conectado'.",
    "7.  Cierra Configuracion y haz clic en 'Listo' aqui."
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

# Separador
$sep = New-Object System.Windows.Forms.Label
$sep.BorderStyle = "Fixed3D"
$sep.Size = New-Object System.Drawing.Size(460, 2)
$sep.Location = New-Object System.Drawing.Point(30, 330)
$form.Controls.Add($sep)

# Boton Abrir Bluetooth
$btnBT = New-Object System.Windows.Forms.Button
$btnBT.Text = "Abrir Bluetooth"
$btnBT.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$btnBT.Size = New-Object System.Drawing.Size(160, 38)
$btnBT.Location = New-Object System.Drawing.Point(30, 348)
$btnBT.BackColor = [System.Drawing.Color]::FromArgb(0, 103, 192)
$btnBT.ForeColor = [System.Drawing.Color]::White
$btnBT.FlatStyle = "Flat"
$btnBT.Add_Click({ Start-Process "ms-settings:bluetooth" })
$form.Controls.Add($btnBT)

# Boton Listo
$btnOK = New-Object System.Windows.Forms.Button
$btnOK.Text = "Listo, Continuar"
$btnOK.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$btnOK.Size = New-Object System.Drawing.Size(160, 38)
$btnOK.Location = New-Object System.Drawing.Point(310, 348)
$btnOK.BackColor = [System.Drawing.Color]::FromArgb(0, 150, 80)
$btnOK.ForeColor = [System.Drawing.Color]::White
$btnOK.FlatStyle = "Flat"
$btnOK.DialogResult = [System.Windows.Forms.DialogResult]::OK
$form.AcceptButton = $btnOK
$form.Controls.Add($btnOK)

$form.ShowDialog() | Out-Null
