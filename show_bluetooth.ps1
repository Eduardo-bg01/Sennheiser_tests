param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("connect","disconnect")]
    [string]$Mode
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if ($Mode -eq "connect") {
    $title = "Conectar Dispositivo Bluetooth"
    $subtitle = "Sigue los pasos antes de continuar con las pruebas:"
    $headerColor = [System.Drawing.Color]::FromArgb(0, 103, 192)
    $btnColor = [System.Drawing.Color]::FromArgb(0, 103, 192)
    $steps = @(
        "1.  Enciende los audifonos y activa el modo emparejamiento.",
        "2.  Haz clic en 'Abrir Bluetooth' para abrir Configuracion.",
        "3.  Haz clic en '+ Agregar dispositivo'.",
        "4.  Selecciona 'Bluetooth' en la ventana que aparece.",
        "5.  Elige tu dispositivo de la lista.",
        "6.  Espera a que muestre 'Conectado'.",
        "7.  Cierra Configuracion y haz clic en 'Listo' aqui."
    )
    $noteText = $null
} else {
    $title = "Desconectar Dispositivo Bluetooth"
    $subtitle = "Las pruebas han finalizado. Retira el dispositivo Bluetooth:"
    $headerColor = [System.Drawing.Color]::FromArgb(180, 50, 50)
    $btnColor = [System.Drawing.Color]::FromArgb(180, 50, 50)
    $steps = @(
        "1.  Haz clic en 'Abrir Bluetooth' para abrir Configuracion.",
        "2.  Busca el dispositivo en la lista de 'Dispositivos'.",
        "3.  Haz clic en el dispositivo y selecciona 'Quitar dispositivo'.",
        "4.  Confirma haciendo clic en 'Si'.",
        "5.  Cierra Configuracion y haz clic en 'Listo' aqui."
    )
    $noteText = "Nota: Al hacer clic en 'Listo' se limpiaran automaticamente los dispositivos restantes."
}

$form = New-Object System.Windows.Forms.Form
$form.Text = $title
$form.Size = New-Object System.Drawing.Size(520, 520)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::White

# Header
$headerPanel = New-Object System.Windows.Forms.Panel
$headerPanel.Size = New-Object System.Drawing.Size(520, 70)
$headerPanel.Location = New-Object System.Drawing.Point(0, 0)
$headerPanel.BackColor = $headerColor
$form.Controls.Add($headerPanel)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = $title
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$titleLabel.Size = New-Object System.Drawing.Size(480, 40)
$titleLabel.Location = New-Object System.Drawing.Point(20, 15)
$headerPanel.Controls.Add($titleLabel)

# Subtitle
$subLabel = New-Object System.Windows.Forms.Label
$subLabel.Text = $subtitle
$subLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$subLabel.ForeColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
$subLabel.Size = New-Object System.Drawing.Size(460, 24)
$subLabel.Location = New-Object System.Drawing.Point(30, 85)
$form.Controls.Add($subLabel)

# Steps
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

# Note (disconnect only)
$noteSepY = 330
if ($noteText) {
    $noteLabel = New-Object System.Windows.Forms.Label
    $noteLabel.Text = $noteText
    $noteLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Italic)
    $noteLabel.ForeColor = [System.Drawing.Color]::FromArgb(120, 120, 120)
    $noteLabel.Size = New-Object System.Drawing.Size(460, 22)
    $noteLabel.Location = New-Object System.Drawing.Point(30, 290)
    $form.Controls.Add($noteLabel)
    $noteSepY = 318
}

# Separator
$sep = New-Object System.Windows.Forms.Label
$sep.BorderStyle = "Fixed3D"
$sep.Size = New-Object System.Drawing.Size(460, 2)
$sep.Location = New-Object System.Drawing.Point(30, $noteSepY)
$form.Controls.Add($sep)

# Open Bluetooth button
$btnY = $noteSepY + 18
$btnBT = New-Object System.Windows.Forms.Button
$btnBT.Text = "Abrir Bluetooth"
$btnBT.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$btnBT.Size = New-Object System.Drawing.Size(160, 38)
$btnBT.Location = New-Object System.Drawing.Point(30, $btnY)
$btnBT.BackColor = $btnColor
$btnBT.ForeColor = [System.Drawing.Color]::White
$btnBT.FlatStyle = "Flat"
$btnBT.Add_Click({ Start-Process "ms-settings:bluetooth" })
$form.Controls.Add($btnBT)

# Done button
$btnOK = New-Object System.Windows.Forms.Button
$btnOK.Text = "Listo, Continuar"
$btnOK.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$btnOK.Size = New-Object System.Drawing.Size(160, 38)
$btnOK.Location = New-Object System.Drawing.Point(310, $btnY)
$btnOK.BackColor = [System.Drawing.Color]::FromArgb(0, 150, 80)
$btnOK.ForeColor = [System.Drawing.Color]::White
$btnOK.FlatStyle = "Flat"
$btnOK.DialogResult = [System.Windows.Forms.DialogResult]::OK
$form.AcceptButton = $btnOK
$form.Controls.Add($btnOK)

$form.ShowDialog() | Out-Null
