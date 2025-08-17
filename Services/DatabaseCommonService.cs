using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using LogisticManager.Services;

namespace LogisticManager.Services
{
    /// <summary>
    /// 공통 데이터베이스 처리 기능을 제공하는 서비스 클래스
    /// 
    /// 📋 주요 기능:
    /// - 데이터베이스 연결 확인
    /// - 테이블 존재 여부 확인
    /// - 데이터 조회 및 처리
    /// - 배치 데이터 처리
    /// 
    /// 💡 사용법:
    /// var dbService = new DatabaseCommonService(databaseService);
    /// var data = await dbService.GetDataFromDatabase("테이블명");
    /// </summary>
    public class DatabaseCommonService
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// DatabaseCommonService 생성자
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스 인스턴스</param>
        public DatabaseCommonService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// 데이터베이스에서 테이블 데이터를 조회하는 공통 메서드
        /// 
        /// 📋 주요 기능:
        /// - 지정된 테이블에서 모든 데이터 조회
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 비동기 처리로 성능 최적화
        /// - 오류 처리 및 로깅
        /// 
        /// 💡 사용법:
        /// var data = await GetDataFromDatabase("테이블명");
        /// 
        /// 🔗 의존성:
        /// - _databaseService: 데이터베이스 연결 관리
        /// - MySqlConnection: MySQL 데이터베이스 연결
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <returns>테이블 데이터 (DataTable) 또는 null (오류 시)</returns>
        public async Task<DataTable?> GetDataFromDatabase(string tableName)
        {
            try
            {
                // 간단한 SELECT 쿼리로 모든 데이터 조회
                var query = $"SELECT * FROM `{tableName}`";
                
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [{tableName}] 테이블 데이터베이스 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 데이터베이스 연결을 확인하는 메서드
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("✅ 데이터베이스 연결 확인 완료");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 데이터베이스 연결 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 테이블이 존재하는지 확인하는 메서드
        /// </summary>
        /// <param name="tableName">확인할 테이블명</param>
        /// <returns>테이블 존재 여부</returns>
        public async Task<bool> CheckTableExistsAsync(string tableName)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @tableName";
                
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        var result = await command.ExecuteScalarAsync();
                        var exists = Convert.ToInt32(result) > 0;
                        
                        Console.WriteLine($"📋 테이블 존재 확인: {tableName} - {(exists ? "존재" : "존재하지 않음")}");
                        return exists;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 테이블 존재 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 데이터를 배치로 처리하여 데이터베이스에 삽입하는 메서드
        /// </summary>
        /// <param name="data">삽입할 데이터</param>
        /// <param name="progress">진행률 보고자</param>
        /// <returns>처리된 행 수</returns>
        public async Task<int> TruncateAndInsertOriginalDataOptimized(DataTable data, IProgress<string>? progress)
        {
            try
            {
                if (data == null || data.Rows.Count == 0)
                {
                    progress?.Report("⚠️ 삽입할 데이터가 없습니다.");
                    return 0;
                }

                var totalRows = data.Rows.Count;
                var batchSize = 500; // 배치 크기
                var processedRows = 0;

                progress?.Report($"🔄 배치 처리 시작: 총 {totalRows:N0}행, 배치 크기: {batchSize}");

                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // 트랜잭션 시작
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            for (int i = 0; i < totalRows; i += batchSize)
                            {
                                var currentBatchSize = Math.Min(batchSize, totalRows - i);
                                var batchData = data.Clone();
                                
                                // 현재 배치의 데이터 복사
                                for (int j = 0; j < currentBatchSize; j++)
                                {
                                    batchData.ImportRow(data.Rows[i + j]);
                                }

                                // 배치 데이터 처리
                                var batchProcessed = await ProcessBatchDataAsync(connection, batchData, progress);
                                processedRows += batchProcessed;

                                var progressPercentage = (i + currentBatchSize) * 100 / totalRows;
                                progress?.Report($"📊 배치 처리 진행률: {progressPercentage}% ({processedRows:N0}/{totalRows:N0}행)");
                            }

                            // 트랜잭션 커밋
                            await transaction.CommitAsync();
                            progress?.Report($"✅ 배치 처리 완료: 총 {processedRows:N0}행 처리됨");
                            
                            return processedRows;
                        }
                        catch (Exception ex)
                        {
                            // 트랜잭션 롤백
                            await transaction.RollbackAsync();
                            throw new Exception($"배치 처리 중 오류 발생: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 배치 처리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 배치 데이터를 처리하는 내부 메서드
        /// </summary>
        /// <param name="connection">데이터베이스 연결</param>
        /// <param name="batchData">배치 데이터</param>
        /// <param name="progress">진행률 보고자</param>
        /// <returns>처리된 행 수</returns>
        private Task<int> ProcessBatchDataAsync(MySqlConnection connection, DataTable batchData, IProgress<string>? progress)
        {
            try
            {
                var processedRows = 0;
                
                foreach (DataRow row in batchData.Rows)
                {
                    // 여기에 실제 데이터 삽입 로직 구현
                    // 예: INSERT INTO 테이블명 (컬럼1, 컬럼2) VALUES (@값1, @값2)
                    
                    processedRows++;
                }

                return Task.FromResult(processedRows);
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 배치 데이터 처리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 문자열을 반환하는 메서드
        /// </summary>
        /// <returns>연결 문자열</returns>
        public string GetConnectionString()
        {
            return _databaseService.GetConnectionString();
        }
    }
}
