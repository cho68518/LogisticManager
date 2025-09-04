# Password Encryption/Decryption Test Script
# Test SecurityService functionality in LogisticManager project

Write-Host "Password Encryption/Decryption Test Start" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# Test passwords
$testPasswords = @(
    "password123",
    "admin123",
    "user123",
    "TestPassword2024"
)

Write-Host "Test Passwords:" -ForegroundColor Yellow
foreach ($password in $testPasswords) {
    Write-Host "   - $password" -ForegroundColor White
}

Write-Host "`nEncryption Test:" -ForegroundColor Cyan

# AES encryption test (same method as SecurityService)
$encryptionKey = "MySecretKey123!"
$keyBytes = [System.Text.Encoding]::UTF8.GetBytes($encryptionKey.PadRight(32, '0'))

foreach ($password in $testPasswords) {
    try {
        # AES encryption
        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.Key = $keyBytes
        $aes.Mode = [System.Security.Cryptography.CipherMode]::ECB
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
        
        $encryptor = $aes.CreateEncryptor()
        $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($password)
        $encryptedBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
        $encryptedBase64 = [Convert]::ToBase64String($encryptedBytes)
        
        Write-Host "SUCCESS: '$password' -> '$encryptedBase64'" -ForegroundColor Green
        
        # Decryption test
        $decryptor = $aes.CreateDecryptor()
        $decryptedBytes = $decryptor.TransformFinalBlock($encryptedBytes, 0, $encryptedBytes.Length)
        $decryptedPassword = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)
        
        if ($decryptedPassword -eq $password) {
            Write-Host "   Decryption SUCCESS: '$decryptedPassword'" -ForegroundColor Green
        } else {
            Write-Host "   Decryption FAILED: '$decryptedPassword' != '$password'" -ForegroundColor Red
        }
        
        # Cleanup AES objects
        $aes.Dispose()
        $encryptor.Dispose()
        $decryptor.Dispose()
        
    } catch {
        Write-Host "   Encryption/Decryption ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nDatabase SQL Generation:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Green

foreach ($password in $testPasswords) {
    try {
        # Encryption
        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.Key = $keyBytes
        $aes.Mode = [System.Security.Cryptography.CipherMode]::ECB
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
        
        $encryptor = $aes.CreateEncryptor()
        $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($password)
        $encryptedBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
        $encryptedBase64 = [Convert]::ToBase64String($encryptedBytes)
        
        # Generate SQL statement
        $sql = "UPDATE Users SET password = '$encryptedBase64' WHERE username = 'test_user';"
        Write-Host "Password '$password' encrypted SQL:" -ForegroundColor Yellow
        Write-Host "   $sql" -ForegroundColor White
        
        $aes.Dispose()
        $encryptor.Dispose()
        
    } catch {
        Write-Host "   SQL generation ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nReal Usage Example:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Green

# Real usage scenario
$samplePassword = "admin123"
Write-Host "Sample Password: $samplePassword" -ForegroundColor White

try {
    # 1. Encryption
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $keyBytes
    $aes.Mode = [System.Security.Cryptography.CipherMode]::ECB
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    
    $encryptor = $aes.CreateEncryptor()
    $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($samplePassword)
    $encryptedBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
    $encryptedBase64 = [Convert]::ToBase64String($encryptedBytes)
    
    Write-Host "Step 1: Encryption Complete" -ForegroundColor Green
    Write-Host "   Original: $samplePassword" -ForegroundColor White
    Write-Host "   Encrypted: $encryptedBase64" -ForegroundColor White
    
    # 2. Database storage (simulation)
    Write-Host "Step 2: Stored in Database" -ForegroundColor Green
    
    # 3. Decryption for login verification
    $decryptor = $aes.CreateDecryptor()
    $decryptedBytes = $decryptor.TransformFinalBlock($encryptedBytes, 0, $encryptedBytes.Length)
    $decryptedPassword = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)
    
    Write-Host "Step 3: Decryption for Login" -ForegroundColor Green
    Write-Host "   Stored Encrypted: $encryptedBase64" -ForegroundColor White
    Write-Host "   Decrypted Result: $decryptedPassword" -ForegroundColor White
    
    # 4. Password verification
    if ($decryptedPassword -eq $samplePassword) {
        Write-Host "Step 4: Password Verification SUCCESS!" -ForegroundColor Green
        Write-Host "   Login Allowed" -ForegroundColor White
    } else {
        Write-Host "Step 4: Password Verification FAILED!" -ForegroundColor Red
        Write-Host "   Login Denied" -ForegroundColor White
    }
    
    # Cleanup resources
    $aes.Dispose()
    $encryptor.Dispose()
    $decryptor.Dispose()
    
} catch {
    Write-Host "Test execution ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "Now you can login to LogisticManager with encrypted passwords." -ForegroundColor Yellow
