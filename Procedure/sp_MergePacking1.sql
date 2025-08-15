DELIMITER $$

DROP PROCEDURE IF EXISTS sp_MergePacking1$$

CREATE PROCEDURE sp_MergePacking1()
BEGIN
    /*--================================================================================
	-- 송장출력_특수출력_합포장 (합포장 처리)
    --================================================================================*/
    -- 오류 처리용 변수들 (EXIT HANDLER에서 사용)
    DECLARE error_info TEXT DEFAULT '';
    DECLARE error_code INT DEFAULT 0;
	
    -- 오류 발생 시 자동 롤백, 상세한 오류 메시지를 반환
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
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
	
    -- 트랜잭션 시작
    START TRANSACTION;

    -- 임시 로그 테이블 초기화
    TRUNCATE TABLE sp_execution_log;
	
    -- 대체코드1이 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환_Dev (
        msg1, msg2, msg3, msg4, msg5, msg6, 택배비용, 전화번호1, 전화번호2, 우편번호, 주소,
        수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 
        품목코드, 옵션명, 박스크기, 출력개수, 송장수량, 
        별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 
        결제수단, 면과세구분, 주문상태, 배송송
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.택배비용, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        (dev.수량 * CAST(spec.대체1수량 AS DECIMAL(10,2))), dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드1, spec.대체코드1품목명, 
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, dev.결제금액, dev.주문금액, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드1 IS NOT NULL AND spec.대체코드1 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드1 처리', ROW_COUNT());
	
    -- 대체코드2가 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환_Dev (
        msg1, msg2, msg3, msg4, msg5, msg6, 택배비용, 전화번호1, 전화번호2, 우편번호, 주소,
        수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 
        품목코드, 옵션명, 박스크기, 출력개수, 송장수량, 
        별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 
        결제수단, 면과세구분, 주문상태, 배송송
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.택배비용, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        (dev.수량 * CAST(spec.대체2수량 AS DECIMAL(10,2))), dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드2, spec.대체코드2품목명, 
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0,
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드2 IS NOT NULL AND spec.대체코드2 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드2 처리', ROW_COUNT());
	
    -- 대체코드3이 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환_Dev (
        msg1, msg2, msg3, msg4, msg5, msg6, 택배비용, 전화번호1, 전화번호2, 우편번호, 주소,
        수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 
        품목코드, 옵션명, 박스크기, 출력개수, 송장수량, 
        별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 
        결제수단, 면과세구분, 주문상태, 배송송
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.택배비용, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        (dev.수량 * CAST(spec.대체3수량 AS DECIMAL(10,2))), dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드3, spec.대체코드3품목명,
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드3 IS NOT NULL AND spec.대체코드3 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드3 처리', ROW_COUNT());
	
    -- 대체코드4가 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환_Dev (
        msg1, msg2, msg3, msg4, msg5, msg6, 택배비용, 전화번호1, 전화번호2, 우편번호, 주소,
        수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 
        품목코드, 옵션명, 박스크기, 출력개수, 송장수량, 
        별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 
        결제수단, 면과세구분, 주문상태, 배송송
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.택배비용, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        (dev.수량 * CAST(spec.대체4수량 AS DECIMAL(10,2))), dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드4, spec.대체코드4품목명,
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드4 IS NOT NULL AND spec.대체코드4 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드4 처리', ROW_COUNT());
	
    -- 원본 합포장 주문 데이터를 삭제
    DELETE FROM 송장출력_사방넷원본변환_Dev
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] 원본 데이터의 주문 데이터 삭제', ROW_COUNT());
	
    COMMIT;

    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 후 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
