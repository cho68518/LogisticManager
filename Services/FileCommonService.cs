using System;
using System.IO;
using System.Configuration;
using System.Threading.Tasks; // Added for Task

namespace LogisticManager.Services
{
    /// <summary>
    /// 공통 파일 처리 기능을 제공하는 서비스 클래스
    /// 
    /// 📋 주요 기능:
    /// - Excel 파일명 생성
    /// - Dropbox 파일 업로드
    /// - Dropbox 공유 링크 생성
    /// - 파일 상태 확인
    /// 
    /// 💡 사용법:
    /// var fileService = new FileCommonService();
    /// var fileName = fileService.GenerateExcelFileName("접두사", "설명");
    /// </summary>
    public class FileCommonService
    {
        /// <summary>
        /// Excel 파일명을 생성하는 공통 메서드
        /// 
        /// 📋 주요 기능:
        /// - 접두사와 설명을 기반으로 파일명 생성
        /// - 현재 시간을 포함한 고유한 파일명 생성
        /// - 일관된 형식으로 파일명 표준화
        /// 
        /// 💡 사용법:
        /// var fileName = GenerateExcelFileName("판매입력", "이카운트자료");
        /// var fileName = GenerateExcelFileName("서울냉동최종", "이카운트자료");
        /// 
        /// 📁 파일명 형식:
        /// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
        /// 예: 판매입력_이카운트자료_240731_14시30분.xlsx
        /// </summary>
        /// <param name="prefix">파일명 접두사 (예: "판매입력", "서울냉동최종")</param>
        /// <param name="description">파일 설명 (예: "이카운트자료")</param>
        /// <returns>생성된 Excel 파일명</returns>
        public string GenerateExcelFileName(string prefix, string description)
        {
            var now = DateTime.Now;
            var fileName = $"{prefix}_{description}_{now:yyMMdd}_{now:HH}시{now:mm}분.xlsx";
            return fileName;
        }

        /// <summary>
        /// Dropbox에 파일을 업로드하는 메서드
        /// </summary>
        /// <param name="localFilePath">로컬 파일 경로</param>
        /// <param name="dropboxFolderPath">Dropbox 폴더 경로</param>
        /// <returns>업로드된 파일의 Dropbox 경로</returns>
        public async Task<string?> UploadFileToDropbox(string localFilePath, string dropboxFolderPath)
        {
            try
            {
                var dropboxService = DropboxService.Instance;
                var uploadResult = await dropboxService.UploadFileAsync(localFilePath, dropboxFolderPath);
                return uploadResult; // UploadFileAsync에서 반환된 실제 파일 경로 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Dropbox 업로드 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Dropbox 공유 링크를 생성하는 메서드
        /// </summary>
        /// <param name="dropboxFilePath">Dropbox 파일 경로</param>
        /// <returns>공유 링크</returns>
        public async Task<string?> CreateDropboxSharedLink(string dropboxFilePath)
        {
            try
            {
                Console.WriteLine($"🔗 [{nameof(CreateDropboxSharedLink)}] 공유 링크 생성 시작: {dropboxFilePath}");
                
                var dropboxService = DropboxService.Instance;
                Console.WriteLine($"🔗 [{nameof(CreateDropboxSharedLink)}] DropboxService 인스턴스 획득 완료");
                
                var sharedLink = await dropboxService.CreateSharedLinkAsync(dropboxFilePath);
                
                if (string.IsNullOrEmpty(sharedLink))
                {
                    Console.WriteLine($"❌ [{nameof(CreateDropboxSharedLink)}] 공유 링크가 null 또는 빈 문자열로 반환됨");
                    return null;
                }
                
                Console.WriteLine($"✅ [{nameof(CreateDropboxSharedLink)}] 공유 링크 생성 성공: {sharedLink}");
                return sharedLink;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [{nameof(CreateDropboxSharedLink)}] Dropbox 공유 링크 생성 실패: {ex.Message}");
                Console.WriteLine($"📋 [{nameof(CreateDropboxSharedLink)}] 예외 타입: {ex.GetType().Name}");
                Console.WriteLine($"📋 [{nameof(CreateDropboxSharedLink)}] 스택 트레이스: {ex.StackTrace}");
                
                // 내부 예외가 있는 경우 추가 로그
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"📋 [{nameof(CreateDropboxSharedLink)}] 내부 예외: {ex.InnerException.Message}");
                }
                
                return null;
            }
        }

        /// <summary>
        /// 파일이 쓰기 가능한지 확인하는 메서드
        /// </summary>
        /// <param name="filePath">확인할 파일 경로</param>
        /// <returns>쓰기 가능 여부</returns>
        public bool CanWriteToFile(string filePath)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    directoryPath = Environment.CurrentDirectory;
                }

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 테스트 파일 생성 시도
                var testPath = Path.Combine(directoryPath, "test_write.tmp");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 파일 쓰기 권한 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 임시 파일을 정리하는 메서드
        /// </summary>
        /// <param name="filePath">정리할 파일 경로</param>
        /// <param name="methodName">호출 메서드명 (로깅용)</param>
        /// <returns>정리 성공 여부</returns>
        public bool CleanupTempFile(string filePath, string methodName = "")
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    var logPrefix = string.IsNullOrEmpty(methodName) ? "" : $"[{methodName}] ";
                    Console.WriteLine($"🗑️ {logPrefix}임시 파일 정리 완료: {filePath}");
                    return true;
                }
                return true; // 파일이 없어도 성공으로 처리
            }
            catch (Exception ex)
            {
                var logPrefix = string.IsNullOrEmpty(methodName) ? "" : $"[{methodName}] ";
                Console.WriteLine($"⚠️ {logPrefix}임시 파일 정리 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파일 크기를 MB 단위로 반환하는 메서드
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>파일 크기 (MB)</returns>
        public double GetFileSizeInMB(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length / (1024.0 * 1024.0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 파일 크기 확인 실패: {ex.Message}");
                return 0;
            }
        }
    }
}
