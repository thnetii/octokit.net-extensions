[System.Net.ServicePointManager]::SecurityProtocol = `
    [System.Net.ServicePointManager]::SecurityProtocol -bor `
    [System.Net.SecurityProtocolType]::Tls12

$smtpConfigPath = Join-Path $PSScriptRoot "smtp-secrets.json"
$smtpSettings = Get-Content -Path $smtpConfigPath -Encoding utf8 `
| ConvertFrom-Json
$smtpUsername = $smtpSettings.username
$smtpPassword = ConvertTo-SecureString $smtpSettings.password -AsPlainText -Force
$smtpCredentials = New-Object pscredential @($smtpUsername, $smtpPassword)

$envName = [System.IO.Path]::GetFileName($PSScriptRoot)
$envDir = Join-Path $PSScriptRoot "../../bld/$envName"
if (-not $(Test-Path $envDir -PathType Container)) {
    New-Item -Path $envDir -ItemType Directory -Verbose | Out-Null
}
$envFile = Join-Path $envDir "env.json"
$envDict = [System.Environment]::GetEnvironmentVariables()
$envDict | ConvertTo-Json | Out-File -Force -Verbose -Encoding utf8 -FilePath $envFile

Send-MailMessage -Verbose -Credential $smtpCredentials `
-Body "Contents of Environment Dictionary in attachment." -Encoding utf8 `
-Subject "Environment: $envName" -Attachments $envFile `
-To $smtpSettings.receiver -From $smtpSettings.sender `
-SmtpServer $smtpSettings.smtpserver -Port $smtpSettings.smtpport `
-UseSsl $smtpSettings.usessl
