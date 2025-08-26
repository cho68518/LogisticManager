DELIMITER $$

DROP PROCEDURE IF EXISTS sp_BusanCheonggwaDocProcess$$

CREATE PROCEDURE sp_BusanCheonggwaDocProcess()
BEGIN
    /*--================================================================================
	-- 부산청과자료 처리
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
	-- (부산청과자료) 송장출력_부산청과자료
    --================================================================================*/
	TRUNCATE TABLE 송장출력_부산청과자료;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(부산청과자료) 송장출력_부산청과자료 초기화', 0);
	
	INSERT INTO 송장출력_부산청과자료 (품목코드, 송장명, 수량)
		SELECT 
			품목코드, 
			송장명, 
			수량
		FROM 
			송장출력_사방넷원본변환
		WHERE 
			송장구분자 LIKE 'YC_%'; -- LIKE 대신 LEFT(송장구분자, 3) = 'YC_'
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_부산청과자료', ROW_COUNT());
	
    /*--================================================================================
	-- (부산청과자료) 송장출력_부산청과자료_단품
	--                단품(주소가 유니크한) 데이터 처리
    --================================================================================*/
	TRUNCATE TABLE 송장출력_부산청과자료_단품;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(부산청과자료) 송장출력_부산청과자료_단품 초기화', 0);
	
	-- 원본 테이블에서 '주소'가 유니크한(전체 테이블에서 딱 한 번만 등장하는)
    INSERT INTO 송장출력_부산청과자료_단품 (품목코드, 송장명, 수량)
		SELECT
			품목코드,
			송장명,
			수량
		FROM
			송장출력_부산청과변환
		WHERE
			주소 IN (
				SELECT 주소
				FROM 송장출력_부산청과변환
				WHERE 주소 IS NOT NULL
				GROUP BY 주소
				HAVING COUNT(*) = 1
			);	
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_부산청과자료_단품', ROW_COUNT());
	
    -- =================================================================
    -- (부산청과자료) 합포(주소가 중복되는) 데이터 처리
    -- =================================================================
    TRUNCATE TABLE 송장출력_부산청과자료_합포;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(부산청과자료) 송장출력_부산청과자료_합포 초기화', 0);
	
    -- 원본 테이블에서 '주소'가 중복되는 데이터를 조회하여 한 번에 삽입
    INSERT INTO 송장출력_부산청과자료_합포 (품목코드, 송장명, 수량)
		SELECT
			품목코드,
			송장명,
			수량
		FROM
			송장출력_부산청과변환
		WHERE
			주소 IN (
				SELECT 주소
				FROM 송장출력_부산청과변환
				WHERE 주소 IS NOT NULL
				GROUP BY 주소
				HAVING COUNT(*) > 1
			);
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_부산청과자료_합포', ROW_COUNT());
		
    -- =================================================================
    -- (부산청과자료)  각 품목코드별로 수량 합계 계산
    -- =================================================================
    UPDATE 
        송장출력_부산청과자료 AS main
    JOIN 
        (
            SELECT 
                품목코드, 
                SUM(수량) AS c_sum
            FROM 
                송장출력_부산청과자료
            GROUP BY 
                품목코드
        ) AS sub ON main.품목코드 = sub.품목코드
    SET 
        main.총수량 = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_부산청과자료 (총수량)', ROW_COUNT());	
	
    -- =================================================================
    -- (부산청과자료)  각 품목코드별로 수량 합계 계산
    -- =================================================================	
    UPDATE 
        송장출력_부산청과자료_단품 AS main
    JOIN 
        (
            SELECT 
                품목코드, 
                SUM(수량) AS c_sum
            FROM 
                송장출력_부산청과자료_단품
            GROUP BY 
                품목코드
        ) AS sub ON main.품목코드 = sub.품목코드
    SET 
        main.총수량 = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_부산청과자료_단품 (총수량)', ROW_COUNT());	
	
    -- =================================================================
    -- (부산청과자료)  각 품목코드별로 수량 합계 계산
    -- =================================================================	
    UPDATE 
        송장출력_부산청과자료_합포 AS main
    JOIN 
        (
            SELECT 
                품목코드, 
                SUM(수량) AS c_sum
            FROM 
                송장출력_부산청과자료_합포
            GROUP BY 
                품목코드
        ) AS sub ON main.품목코드 = sub.품목코드
    SET 
        main.총수량 = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_부산청과자료_합포 (총수량)', ROW_COUNT());	
	
    -- =================================================================
    -- (부산청과자료) 송장출력_부산청과자료_최종
    -- =================================================================
    TRUNCATE TABLE 송장출력_부산청과자료_최종;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(부산청과자료) 송장출력_부산청과자료_최종 초기화', 0);

    INSERT INTO 송장출력_부산청과자료_최종 (
        품목코드, 송장명, 총수량, 
        단품품목코드, 단품송장명, 단품총수량, 
        합포품목코드, 합포송장명, 합포총수량
    )
    SELECT
        -- '전체'
        t1.품목코드,
        t1.송장명,
        t1.총수량,
        -- '단품'
        t2.품목코드,
        t2.송장명,
        t2.총수량,
        -- '합포'
        t3.품목코드,
        t3.송장명,
        t3.총수량
    FROM
        -- 마스터 '품목코드'
        (
            SELECT 품목코드 FROM 송장출력_부산청과자료      UNION
            SELECT 품목코드 FROM 송장출력_부산청과자료_단품 UNION
            SELECT 품목코드 FROM 송장출력_부산청과자료_합포
        ) AS m_code
    -- 마스터 '품목코드' 목록에 각 테이블의 집계 결과를 연결.
    LEFT JOIN 
        (
            SELECT 품목코드, REPLACE(MAX(송장명), 'YC_', '') AS 송장명, SUM(수량) AS 총수량
            FROM 송장출력_부산청과자료 GROUP BY 품목코드
        ) AS t1 ON m_code.품목코드 = t1.품목코드
    LEFT JOIN 
        (
            SELECT 품목코드, REPLACE(MAX(송장명), 'YC_', '') AS 송장명, SUM(수량) AS 총수량
            FROM 송장출력_부산청과자료_단품 GROUP BY 품목코드
        ) AS t2 ON m_code.품목코드 = t2.품목코드
    LEFT JOIN 
        (
            SELECT 품목코드, REPLACE(MAX(송장명), 'YC_', '') AS 송장명, SUM(수량) AS 총수량
            FROM 송장출력_부산청과자료_합포 GROUP BY 품목코드
        ) AS t3 ON m_code.품목코드 = t3.품목코드
    ORDER BY 
        m_code.품목코드 ASC;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_부산청과자료_최종', ROW_COUNT());			
		
    COMMIT;
    
    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 전 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
