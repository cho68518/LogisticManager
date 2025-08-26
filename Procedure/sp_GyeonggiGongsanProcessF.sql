DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GyeonggiGongsanProcessF$$

CREATE PROCEDURE sp_GyeonggiGongsanProcessF()
BEGIN
    -- =================================================================================
    -- (������) ó��
    -- =================================================================================
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            @sqlstate = RETURNED_SQLSTATE,
            @errno    = MYSQL_ERRNO,
            @text     = MESSAGE_TEXT;

        ROLLBACK;

        INSERT INTO error_log (procedure_name, error_code, error_message)
        VALUES ('sp_GyeonggiGongsanProcessF', @errno, @text);

        DROP TEMPORARY TABLE IF EXISTS sp_execution_log, temp_sorted_data;
        SELECT '������ �߻��Ͽ� ��� �۾��� �ѹ�Ǿ����ϴ�.' AS Message;

        SHOW ERRORS;
    END;

    CREATE TEMPORARY TABLE sp_execution_log ( StepID INT AUTO_INCREMENT PRIMARY KEY, OperationDescription VARCHAR(255), AffectedRows INT );
    START TRANSACTION;

    /*-- =================================================================================
    -- �ӽ� �۾� ���̺� ����
    -- =================================================================================*/
    DROP TEMPORARY TABLE IF EXISTS temp_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

    CREATE TEMPORARY TABLE temp_sorted_data LIKE �������_������_����;
	CREATE TEMPORARY TABLE temp_invoices LIKE �������_������;
	CREATE TEMPORARY TABLE temp_additional_invoices LIKE �������_������;
	
    /*--================================================================================
	-- (������) �����곹�� �з�
    --================================================================================*/
    INSERT INTO temp_invoices (
		msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�, ����, ��۸޼���, 
        �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, 
        �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ
    )
		SELECT 
			msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�, ����, ��۸޼���, 
			�ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, 
			�ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ
        FROM �������_���ݿ�����ȯ WHERE ���屸������ = '���곹��';


    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[TEMP] (������) ���� �� ���곹�� ������ �з�', ROW_COUNT());

    /*--=====================================================================================================
	-- (������) �ù���� ��� �� ���屸���� ������Ʈ
	--             �ù������ �̿��Ͽ� 1�� ���� ����� �Ҽ��� �� �ڸ����� �����Ͽ� ���屸���� �÷��� ������Ʈ
    --======================================================================================================*/
    UPDATE temp_invoices SET ���屸���� = FLOOR((1 / IFNULL(NULLIF(CAST(�ù���� AS DECIMAL), 0), 10)) * 1000) / 1000;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �ù�������� ���屸���� ���', ROW_COUNT());

    /*--================================================================================
	-- (������) ���屸���ڿ� ���� �� ������Ʈ
    --================================================================================*/
    UPDATE temp_invoices SET ���屸���� = ���屸���� * ����;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) ���屸���ڿ� ���� ����', ROW_COUNT());

    /*--================================================================================
	-- (������) �ּ� + �����θ� ��� ���屸���� �ջ�
    --================================================================================*/
    UPDATE temp_invoices AS t JOIN (
			SELECT �ּ�, �����θ�, SUM(���屸����) AS ��������
			  FROM temp_invoices GROUP BY �ּ�, �����θ�
		 ) AS s ON t.�ּ� = s.�ּ� AND t.�����θ� = s.�����θ�
		SET t.�ù����1 = s.��������;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �ּ�/�����θ� ���屸���� �ջ�', ROW_COUNT());

    /*--================================================================================
	-- (������) �ù����1 �ø� ó��  ('����' �� �����ִ� ���� ����)
    --================================================================================*/
    UPDATE temp_invoices SET �ù����1 = CEIL(IFNULL(�ù����1, 1));
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �ù���� �ø� ó��', ROW_COUNT());

    /*--================================================================================
	-- (������) �ù����1�� ���� ���屸�� ������Ʈ
    --================================================================================*/
    UPDATE temp_invoices SET ���屸�� = CASE WHEN �ù����1 > 1 THEN '�߰�' ELSE '1��' END;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) ���屸�� ���� (�߰�/1��)', ROW_COUNT());

    /*--================================================================================
	-- (������) �ּ� �� �����θ� ���ϼ��� ���� ���屸�� ������Ʈ ����
	--             �ּ� + �����θ� ������ ������ ��� '����'�� ������Ʈ
    --================================================================================*/
    UPDATE temp_invoices t1 JOIN (
			SELECT �ּ�, �����θ� FROM temp_invoices WHERE ���屸�� = '1��' GROUP BY �ּ�, �����θ� HAVING COUNT(*) = 1
		) AS unique_address ON t1.�ּ� = unique_address.�ּ� AND t1.�����θ� = unique_address.�����θ�
		SET t1.���屸�� = '����' WHERE t1.���屸�� = '1��';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) ���屸�� ���� (����)', ROW_COUNT());

    /*--================================================================================
	-- (������) ǰ���ڵ庰 ���� �ջ� �� ǰ�񰳼�
    --================================================================================*/
    UPDATE temp_invoices t1 JOIN (
			SELECT ǰ���ڵ�, SUM(����) AS total_quantity FROM temp_invoices WHERE ���屸�� = '����' GROUP BY ǰ���ڵ�
		) AS t2 ON t1.ǰ���ڵ� = t2.ǰ���ڵ�
		SET t1.ǰ�񰳼� = t2.total_quantity WHERE t1.���屸�� = '����';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) ���� ������ ǰ�񰳼� �ջ�', ROW_COUNT());

    /*--================================================================================
	-- (������) �������_�������߰����� ���̺�� ����ũ �ּ� �� �̵�
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
		
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (������) �߰����� �⺻ ������ ����', ROW_COUNT());

    /*--================================================================================
	-- (������) �������߰����� ������Ʈ
	--             �ֹ���ȣ �߰�(20250821) �������� �ʴ� ������ �ֹ��� �������� �ʱ� ����
    --================================================================================*/
    UPDATE temp_additional_invoices
		SET ���� = 1, ����� = '+++', �ɼǸ� = '+++', ǰ���ڵ� = '0000', �ֹ���ȣ = '1234567890';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �߰����� ���� ���� (+++)', ROW_COUNT());

    /*--================================================================================
	-- (������) ������ �߰����� �ø���
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
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (������) �߰����� �ø���', ROW_COUNT());
    
    /*--================================================================================
	-- (������) ������ �߰����� ���� �ű��
    --================================================================================*/
    UPDATE temp_additional_invoices AS t JOIN (
			SELECT id, @seq := IF(@current_group = �ּ�, @seq + 1, 1) AS rn, @current_group := �ּ�
			FROM temp_additional_invoices, (SELECT @seq := 0, @current_group := '') AS vars ORDER BY �ּ�, id
		) AS s ON t.id = s.id
		SET t.���屸���� = CONCAT('[', s.rn, ']');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �߰����� ���� �ű��', ROW_COUNT());

    /*--================================================================================
	-- (������) �������߰����� �ּҾ�����Ʈ
    --================================================================================*/
    UPDATE temp_additional_invoices SET �ּ� = CONCAT(�ּ�, ���屸����);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (������) �߰����� �ּҿ� ���� �߰�', ROW_COUNT());

    /*--================================================================================
	-- (������) ������ ���̺� ����������
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
			  FROM �������_����ڽ� WHERE ���屸������ = '����ڽ�'
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
    
    TRUNCATE TABLE �������_������_����;
	
    INSERT INTO �������_������_���� SELECT * FROM temp_sorted_data;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (������) ���� ���̺� ������ ����', ROW_COUNT());
	
    /*--================================================================================
	-- (������) �������_������_���� ���̺� ������Ʈ
    --================================================================================*/
    UPDATE �������_������_����, (
        SELECT
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_COST')  AS c_cost,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_SIZE')  AS c_size,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_COUNT') AS c_count
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