param(
    [string]$Password
)

if ([string]::IsNullOrEmpty($Password)) {
    Write-Host "사용법: .\simple-encrypt.ps1 '비밀번호'" -ForegroundColor Red
    exit 1
}

$Key = "MySecretKey123!"
Write-Host "=== 비밀번호 암호화 ===" -ForegroundColor Green
Write-Host "원본 비밀번호: $Password" -ForegroundColor Yellow

# 암호화
$KeyBytes = [Text.Encoding]::UTF8.GetBytes($Key.PadRight(32, '0'))
$PlainBytes = [Text.Encoding]::UTF8.GetBytes($Password)

$AES = [Security.Cryptography.Aes]::Create()
$AES.Key = $KeyBytes
$AES.Mode = [Security.Cryptography.CipherMode]::ECB
$AES.Padding = [Security.Cryptography.PaddingMode]::PKCS7

$Encryptor = $AES.CreateEncryptor()
$EncryptedBytes = $Encryptor.TransformFinalBlock($PlainBytes, 0, $PlainBytes.Length)
$EncryptedText = [Convert]::ToBase64String($EncryptedBytes)

Write-Host "암호화된 비밀번호:" -ForegroundColor Cyan
Write-Host $EncryptedText -ForegroundColor White

$AES.Dispose()
Write-Host "=== 완료 ===" -ForegroundColor Green
