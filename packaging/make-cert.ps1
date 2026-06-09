# Generates a self-signed code-signing certificate matching the AppxManifest's
# Publisher (CN=Noty4Me Dev) and exports it to packaging\Noty4Me.pfx.
# Also exports the public cert (.cer) so end users can trust it before install.

param(
    [string]$Subject       = "CN=Noty4Me Dev",
    [string]$PfxPath       = "$PSScriptRoot\Noty4Me.pfx",
    [string]$CerPath       = "$PSScriptRoot\Noty4Me.cer",
    [SecureString]$Password,
    [int]$ValidYears       = 3
)

if (-not $Password) {
    Write-Host "No -Password provided. Enter a password to protect the .pfx (it will not echo):"
    $Password = Read-Host -AsSecureString
    if ($Password.Length -eq 0) { throw "Password is required." }
}

Write-Host "Creating self-signed cert with Subject: $Subject"
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "Noty4Me Dev Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

Write-Host "Thumbprint: $($cert.Thumbprint)"

Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $Password | Out-Null
Export-Certificate    -Cert $cert -FilePath $CerPath | Out-Null

Write-Host "Wrote $PfxPath"
Write-Host "Wrote $CerPath"
Write-Host ""
Write-Host "To trust the cert on this machine for MSIX install (admin shell):"
Write-Host "  Import-Certificate -FilePath '$CerPath' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
