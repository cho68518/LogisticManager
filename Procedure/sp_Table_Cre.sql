DELIMITER $$

DROP PROCEDURE IF EXISTS sp_Table_Cre$$

CREATE PROCEDURE sp_Table_Cre()
BEGIN
    -- 'gramwonlogis2' 데이터베이스의 송장 관련 테이블 데이터 삭제
    TRUNCATE TABLE gramwonlogis2.송장출력_사방넷원본변환;
    TRUNCATE TABLE gramwonlogis2.송장출력_메세지;
    TRUNCATE TABLE gramwonlogis2.송장출력_메세지부산;
    TRUNCATE TABLE gramwonlogis2.송장출력_메세지청과;
    TRUNCATE TABLE gramwonlogis2.송장출력_특수출력_합포장변경;
    TRUNCATE TABLE gramwonlogis2.송장출력_특수출력_감천분리출고;
    TRUNCATE TABLE gramwonlogis2.송장출력_주문정보;
    TRUNCATE TABLE gramwonlogis2.송장출력_톡딜불가;
    TRUNCATE TABLE gramwonlogis2.송장출력_관리자;
    TRUNCATE TABLE gramwonlogis2.송장출력_관리자_배송메세지;
    TRUNCATE TABLE gramwonlogis2.송장출력_관리자_이름;
	TRUNCATE TABLE gramwonlogis2.별표송장;
	TRUNCATE TABLE gramwonlogis2.송장출력_공통박스;

END $$

DELIMITER ;






CALL sp_Table_Cre();


