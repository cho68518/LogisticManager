DELIMITER $$

DROP PROCEDURE IF EXISTS sp_MergePacking$$

CREATE PROCEDURE sp_MergePacking()
BEGIN
    /*--================================================================================
	-- 합포장 처리
    -- 각 단계별 처리된 행의 수를 반환하도록 수정
    --================================================================================*/
    -- DECLARE 문은 다른 실행문보다 항상 먼저 와야 합니다.
    DECLARE v_sqlstate CHAR(5);
    DECLARE v_error_no INT;
    DECLARE v_error_message TEXT;

    -- 오류 발생 시 자동으로 롤백하고 상세한 오류 메시지를 반환하는 코드 블록
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- 발생한 오류의 상세 정보를 가져옵니다.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- 트랜잭션을 롤백합니다.
        ROLLBACK;

        -- 임시 테이블이 존재하면 삭제합니다.
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

        -- 상세한 오류 메시지를 반환합니다.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message, 
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- DECLARE 문들이 모두 끝난 후 임시 테이블 생성 및 기타 로직을 시작합니다.
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
    INSERT INTO 송장출력_사방넷원본변환 (
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
        (dev.수량 * CAST(spec.대체1수량 AS DECIMAL(10,2))) AS 수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드1, spec.대체코드1품목명, 
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, dev.결제금액, dev.주문금액, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환 dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드1 IS NOT NULL AND spec.대체코드1 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드1 처리', ROW_COUNT());

    -- 대체코드2가 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환 (
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
        (dev.수량 * CAST(spec.대체2수량 AS DECIMAL(10,2))) AS 수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드2, spec.대체코드2품목명, 
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0,
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환 dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드2 IS NOT NULL AND spec.대체코드2 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드2 처리', ROW_COUNT());

    -- 대체코드3이 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환 (
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
        (dev.수량 * CAST(spec.대체3수량 AS DECIMAL(10,2))) AS 수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드3, spec.대체코드3품목명,
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환 dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드3 IS NOT NULL AND spec.대체코드3 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드3 처리', ROW_COUNT());

    -- 대체코드4가 있는 데이터 처리
    INSERT INTO 송장출력_사방넷원본변환 (
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
        (dev.수량 * CAST(spec.대체4수량 AS DECIMAL(10,2))) AS 수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장명, 
        spec.대체코드4, spec.대체코드4품목명,
        NULL, NULL, NULL,
        dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, 0, 0, 
        dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송
    FROM 송장출력_사방넷원본변환 dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
    WHERE spec.대체코드4 IS NOT NULL AND spec.대체코드4 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 대체코드4 처리', ROW_COUNT());

    /*--================================================================================
	-- 합포장 해제 후 후속 처리
    --================================================================================*/
    -- 냉동 렉 위치 입력 (품목등록 테이블 기반)
    UPDATE 송장출력_사방넷원본변환 dev
	  JOIN 품목등록 p ON dev.품목코드 = p.품목코드 
	   SET dev.위치 = p.품목그룹2코드;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 품목 위치 정보 업데이트', ROW_COUNT());

    -- 택배 박스/낱개 구분
    UPDATE 송장출력_사방넷원본변환
	   SET 택배수량1 = CASE WHEN CAST(택배수량 AS UNSIGNED) = 1 THEN '박스' ELSE '낱개' END 
	 WHERE 택배수량 REGEXP '^[0-9]+$';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 택배 박스/낱개 구분', ROW_COUNT());

    -- 출력개수, 택배비용 등 기본값 설정
    UPDATE 송장출력_사방넷원본변환
       SET 택배비용 = IFNULL(택배비용, 2150), 
           박스크기 = IFNULL(박스크기, '극소'), 
           출력개수 = IFNULL(출력개수, 1)
     WHERE 품목코드 IN (SELECT 대체코드1 FROM 송장출력_특수출력_합포장변경 WHERE 대체코드1 IS NOT NULL AND 대체코드1 != ''
                      UNION SELECT 대체코드2 FROM 송장출력_특수출력_합포장변경 WHERE 대체코드2 IS NOT NULL AND 대체코드2 != ''
                      UNION SELECT 대체코드3 FROM 송장출력_특수출력_합포장변경 WHERE 대체코드3 IS NOT NULL AND 대체코드3 != ''
                      UNION SELECT 대체코드4 FROM 송장출력_특수출력_합포장변경 WHERE 대체코드4 IS NOT NULL AND 대체코드4 != '');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 새로 추가된 품목 기본값 설정', ROW_COUNT());

    -- 원본 합포장 주문 데이터를 삭제
    DELETE FROM 송장출력_사방넷원본변환
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] 원본 합포장 데이터(송장출력_사방넷원본변환) 삭제', ROW_COUNT());
    
    -- 작업 완료 후 커밋
    COMMIT;

    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 전 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
