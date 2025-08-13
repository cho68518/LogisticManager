DELIMITER $$

CREATE PROCEDURE MergePacking()
BEGIN
    -- 오류 발생 시 롤백 및 상세 오류 메시지 출력을 위한 핸들러 선언
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- 오류 정보를 담을 변수 선언
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

        -- 상세 오류 정보를 포함한 메시지를 출력합니다.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message, 
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- 트랜잭션 시작
    START TRANSACTION;

    -- 처리 대상 원본 주문 데이터를 CTE로 정의
 WITH PackingSource AS (
    -- dev.* 대신 필요한 모든 컬럼을 명시적으로 지정.
    SELECT 
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.수취인명, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        dev.옵션명, dev.수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간,
        dev.송장수량, dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`,
        dev.결제금액, dev.주문금액, dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송,
        -- spec 테이블에서는 대체코드 관련 컬럼만 가져옵니다.
        spec.대체코드1, spec.대체코드1품목명, spec.대체1수량,
        spec.대체코드2, spec.대체코드2품목명, spec.대체2수량,
        spec.대체코드3, spec.대체코드3품목명, spec.대체3수량,
        spec.대체코드4, spec.대체코드4품목명, spec.대체4수량
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
)
-- 합포장 변환 데이터를 원본 테이블에 INSERT
-- CTE(WITH)를 사용하는 대신, FROM 절에 직접 서브쿼리를 사용합니다.
INSERT INTO 송장출력_사방넷원본변환_Dev (
    msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소,
    옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 
    송장명, 품목코드, 택배비용, 박스크기, 출력개수, 
    송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
    송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 
    결제수단, 면과세구분, 주문상태, 배송송
)
SELECT
    sub.msg1, sub.msg2, sub.msg3, sub.msg4, sub.msg5, sub.msg6, sub.수취인명, sub.전화번호1, sub.전화번호2, sub.우편번호, sub.주소,
    sub.옵션명, (sub.수량 * sub.대체1수량), sub.배송메세지, sub.주문번호, sub.쇼핑몰, sub.수집시간, 
    sub.대체코드1품목명, sub.대체코드1, 
    NULL, NULL, NULL,
    sub.송장수량, sub.별표1, sub.별표2, sub.품목개수, sub.택배수량, sub.택배수량1, sub.택배수량합산,
    sub.송장구분자, sub.송장구분, sub.송장구분최종, sub.위치, sub.위치변환, sub.`주문번호(쇼핑몰)`, sub.결제금액, sub.주문금액, 
    sub.결제수단, sub.면과세구분, sub.주문상태, sub.배송송
FROM (
    -- 이 부분이 기존의 CTE(PackingSource)와 동일한 역할을 합니다.
    SELECT 
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.수취인명, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소,
        dev.옵션명, dev.수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간,
        dev.송장수량, dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산,
        dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`,
        dev.결제금액, dev.주문금액, dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송,
        spec.대체코드1, spec.대체코드1품목명, spec.대체1수량,
        spec.대체코드2, spec.대체코드2품목명, spec.대체2수량,
        spec.대체코드3, spec.대체코드3품목명, spec.대체3수량,
        spec.대체코드4, spec.대체코드4품목명, spec.대체4수량
    FROM 송장출력_사방넷원본변환_Dev dev
    JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드
) AS sub -- 서브쿼리에는 반드시 별칭(AS)이 필요합니다.
WHERE sub.대체코드1 IS NOT NULL AND sub.대체코드1 != ''
UNION ALL
-- (이하 UNION ALL 구문들도 모두 sub.컬럼명 형태로 수정)
SELECT
    sub.msg1, sub.msg2, sub.msg3, sub.msg4, sub.msg5, sub.msg6, sub.수취인명, sub.전화번호1, sub.전화번호2, sub.우편번호, sub.주소,
    sub.옵션명, (sub.수량 * sub.대체2수량), sub.배송메세지, sub.주문번호, sub.쇼핑몰, sub.수집시간, 
    sub.대체코드2품목명, sub.대체코드2, 
    NULL, NULL, NULL,
    sub.송장수량, sub.별표1, sub.별표2, sub.품목개수, sub.택배수량, sub.택배수량1, sub.택배수량합산,
    sub.송장구분자, sub.송장구분, sub.송장구분최종, sub.위치, sub.위치변환, sub.`주문번호(쇼핑몰)`, 0, 0,
    sub.결제수단, sub.면과세구분, sub.주문상태, sub.배송송
FROM (SELECT dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, dev.수취인명, dev.전화번호1, dev.전화번호2, dev.우편번호, dev.주소, dev.옵션명, dev.수량, dev.배송메세지, dev.주문번호, dev.쇼핑몰, dev.수집시간, dev.송장수량, dev.별표1, dev.별표2, dev.품목개수, dev.택배수량, dev.택배수량1, dev.택배수량합산, dev.송장구분자, dev.송장구분, dev.송장구분최종, dev.위치, dev.위치변환, dev.`주문번호(쇼핑몰)`, dev.결제금액, dev.주문금액, dev.결제수단, dev.면과세구분, dev.주문상태, dev.배송송, spec.대체코드1, spec.대체코드1품목명, spec.대체1수량, spec.대체코드2, spec.대체코드2품목명, spec.대체2수량, spec.대체코드3, spec.대체코드3품목명, spec.대체3수량, spec.대체코드4, spec.대체코드4품목명, spec.대체4수량 FROM 송장출력_사방넷원본변환_Dev dev JOIN 송장출력_특수출력_합포장변경 spec ON dev.품목코드 = spec.품목코드) AS sub
WHERE sub.대체코드2 IS NOT NULL AND sub.대체코드2 != ''
-- (이하 UNION ALL 3, 4도 동일한 패턴으로 수정)
;

    -- 원본 주문 데이터 삭제
    DELETE FROM 송장출력_사방넷원본변환_Dev
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);
    
    -- 작업 성공 시 영구 저장
    COMMIT;
	
	

END$$

DELIMITER ;