DELIMITER $$

CREATE PROCEDURE MergePacking1()
BEGIN
    -- 변수 선언
    DECLARE v_emp_id INT;
    DECLARE v_emp_name VARCHAR(100);
    DECLARE v_salary DECIMAL(10,2);

	DECLARE v_receiver_name         VARCHAR(255); -- 수취인명
	DECLARE v_phone1                VARCHAR(255); -- 전화번호1
	DECLARE v_phone2                VARCHAR(255); -- 전화번호2
	DECLARE v_zipcode               VARCHAR(255); -- 우편번호
	DECLARE v_address               VARCHAR(255); -- 주소
	DECLARE v_option_name           VARCHAR(255); -- 옵션명
	DECLARE v_quantity              INT(11);      -- 수량
	DECLARE v_delivery_msg          VARCHAR(255); -- 배송메세지
	DECLARE v_order_no              VARCHAR(255); -- 주문번호
	DECLARE v_shop_name             VARCHAR(255); -- 쇼핑몰
	DECLARE v_collected_at          DATETIME;     -- 수집시간
	DECLARE v_invoice_name          VARCHAR(255); -- 송장명
	DECLARE v_item_code             VARCHAR(255); -- 품목코드
	DECLARE v_shipping_cost         VARCHAR(255); -- 택배비용
	DECLARE v_box_size              VARCHAR(255); -- 박스크기
	DECLARE v_print_count           VARCHAR(255); -- 출력개수
	DECLARE v_invoice_qty           VARCHAR(255); -- 송장수량
	DECLARE v_star1                 VARCHAR(255); -- 별표1
	DECLARE v_star2                 VARCHAR(255); -- 별표2
	DECLARE v_item_count            VARCHAR(255); -- 품목개수
	DECLARE v_shipping_qty          VARCHAR(255); -- 택배수량
	DECLARE v_shipping_qty1         VARCHAR(255); -- 택배수량1
	DECLARE v_shipping_qty_total    VARCHAR(255); -- 택배수량합산
	DECLARE v_invoice_divider       VARCHAR(255); -- 송장구분자
	DECLARE v_invoice_div           VARCHAR(256); -- 송장구분
	DECLARE v_invoice_div_final     VARCHAR(256); -- 송장구분최종
	DECLARE v_location              VARCHAR(255); -- 위치
	DECLARE v_location_conv         VARCHAR(255); -- 위치변환
	DECLARE v_order_no_shop         VARCHAR(255); -- 주문번호(쇼핑몰)
	DECLARE v_payment_amount        VARCHAR(255); -- 결제금액
	DECLARE v_order_amount          VARCHAR(255); -- 주문금액
	DECLARE v_payment_method        VARCHAR(255); -- 결제수단
	DECLARE v_tax_type              VARCHAR(255); -- 면과세구분
	DECLARE v_order_status          VARCHAR(255); -- 주문상태
	DECLARE v_shipping_send         VARCHAR(255); -- 배송송

    DECLARE v_item_name             VARCHAR(255); -- 품목명
	DECLARE v_alt_code1             VARCHAR(255); -- 대체코드1
	DECLARE v_alt_code1_name        VARCHAR(255); -- 대체코드1품목명
	DECLARE v_alt1_qty              VARCHAR(255); -- 대체1수량
	DECLARE v_alt_code2             VARCHAR(255); -- 대체코드2
	DECLARE v_alt_code2_name        VARCHAR(255); -- 대체코드2품목명
	DECLARE v_alt2_qty              VARCHAR(255); -- 대체2수량
	DECLARE v_alt_code3             VARCHAR(255); -- 대체코드3
	DECLARE v_alt_code3_name        VARCHAR(255); -- 대체코드3품목명
	DECLARE v_alt3_qty              VARCHAR(255); -- 대체3수량
	DECLARE v_alt_code4             VARCHAR(255); -- 대체코드4
	DECLARE v_alt_code4_name        VARCHAR(255); -- 대체코드4품목명
	DECLARE v_alt4_qty              VARCHAR(255); -- 대체4수량

    -- 커서 종료 감지용 변수
    DECLARE done INT DEFAULT 0;

    -- 커서 선언 (조건 없이 전체 employees 테이블 읽기)
	DECLARE cur1 CURSOR FOR	
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명,
			   수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 박스크기, 출력개수, 송장수량,
		   	   별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치,
			   위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
	     FROM 송장출력_사방넷원본변환_합포장변환;

	DECLARE cur2 CURSOR FOR	
		SELECT 품목코드, 품목명, 
		       대체코드1, 대체코드1품목명, 대체1수량,
		   	   대체코드2, 대체코드2품목명, 대체2수량,
			   대체코드3, 대체코드3품목명, 대체3수량,
			   대체코드4, 대체코드4품목명, 대체4수량
	     FROM 송장출력_특수출력_합포장변경;
		 
    -- 커서 루프 종료 핸들러
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    ---------------------------------------------------------------------
	-- 전처리
    ---------------------------------------------------------------------
	-- 합포장 변경 주문접수건만 적재
	TRUNCATE TABLE 송장출력_사방넷원본변환_합포장변환;
	
    INSERT INTO 송장출력_사방넷원본변환_합포장변환
    SELECT * FROM 송장출력_사방넷원본변환_Dev
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);

    -------------------------------------------------------------------------------------------------------
    -- 합포장 관련 특수처리 및 적재
    -------------------------------------------------------------------------------------------------------	
    -- 합포장 대상 주문건은 삭제 (마지막 처리시 대체품목건으로 주문건 Insert)
    DELETE FROM 송장출력_사방넷원본변환_Dev
    WHERE 품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_합포장변경);

    -- 합포장변환1 (대체코드2 주문접수건만 적재)
	TRUNCATE TABLE 송장출력_사방넷원본변환_합포장변환1;
	
    INSERT INTO 송장출력_사방넷원본변환_합포장변환1
    SELECT a.*
      FROM 송장출력_사방넷원본변환_합포장변환 a
      JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
     WHERE b.대체코드2 != '';

    -- 합포장변환2 (대체코드3 주문접수건만 적재)
	TRUNCATE TABLE 송장출력_사방넷원본변환_합포장변환2;
	
    INSERT INTO 송장출력_사방넷원본변환_합포장변환2
    SELECT a.*
      FROM 송장출력_사방넷원본변환_합포장변환 a
      JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
     WHERE b.대체코드3 != '';

    -- 합포장변환3 (대체코드4 주문접수건만 적재)	
	TRUNCATE TABLE 송장출력_사방넷원본변환_합포장변환3;	
	
    INSERT INTO 송장출력_사방넷원본변환_합포장변환3
    SELECT a.*
      FROM 송장출력_사방넷원본변환_합포장변환 a
      JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
     WHERE b.대체코드4 != '';		 

    -- 합포장변환 11		 
    UPDATE 송장출력_사방넷원본변환_합포장변환 a
      JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
       SET 
           a.택배비용 = b.대체코드1,
           a.박스크기 = b.대체코드1품목명,
           a.출력개수 = b.대체1수량
     WHERE b.대체코드1 != '';		 

   -- 합포장변환 22		 
   UPDATE 송장출력_사방넷원본변환_합포장변환1 a
     JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
      SET 
          a.택배비용 = b.대체코드2,
          a.박스크기 = b.대체코드2품목명,
          a.출력개수 = b.대체2수량
    WHERE b.대체코드2 != '';
		 
   -- 합포장변환 33
   UPDATE 송장출력_사방넷원본변환_합포장변환2 a
     JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
      SET 
          a.택배비용 = b.대체코드3,
          a.박스크기 = b.대체코드3품목명,
          a.출력개수 = b.대체3수량
    WHERE b.대체코드3 != '';
		 
   -- 합포장변환 44
   UPDATE 송장출력_사방넷원본변환_합포장변환3 a
     JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
      SET 
          a.택배비용 = b.대체코드4,
          a.박스크기 = b.대체코드4품목명,
          a.출력개수 = b.대체4수량
    WHERE b.대체코드4 != '';

    -- 박스크기가 비어있는 행 삭제
	DELETE FROM 송장출력_사방넷원본변환_합포장변환1
     WHERE 박스크기 IS NULL OR 박스크기 = '';
		 
    DELETE FROM 송장출력_사방넷원본변환_합포장변환2
     WHERE 박스크기 IS NULL OR 박스크기 = '';
		 
    DELETE FROM 송장출력_사방넷원본변환_합포장변환3
     WHERE 박스크기 IS NULL OR 박스크기 = '';
		 
    UPDATE 송장출력_사방넷원본변환_합포장변환
       SET 
           품목코드 = 택배비용,
           송장명   = 박스크기,
           수량     = 수량 * 출력개수,
           택배비용 = NULL,
           박스크기 = NULL,
           출력개수 = NULL;

    UPDATE 송장출력_사방넷원본변환_합포장변환1
       SET 
           품목코드 = 택배비용,
           송장명   = 박스크기,
           수량     = 수량 * 출력개수,
           택배비용 = NULL,
           박스크기 = NULL,
           출력개수 = NULL;
		   
    UPDATE 송장출력_사방넷원본변환_합포장변환2
       SET 
           품목코드 = 택배비용,
           송장명   = 박스크기,
           수량     = 수량 * 출력개수,
           택배비용 = NULL,
           박스크기 = NULL,
           출력개수 = NULL;
		   
    UPDATE 송장출력_사방넷원본변환_합포장변환3
       SET 
           품목코드 = 택배비용,
           송장명   = 박스크기,
           수량     = 수량 * 출력개수,
           택배비용 = NULL,
           박스크기 = NULL,
           출력개수 = NULL;	
		   
    UPDATE 송장출력_사방넷원본변환_합포장변환1 s
        JOIN 품목등록 p ON s.품목코드 = p.품목코드
        SET s.결제금액 = 0,
            s.주문금액 = 0;
			
    UPDATE 송장출력_사방넷원본변환_합포장변환2 s
        JOIN 품목등록 p ON s.품목코드 = p.품목코드
        SET s.결제금액 = 0,
            s.주문금액 = 0;
			
    UPDATE 송장출력_사방넷원본변환_합포장변환3 s
        JOIN 품목등록 p ON s.품목코드 = p.품목코드
        SET s.결제금액 = 0,
            s.주문금액 = 0;			
		   
		   
    -- 데이터 병합 및 삽입 시작		   
	TRUNCATE TABLE 송장출력_사방넷원본변환_합포장변환결과;		   
		   
    INSERT INTO 송장출력_사방넷원본변환_합포장변환결과 (
        msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
        옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
        박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
    )
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
        옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
        박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
    FROM 송장출력_사방넷원본변환_합포장변환
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
        옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
        박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
    FROM 송장출력_사방넷원본변환_합포장변환1
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
        옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
        박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
    FROM 송장출력_사방넷원본변환_합포장변환2
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
        옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
        박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
        송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
    FROM 송장출력_사방넷원본변환_합포장변환3;

    -------------------------------------------------------------------------------------------------------
    -- 송장출력_사방넷원본변환(주문수집 원본테이블) 테이블에 데이터 추가
    -------------------------------------------------------------------------------------------------------	
   INSERT INTO 송장출력_사방넷원본변환 (
       msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
       옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
       박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
       송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
   )
   SELECT 
       msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
       옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 
       박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산, 
       송장구분자, 송장구분, 송장구분최종, 위치, 위치변환, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
   FROM 송장출력_사방넷원본변환_합포장변환결과;







    ---------------------------------------------------------------------
    -- 커서 열기
    ---------------------------------------------------------------------	
    /*OPEN cur2;

    read_loop: LOOP
        -- 커서에서 한 행 읽기
        --FETCH cur1 INTO v_receiver_name, v_phone1, v_phone2, v_zipcode, v_address, v_option_name,
        --                v_quantity, v_delivery_msg, v_order_no, v_shop_name, v_collected_at,
        --                v_invoice_name, v_item_code, v_shipping_cost, v_box_size, v_print_count,
        --                v_invoice_qty, v_star1, v_star2, v_item_count, v_shipping_qty, v_shipping_qty1,
        --                v_shipping_qty_total, v_invoice_divider, v_invoice_div, v_invoice_div_final,
        --                v_location, v_location_conv, v_order_no_shop, v_payment_amount, v_order_amount,
        --                v_payment_method, v_tax_type, v_order_status, v_shipping_send;

        FETCH cur2 INTO v_item_code, v_item_name, 
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty;


        -- 더 이상 읽을 데이터가 없으면 루프 종료
        IF done = 1 THEN
            LEAVE read_loop;
        END IF;






        -- 조건 처리: 월급이 5000 이상인 경우만 다른 테이블에 저장
        --IF v_salary >= 5000 THEN
        --    INSERT INTO high_salary_employees(emp_id, emp_name, salary, processed_at)
        --    VALUES (v_emp_id, v_emp_name, v_salary, NOW());
        --END IF;

		SELECT 대체코드1, 대체코드1품목명, 대체1수량,
		       대체코드2, 대체코드2품목명, 대체2수량,
			   대체코드3, 대체코드3품목명, 대체3수량,
			   대체코드4, 대체코드4품목명, 대체4수량
	      INTO v_alt_code1, v_alt_code1_name, v_alt_code1_qty,
		       v_alt_code2, v_alt_code2_name, v_alt_code2_qty,
			   v_alt_code3, v_alt_code3_name, v_alt_code3_qty,
			   v_alt_code4, v_alt_code4_name, v_alt_code4_qty
		  FROM 송장출력_특수출력_합포장변경
		 WHERE ;
		
		UPDATE 송장출력_사방넷원본변환_합포장변환
          JOIN 송장출력_특수출력_합포장변경 b ON a.품목코드 = b.품목코드
           SET 
               a.택배비용 = b.대체코드1,
               a.박스크기 = b.대체코드1품목명,
               a.출력개수 = b.대체1수량
         WHERE 품목코드 = v_item_code     v_item_code = b.대체코드1 != ''
		
		
		
		
		
		
		
		
		
    END LOOP;

    ---------------------------------------------------------------------
    CLOSE cur2;
    ---------------------------------------------------------------------
	
	*/
	
	
	
END$$

DELIMITER ;