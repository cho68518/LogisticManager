DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GyeonggiProcessF$$

CREATE PROCEDURE sp_GyeonggiProcessF()
BEGIN
    /*--================================================================================
    -- (���õ�) ó��
    --================================================================================*/
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            @sqlstate = RETURNED_SQLSTATE,
            @errno    = MYSQL_ERRNO,
            @text     = MESSAGE_TEXT;

        INSERT INTO error_log (procedure_name, error_code, error_message)
        VALUES ('sp_GyeonggiProcessF', @errno, @text);

        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log, temp_sorted_data;
        SELECT '������ �߻��Ͽ� ��� �۾��� �ѹ�Ǿ����ϴ�.' AS Message;

        SHOW ERRORS;
    END;

    CREATE TEMPORARY TABLE sp_execution_log ( StepID INT AUTO_INCREMENT PRIMARY KEY, OperationDescription VARCHAR(255), AffectedRows INT );
    START TRANSACTION;

    /*--TRUNCATE TABLE temp_invoices;
    --TRUNCATE TABLE temp_additional_invoices;*/
    TRUNCATE TABLE error_log;	
		
    /*-- =================================================================================
    -- �ӽ� �۾� ���̺� ����
    -- =================================================================================*/
    DROP TEMPORARY TABLE IF EXISTS temp_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

    CREATE TEMPORARY TABLE temp_sorted_data LIKE �������_���õ�_����;
	CREATE TEMPORARY TABLE temp_invoices LIKE �������_���õ�;
	CREATE TEMPORARY TABLE temp_additional_invoices LIKE �������_���õ�;
	
    /*--================================================================================
	-- (���õ�) �õ����� �з�
    --================================================================================*/
    INSERT INTO temp_invoices (
		msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�, ����, ��۸޼���, 
        �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, 
        �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ
        )
		SELECT 
			msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�,
            CASE WHEN ���� REGEXP '^[0-9]+$' THEN CAST(���� AS SIGNED) ELSE 0 END,
            ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�,
            CASE WHEN �ù��� REGEXP '^[0-9]+$' THEN CAST(�ù��� AS SIGNED) ELSE 0 END,
            �ڽ�ũ��,
            CASE WHEN ��°��� REGEXP '^[0-9]+$' THEN CAST(��°��� AS SIGNED) ELSE 0 END,
            CASE WHEN ������� REGEXP '^[0-9]+$' THEN CAST(������� AS SIGNED) ELSE 0 END,
            ��ǥ1, ��ǥ2,
            CASE WHEN ǰ�񰳼� REGEXP '^[0-9]+$' THEN CAST(ǰ�񰳼� AS SIGNED) ELSE 0 END,
            CASE WHEN �ù���� REGEXP '^[0-9]+(\\.[0-9]+)?$' THEN CAST(�ù���� AS DECIMAL(10,3)) ELSE 0 END,
            NULL, -- �ù����1
            NULL, -- �ù�����ջ�
            ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ
        FROM �������_���ݿ�����ȯ WHERE ���屸������ = '�õ�����';
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (���õ�) ���� �� �õ����� ������ �з�', ROW_COUNT());

    /*--=====================================================================================================
	-- (���õ�) �ù���� ��� �� ���屸���� ������Ʈ
    --======================================================================================================*/
    UPDATE temp_invoices SET ���屸���� = FLOOR((1 / IFNULL(NULLIF(�ù����, 0), 10)) * 1000) / 1000;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ���屸����', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) ���屸���ڿ� ���� �� ������Ʈ
    --================================================================================*/	
    UPDATE temp_invoices SET ���屸���� = ���屸���� * ����;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ���屸����*����', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) �ּ� + �����θ� ��� ���屸���� �ջ�
    --================================================================================*/	
    UPDATE temp_invoices AS t JOIN (
        SELECT �ּ�, �����θ�, SUM(���屸����) AS ��������
          FROM temp_invoices GROUP BY �ּ�, �����θ�
		) AS s ON t.�ּ� = s.�ּ� AND t.�����θ� = s.�����θ�
		SET t.�ù����1 = s.��������;
		   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) �ּ�, �����θ�, �ù����', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) �ù����1 �ø� ó��  ('����' �� �����ִ� ���� ����)
    --================================================================================*/	
    UPDATE temp_invoices SET �ù����1 = CEIL(IFNULL(�ù����1, 1));
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) �ù����1', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) �ù����1�� ���� ���屸�� ������Ʈ
    --================================================================================*/	
    UPDATE temp_invoices SET ���屸�� = CASE WHEN �ù����1 > 1 THEN '�߰�' ELSE '1��' END;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ���屸��', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) �ּ� �� �����θ� ���ϼ��� ���� ���屸�� ������Ʈ ����
	--             �ּ� + �����θ� ������ ������ ��� '����'�� ������Ʈ
    --================================================================================*/	
    UPDATE temp_invoices t1 JOIN (
        SELECT �ּ�, �����θ� FROM temp_invoices WHERE ���屸�� = '1��' GROUP BY �ּ�, �����θ� HAVING COUNT(*) = 1
		) AS unique_address ON t1.�ּ� = unique_address.�ּ� AND t1.�����θ� = unique_address.�����θ�
		SET t1.���屸�� = '����' WHERE t1.���屸�� = '1��';
	  
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ���屸��(����, 1��)', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) ǰ���ڵ庰 ���� �ջ� �� ǰ�񰳼�
    --================================================================================*/	
    UPDATE temp_invoices t1 JOIN (
        SELECT ǰ���ڵ�, SUM(����) AS total_quantity FROM temp_invoices WHERE ���屸�� = '����' GROUP BY ǰ���ڵ�
		) AS t2 ON t1.ǰ���ڵ� = t2.ǰ���ڵ�
		SET t1.ǰ�񰳼� = t2.total_quantity WHERE t1.���屸�� = '����';
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ǰ�񰳼�', ROW_COUNT());
	
    /*-- =================================================================================
    -- '�߰�' ���� ����
	-- (���õ�) �������_��������߰����� ���̺�� ����ũ �ּ� �� �̵�
    --================================================================================*/	
	INSERT INTO temp_additional_invoices (msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
	                                      �����ȣ, �ּ�, �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, 
										  �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, 
										  ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, 
										  ��ġ, ��ġ��ȯ)
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
			   �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���,
			   �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
			   ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ
		FROM (
			SELECT *, ROW_NUMBER() OVER (PARTITION BY �ּ� ORDER BY id ASC) AS rn
			FROM temp_invoices WHERE ���屸�� = '�߰�'
		) AS subquery WHERE subquery.rn = 1;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (���õ�) �߰����� �⺻ ������ ����', ROW_COUNT());

    /*--================================================================================
	-- (���õ�) ���õ��߰����� ������Ʈ
	--             �ֹ���ȣ �߰�(20250821) �������� �ʴ� ������ �ֹ��� �������� �ʱ� ����
    --================================================================================*/	
    UPDATE temp_additional_invoices SET ���� = 1, ����� = '+++', �ɼǸ� = '+++', ǰ���ڵ� = '0000', �ֹ���ȣ = '1234567890';
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ����, �����, �ɼǸ�, ǰ���ڵ�, �ֹ���ȣ', ROW_COUNT());	

    /*--================================================================================
	-- (���õ�) ���õ� �߰����� �ø���
    --================================================================================*/	
    INSERT INTO temp_additional_invoices 
		(msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, 
		 �ּ�, �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, 
		 �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, 
		 �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ)
		SELECT t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.�����θ�, t.��ȭ��ȣ1, t.��ȭ��ȣ2, t.�����ȣ, 
		       t.�ּ�, t.�ɼǸ�, t.����, t.��۸޼���, t.�ֹ���ȣ, t.���θ�, t.�����ð�, t.�����, t.ǰ���ڵ�, 
			   t.�ù���, t.�ڽ�ũ��, t.��°���, t.�������, t.��ǥ1, t.��ǥ2, t.ǰ�񰳼�, t.�ù����, t.�ù����1, 
			   t.�ù�����ջ�, t.���屸����, t.���屸��, t.���屸������, t.��ġ, t.��ġ��ȯ
        FROM temp_additional_invoices AS t JOIN Numbers AS n ON n.n <= (t.�ù����1 - 1) WHERE t.�ù����1 > 1;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (���õ�) �߰����� �ø���', ROW_COUNT());	

    /*--================================================================================
	-- (���õ�) ���õ��߰����� ���� �ű��
    --================================================================================*/	
    UPDATE temp_additional_invoices AS t JOIN (
        SELECT id, @seq := IF(@current_group = �ּ�, @seq + 1, 1) AS rn, @current_group := �ּ�
        FROM temp_additional_invoices, (SELECT @seq := 0, @current_group := '') AS vars ORDER BY �ּ�, id
    ) AS s ON t.id = s.id
    SET t.���屸���� = CONCAT('[', s.rn, ']');
	   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) �߰����� ���� �ű��', ROW_COUNT());	

    /*--================================================================================
	-- (���õ�) ���õ��߰����� �ּҾ�����Ʈ
    --================================================================================*/	
    UPDATE temp_additional_invoices SET �ּ� = CONCAT(�ּ�, ���屸����);
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) �߰����� �ּҿ� ���� �߰�', ROW_COUNT());	

	
    /*--================================================================================
	-- ���� ������ ����
	--    ��ǥ �ִ� �����͸� ����, �� �ȿ����� �ּ� ������, �ּҵ� ������ �ɼǸ� ��
    --================================================================================*/
	INSERT INTO temp_sorted_data
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2,
               �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
               �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
		FROM (
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
			       �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
                   �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
			  FROM temp_invoices WHERE ���屸�� = '����'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
			       �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
                   �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
			  FROM �������_����ڽ� WHERE ���屸������ = '�õ��ڽ�'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
			       �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
                   �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
			  FROM temp_invoices WHERE ���屸�� = '1��'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
			       �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
                   �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
			  FROM temp_invoices WHERE ���屸�� = '�߰�'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, 
			       �����ȣ, �ּ�, �����, ����, ��۸޼���, �ֹ���ȣ, ���θ�, ǰ���ڵ�,
                   �ù���, �ڽ�ũ��, ��°���, ��ǥ1, ��ǥ2, ǰ�񰳼�
			  FROM temp_additional_invoices
		) AS final_union
		ORDER BY
			CASE WHEN ��ǥ1 <> '' OR ��ǥ2 <> '' THEN 0 ELSE 1 END,
			/*�ּ� ASC, �ɼǸ� ASC;*/
			�ּ� ASC, ����� ASC;
    
    TRUNCATE TABLE �������_���õ�_����;
	
    INSERT INTO �������_���õ�_���� SELECT * FROM temp_sorted_data;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (���õ�) ���� ���̺� ������ ����', ROW_COUNT());

    /*-- =================================================================================
    -- ��ó��
    -- =================================================================================*/
	UPDATE �������_���õ�_���� AS d
		JOIN (
			SELECT IFNULL(��ȭ��ȣ1, '') AS ��ȭ��ȣ1, IFNULL(��ȭ��ȣ2, '') AS ��ȭ��ȣ2, IFNULL(�����ȣ, '') AS �����ȣ, IFNULL(�����θ�, '') AS �����θ�, MAX(�ּ�) AS �ּ�
			FROM �������_���õ�_����
			WHERE ����� <> '����̾��̽� �߰�' AND �ּ� IS NOT NULL AND �ּ� <> ''
			GROUP BY IFNULL(��ȭ��ȣ1, ''), IFNULL(��ȭ��ȣ2, ''), IFNULL(�����ȣ, ''), IFNULL(�����θ�, '')
		) AS src ON
			IFNULL(d.��ȭ��ȣ1, '') = src.��ȭ��ȣ1 AND IFNULL(d.��ȭ��ȣ2, '') = src.��ȭ��ȣ2 AND
			IFNULL(d.�����ȣ, '') = src.�����ȣ AND IFNULL(d.�����θ�, '') = src.�����θ�
		SET d.�ּ� = src.�ּ�
		WHERE d.����� = '����̾��̽� �߰�' AND d.�ּ� IS NULL;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (���õ�) ����̾��̽� �߰��� �ּ� ������Ʈ', ROW_COUNT());
	
    UPDATE �������_���õ�_����, (
        SELECT
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COST')  AS c_cost,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_SIZE')  AS c_size,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COUNT') AS c_count
    ) AS p_values
    SET �ù��� = p_values.c_cost, �ڽ�ũ�� = p_values.c_size, ��°��� = p_values.c_count;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] ���� ���̺� �⺻�� ����', ROW_COUNT());
    
    COMMIT;

    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;
	
    DROP TEMPORARY TABLE sp_execution_log;
    DROP TEMPORARY TABLE temp_invoices;
    DROP TEMPORARY TABLE temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

END$$

DELIMITER ;