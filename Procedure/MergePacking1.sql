DELIMITER $$

CREATE PROCEDURE MergePacking1()
BEGIN
    -- 오류 발생 시 자동으로 롤백하고 상세한 오류 메시지를 반환하는 코드 블록
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- 오류 정보를 저장할 변수 선언
        DECLARE v_sqlstate CHAR(5);
        DECLARE v_error_no INT;
        DECLARE v_error_message TEXT;

        -- 발생한 오류의 상세 정보를 가져옵니다.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- 트랜잭션을 롤백합니다.
        ROLLBACK;

        -- 상세한 오류 메시지를 반환합니다.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message, 
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- 트랜잭션 시작
    START TRANSACTION;

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

    -- 원본 합포장 주문 데이터를 삭제
    DELETE FROM 송장출력_사방넷원본변환_Dev
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);
    
    -- 작업 완료 후 커밋
    COMMIT;

    -- 성공 메시지 반환
    SELECT 'MergePacking1 procedure completed successfully' AS ResultMessage;

END$$

DELIMITER ;
