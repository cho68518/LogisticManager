DELIMITER $$

DROP PROCEDURE IF EXISTS sp_BusanExtShipmentProcess$$

CREATE PROCEDURE sp_BusanExtShipmentProcess()
BEGIN
    /*--================================================================================
	-- �λ� �ܺ���� ó��
    -- �������_�λ�û��_������ȯ
    --================================================================================*/
    DECLARE done INT DEFAULT FALSE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
		DECLARE error_info TEXT DEFAULT '';
		DECLARE error_code INT DEFAULT 0;
		
		-- MySQL ���� ���� ����
		GET DIAGNOSTICS CONDITION 1
			error_code = MYSQL_ERRNO,
			error_info = MESSAGE_TEXT;
			
        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        SELECT '������ �߻��Ͽ� ��� �۾��� �ѹ�Ǿ����ϴ�.' AS ErrorMessage,
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

	TRUNCATE TABLE �������_�λ�û��_������ȯ;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(�λ�û��) �������_�λ�û����ȯ �ʱ�ȭ', 0);

    INSERT INTO �������_�λ�û��_������ȯ
        (msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2,
         �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�,
         ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�)
    SELECT 
        t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.�����θ�, t.��ȭ��ȣ1, t.��ȭ��ȣ2,
        t.�����ȣ, t.�ּ�, t.�����, t.����, t.��۸޼���, t.�ֹ���ȣ, t.���θ�,
        t.ǰ���ڵ�, t.�ù���, t.�ڽ�ũ��, t.��°���, t.��ǥ1, t.��ǥ2, t.ǰ�񰳼�
    FROM 
        �������_�λ�û��_���� t
    JOIN 
        �������_�λ�û��_�ܺ���� e ON t.ǰ���ڵ� = e.ǰ���ڵ�;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�λ�û��_������ȯ', ROW_COUNT());

		
    DELETE t
    FROM 
        �������_�λ�û��_���� t
    JOIN 
        �������_�λ�û��_�ܺ���� e ON t.ǰ���ڵ� = e.ǰ���ڵ�;	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] �������_�λ�û��_����', ROW_COUNT());	
		
    COMMIT;
    
    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
