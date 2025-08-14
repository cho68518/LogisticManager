DELIMITER $$

DROP PROCEDURE IF EXISTS sp_ProcessStarInvoice$$

CREATE PROCEDURE sp_ProcessStarInvoice()
BEGIN
    /*--================================================================================
	-- ������°��� ó��
    -- �� �ܰ躰 ó���� ���� ���� ��ȯ�ϵ��� ����
    --==================================================================================*/
	DECLARE var_�����ڸ޼��� VARCHAR(255);
	DECLARE done_msg BOOLEAN DEFAULT FALSE;
    DECLARE loop_updated_rows INT DEFAULT 0;

	DECLARE cur_msg CURSOR FOR SELECT TRIM(��۸޼���) FROM �������_������_��۸޼��� WHERE TRIM(��۸޼���) != '';
	
	DECLARE CONTINUE HANDLER FOR NOT FOUND SET done_msg = TRUE;
	
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

    TRUNCATE TABLE �������_������;
    TRUNCATE TABLE �������_������_��۸޼���;
    TRUNCATE TABLE �������_������_�̸�;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('������ ���̺� 3�� �ʱ�ȭ (TRUNCATE)', 0);

    -- ǰ���ڵ� �Ǵ� ��ǰ��
    INSERT INTO �������_������ (ǰ����ǰ�ڵ�, ��ǰ��)
         SELECT TRIM(ǰ���ڵ�), TRIM(��ǰ��) FROM ��ǥ����
          WHERE (TRIM(ǰ���ڵ�) IS NOT NULL AND TRIM(ǰ���ڵ�) != '') OR (TRIM(��ǰ��) IS NOT NULL AND TRIM(��ǰ��) != '');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ������_ǰ����ǰ�ڵ�/��ǰ��', ROW_COUNT());

    -- ��۸޼���
    INSERT INTO �������_������_��۸޼��� (��۸޼���)
         SELECT TRIM(��۸޼���) FROM ��ǥ����
          WHERE TRIM(��۸޼���) IS NOT NULL AND TRIM(��۸޼���) != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ������_��۸޼���', ROW_COUNT());

    -- �����θ�
    INSERT INTO �������_������_�̸� (�����θ�)
         SELECT TRIM(�����θ�) FROM ��ǥ����
          WHERE TRIM(�����θ�) IS NOT NULL AND TRIM(�����θ�) != '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ������_�����θ�', ROW_COUNT());

    /*--=============================================================
    -- ��۸޼������� ��ǥ�����
    --=============================================================*/
    UPDATE �������_���ݿ�����ȯ_Dev
       SET ��۸޼��� = REPLACE(��۸޼���, '��', '')
     WHERE ��۸޼��� LIKE '%��%';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��۸޼��� ��ǥ ����', ROW_COUNT());

	/*--=============================================================
	-- ��ǥ ǰ���ڵ� �������Է�
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ AS s 
	  JOIN �������_������ AS a ON s.ǰ���ڵ� = a.ǰ����ǰ�ڵ� 
	   SET s.��ǥ1 = '�ڡڡ�';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��ǥ ǰ���ڵ� �Է�', ROW_COUNT());

    /*--============================================================================================================
    -- �������_���ݿ�����ȯ ���̺��� ��۸޼����� �������_������_��۸޼��� ���̺��� ��۸޼�����
    --    TRIM �Լ��� �̿��� ��ĭ�� ������ �� ���ԵǴ� ��쿡
    --    �ش��ϴ� ���� ã��, �׿� �ش��ϴ� �������_���ݿ�����ȯ ���̺��� ��ǥ1 �÷��� ��ǥ �� ���� �־��ݴϴ�.
    --============================================================================================================*/
	OPEN cur_msg;

	msg_loop: LOOP
		FETCH cur_msg INTO var_�����ڸ޼���;
		IF done_msg THEN
			LEAVE msg_loop;
		END IF;

		-- FULLTEXT �ε����� ����Ͽ� �ſ� ������ �˻��ϰ� ������Ʈ�մϴ�.
		UPDATE �������_���ݿ�����ȯ
		   SET ��ǥ1 = '�ڡڡ�'
		 WHERE MATCH(��۸޼���) AGAINST(var_�����ڸ޼��� IN BOOLEAN MODE);
        SET loop_updated_rows = loop_updated_rows + ROW_COUNT();
	END LOOP;

	CLOSE cur_msg;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��۸޼��� �˻����� ��ǥ �Է� (���� ����)', loop_updated_rows);

    /*--=============================================================
    -- ��ǥ ������ �������Է�
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ AS s 
	  JOIN �������_������_�̸� AS a 
	    ON s.�����θ� = a.�����θ� 
	   SET s.��ǥ1 = '�ڡڡ�';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��ǥ ������ �Է�', ROW_COUNT());

    /*--=============================================================	
    -- ��ǥ ���ֵ�
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ 
	   SET ��ǥ2 = '����' 
	 WHERE �ּ� LIKE '%����Ư��%' OR �ּ� LIKE '%���� ����%';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ���ֵ� �ּ� ǥ��', ROW_COUNT());

    /*--=============================================================	
    -- ��ǥ �� ���� ��ŷ
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ AS t1 
	  JOIN (SELECT �ּ�, �����θ� FROM �������_���ݿ�����ȯ WHERE ��ǥ1 = '�ڡڡ�') AS t2 
        ON t1.�ּ� = t2.�ּ� AND t1.�����θ� = t2.�����θ� 
 	   SET t1.��ǥ1 = '�ڡڡ�';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��ǥ �� ���� ��ŷ', ROW_COUNT());

    /*--=============================================================		
    -- �ڽ���ǰ ��Ī����	
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ 
	   SET ����� = CONCAT('�ɢʢ� ', �����) 
	 WHERE ����� LIKE '%�ڽ�%';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ���ǰ ��Ī ����', ROW_COUNT());

	/*--=============================================================		
	-- �ù� �ڽ� ���� ������
    --===============================================================*/
    -- �ù���� �÷��� ���� 1�� ��쿡�� '�ڽ�', �� ���� ��쿡�� '����'�� ����.
    UPDATE �������_���ݿ�����ȯ 
	   SET �ù����1 = CASE WHEN CAST(�ù���� AS UNSIGNED) = 1 THEN '�ڽ�' ELSE '����' END 
	 WHERE �ù���� REGEXP '^[0-9]+$';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ù� �ڽ�/���� ����', ROW_COUNT());

	/*--=============================================================		
	-- ���� ��������� ����
    --===============================================================*/
    UPDATE �������_���ݿ�����ȯ 
	   SET `���屸������` = CONCAT(`���屸��`, IFNULL(`�ù����1`, ''));
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ���屸������ ����', ROW_COUNT());

    /*--=============================================================		
    -- �õ� �� ��ġ �Է�
    --===============================================================*/
    -- ǰ���� ���̺�� �������_���ݿ�����ȯ ���̺��� JOIN�Ͽ�
    -- 'ǰ���ڵ�'�� ��ġ�ϴ� ���, 'ǰ����' ���̺��� 'ǰ��׷�2�ڵ�' ������
    -- '�������_���ݿ�����ȯ' ���̺��� '��ġ' �ʵ带 �� ���� ������Ʈ.
    UPDATE �������_���ݿ�����ȯ AS s 
	  JOIN ǰ���� AS p 
	    ON s.ǰ���ڵ� = p.ǰ���ڵ� 
	   SET s.��ġ     = p.ǰ��׷�2�ڵ�;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �õ� �� ��ġ �Է�', ROW_COUNT());

	/*--=============================================================	
	-- �� ��ġ ������Ʈ
    --=============================================================*/
    UPDATE �������_���ݿ�����ȯ 
	   SET ��ġ = '99-1' 
	 WHERE ��ġ IS NULL OR ��ġ = '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �� ��ġ�� 99-1�� ����', ROW_COUNT());

	/*--=============================================================
	-- '��ġ��ȯ' ������Ʈ
    --=============================================================*/
    -- '��ġ' ���� ���� '��ġ��ȯ' �÷��� ���Ǻη� ������Ʈ.
    UPDATE �������_���ݿ�����ȯ
       SET ��ġ��ȯ =
           CASE
               WHEN ��ġ = '��2' THEN CONCAT(��ġ, ǰ���ڵ�, LPAD(����, 2, '0'), �����θ�, �ּ�, ��ȭ��ȣ1)
               ELSE CONCAT(��ġ, '0', ǰ���ڵ�, LPAD(����, 2, '0'), �����θ�, �ּ�, ��ȭ��ȣ1)
           END;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ��ġ��ȯ ����', ROW_COUNT());

    /*--=============================================================
    -- �õ� ���� ��ǰ �� ���� ǰ�� ������ó��
    --=============================================================*/
    UPDATE �������_���ݿ�����ȯ
       SET ��ġ = '��3'
     WHERE ��ġ = '��2' AND �ù���� BETWEEN 20 AND 1000000;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ������ ó�� (��3)', ROW_COUNT());

    /*--=============================================================
    -- �õ�â�� ����ǰ ����и��Է�
    --=============================================================*/
    UPDATE �������_���ݿ�����ȯ
       SET �ּ� = CONCAT(�ּ�, '��')
     WHERE ��ġ = '��2';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �õ�â�� ����ǰ ����и� (�ּҿ� �� �߰�)', ROW_COUNT());

    /*--=============================================================
    -- ����ڽ� �з��۾�
    --=============================================================*/
    TRUNCATE TABLE �������_����ڽ�;
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('����ڽ� ���̺� �ʱ�ȭ (TRUNCATE)', 0);
	
	-- ������ 2 �̻��� �ุ�� �����Ͽ� �߰�
    INSERT INTO �������_����ڽ� (
	            msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�, ����, 
				��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, 
				��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, 
				��ġ��ȯ)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�, ����, 
		        ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, 
				��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, 
				��ġ��ȯ
           FROM �������_���ݿ�����ȯ
          WHERE �ù����1 = '�ڽ�';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] ����ڽ� ������ ����', ROW_COUNT());

	/*--=============================================================	
	-- �ڽ� ����ø���
    --=============================================================*/
    INSERT INTO �������_����ڽ� (
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2,
        �����ȣ, �ּ�, �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�,
        �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2,
        ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��,
        ���屸������, ��ġ, ��ġ��ȯ
        )
		SELECT
			t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.�����θ�, t.��ȭ��ȣ1, t.��ȭ��ȣ2,
			t.�����ȣ, t.�ּ�, t.�ɼǸ�, t.����, t.��۸޼���, t.�ֹ���ȣ, t.���θ�, t.�����ð�,
			t.�����, t.ǰ���ڵ�, t.�ù���, t.�ڽ�ũ��, t.��°���, t.�������, t.��ǥ1, t.��ǥ2,
			t.ǰ�񰳼�, t.�ù����, t.�ù����1, t.�ù�����ջ�, t.���屸����, t.���屸��,
			t.���屸������, t.��ġ, t.��ġ��ȯ
		FROM
			�������_����ڽ� AS t
		-- ���� ���̺�� �����Ͽ� ���� ����
		JOIN
			Numbers AS n ON n.n <= (t.���� - 1)
		WHERE
			t.���� > 1;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �ڽ� ������ŭ �� ����', ROW_COUNT());

    /*--=============================================================	
    -- �ڽ��ֹ� ���� �ű��	1
    --=============================================================*/
    -- ����� ������ �̿��� UPDATE
    -- ������������ ��ġ��ȯ, id ������ ���� ��,
    -- @current_location ������ �׷��� �ٲ�� ���� �����Ͽ�
    -- @seq ������ 1�� �ʱ�ȭ�ϰų� 1�� ������Ŵ
	-- �������_����ڽ� ���̺��� ���屸���� ������Ʈ
    UPDATE
        �������_����ڽ� AS t
        JOIN
        (
            SELECT
                id,
                -- @current_location ������ ���� ���� ��ġ��ȯ ���� ��
                -- ������ ����(@seq)�� 1 ����, �ٸ��� 1�� �ʱ�ȭ
                @seq := IF(@current_location = ��ġ��ȯ, @seq + 1, 1) AS sequence_num,
                -- @current_location ������ ���� ��ġ��ȯ ���� �Ҵ��Ͽ� ���� �࿡�� ���� �� �ֵ��� �غ�
                @current_location := ��ġ��ȯ
            FROM
                �������_����ڽ�,
                -- ���ν��� ������ ����� ����� ���� �ʱ�ȭ
                (SELECT @seq := 0, @current_location := '') AS vars
            ORDER BY
                ��ġ��ȯ, id
        ) AS s ON t.id = s.id
    SET
        t.���屸���� = CONCAT('(', s.sequence_num, ')');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ��ֹ� ���� �ű�� 1', ROW_COUNT());

	/*--=============================================================
	-- �ڽ��ֹ� �ּҾ�����Ʈ
    --=============================================================*/
	UPDATE �������_����ڽ� SET �ּ� = CONCAT(�ּ�, ���屸����);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ��ֹ� �ּҿ� ���� �߰�', ROW_COUNT());

    /*--=============================================================	
    -- �ڽ��ֹ� ���� �ű��	2
    --=============================================================*/
	-- ������ �ּ� �׷� ������ ù ��° �ּҴ� �״�� �ΰ�, �� ��° �ּҺ��� ������ (2), (3)... �������� ���̴� ��
	-- ����� ������ Ȱ���Ͽ� ���� �ּ� �׷쿡 ������ �ű��,
    -- ������ 2 �̻��� ��쿡�� �ּ� �ڿ� '(����)'�� �߰��մϴ�.
	-- �������_����ڽ� ���̺��� �ּ� ������Ʈ
    UPDATE
        �������_����ڽ� AS t
        JOIN
        (
            SELECT
                id,
                -- ���� �ּ� ���� �����Ͽ� CONCAT�� ���
                �ּ� AS original_address,
                -- @current_address ������ ���� ���� �ּ� ���� ��
                -- ������ ����(@seq)�� 1 ����, �ٸ��� 1�� �ʱ�ȭ�Ͽ� ����(rn) ����
                @seq := IF(@current_address = �ּ�, @seq + 1, 1) AS rn,
                -- @current_address ������ ���� �ּ� ���� �Ҵ��Ͽ� ���� �࿡�� ��
                @current_address := �ּ�
            FROM
                �������_����ڽ�,
                -- ���ν��� ���� ��, ����� ������ 0�� �� ���ڿ��� �ʱ�ȭ
                (SELECT @seq := 0, @current_address := '') AS vars
            ORDER BY
                �ּ�, id
        ) AS s ON t.id = s.id
    SET
        t.�ּ� = CONCAT(s.original_address, ' (', s.rn, ')')
    -- ���� ����(rn)�� 1���� ū, �� �׷� �� �� ��° �̻��� �����͸� ������Ʈ
    WHERE
        s.rn > 1;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ��ֹ� ���� �ű�� 2', ROW_COUNT());

	/*--================================================================================
	-- �ڽ��ֹ� ����1�κ���
    --================================================================================*/
    UPDATE �������_����ڽ� SET ���� = 1;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ��ֹ� ������ 1�� ����', ROW_COUNT());

	/*--================================================================================
	-- �ڽ��ֹ� �����ڼ��� (��ġ��ȯ)
    --================================================================================*/
	UPDATE �������_����ڽ� 
	   SET ��ġ��ȯ = CONCAT('0',��ġ, ǰ���ڵ�, LPAD(����, 2, '0'), �����θ�, �ּ�, ��ȭ��ȣ1);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ڽ��ֹ� ��ġ��ȯ �缳��', ROW_COUNT());

	/*--================================================================================
	--����ڽ� ���� ó�� (ǰ���ڵ庰�� ������ �ջ��Ͽ� ��°����� ������Ʈ)
    --================================================================================*/
	UPDATE �������_����ڽ� AS t1 
        INNER JOIN (
            SELECT ǰ���ڵ�, SUM(����) AS total_quantity
            FROM �������_����ڽ�
            GROUP BY ǰ���ڵ�
        ) AS t2 ON t1.ǰ���ڵ� = t2.ǰ���ڵ�
        SET t1.ǰ�񰳼� = t2.total_quantity;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ����ڽ� ǰ�񰳼� �ջ�', ROW_COUNT());

	/*--================================================================================
	-- �����۾�1	(���屸������ ������Ʈ)
	--   : ���ﳹ�� �� ���� �ּ� + ���θ� ����
	--   : ����ڽ� �� ���� �ּ� + ���θ� ����
    --================================================================================*/
	UPDATE �������_���ݿ�����ȯ
	   SET
			-- `���屸������`�� ���� ���� ���� ���ο� ���� ���Ǻη� �Ҵ��մϴ�.
			���屸������ = CASE ���屸������
				WHEN '�õ�����' THEN '���ﳹ��'
				WHEN '�õ��ڽ�' THEN '����ڽ�'
			END
	   WHERE
			-- �� ������Ʈ ������ WHERE ������ �����մϴ�.
			���屸������ IN ('�õ�����', '�õ��ڽ�')
			AND SUBSTRING(
					TRIM(SUBSTRING_INDEX(�ּ�, ']', -1)), 1, 2
				) = '����'
			AND ���θ� IN ('Cafe24(��)', 'Gfresh');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �����۾� 1 (���ﳹ��/����ڽ�)', ROW_COUNT());
			
	/*--================================================================================
	-- �����۾�2	(���屸������ ������Ʈ)
	--   : ����ڽ� �� ���� �ּ� �� ����ڽ�
    --================================================================================*/
    UPDATE �������_����ڽ�
       SET ���屸������ = '����ڽ�'
     WHERE ���屸������ = '�õ��ڽ�'
       AND SUBSTRING(
           TRIM(SUBSTRING_INDEX(�ּ�, ']', -1)), 1, 2
           ) = '����'
       AND ���θ� IN ('Cafe24(��)', 'Gfresh');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �����۾� 2 (����ڽ� �� ����ڽ�)', ROW_COUNT());

    COMMIT;

    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
