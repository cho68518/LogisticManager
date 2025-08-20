DELIMITER $$

DROP PROCEDURE IF EXISTS sp_InvoiceFinalProcess$$

CREATE PROCEDURE sp_InvoiceFinalProcess()
BEGIN
    /*--================================================================================
	-- ������� ���� ó��
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

    START TRANSACTION;

    TRUNCATE TABLE sp_execution_log;

    /*--================================================================================
	-- (������� ����) �� ���̺��� �����͸� �������_���� ���̺� insert
    --================================================================================*/
	TRUNCATE TABLE �������_����;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(������� ����) �������_���� �ʱ�ȭ', 0);
	
    INSERT INTO �������_����
		SELECT * FROM �������_���õ�_����
		UNION ALL
		SELECT * FROM �������_������_����
		UNION ALL
		SELECT * FROM �������_�λ�û��_����
		UNION ALL
		SELECT * FROM �������_�������_����
		UNION ALL
		SELECT * FROM �������_����õ�_����
		UNION ALL
		SELECT * FROM �������_��õ�õ�_����;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_����', ROW_COUNT());
		
    COMMIT;
    
    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
