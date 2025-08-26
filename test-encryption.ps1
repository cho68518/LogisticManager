# 암호화/복호화 테스트 스크립트
$Key = "MySecretKey123!"
$Password = "test123"

Write-Host "=== 암호화 테스트 ===" -ForegroundColor Green
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

Write-Host "암호화된 비밀번호: $EncryptedText" -ForegroundColor Cyan

# 복호화
$Decryptor = $AES.CreateDecryptor()
$DecryptedBytes = $Decryptor.TransformFinalBlock($EncryptedBytes, 0, $EncryptedBytes.Length)
$DecryptedText = [Text.Encoding]::UTF8.GetString($DecryptedBytes)

Write-Host "복호화된 비밀번호: $DecryptedText" -ForegroundColor Cyan
Write-Host "일치 여부: $($Password -eq $DecryptedText)" -ForegroundColor Green

$AES.Dispose()
Write-Host "=== 테스트 완료 ===" -ForegroundColor Green
