DELIMITER $$

DROP PROCEDURE IF EXISTS sp_InvoiceFinalProcess$$

CREATE PROCEDURE sp_InvoiceFinalProcess()
BEGIN
    /*--================================================================================
	-- 송장출력 최종 처리
    --================================================================================*/
    DECLARE done INT DEFAULT FALSE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
		DECLARE error_info TEXT DEFAULT '';
		DECLARE error_code INT DEFAULT 0;
		
		-- MySQL 오류 정보 수집
		GET DIAGNOSTICS CONDITION 1
			error_code = MYSQL_ERRNO,
			error_info = MESSAGE_TEXT;
			
        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        SELECT '오류가 발생하여 모든 작업이 롤백되었습니다.' AS ErrorMessage,
		        error_code AS MySQLErrorCode,
                error_info AS MySQLErrorMessage;
    END;

    CREATE TEMPORARY TABLE IF NOT EXISTS sp_execution_log (
		StepID INT AUTO_INCREMENT PRIMARY KEY,
		OperationDescription VARCHAR(255),
		AffectedRows INT
	);

    START TRANSACTION;

    TRUNCATE TABLE sp_execution_log;

    /*--================================================================================
	-- (송장출력 최종) 각 테이블의 데이터를 송장출력_최종 테이블에 insert
    --================================================================================*/
	TRUNCATE TABLE 송장출력_최종;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(송장출력 최종) 송장출력_최종 초기화', 0);
	
    INSERT INTO 송장출력_최종
		SELECT * FROM 송장출력_경기냉동_최종
		UNION ALL
		SELECT * FROM 송장출력_경기공산_최종
		UNION ALL
		SELECT * FROM 송장출력_부산청과_최종
		UNION ALL
		SELECT * FROM 송장출력_서울공산_최종
		UNION ALL
		SELECT * FROM 송장출력_서울냉동_최종
		UNION ALL
		SELECT * FROM 송장출력_감천냉동_최종;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_최종', ROW_COUNT());
		
    COMMIT;
    
    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 전 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
