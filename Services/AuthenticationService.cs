using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using LogisticManager.Models;
using LogisticManager.Services;
using System.Collections.Generic;

namespace LogisticManager.Services
{
    /// <summary>
    /// 사용자 인증 서비스
    /// </summary>
    public class AuthenticationService
    {
        private readonly DatabaseService _databaseService;
        private User? _currentUser;

        /// <summary>
        /// 현재 로그인된 사용자
        /// </summary>
        public User? CurrentUser => _currentUser;

        /// <summary>
        /// 로그인 상태
        /// </summary>
        public bool IsLoggedIn => _currentUser?.IsAuthenticated == true;

        public AuthenticationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// 현재 로그인된 사용자의 비밀번호를 변경
        /// </summary>
        /// <param name="currentPassword">현재 비밀번호</param>
        /// <param name="newPassword">새 비밀번호</param>
        /// <returns>변경 성공 여부</returns>
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                // 현재 사용자가 로그인되어 있는지 확인
                if (_currentUser == null || !_currentUser.IsAuthenticated)
                {
                    Console.WriteLine("⚠️ 비밀번호 변경 불가: 로그인 상태가 아님");
                    return false;
                }

                // 입력값 검증
                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    Console.WriteLine("⚠️ 비밀번호 변경 불가: 입력값이 유효하지 않음");
                    return false;
                }

                // 현재 비밀번호 검증
                var isCurrentValid = await VerifyPasswordAsync(currentPassword, _currentUser.Username);
                if (!isCurrentValid)
                {
                    Console.WriteLine("❌ 비밀번호 변경 실패: 현재 비밀번호 불일치");
                    return false;
                }

                // 새 비밀번호 암호화
                var encryptedNewPassword = SecurityService.EncryptString(newPassword);
                if (string.IsNullOrEmpty(encryptedNewPassword))
                {
                    Console.WriteLine("❌ 비밀번호 변경 실패: 새 비밀번호 암호화 오류");
                    return false;
                }

                // 데이터베이스 업데이트
                var updateQuery = "UPDATE Users SET password = @password WHERE id = @userId";
                var parameters = new Dictionary<string, object>
                {
                    ["@password"] = encryptedNewPassword,
                    ["@userId"] = _currentUser.Id
                };

                await _databaseService.ExecuteNonQueryAsync(updateQuery, parameters);

                Console.WriteLine("✅ 비밀번호 변경 성공");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 비밀번호 변경 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사용자 로그인 처리
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <param name="password">비밀번호</param>
        /// <returns>로그인 성공 여부</returns>
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return false;
                }

                // 데이터베이스에서 사용자 정보 조회
                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return false;
                }

                // 비밀번호 검증
                if (!await VerifyPasswordAsync(password, user.Username))
                {
                    return false;
                }

                // 로그인 성공 처리
                user.IsAuthenticated = true;
                user.LastLogin = DateTime.Now;
                _currentUser = user;

                // 마지막 로그인 시간 업데이트
                await UpdateLastLoginTimeAsync(user.Id);

                return true;
            }
            catch (Exception ex)
            {
                // 로그 기록 (실제 구현 시 LoggingService 사용)
                Console.WriteLine($"로그인 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 로그아웃 처리
        /// </summary>
        public void Logout()
        {
            if (_currentUser != null)
            {
                _currentUser.IsAuthenticated = false;
                _currentUser = null;
            }
        }

        /// <summary>
        /// 사용자명으로 사용자 정보 조회
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <returns>사용자 정보</returns>
        private async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                // Users 테이블에서 사용자 정보 조회 (name 컬럼 포함)
                var query = "SELECT id, username, name, email, created_at, last_login FROM Users WHERE username = @username";
                var parameters = new Dictionary<string, object>
                {
                    ["@username"] = username
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                
                if (result != null && result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return new User
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Username = row["username"].ToString() ?? string.Empty,
                        Name = row["name"]?.ToString() ?? string.Empty,
                        Email = row["email"]?.ToString(),
                        CreatedAt = Convert.ToDateTime(row["created_at"]),
                        LastLogin = row["last_login"] != DBNull.Value ? Convert.ToDateTime(row["last_login"]) : null
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"사용자 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 비밀번호 검증 (암호화된 비밀번호를 복호화하여 검증)
        /// </summary>
        /// <param name="inputPassword">입력된 비밀번호</param>
        /// <param name="username">사용자명 (비밀번호 생성용)</param>
        /// <returns>비밀번호 일치 여부</returns>
        private async Task<bool> VerifyPasswordAsync(string inputPassword, string username)
        {
            try
            {
                // 데이터베이스에서 암호화된 비밀번호 조회
                var query = "SELECT password FROM Users WHERE username = @username";
                var parameters = new Dictionary<string, object>
                {
                    ["@username"] = username
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                
                if (result != null && result.Rows.Count > 0)
                {
                    var encryptedPassword = result.Rows[0]["password"].ToString();
                    
                    if (string.IsNullOrEmpty(encryptedPassword))
                    {
                        Console.WriteLine($"⚠️ 사용자 '{username}'의 비밀번호가 데이터베이스에 저장되지 않음");
                        return false;
                    }
                    
                    try
                    {
                        // 암호화된 비밀번호를 복호화
                        // 현재 포맷 우선, 실패 시 호환 복호화 시도
                        var decryptedPassword = SecurityService.DecryptString(encryptedPassword);
                        if (string.IsNullOrEmpty(decryptedPassword))
                        {
                            decryptedPassword = SecurityService.DecryptStringCompat(encryptedPassword);
                        }
                        
                        if (string.IsNullOrEmpty(decryptedPassword))
                        {
                            Console.WriteLine($"⚠️ 사용자 '{username}'의 비밀번호 복호화 실패");
                            return false;
                        }
                        
                        // 복호화된 비밀번호와 입력된 비밀번호 비교
                        var isPasswordValid = decryptedPassword.Equals(inputPassword, StringComparison.Ordinal);
                        
                        if (isPasswordValid)
                        {
                            Console.WriteLine($"✅ 사용자 '{username}' 비밀번호 검증 성공");
                        }
                        else
                        {
                            Console.WriteLine($"❌ 사용자 '{username}' 비밀번호 불일치");
                        }
                        
                        return isPasswordValid;
                    }
                    catch (Exception decryptEx)
                    {
                        Console.WriteLine($"❌ 사용자 '{username}' 비밀번호 복호화 중 오류: {decryptEx.Message}");
                        return false;
                    }
                }

                Console.WriteLine($"⚠️ 사용자 '{username}'를 찾을 수 없음");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 비밀번호 검증 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 마지막 로그인 시간 업데이트
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        private async Task UpdateLastLoginTimeAsync(int userId)
        {
            try
            {
                var query = "UPDATE Users SET last_login = @lastLogin WHERE id = @userId";
                var parameters = new Dictionary<string, object>
                {
                    ["@lastLogin"] = DateTime.Now,
                    ["@userId"] = userId
                };

                await _databaseService.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 시간 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 비밀번호 해시 생성 (향후 보안 강화용)
        /// </summary>
        /// <param name="password">원본 비밀번호</param>
        /// <returns>해시된 비밀번호</returns>
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// 비밀번호를 암호화하여 데이터베이스에 저장할 수 있는 형태로 변환
        /// </summary>
        /// <param name="plainPassword">원본 비밀번호</param>
        /// <returns>암호화된 비밀번호</returns>
        public static string EncryptPasswordForStorage(string plainPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(plainPassword))
                {
                    throw new ArgumentException("비밀번호는 null이거나 빈 문자열일 수 없습니다.");
                }

                // SecurityService를 사용하여 비밀번호 암호화
                var encryptedPassword = SecurityService.EncryptString(plainPassword);
                
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    throw new InvalidOperationException("비밀번호 암호화에 실패했습니다.");
                }

                Console.WriteLine($"✅ 비밀번호 암호화 완료");
                return encryptedPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 비밀번호 암호화 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 암호화된 비밀번호를 복호화하여 원본 비밀번호 확인
        /// </summary>
        /// <param name="encryptedPassword">암호화된 비밀번호</param>
        /// <returns>복호화된 원본 비밀번호</returns>
        public static string DecryptPasswordFromStorage(string encryptedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedPassword))
                {
                    throw new ArgumentException("암호화된 비밀번호는 null이거나 빈 문자열일 수 없습니다.");
                }

                // SecurityService를 사용하여 비밀번호 복호화
                var decryptedPassword = SecurityService.DecryptString(encryptedPassword);
                
                if (string.IsNullOrEmpty(decryptedPassword))
                {
                    throw new InvalidOperationException("비밀번호 복호화에 실패했습니다.");
                }

                Console.WriteLine($"✅ 비밀번호 복호화 완료");
                return decryptedPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 비밀번호 복호화 실패: {ex.Message}");
                throw;
            }
        }
    }
}
