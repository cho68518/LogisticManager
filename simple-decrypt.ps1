param(
    [string]$EncryptedPassword
)

if ([string]::IsNullOrEmpty($EncryptedPassword)) {
    Write-Host "사용법: .\simple-decrypt.ps1 '암호화된_비밀번호'" -ForegroundColor Red
    exit 1
}

$Key = "MySecretKey123!"
Write-Host "=== 비밀번호 복호화 ===" -ForegroundColor Green
Write-Host "암호화된 비밀번호: $EncryptedPassword" -ForegroundColor Yellow

try {
    # 복호화
    $KeyBytes = [Text.Encoding]::UTF8.GetBytes($Key.PadRight(32, '0'))
    $EncryptedBytes = [Convert]::FromBase64String($EncryptedPassword)
    
    $AES = [Security.Cryptography.Aes]::Create()
    $AES.Key = $KeyBytes
    $AES.Mode = [Security.Cryptography.CipherMode]::ECB
    $AES.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    
    $Decryptor = $AES.CreateDecryptor()
    $DecryptedBytes = $Decryptor.TransformFinalBlock($EncryptedBytes, 0, $EncryptedBytes.Length)
    $DecryptedText = [Text.Encoding]::UTF8.GetString($DecryptedBytes)
    
    Write-Host "복호화된 비밀번호:" -ForegroundColor Cyan
    Write-Host $DecryptedText -ForegroundColor White
    
    $AES.Dispose()
    Write-Host "=== 완료 ===" -ForegroundColor Green
}
catch {
    Write-Host "복호화 실패: $($_.Exception.Message)" -ForegroundColor Red
}
