using LogisticManager.Models;
using LogisticManager.Services;
using MySqlConnector;
using System.Data;

namespace LogisticManager.Repositories
{
    /// <summary>
    /// 공통코드 데이터 접근을 위한 리포지토리 구현체
    /// </summary>
    public class CommonCodeRepository : ICommonCodeRepository
    {
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// CommonCodeRepository 생성자
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스</param>
        public CommonCodeRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// 모든 그룹코드 목록을 조회합니다.
        /// </summary>
        /// <returns>그룹코드 목록</returns>
        public async Task<List<string>> GetAllGroupCodesAsync()
        {
            try
            {
                LogManagerService.LogInfo("=== GetAllGroupCodesAsync 시작 ===");
                
                // 단일 연결을 사용하여 모든 쿼리를 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();
                
                // 먼저 테이블 존재 여부와 데이터 개수를 확인
                var checkQuery = "SELECT COUNT(*) FROM CommonCode";
                LogManagerService.LogInfo($"데이터 개수 확인 쿼리: {checkQuery}");
                
                using var countCommand = new MySqlConnector.MySqlCommand(checkQuery, connection);
                var countResult = await countCommand.ExecuteScalarAsync();
                var totalCount = Convert.ToInt32(countResult);
                
                LogManagerService.LogInfo($"CommonCode 테이블 총 데이터 개수: {totalCount}");

                if (totalCount == 0)
                {
                    LogManagerService.LogWarning("CommonCode 테이블에 데이터가 없습니다!");
                    return new List<string>();
                }

                var query = @"
                    SELECT DISTINCT GroupCode 
                    FROM CommonCode 
                    ORDER BY GroupCode";
                
                LogManagerService.LogInfo($"그룹코드 조회 쿼리: {query}");

                using var command = new MySqlConnector.MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var groupCodes = new List<string>();
                var rowCount = 0;

                while (await reader.ReadAsync())
                {
                    var groupCode = reader["GroupCode"].ToString() ?? string.Empty;
                    groupCodes.Add(groupCode);
                    rowCount++;
                    LogManagerService.LogInfo($"그룹코드 조회: '{groupCode}' (길이: {groupCode.Length})");
                }

                LogManagerService.LogInfo($"쿼리 실행 결과: {rowCount}개 행 반환됨");
                LogManagerService.LogInfo($"총 {groupCodes.Count}개의 그룹코드 조회됨: [{string.Join(", ", groupCodes)}]");
                LogManagerService.LogInfo("=== GetAllGroupCodesAsync 완료 ===");
                return groupCodes;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 그룹코드 조회 중 오류 발생: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 특정 그룹코드에 속한 모든 공통코드를 조회합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <returns>공통코드 목록</returns>
        public async Task<List<CommonCode>> GetCommonCodesByGroupAsync(string groupCode)
        {
            try
            {
                LogManagerService.LogInfo($"=== GetCommonCodesByGroupAsync 시작 - 그룹코드: '{groupCode}' ===");

                // 단일 연결을 사용하여 모든 쿼리를 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                // 먼저 해당 그룹의 데이터 개수 확인
                var countQuery = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode";
                
                LogManagerService.LogInfo($"데이터 개수 확인 쿼리: {countQuery.Replace("@GroupCode", $"'{groupCode}'")}");
                
                using var countCommand = new MySqlConnector.MySqlCommand(countQuery, connection);
                countCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                var countResult = await countCommand.ExecuteScalarAsync();
                var groupCount = Convert.ToInt32(countResult);
                
                LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 데이터 개수: {groupCount}");

                if (groupCount == 0)
                {
                    LogManagerService.LogWarning($"그룹코드 '{groupCode}'에 데이터가 없습니다!");
                    return new List<CommonCode>();
                }

                var query = @"
                    SELECT GroupCode, Code, CodeName, Description, SortOrder, 
                           IsUsed, Attribute1, Attribute2, CreatedBy, CreatedAt, 
                           UpdatedBy, UpdatedAt, GroupCodeNm
                    FROM CommonCode 
                    WHERE GroupCode = @GroupCode 
                    ORDER BY SortOrder, Code";

                LogManagerService.LogInfo($"상세 조회 쿼리: {query.Replace("@GroupCode", $"'{groupCode}'")}");
                
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                using var reader = await command.ExecuteReaderAsync();
                
                var commonCodes = new List<CommonCode>();
                var rowCount = 0;

                while (await reader.ReadAsync())
                {
                    rowCount++;
                    LogManagerService.LogInfo($"=== 행 데이터 처리 시작 (행 {rowCount}) ===");
                    
                    // 원본 데이터 로깅
                    LogManagerService.LogInfo($"원본 GroupCode: '{reader["GroupCode"]}' (타입: {reader["GroupCode"]?.GetType().Name})");
                    LogManagerService.LogInfo($"원본 Code: '{reader["Code"]}' (타입: {reader["Code"]?.GetType().Name})");
                    LogManagerService.LogInfo($"원본 CodeName: '{reader["CodeName"]}' (타입: {reader["CodeName"]?.GetType().Name})");
                    
                    var commonCode = new CommonCode
                    {
                        GroupCode = reader["GroupCode"].ToString() ?? string.Empty,
                        Code = reader["Code"].ToString() ?? string.Empty,
                        CodeName = reader["CodeName"].ToString() ?? string.Empty,
                        Description = reader["Description"]?.ToString(),
                        SortOrder = Convert.ToInt32(reader["SortOrder"]),
                        IsUsed = Convert.ToBoolean(reader["IsUsed"]),
                        Attribute1 = reader["Attribute1"]?.ToString(),
                        Attribute2 = reader["Attribute2"]?.ToString(),
                        CreatedBy = reader["CreatedBy"]?.ToString(),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        UpdatedBy = reader["UpdatedBy"]?.ToString(),
                        UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : null,
                        GroupCodeNm = reader["GroupCodeNm"]?.ToString()
                    };

                    commonCodes.Add(commonCode);
                    LogManagerService.LogInfo($"  - {commonCode.Code}: {commonCode.CodeName}");
                    LogManagerService.LogInfo($"=== 행 데이터 처리 완료 (행 {rowCount}) ===");
                }

                LogManagerService.LogInfo($"쿼리 결과: {rowCount}개 행 반환됨");
                LogManagerService.LogInfo($"그룹코드 '{groupCode}'에서 {commonCodes.Count}개의 공통코드 조회 완료");
                LogManagerService.LogInfo($"=== GetCommonCodesByGroupAsync 완료 ===");
                return commonCodes;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 조회 중 오류 발생 (그룹코드: {groupCode}): {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 공통코드를 추가합니다.
        /// </summary>
        /// <param name="commonCode">추가할 공통코드</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> AddCommonCodeAsync(CommonCode commonCode)
        {
            try
            {
                LogManagerService.LogInfo($"공통코드 추가 시작: {commonCode.GroupCode}.{commonCode.Code}");

                // 단일 연결을 사용하여 추가 처리
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO CommonCode (
                        GroupCode, Code, CodeName, Description, SortOrder, 
                        IsUsed, Attribute1, Attribute2, CreatedBy, CreatedAt
                    ) VALUES (
                        @GroupCode, @Code, @CodeName, @Description, @SortOrder, 
                        @IsUsed, @Attribute1, @Attribute2, @CreatedBy, @CreatedAt
                    )";

                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", commonCode.GroupCode));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", commonCode.Code));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@CodeName", commonCode.CodeName));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Description", commonCode.Description ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@SortOrder", commonCode.SortOrder));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@IsUsed", commonCode.IsUsed));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute1", commonCode.Attribute1 ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute2", commonCode.Attribute2 ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedBy", commonCode.CreatedBy ?? "SYSTEM"));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedAt", commonCode.CreatedAt));

                var result = await command.ExecuteNonQueryAsync();
                
                LogManagerService.LogInfo($"공통코드 추가 완료: {commonCode.GroupCode}.{commonCode.Code}, 영향받은 행: {result}개");
                return result > 0;
            }
            catch (MySqlConnector.MySqlException mysqlEx)
            {
                LogManagerService.LogError($"MySQL 오류 ({mysqlEx.Number}): {mysqlEx.Message}");
                LogManagerService.LogError($"스택 트레이스: {mysqlEx.StackTrace}");
                throw new InvalidOperationException($"데이터베이스 오류가 발생했습니다.\n오류 코드: {mysqlEx.Number}\n메시지: {mysqlEx.Message}");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 추가 중 오류 발생: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 공통코드를 수정합니다.
        /// </summary>
        /// <param name="commonCode">수정할 공통코드</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> UpdateCommonCodeAsync(CommonCode commonCode)
        {
            try
            {
                LogManagerService.LogInfo($"공통코드 수정 시작: {commonCode.GroupCode}.{commonCode.Code}");

                // 단일 연결을 사용하여 수정 처리
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                var query = @"
                    UPDATE CommonCode SET 
                        CodeName = @CodeName, 
                        Description = @Description, 
                        SortOrder = @SortOrder, 
                        IsUsed = @IsUsed, 
                        Attribute1 = @Attribute1, 
                        Attribute2 = @Attribute2, 
                        UpdatedBy = @UpdatedBy, 
                        UpdatedAt = @UpdatedAt
                    WHERE GroupCode = @GroupCode AND Code = @Code";

                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", commonCode.GroupCode));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", commonCode.Code));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@CodeName", commonCode.CodeName));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Description", commonCode.Description ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@SortOrder", commonCode.SortOrder));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@IsUsed", commonCode.IsUsed));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute1", commonCode.Attribute1 ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute2", commonCode.Attribute2 ?? (object)DBNull.Value));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@UpdatedBy", commonCode.UpdatedBy ?? "SYSTEM"));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@UpdatedAt", DateTime.Now));

                var result = await command.ExecuteNonQueryAsync();
                
                LogManagerService.LogInfo($"공통코드 수정 완료: {commonCode.GroupCode}.{commonCode.Code}, 영향받은 행: {result}개");
                return result > 0;
            }
            catch (MySqlConnector.MySqlException mysqlEx)
            {
                LogManagerService.LogError($"MySQL 오류 ({mysqlEx.Number}): {mysqlEx.Message}");
                LogManagerService.LogError($"스택 트레이스: {mysqlEx.StackTrace}");
                throw new InvalidOperationException($"데이터베이스 오류가 발생했습니다.\n오류 코드: {mysqlEx.Number}\n메시지: {mysqlEx.Message}");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 수정 중 오류 발생: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 공통코드를 삭제합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <param name="code">코드</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> DeleteCommonCodeAsync(string groupCode, string code)
        {
            try
            {
                LogManagerService.LogInfo($"공통코드 삭제 처리 시작: {groupCode}.{code}");

                // 단일 연결을 사용하여 삭제 처리
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                // 먼저 해당 공통코드가 존재하는지 확인
                var existsQuery = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode AND Code = @Code";
                using var existsCommand = new MySqlConnector.MySqlCommand(existsQuery, connection);
                existsCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                existsCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", code));
                
                var existsCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
                LogManagerService.LogInfo($"삭제 대상 공통코드 존재 확인: {existsCount}개");
                
                if (existsCount == 0)
                {
                    LogManagerService.LogWarning($"삭제하려는 공통코드가 존재하지 않습니다: {groupCode}.{code}");
                    return false;
                }

                // 공통코드 삭제 실행
                var deleteQuery = @"
                    DELETE FROM CommonCode 
                    WHERE GroupCode = @GroupCode AND Code = @Code";

                using var deleteCommand = new MySqlConnector.MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                deleteCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", code));

                var result = await deleteCommand.ExecuteNonQueryAsync();
                
                LogManagerService.LogInfo($"공통코드 삭제 완료: {groupCode}.{code}, 영향받은 행: {result}개");
                return result > 0;
            }
            catch (MySqlConnector.MySqlException mysqlEx)
            {
                LogManagerService.LogError($"MySQL 오류 ({mysqlEx.Number}): {mysqlEx.Message}");
                LogManagerService.LogError($"스택 트레이스: {mysqlEx.StackTrace}");
                throw new InvalidOperationException($"데이터베이스 오류가 발생했습니다.\n오류 코드: {mysqlEx.Number}\n메시지: {mysqlEx.Message}");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 삭제 중 오류 발생: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 여러 공통코드를 일괄 저장합니다.
        /// </summary>
        /// <param name="commonCodes">저장할 공통코드 목록</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> SaveCommonCodesAsync(List<CommonCode> commonCodes)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    foreach (var commonCode in commonCodes)
                    {
                        // 중복 키 확인을 위해 더 정확한 체크
                        var existsQuery = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode AND Code = @Code";
                        using var existsCommand = new MySqlConnector.MySqlCommand(existsQuery, connection, transaction);
                        existsCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", commonCode.GroupCode));
                        existsCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", commonCode.Code));
                        
                        var existsCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
                        var isExisting = existsCount > 0;

                        if (isExisting)
                        {
                            // 기존 코드 수정
                            LogManagerService.LogInfo($"기존 공통코드 수정: {commonCode.GroupCode}.{commonCode.Code}");
                            
                            var updateQuery = @"
                                UPDATE CommonCode SET 
                                    CodeName = @CodeName, 
                                    Description = @Description, 
                                    SortOrder = @SortOrder, 
                                    IsUsed = @IsUsed, 
                                    Attribute1 = @Attribute1, 
                                    Attribute2 = @Attribute2, 
                                    UpdatedBy = @UpdatedBy, 
                                    UpdatedAt = @UpdatedAt
                                WHERE GroupCode = @GroupCode AND Code = @Code";

                            using var updateCommand = new MySqlConnector.MySqlCommand(updateQuery, connection, transaction);
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", commonCode.GroupCode));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", commonCode.Code));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CodeName", commonCode.CodeName));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Description", commonCode.Description ?? (object)DBNull.Value));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@SortOrder", commonCode.SortOrder));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@IsUsed", commonCode.IsUsed));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute1", commonCode.Attribute1 ?? (object)DBNull.Value));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute2", commonCode.Attribute2 ?? (object)DBNull.Value));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@UpdatedBy", commonCode.UpdatedBy ?? "SYSTEM"));
                            updateCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@UpdatedAt", DateTime.Now));

                            await updateCommand.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            // 새 코드 추가
                            LogManagerService.LogInfo($"새 공통코드 추가: {commonCode.GroupCode}.{commonCode.Code}");
                            
                            var insertQuery = @"
                                INSERT INTO CommonCode (
                                    GroupCode, Code, CodeName, Description, SortOrder, 
                                    IsUsed, Attribute1, Attribute2, CreatedBy, CreatedAt
                                ) VALUES (
                                    @GroupCode, @Code, @CodeName, @Description, @SortOrder, 
                                    @IsUsed, @Attribute1, @Attribute2, @CreatedBy, @CreatedAt
                                )";

                            using var insertCommand = new MySqlConnector.MySqlCommand(insertQuery, connection, transaction);
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", commonCode.GroupCode));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", commonCode.Code));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CodeName", commonCode.CodeName));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Description", commonCode.Description ?? (object)DBNull.Value));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@SortOrder", commonCode.SortOrder));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@IsUsed", commonCode.IsUsed));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute1", commonCode.Attribute1 ?? (object)DBNull.Value));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute2", commonCode.Attribute2 ?? (object)DBNull.Value));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedBy", commonCode.CreatedBy ?? "SYSTEM"));
                            insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedAt", commonCode.CreatedAt));

                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    LogManagerService.LogInfo($"공통코드 일괄 저장 완료: {commonCodes.Count}개 처리됨");
                    return true;
                }
                catch (MySqlConnector.MySqlException mysqlEx)
                {
                    await transaction.RollbackAsync();
                    
                    // 중복 키 오류를 더 친화적으로 처리
                    if (mysqlEx.Number == 1062) // Duplicate entry error
                    {
                        var errorMessage = $"중복된 공통코드가 발견되었습니다.\n\n" +
                                         $"그룹코드와 코드의 조합이 이미 존재합니다.\n" +
                                         $"오류 코드: {mysqlEx.Number}\n" +
                                         $"상세 메시지: {mysqlEx.Message}";
                        
                        LogManagerService.LogError($"중복 키 오류: {errorMessage}");
                        throw new InvalidOperationException(errorMessage);
                    }
                    else
                    {
                        LogManagerService.LogError($"MySQL 오류 ({mysqlEx.Number}): {mysqlEx.Message}");
                        throw;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 일괄 저장 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 특정 그룹코드가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <returns>존재 여부</returns>
        public async Task<bool> ExistsGroupCodeAsync(string groupCode)
        {
            try
            {
                // 단일 연결을 사용하여 쿼리 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();
                
                var query = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode";
                
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));

                var result = await command.ExecuteScalarAsync();
                var exists = Convert.ToInt32(result) > 0;
                
                LogManagerService.LogInfo($"그룹코드 존재 확인: {groupCode} = {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"그룹코드 존재 확인 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 특정 공통코드가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <param name="code">코드</param>
        /// <returns>존재 여부</returns>
        public async Task<bool> ExistsCommonCodeAsync(string groupCode, string code)
        {
            try
            {
                // 단일 연결을 사용하여 쿼리 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();
                
                var query = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode AND Code = @Code";
                
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", code));

                var result = await command.ExecuteScalarAsync();
                var exists = Convert.ToInt32(result) > 0;
                
                LogManagerService.LogInfo($"공통코드 존재 확인: {groupCode}.{code} = {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 존재 확인 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 새 그룹코드를 추가합니다.
        /// </summary>
        /// <param name="groupCode">새 그룹코드</param>
        /// <param name="groupCodeName">그룹코드명 (선택사항)</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> AddGroupCodeAsync(string groupCode, string? groupCodeName = null)
        {
            try
            {
                LogManagerService.LogInfo($"새 그룹코드 추가 시작: {groupCode}");

                // 단일 연결을 사용하여 쿼리 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                // 그룹코드가 이미 존재하는지 확인
                var existsQuery = "SELECT COUNT(*) FROM CommonCode WHERE GroupCode = @GroupCode";
                using var existsCommand = new MySqlConnector.MySqlCommand(existsQuery, connection);
                existsCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                
                var existsCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
                
                if (existsCount > 0)
                {
                    LogManagerService.LogWarning($"그룹코드 '{groupCode}'가 이미 존재합니다!");
                    return false;
                }

                // 새 그룹코드에 기본 공통코드 추가 (그룹코드 자체를 첫 번째 코드로)
                var insertQuery = @"
                    INSERT INTO CommonCode (
                        GroupCode, Code, CodeName, Description, SortOrder, 
                        IsUsed, Attribute1, Attribute2, CreatedBy, CreatedAt, GroupCodeNm
                    ) VALUES (
                        @GroupCode, @Code, @CodeName, @Description, @SortOrder, 
                        @IsUsed, @Attribute1, @Attribute2, @CreatedBy, @CreatedAt, @GroupCodeNm
                    )";

                using var insertCommand = new MySqlConnector.MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Code", "DEFAULT"));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CodeName", groupCodeName ?? groupCode));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Description", $"새로 추가된 그룹코드: {groupCode}"));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@SortOrder", 1));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@IsUsed", true));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute1", "NEW_GROUP"));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@Attribute2", (object)DBNull.Value));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedBy", "SYSTEM"));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@CreatedAt", DateTime.Now));
                insertCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCodeNm", groupCodeName ?? groupCode));

                var result = await insertCommand.ExecuteNonQueryAsync();
                
                if (result > 0)
                {
                    LogManagerService.LogInfo($"새 그룹코드 '{groupCode}' 추가 완료");
                    return true;
                }
                else
                {
                    LogManagerService.LogError($"새 그룹코드 '{groupCode}' 추가 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"새 그룹코드 추가 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 그룹코드를 삭제합니다 (해당 그룹의 모든 공통코드도 함께 삭제).
        /// </summary>
        /// <param name="groupCode">삭제할 그룹코드</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> DeleteGroupCodeAsync(string groupCode)
        {
            try
            {
                LogManagerService.LogInfo($"그룹코드 삭제 시작: {groupCode}");

                // 단일 연결을 사용하여 쿼리 실행
                using var connection = await _databaseService.GetConnectionAsync();
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 해당 그룹의 모든 공통코드 삭제
                    var deleteQuery = "DELETE FROM CommonCode WHERE GroupCode = @GroupCode";
                    using var deleteCommand = new MySqlConnector.MySqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.Add(new MySqlConnector.MySqlParameter("@GroupCode", groupCode));

                    var result = await deleteCommand.ExecuteNonQueryAsync();
                    
                    await transaction.CommitAsync();
                    
                    LogManagerService.LogInfo($"그룹코드 '{groupCode}' 삭제 완료: {result}개 공통코드 삭제됨");
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"그룹코드 삭제 중 오류 발생: {ex.Message}");
                throw;
            }
        }
    }
}
