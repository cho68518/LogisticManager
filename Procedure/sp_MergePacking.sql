DELIMITER $$

DROP PROCEDURE IF EXISTS sp_MergePacking$$

CREATE PROCEDURE sp_MergePacking()
BEGIN
    /*--================================================================================
	-- ������ ó��
    -- �� �ܰ躰 ó���� ���� ���� ��ȯ�ϵ��� ����
    --================================================================================*/
    -- DECLARE ���� �ٸ� ���๮���� �׻� ���� �;� �մϴ�.
    DECLARE v_sqlstate CHAR(5);
    DECLARE v_error_no INT;
    DECLARE v_error_message TEXT;

    -- ���� �߻� �� �ڵ����� �ѹ��ϰ� ���� ���� �޽����� ��ȯ�ϴ� �ڵ� ���
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- �߻��� ������ �� ������ �����ɴϴ�.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- Ʈ������� �ѹ��մϴ�.
        ROLLBACK;

        -- �ӽ� ���̺��� �����ϸ� �����մϴ�.
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

        -- ���� ���� �޽����� ��ȯ�մϴ�.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message, 
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- DECLARE ������ ��� ���� �� �ӽ� ���̺� ���� �� ��Ÿ ������ �����մϴ�.
    CREATE TEMPORARY TABLE IF NOT EXISTS sp_execution_log (
		StepID INT AUTO_INCREMENT PRIMARY KEY,
		OperationDescription VARCHAR(255),
		AffectedRows INT
	);

    -- Ʈ����� ����
    START TRANSACTION;

    -- �ӽ� �α� ���̺� �ʱ�ȭ
    TRUNCATE TABLE sp_execution_log;

    -- ��ü�ڵ�1�� �ִ� ������ ó��
    INSERT INTO �������_���ݿ�����ȯ (
        msg1, msg2, msg3, msg4, msg5, msg6, �ù���, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
        ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, 
        ǰ���ڵ�, �ɼǸ�, �ڽ�ũ��, ��°���, �������, 
        ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, 
        ��������, ���������, �ֹ�����, ��ۼ�
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�ù���, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        (dev.���� * CAST(spec.��ü1���� AS DECIMAL(10,2))) AS ����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�, dev.�����, 
        spec.��ü�ڵ�1, spec.��ü�ڵ�1ǰ���, 
        NULL, NULL, NULL,
        dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`, dev.�����ݾ�, dev.�ֹ��ݾ�, 
        dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�
    FROM �������_���ݿ�����ȯ dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
    WHERE spec.��ü�ڵ�1 IS NOT NULL AND spec.��ü�ڵ�1 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ��ü�ڵ�1 ó��', ROW_COUNT());

    -- ��ü�ڵ�2�� �ִ� ������ ó��
    INSERT INTO �������_���ݿ�����ȯ (
        msg1, msg2, msg3, msg4, msg5, msg6, �ù���, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
        ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, 
        ǰ���ڵ�, �ɼǸ�, �ڽ�ũ��, ��°���, �������, 
        ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, 
        ��������, ���������, �ֹ�����, ��ۼ�
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�ù���, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        (dev.���� * CAST(spec.��ü2���� AS DECIMAL(10,2))) AS ����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�, dev.�����, 
        spec.��ü�ڵ�2, spec.��ü�ڵ�2ǰ���, 
        NULL, NULL, NULL,
        dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`, 0, 0,
        dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�
    FROM �������_���ݿ�����ȯ dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
    WHERE spec.��ü�ڵ�2 IS NOT NULL AND spec.��ü�ڵ�2 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ��ü�ڵ�2 ó��', ROW_COUNT());

    -- ��ü�ڵ�3�� �ִ� ������ ó��
    INSERT INTO �������_���ݿ�����ȯ (
        msg1, msg2, msg3, msg4, msg5, msg6, �ù���, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
        ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, 
        ǰ���ڵ�, �ɼǸ�, �ڽ�ũ��, ��°���, �������, 
        ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, 
        ��������, ���������, �ֹ�����, ��ۼ�
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�ù���, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        (dev.���� * CAST(spec.��ü3���� AS DECIMAL(10,2))) AS ����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�, dev.�����, 
        spec.��ü�ڵ�3, spec.��ü�ڵ�3ǰ���,
        NULL, NULL, NULL,
        dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`, 0, 0, 
        dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�
    FROM �������_���ݿ�����ȯ dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
    WHERE spec.��ü�ڵ�3 IS NOT NULL AND spec.��ü�ڵ�3 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ��ü�ڵ�3 ó��', ROW_COUNT());

    -- ��ü�ڵ�4�� �ִ� ������ ó��
    INSERT INTO �������_���ݿ�����ȯ (
        msg1, msg2, msg3, msg4, msg5, msg6, �ù���, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
        ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, 
        ǰ���ڵ�, �ɼǸ�, �ڽ�ũ��, ��°���, �������, 
        ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, 
        ��������, ���������, �ֹ�����, ��ۼ�
    )
    SELECT
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�ù���, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        (dev.���� * CAST(spec.��ü4���� AS DECIMAL(10,2))) AS ����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�, dev.�����, 
        spec.��ü�ڵ�4, spec.��ü�ڵ�4ǰ���,
        NULL, NULL, NULL,
        dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`, 0, 0, 
        dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�
    FROM �������_���ݿ�����ȯ dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
    WHERE spec.��ü�ڵ�4 IS NOT NULL AND spec.��ü�ڵ�4 != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ��ü�ڵ�4 ó��', ROW_COUNT());

    /*--================================================================================
	-- ������ ���� �� �ļ� ó��
    --================================================================================*/
    -- �õ� �� ��ġ �Է� (ǰ���� ���̺� ���)
    UPDATE �������_���ݿ�����ȯ dev
	  JOIN ǰ���� p ON dev.ǰ���ڵ� = p.ǰ���ڵ� 
	   SET dev.��ġ = p.ǰ��׷�2�ڵ�;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ǰ�� ��ġ ���� ������Ʈ', ROW_COUNT());

    -- �ù� �ڽ�/���� ����
    UPDATE �������_���ݿ�����ȯ
	   SET �ù����1 = CASE WHEN CAST(�ù���� AS UNSIGNED) = 1 THEN '�ڽ�' ELSE '����' END 
	 WHERE �ù���� REGEXP '^[0-9]+$';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ù� �ڽ�/���� ����', ROW_COUNT());

    -- ��°���, �ù��� �� �⺻�� ����
    UPDATE �������_���ݿ�����ȯ
       SET �ù��� = IFNULL(�ù���, 2150), 
           �ڽ�ũ�� = IFNULL(�ڽ�ũ��, '�ؼ�'), 
           ��°��� = IFNULL(��°���, 1)
     WHERE ǰ���ڵ� IN (SELECT ��ü�ڵ�1 FROM �������_Ư�����_�����庯�� WHERE ��ü�ڵ�1 IS NOT NULL AND ��ü�ڵ�1 != ''
                      UNION SELECT ��ü�ڵ�2 FROM �������_Ư�����_�����庯�� WHERE ��ü�ڵ�2 IS NOT NULL AND ��ü�ڵ�2 != ''
                      UNION SELECT ��ü�ڵ�3 FROM �������_Ư�����_�����庯�� WHERE ��ü�ڵ�3 IS NOT NULL AND ��ü�ڵ�3 != ''
                      UNION SELECT ��ü�ڵ�4 FROM �������_Ư�����_�����庯�� WHERE ��ü�ڵ�4 IS NOT NULL AND ��ü�ڵ�4 != '');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ���� �߰��� ǰ�� �⺻�� ����', ROW_COUNT());

    -- ���� ������ �ֹ� �����͸� ����
    DELETE FROM �������_���ݿ�����ȯ
    WHERE ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_�����庯��);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] ���� ������ ������(�������_���ݿ�����ȯ) ����', ROW_COUNT());
    
    -- �۾� �Ϸ� �� Ŀ��
    COMMIT;

    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
