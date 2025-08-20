DELIMITER $$

DROP PROCEDURE IF EXISTS sp_BusanExtShipmentProcess$$

CREATE PROCEDURE sp_BusanExtShipmentProcess()
BEGIN
    /*--================================================================================
	-- 부산 외부출고 처리
    -- 송장출력_부산청과_최종변환
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

    /*--================================================================================*/
    START TRANSACTION;

    TRUNCATE TABLE sp_execution_log;

	TRUNCATE TABLE 송장출력_부산청과_최종변환;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(부산청과) 송장출력_부산청과변환 초기화', 0);

    INSERT INTO 송장출력_부산청과_최종변환
        (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2,
         우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰,
         품목코드, 택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수)
    SELECT 
        t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.수취인명, t.전화번호1, t.전화번호2,
        t.우편번호, t.주소, t.송장명, t.수량, t.배송메세지, t.주문번호, t.쇼핑몰,
        t.품목코드, t.택배비용, t.박스크기, t.출력개수, t.별표1, t.별표2, t.품목개수
    FROM 
        송장출력_부산청과_최종 t
    JOIN 
        송장출력_부산청과_외부출고 e ON t.품목코드 = e.품목코드;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_부산청과_최종변환', ROW_COUNT());

		
    DELETE t
    FROM 
        송장출력_부산청과_최종 t
    JOIN 
        송장출력_부산청과_외부출고 e ON t.품목코드 = e.품목코드;	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] 송장출력_부산청과_최종', ROW_COUNT());	
		
    COMMIT;
    
    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 전 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
